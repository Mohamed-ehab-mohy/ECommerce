# T-TST-003 S8 webhook receiver. Accepts signed webhook POSTs from the staging API
# container (which reaches the host via host.docker.internal) and records each hit.
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File perf/k6/receiver.ps1 -Port 9099 -LogFile perf/k6/wh-receiver.log
param(
    [int]$Port = 9099,
    [string]$LogFile = (Join-Path $PSScriptRoot 'wh-receiver.log')
)

$listener = New-Object System.Net.HttpListener
$listener.Prefixes.Add("http://+:${Port}/wh/")

try {
    $listener.Start()
}
catch [System.Net.HttpListenerException] {
    Write-Host "HttpListener start failed; granting urlacl and retrying..."
    $user = "$env:USERDOMAIN\$env:USERNAME"
    netsh http add urlacl url="http://+:${Port}/" user="$user" | Out-Null
    $listener.Start()
}

Write-Host "Webhook receiver listening on http://+:${Port}/wh/ -> $LogFile"

while ($true) {
    $context = $listener.GetContext()
    try {
        $request = $context.Request
        $response = $context.Response

        $eventId = $request.Headers['X-Event-Id']
        $signature = $request.Headers['X-Signature']
        $reader = New-Object System.IO.StreamReader($request.InputStream, $request.ContentEncoding)
        $body = $reader.ReadToEnd()
        $reader.Close()

        $line = "{0}|{1}|{2}|{3}|{4}" -f `
            (Get-Date).ToString('o'), $request.HttpMethod, $eventId, $signature, $body.Length
        Add-Content -Path $LogFile -Value $line -Encoding UTF8

        $bytes = [System.Text.Encoding]::UTF8.GetBytes('{"ok":true}')
        $response.StatusCode = 200
        $response.ContentType = 'application/json'
        $response.ContentLength64 = $bytes.Length
        $response.OutputStream.Write($bytes, 0, $bytes.Length)
        $response.Close()
    }
    catch {
        Write-Host "Receiver error: $($_.Exception.Message)"
    }
}
