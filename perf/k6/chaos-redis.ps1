$ErrorActionPreference = 'Continue'
$resultsDir = Join-Path $PSScriptRoot "results-chaos-redis"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not (Test-Path $resultsDir)) { New-Item -ItemType Directory -Path $resultsDir -Force | Out-Null }

Write-Output "=== CHAOS TEST: Redis Kill Under Load ==="
Write-Output "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

function Invoke-Pg($sql) { docker exec ecommerce-staging-postgres psql -U ecommerce -d ecommerce_staging -t -c $sql }

# Baseline
Write-Output "`n--- Pre-chaos baseline ---"
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/live" -UseBasicParsing -TimeoutSec 5; Write-Output "API live: $($r.StatusCode)" } catch { Write-Output "API live: FAIL" }
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/health/ready" -UseBasicParsing -TimeoutSec 5; Write-Output "API ready: $($r.StatusCode)" } catch { Write-Output "API ready: FAIL" }
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/products" -UseBasicParsing -TimeoutSec 5; Write-Output "Products: $($r.StatusCode)" } catch { Write-Output "Products: FAIL" }

Write-Output "`n--- Outbox baseline ---"
Invoke-Pg "SELECT COUNT(*) as pending FROM outbox_events WHERE processed_on IS NULL;"

# Start k6 via Start-Job
Write-Output "`n--- Starting k6 catalog browse (background job) ---"
$k6Json = Join-Path $resultsDir "s2-redis-chaos.json"
$k6Job = Start-Job -ScriptBlock { param($wd, $out) Set-Location $wd; & k6 run --out "json=$out" perf/k6/s2-catalog-browse.js 2>&1 } -ArgumentList $repoRoot, $k6Json
Write-Output "k6 Job ID: $($k6Job.Id)"
Start-Sleep -Seconds 10
Write-Output "k6 Job state: $($k6Job.State)"

# Pre-kill baseline latency
Write-Output "`n--- Pre-kill latency ---"
$sw = [System.Diagnostics.Stopwatch]::StartNew()
try { $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/products" -UseBasicParsing -TimeoutSec 5; $sw.Stop(); Write-Output "Products: $($r.StatusCode) $($sw.ElapsedMilliseconds)ms" } catch { $sw.Stop(); Write-Output "Products: FAIL $($sw.ElapsedMilliseconds)ms" }

# === KILL REDIS ===
Write-Output "`n>>> KILLING REDIS at $(Get-Date -Format 'HH:mm:ss') <<<"
docker stop ecommerce-staging-redis
Start-Sleep -Seconds 3
Write-Output "Redis container: $(docker ps --filter 'name=ecommerce-staging-redis' --format '{{.Status}}')"

# Monitor API during Redis outage (90 seconds)
Write-Output "`n--- Monitoring API during Redis outage (90s) ---"
$failedReqs = 0
$totalReqs = 0
for ($i = 1; $i -le 18; $i++) {
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:8080/api/v1/products" -UseBasicParsing -TimeoutSec 5
        $sw.Stop()
        Write-Output "[$i/18] Products: $($r.StatusCode) $($sw.ElapsedMilliseconds)ms"
    } catch {
        $sw.Stop()
        $failedReqs++
        Write-Output "[$i/18] Products: FAILED [$($sw.ElapsedMilliseconds)ms]"
    }
    $totalReqs++
    Start-Sleep -Seconds 5
}

Write-Output "`n--- Outbox during Redis outage ---"
Invoke-Pg "SELECT COUNT(*) as pending FROM outbox_events WHERE processed_on IS NULL;"

# Check k6 job
Write-Output "k6 Job state: $($k6Job.State)"

# === RESTORE REDIS ===
Write-Output "`n>>> RESTORING REDIS at $(Get-Date -Format 'HH:mm:ss') <<<"
docker start ecommerce-staging-redis

for ($i = 1; $i -le 12; $i++) {
    Start-Sleep -Seconds 3
    try {
        $status = docker inspect --format='{{if .State.Health}}{{.State.Health.Status}}{{else}}no-healthcheck{{end}}' ecommerce-staging-redis 2>$null
        Write-Output "Redis health: $status (attempt $i/12)"
        if ($status -eq "healthy") { break }
    } catch {
        Write-Output "Redis health: checking... (attempt $i/12)"
    }
}

# Post-recovery monitoring (30s)
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

Write-Output "`n--- Outbox post-recovery ---"
Invoke-Pg "SELECT COUNT(*) as pending FROM outbox_events WHERE processed_on IS NULL;"

# Stop k6 job
if ($k6Job.State -eq 'Running') {
    Write-Output "`n--- Stopping k6 job ---"
    Stop-Job -Id $k6Job.Id
}
$k6Output = Receive-Job $k6Job -ErrorAction SilentlyContinue
$k6Output | Out-File (Join-Path $resultsDir "k6-output.txt")
Write-Output "k6 output saved to results dir"

Write-Output "`n=== REDIS CHAOS COMPLETE ==="
Write-Output "Failed requests during outage: $failedReqs / $totalReqs"
