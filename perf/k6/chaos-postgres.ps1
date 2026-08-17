$ErrorActionPreference = 'Continue'
$resultsDir = Join-Path $PSScriptRoot "results-chaos-postgres"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not (Test-Path $resultsDir)) { New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null }

Write-Output "=== CHAOS TEST: PostgreSQL Kill Under Load ==="
Write-Output "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
Write-Output "NOTE: Single-node staging. No standby to failover to. Testing graceful failure + recovery."

function Invoke-Pg($sql) { docker exec ecommerce-staging-postgres psql -U ecommerce -d ecommerce_staging -t -c $sql }

# Baseline
Write-Output "`n--- Pre-chaos baseline ---"
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/live" -UseBasicParsing -TimeoutSec 5; Write-Output "API live: $($r.StatusCode)" } catch { Write-Output "API live: FAIL" }
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/ready" -UseBasicParsing -TimeoutSec 5; Write-Output "API ready: $($r.StatusCode)" } catch { Write-Output "API ready: FAIL" }

Write-Output "`n--- Orders count baseline ---"
Invoke-Pg "SELECT COUNT(*) FROM orders;"

# Start k6 S1 checkout via Start-Job
Write-Output "`n--- Starting k6 S1 checkout (background job) ---"
$k6Json = Join-Path $resultsDir "s1-pg-chaos.json"
$k6Job = Start-Job -ScriptBlock { param($wd, $out) Set-Location $wd; & k6 run --out "json=$out" perf/k6/s1-checkout-baseline.js 2>&1 } -ArgumentList $repoRoot, $k6Json
Write-Output "k6 Job ID: $($k6Job.Id)"
Start-Sleep -Seconds 15
Write-Output "k6 Job state: $($k6Job.State)"

# === KILL POSTGRES ===
Write-Output "`n>>> KILLING POSTGRESQL at $(Get-Date -Format 'HH:mm:ss') <<<"
docker stop ecommerce-staging-postgres
Start-Sleep -Seconds 3
Write-Output "Postgres container: $(docker ps --filter 'name=ecommerce-staging-postgres' --format '{{.Status}}')"

# Monitor API during PG outage (60 seconds)
Write-Output "`n--- Monitoring API during PostgreSQL outage (60s) ---"
$failedReqs = 0
$totalReqs = 0
for ($i = 1; $i -le 12; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/products" -UseBasicParsing -TimeoutSec 5
        $sw.Stop()
        Write-Output "[$i/12] Products GET: $($r.StatusCode) $($sw.ElapsedMilliseconds)ms"
    } catch {
        $sw.Stop()
        $failedReqs++
        $code = "N/A"
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        Write-Output "[$i/12] Products GET: FAILED (HTTP $code) [$($sw.ElapsedMilliseconds)ms]"
    }
    $totalReqs++
    Start-Sleep -Seconds 5
}

# Check API container alive
Write-Output "`n--- API container ---"
Write-Output "API container: $(docker ps --filter 'name=ecommerce-staging-api' --format '{{.Status}}')"

# === RESTORE POSTGRES ===
Write-Output "`n>>> RESTORING POSTGRESQL at $(Get-Date -Format 'HH:mm:ss') <<<"
docker start ecommerce-staging-postgres

for ($i = 1; $i -le 20; $i++) {
    Start-Sleep -Seconds 3
    try {
        $status = docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else}}no-healthcheck{{end}}' ecommerce-staging-postgres 2>$null
        Write-Output "Postgres health: $status (attempt $i/20)"
        if ($status -eq "healthy") { break }
    } catch {
        Write-Output "Postgres health: checking... (attempt $i/20)"
    }
}

# Post-recovery monitoring (30 seconds)
Write-Output "`n--- Post-recovery monitoring (30s) ---"
for ($i = 1; $i -le 6; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/products" -UseBasicParsing -TimeoutSec 5
        $sw.Stop()
        Write-Output "[$i/6] Products: $($r.StatusCode) $($sw.ElapsedMilliseconds)ms"
    } catch {
        $sw.Stop()
        Write-Output "[$i/6] Products: FAILED [$($sw.ElapsedMilliseconds)ms]"
    }
    Start-Sleep -Seconds 5
}

# Final health
Write-Output "`n--- Final health ---"
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/live" -UseBasicParsing -TimeoutSec 5; Write-Output "Health live: $($r.StatusCode)" } catch { Write-Output "Health live: FAIL" }
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/ready" -UseBasicParsing -TimeoutSec 5; Write-Output "Health ready: $($r.StatusCode)" } catch { Write-Output "Health ready: FAIL" }

# Data integrity
Write-Output "`n--- Data integrity post-recovery ---"
Invoke-Pg "SELECT COUNT(*) FROM orders;"
Invoke-Pg "SELECT COUNT(*) FROM outbox_events WHERE processed_on IS NULL;"

# Stop k6 job
if ($k6Job.State -eq 'Running') {
    Write-Output "`n--- Stopping k6 job ---"
    Stop-Job -Id $k6Job.Id
}
$k6Output = Receive-Job $k6Job -ErrorAction SilentlyContinue
$k6Output | Out-File (Join-Path $resultsDir "k6-output.txt")

Write-Output "`n=== POSTGRESQL CHAOS COMPLETE ==="
Write-Output "Request failures during outage: $failedReqs / $totalReqs"
