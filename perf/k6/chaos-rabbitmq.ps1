$ErrorActionPreference = 'Continue'
$resultsDir = Join-Path $PSScriptRoot "results-chaos-rabbitmq"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not (Test-Path $resultsDir)) { New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null }

Write-Output "=== CHAOS TEST: RabbitMQ Kill Under Load ==="
Write-Output "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

function Invoke-Pg($sql) { docker exec ecommerce-staging-postgres psql -U ecommerce -d ecommerce_staging -t -c $sql }

# Baseline
Write-Output "`n--- Pre-chaos baseline ---"
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/live" -UseBasicParsing -TimeoutSec 5; Write-Output "API live: $($r.StatusCode)" } catch { Write-Output "API live: FAIL" }
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/ready" -UseBasicParsing -TimeoutSec 5; Write-Output "API ready: $($r.StatusCode)" } catch { Write-Output "API ready: FAIL" }

Write-Output "`n--- Outbox baseline ---"
Invoke-Pg "SELECT COUNT(*) as pending FROM outbox_events WHERE processed_on IS NULL;"

Write-Output "`n--- Webhook deliveries baseline ---"
Invoke-Pg "SELECT status, COUNT(*) FROM webhook_deliveries GROUP BY status ORDER BY status;"

# Start k6 S1 checkout via Start-Job
Write-Output "`n--- Starting k6 S1 checkout (background job) ---"
$k6Json = Join-Path $resultsDir "s1-mq-chaos.json"
$k6Job = Start-Job -ScriptBlock { param($wd, $out) Set-Location $wd; & k6 run --out "json=$out" perf/k6/s1-checkout-baseline.js 2>&1 } -ArgumentList $repoRoot, $k6Json
Write-Output "k6 Job ID: $($k6Job.Id)"
Start-Sleep -Seconds 15
Write-Output "k6 Job state: $($k6Job.State)"

# Verify outbox is processing
Write-Output "`n--- Outbox before kill ---"
Invoke-Pg "SELECT COUNT(*) as pending FROM outbox_events WHERE processed_on IS NULL;"

# === KILL RABBITMQ ===
Write-Output "`n>>> KILLING RABBITMQ at $(Get-Date -Format 'HH:mm:ss') <<<"
docker stop ecommerce-staging-rabbitmq
Start-Sleep -Seconds 3
Write-Output "RabbitMQ container: $(docker ps --filter 'name=ecommerce-staging-rabbitmq' --format '{{.Status}}')"

# Monitor API during MQ outage (90 seconds)
Write-Output "`n--- Monitoring API during RabbitMQ outage (90s) ---"
$failedReqs = 0
$totalReqs = 0
for ($i = 1; $i -le 18; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/products" -UseBasicParsing -TimeoutSec 5
        $sw.Stop()
        Write-Output "[$i/18] Products GET: $($r.StatusCode) $($sw.ElapsedMilliseconds)ms"
    } catch {
        $sw.Stop()
        $failedReqs++
        Write-Output "[$i/18] Products GET: FAILED [$($sw.ElapsedMilliseconds)ms]"
    }
    $totalReqs++
    Start-Sleep -Seconds 5
}

# Check outbox accumulation during outage
Write-Output "`n--- Outbox during RabbitMQ outage (should accumulate) ---"
Invoke-Pg "SELECT COUNT(*) as pending FROM outbox_events WHERE processed_on IS NULL;"

Write-Output "k6 Job state: $($k6Job.State)"

# === RESTORE RABBITMQ ===
Write-Output "`n>>> RESTORING RABBITMQ at $(Get-Date -Format 'HH:mm:ss') <<<"
docker start ecommerce-staging-rabbitmq

for ($i = 1; $i -le 12; $i++) {
    Start-Sleep -Seconds 3
    try {
        $status = docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else}}no-healthcheck{{end}}' ecommerce-staging-rabbitmq 2>$null
        Write-Output "RabbitMQ health: $status (attempt $i/12)"
        if ($status -eq "healthy") { break }
    } catch {
        Write-Output "RabbitMQ health: checking... (attempt $i/12)"
    }
}

# Monitor outbox drain after recovery (60 seconds)
Write-Output "`n--- Monitoring outbox drain after MQ recovery (60s) ---"
for ($i = 1; $i -le 12; $i++) {
    $pending = Invoke-Pg "SELECT COUNT(*) FROM outbox_events WHERE processed_on IS NULL;"
    Write-Output "[$i/12] Pending outbox: $($pending.Trim())"
    Start-Sleep -Seconds 5
}

# Webhook deliveries post-recovery
Write-Output "`n--- Webhook deliveries post-recovery ---"
Invoke-Pg "SELECT status, COUNT(*) FROM webhook_deliveries GROUP BY status ORDER BY status;"

# Stop k6 job
if ($k6Job.State -eq 'Running') {
    Write-Output "`n--- Stopping k6 job ---"
    Stop-Job -Id $k6Job.Id
}
$k6Output = Receive-Job $k6Job -ErrorAction SilentlyContinue
$k6Output | Out-File (Join-Path $resultsDir "k6-output.txt")
Write-Output "k6 output saved"

# Final health
Write-Output "`n--- Final health ---"
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/live" -UseBasicParsing -TimeoutSec 5; Write-Output "Health live: $($r.StatusCode)" } catch { Write-Output "Health live: FAIL" }
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/ready" -UseBasicParsing -TimeoutSec 5; Write-Output "Health ready: $($r.StatusCode)" } catch { Write-Output "Health ready: FAIL" }

Write-Output "`n--- Final outbox state ---"
Invoke-Pg "SELECT COUNT(*) as pending FROM outbox_events WHERE processed_on IS NULL;"

Write-Output "`n=== RABBITMQ CHAOS COMPLETE ==="
