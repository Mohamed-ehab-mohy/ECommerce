# T-TST-003 orchestrator: runs load scenarios S1-S8 against the local staging stack.
#
#   powershell -ExecutionPolicy Bypass -File perf/k6/run-s1-s8.ps1 -Scenario all
#   powershell -ExecutionPolicy Bypass -File perf/k6/run-s1-s8.ps1 -Scenario s5
#   powershell -ExecutionPolicy Bypass -File perf/k6/run-s1-s8.ps1 -Scenario s6 -StackUp
#
# Scenarios:
#   s1  checkout baseline (100 orders/min scaled to ~10%)          s1-checkout-baseline.js
#   s2  catalog browse   (5,000 req/min scaled to ~10%)            s2-catalog-browse.js
#   s3  search           (1,000 req/min scaled to ~10%)            s3-search.js
#   s4  flash-sale burst (2x order load, 3 bursts)                 s4-flash-burst.js
#   s5  stock concurrency on 10 units (1,000 VUs, zero oversell)   s5-stock-concurrency.js
#   s6  scale-out (1 replica baseline vs 2 replicas on 8080/8081)  s2-catalog-browse.js
#   s8  webhook flood (~150 orders/min -> order.placed + order.paid)
param(
    [ValidateSet('all', 's1', 's2', 's3', 's4', 's5', 's6', 's8')]
    [string]$Scenario = 'all',
    [switch]$StackUp,
    [string]$Run = (Get-Date -Format 'yyyyMMddHHmmss'),
    [string]$BaseUrl = 'http://localhost:8080'
)

$ErrorActionPreference = 'Stop'

# ---- config ---------------------------------------------------------------
$K6 = 'C:\Program Files\k6\k6.exe'
$ComposeBase = 'deploy/staging/docker-compose.staging.yml'
$ComposeScale = 'deploy/staging/docker-compose.scale.yml'
$PgContainer = 'ecommerce-staging-postgres'
$DbUser = 'ecommerce'
$DbName = 'ecommerce_staging'
$ResultsDir = Join-Path $PSScriptRoot "results-$Run"
$SeedFile = 'deploy/staging/seed-load.sql'
$ResetFile = 'deploy/staging/reset-load.sql'

New-Item -ItemType Directory -Force -Path $ResultsDir | Out-Null

# ---- helpers ---------------------------------------------------------------
function Invoke-PsqlFile {
    param([string]$File)
    Get-Content -LiteralPath $File -Raw -Encoding UTF8 |
        & docker exec -i $PgContainer psql -U $DbUser -d $DbName -v ON_ERROR_STOP=1
    if ($LASTEXITCODE -ne 0) { throw "psql failed for $File" }
}

function Wait-ApiHealthy {
    param([string]$Url, [int]$MaxSec = 90)
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $MaxSec) {
        try {
            $r = Invoke-WebRequest -Uri "$Url/health/live" -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) { Write-Host "API healthy: $Url"; return }
        } catch { }
        Start-Sleep -Seconds 2
    }
    throw "API not healthy at $Url after $MaxSec s"
}

function Invoke-K6 {
    param(
        [string]$Name,
        [string]$Script,
        [hashtable]$EnvVars = @{},
        [string[]]$ExtraArgs = @(),
        [string]$Base = $BaseUrl
    )
    $export = Join-Path $ResultsDir "$Name-summary.json"
    $out = Join-Path $ResultsDir "$Name.log"

    $args = @('run')
    foreach ($k in $EnvVars.Keys) { $args += "-e"; $args += "$k=$($EnvVars[$k])" }
    $args += $Script
    $args += '--summary-export'; $args += $export
    $args += '--quiet'
    $args += $ExtraArgs

    $env:BASE_URL = $Base
    Write-Host "`n=== $Name : k6 run $Script ==="
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    & $K6 @args 2>&1 | Tee-Object -FilePath $out
    $code = $LASTEXITCODE
    $ErrorActionPreference = $prevEap
    Remove-Item Env:BASE_URL -ErrorAction SilentlyContinue
    Write-Host "k6 exit code: $code"
    return $code
}

function Get-SummaryDigest {
    param([string]$LogPath)
    if (-not (Test-Path $LogPath)) { return 'no log' }
    $lines = Get-Content -LiteralPath $LogPath
    $d = ($lines | Select-String -Pattern '^\s+http_req_duration\.\.\.').Line
    $f = ($lines | Select-String -Pattern '^\s+http_req_failed\.\.\.').Line
    $it = ($lines | Select-String -Pattern '^\s+iterations\.\.').Line
    $subs = ($lines | Select-String -Pattern '^\s+http_req_duration\{type:(checkout|authorize|place)\}').Line -join ' | '
    "$d | $f | $it"
    if ($subs) { "  type: $subs" }
}

function Start-WebhookReceiver {
    $existing = docker ps -q -f name=wh-receiver
    if ($existing) { docker rm -f wh-receiver | Out-Null }
    docker run -d --rm --name wh-receiver -p 9099:80 hashicorp/http-echo -listen=:80 -text='{"ok":true}' | Out-Null
    Start-Sleep -Seconds 2
    try {
        $r = Invoke-WebRequest -Uri 'http://localhost:9099/wh/ping' -Method Post -UseBasicParsing -TimeoutSec 5
        if ($r.StatusCode -ne 200) { throw "receiver not ready: $($r.StatusCode)" }
    } catch {
        throw "webhook receiver failed to start: $($_.Exception.Message)"
    }
}

function Stop-WebhookReceiver {
    docker rm -f wh-receiver 2>$null | Out-Null
}

function Get-WebhookReceiverHits {
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try {
        $logs = docker logs wh-receiver 2>&1
    } finally {
        $ErrorActionPreference = $prevEap
    }
    ($logs | Select-String -Pattern '"POST /wh').Count
}

# ---- stack -----------------------------------------------------------------
if ($StackUp) {
    Write-Host 'Bringing up staging stack (single replica)...'
    docker compose -f $ComposeBase up -d --build | Out-Host
    Wait-ApiHealthy -Url $BaseUrl
}

Invoke-PsqlFile $SeedFile

# ---- scenarios --------------------------------------------------------------
switch ($Scenario) {
    'all' { foreach ($s in @('s1','s2','s3','s4','s5','s8','s6')) { & $PSCommandPath -Scenario $s -Run $Run } }
    's1' {
        Invoke-PsqlFile $ResetFile
        $c = Invoke-K6 -Name 's1-checkout' -Script 'perf/k6/s1-checkout-baseline.js' -EnvVars @{ RUN = $Run }
        Write-Host "S1: $(Get-SummaryDigest (Join-Path $ResultsDir 's1-checkout.log'))"
    }
    's2' {
        $c = Invoke-K6 -Name 's2-browse' -Script 'perf/k6/s2-catalog-browse.js' -EnvVars @{ RUN = $Run }
        Write-Host "S2: $(Get-SummaryDigest (Join-Path $ResultsDir 's2-browse.log'))"
    }
    's3' {
        $c = Invoke-K6 -Name 's3-search' -Script 'perf/k6/s3-search.js' -EnvVars @{ RUN = $Run }
        Write-Host "S3: $(Get-SummaryDigest (Join-Path $ResultsDir 's3-search.log'))"
    }
    's4' {
        Invoke-PsqlFile $ResetFile
        $c = Invoke-K6 -Name 's4-flash' -Script 'perf/k6/s4-flash-burst.js' -EnvVars @{ RUN = $Run; VUS = 8; CYCLES = 3 }
        Write-Host "S4: $(Get-SummaryDigest (Join-Path $ResultsDir 's4-flash.log'))"
    }
    's5' {
        Invoke-PsqlFile $ResetFile
        $c = Invoke-K6 -Name 's5-race' -Script 'perf/k6/s5-stock-concurrency.js' -EnvVars @{ RUN = $Run; VUS = 100 }
        $logLines = Get-Content -LiteralPath (Join-Path $ResultsDir 's5-race.log') -ErrorAction SilentlyContinue
        $comp = [int](($logLines | Select-String -Pattern 'place_complete\.\.').Line -replace '.*:\s*(\d+).*', '$1')
        Write-Host "S5: place calls completed = $comp"
        & docker exec $PgContainer psql -U $DbUser -d $DbName -c `
            "SELECT o.status, count(*) FROM orders o JOIN order_items oi ON oi.order_id=o.id WHERE oi.product_id='10000000-0000-0000-0000-000000000003' GROUP BY o.status ORDER BY o.status;"
        & docker exec $PgContainer psql -U $DbUser -d $DbName -c `
            "SELECT on_hand, allocated, on_hand - allocated AS available, (allocated > on_hand) AS oversold FROM stock_items WHERE id='10000000-0000-0000-0000-000000000301';"
    }
    's8' {
        Invoke-PsqlFile $ResetFile
        Start-WebhookReceiver
        $hits = 0
        try {
            $c = Invoke-K6 -Name 's8-webhook' -Script 'perf/k6/s1-checkout-baseline.js' -EnvVars @{ RUN = $Run; PRODUCT_ID = '10000000-0000-0000-0000-000000000001'; SKU = 'LOAD-01'; VUS = 5; DURATION = '5m' }
            $hits = Get-WebhookReceiverHits
        } finally {
            Stop-WebhookReceiver
        }
        Write-Host "S8: receiver hits = $hits"
        & docker exec $PgContainer psql -U $DbUser -d $DbName -c `
            "SELECT event_type, status, count(*) FROM webhook_deliveries WHERE endpoint_id='30000000-0000-0000-0000-000000000001' GROUP BY event_type, status ORDER BY event_type;"
        & docker exec $PgContainer psql -U $DbUser -d $DbName -c `
            "SELECT count(*) AS delivered, count(*) FILTER (WHERE status='Delivered') AS delivered_ok, round(avg(EXTRACT(EPOCH FROM (delivered_at_utc - created_at))*1000)) AS avg_lag_ms FROM webhook_deliveries WHERE endpoint_id='30000000-0000-0000-0000-000000000001';"
        Write-Host "S8: $(Get-SummaryDigest (Join-Path $ResultsDir 's8-webhook.log'))"
    }
    's6' {
        # baseline: 1 replica, 83 req/s catalog browse for 5m
        $c = Invoke-K6 -Name 's6-baseline' -Script 'perf/k6/s2-catalog-browse.js' -EnvVars @{ RUN = $Run; RATE = 83; DURATION = '5m' }
        Write-Host "S6 baseline (1 replica): $(Get-SummaryDigest (Join-Path $ResultsDir 's6-baseline.log'))"

        Write-Host 'Scaling to 2 API replicas (8080/8081)...'
        docker compose -f $ComposeBase -f $ComposeScale up -d --scale api=2 --no-deps | Out-Host
        Wait-ApiHealthy -Url 'http://localhost:8080'
        Wait-ApiHealthy -Url 'http://localhost:8081'

        $p1 = Start-Process -FilePath 'powershell' -ArgumentList @(
            '-ExecutionPolicy','Bypass','-File', (Join-Path $PSScriptRoot 'run-s1-s8.ps1'),
            '-Scenario','s2','-Run', ($Run + '-s6a'), '-BaseUrl','http://localhost:8080') -PassThru -NoNewWindow
        $p2 = Start-Process -FilePath 'powershell' -ArgumentList @(
            '-ExecutionPolicy','Bypass','-File', (Join-Path $PSScriptRoot 'run-s1-s8.ps1'),
            '-Scenario','s2','-Run', ($Run + '-s6b'), '-BaseUrl','http://localhost:8081') -PassThru -NoNewWindow
        $p1.WaitForExit(); $p2.WaitForExit()

        Write-Host 'Scaling back to 1 replica...'
        docker compose -f $ComposeBase -f $ComposeScale up -d --scale api=1 --no-deps | Out-Host
        docker compose -f $ComposeBase up -d | Out-Host
        Wait-ApiHealthy -Url $BaseUrl
        Write-Host 'S6 done: compare s6-baseline p95 vs s6a/s6b summaries under 2 replicas.'
    }
}

Write-Host "`nResults in: $ResultsDir"
