[CmdletBinding()]
param(
    [string]$ProcessName = 'SpreadTraderV2',
    [string]$ProxyHost = '100.76.118.17',
    [int]$ProxyPort = 18088,
    [string]$LogPath = 'C:\Temp\SpreadTrader\health.jsonl',
    [string]$HeartbeatPath = 'C:\Temp\SpreadTrader\heartbeat.json',
    [int]$HeartbeatMaxAgeSeconds = 45
)

$ErrorActionPreference = 'Stop'
$now = [DateTime]::UtcNow
$logDirectory = Split-Path -Parent $LogPath
New-Item -ItemType Directory -Force -Path $logDirectory | Out-Null

$processes = @(Get-CimInstance Win32_Process -Filter "Name='$ProcessName.exe'" -ErrorAction SilentlyContinue)
$process = $processes | Select-Object -First 1
$proxy = [ordered]@{ reachable = $false; health = $null; error = $null }
$heartbeat = [ordered]@{ installed = $false; fresh = $null; ageSeconds = $null; state = $null; error = $null }

if (Test-Path -LiteralPath $HeartbeatPath) {
    $heartbeat.installed = $true
    try {
        $heartbeatData = Get-Content -LiteralPath $HeartbeatPath -Raw | ConvertFrom-Json
        $heartbeat.state = $heartbeatData.state
        $heartbeat.ageSeconds = [Math]::Max(0, ($now - [DateTime]::Parse($heartbeatData.utc).ToUniversalTime()).TotalSeconds)
        $heartbeat.fresh = ($heartbeat.state -eq 'running' -and $heartbeat.ageSeconds -le $HeartbeatMaxAgeSeconds)
    }
    catch {
        $heartbeat.error = $_.Exception.Message
        $heartbeat.fresh = $false
    }
}

try {
    $tcp = [Net.Sockets.TcpClient]::new()
    $connect = $tcp.ConnectAsync($ProxyHost, $ProxyPort)
    if (-not $connect.Wait(2000)) { throw "TCP connection timeout to ${ProxyHost}:$ProxyPort" }
    $proxy.reachable = $true
    $tcp.Dispose()

    try {
        $proxy.health = Invoke-RestMethod -Uri "http://${ProxyHost}:${ProxyPort}/health" -TimeoutSec 3
    }
    catch {
        $proxy.error = "Health endpoint unavailable: $($_.Exception.Message)"
    }
}
catch {
    $proxy.error = $_.Exception.Message
}

$record = [ordered]@{
    utc = $now.ToString('o')
    process = [ordered]@{
        name = $ProcessName
        running = $null -ne $process
        pid = if ($process) { [int]$process.ProcessId } else { $null }
        startUtc = $null
    }
    proxy = $proxy
    heartbeat = $heartbeat
    healthy = (($null -ne $process) -and $proxy.reachable -and (($heartbeat.installed -eq $false) -or $heartbeat.fresh))
}

if ($process -and $process.CreationDate) {
    try {
        $record.process.startUtc = ([Management.ManagementDateTimeConverter]::ToDateTime($process.CreationDate)).ToUniversalTime().ToString('o')
    }
    catch {
        # Some process providers expose a non-DMTF or incomplete timestamp.
        # Liveness remains valid; retain the process/PID result and omit only startUtc.
    }
}

$record | ConvertTo-Json -Compress -Depth 8 | Add-Content -LiteralPath $LogPath -Encoding UTF8
$record | ConvertTo-Json -Depth 8

if (-not $record.healthy) { exit 2 }
exit 0
