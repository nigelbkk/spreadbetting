[CmdletBinding()]
param(
    [string]$Path = 'C:\Temp\spreadtrader-health.json',
    [int]$MaxAgeSeconds = 120
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $Path)) {
    throw "SpreadTrader health snapshot not found: $Path"
}

$item = Get-Item -LiteralPath $Path
$health = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
$age = ((Get-Date).ToUniversalTime() - $item.LastWriteTimeUtc).TotalSeconds

[pscustomobject]@{
    SnapshotUtc       = $health.GeneratedUtc
    SnapshotAgeSec    = [math]::Round($age, 1)
    Fresh             = ($age -le $MaxAgeSeconds)
    ProcessId         = $health.ProcessId
    StreamConnected   = $health.Stream.Connected
    HubUri            = $health.Stream.HubUri
    LastMessageUtc    = $health.Stream.LastMessageUtc
    Subscriptions     = $health.Stream.ActiveSubscriptions
    MarketQueue       = $health.Stream.MarketQueueCount
    OrderQueue        = $health.Stream.OrderQueueCount
    DroppedMarket     = $health.Stream.DroppedMarketMessages
    DroppedOrder      = $health.Stream.DroppedOrderMessages
    RpcCalls          = $health.RpcCalls
    RpcFailures       = $health.RpcFailures
    MarketEngines     = @($health.MarketEngines).Count
} | Format-List

if ($age -gt $MaxAgeSeconds) {
    Write-Warning "SpreadTrader health snapshot is stale ($([math]::Round($age, 1)) seconds old)."
}
