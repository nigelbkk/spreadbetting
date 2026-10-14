[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MarketId,
    [Parameter(Mandatory = $true)]
    [long]$SelectionId,
    [string]$RpcUrl = 'http://100.76.118.17:15055',
    [switch]$ConfirmLiveOrder
)

$ErrorActionPreference = 'Stop'
$size = 2.0
$price = 1.01

if (!$ConfirmLiveOrder) {
    throw 'Refusing live order smoke test. Re-run with -ConfirmLiveOrder.'
}

function Invoke-BetfairRpc([string]$Method, $Params) {
    $body = @{
        jsonrpc = '2.0'
        method = "SportsAPING/v1.0/$Method"
        params = $Params
        id = [guid]::NewGuid().ToString()
    } | ConvertTo-Json -Depth 12 -Compress

    $response = Invoke-RestMethod -Uri $RpcUrl -Method Post -ContentType 'application/json' -Body $body -TimeoutSec 30
    if ($response.error) {
        throw "Betfair RPC $Method failed: $($response.error | ConvertTo-Json -Compress)"
    }
    return $response.result
}

function Get-CurrentOrder([string]$Id, [string]$BetId) {
    $report = Invoke-BetfairRpc 'listCurrentOrders' @{ marketIds = @($Id); betIds = @($BetId) }
    return @($report.currentOrders) | Where-Object { [string]$_.betId -eq $BetId } | Select-Object -First 1
}

$book = @(Invoke-BetfairRpc 'listMarketBook' @{ marketIds = @($MarketId) }) | Select-Object -First 1
if (!$book -or [string]$book.status -ne 'OPEN') { throw "Market $MarketId is not OPEN." }
if (!(@($book.runners) | Where-Object { [long]$_.selectionId -eq $SelectionId })) {
    throw "Selection $SelectionId is not present in market $MarketId."
}

$place = Invoke-BetfairRpc 'placeOrders' @{
    marketId = $MarketId
    instructions = @(@{
        selectionId = $SelectionId
        handicap = 0
        side = 'LAY'
        orderType = 'LIMIT'
        limitOrder = @{ size = $size; price = $price; persistenceType = 'LAPSE' }
    })
}
$instruction = @($place.instructionReports) | Select-Object -First 1
if (!$instruction -or [string]$instruction.status -ne 'SUCCESS' -or !$instruction.betId) {
    throw "Placement did not succeed: $($place | ConvertTo-Json -Depth 12 -Compress)"
}
$betId = [string]$instruction.betId
Write-Output "PLACED betId=$betId marketId=$MarketId selectionId=$SelectionId size=$size price=$price"

try {
    $visible = $null
    for ($attempt = 1; $attempt -le 10 -and !$visible; $attempt++) {
        $visible = Get-CurrentOrder $MarketId $betId
        if (!$visible) { Start-Sleep -Milliseconds 500 }
    }
    if (!$visible) { throw "Placed bet $betId was not returned by listCurrentOrders." }
    Write-Output "DISPLAYED betId=$betId status=$($visible.status) sizeRemaining=$($visible.sizeRemaining)"

    $cancel = Invoke-BetfairRpc 'cancelOrders' @{
        marketId = $MarketId
        instructions = @(@{ betId = [UInt64]$betId })
    }
    Write-Output "CANCEL_REQUESTED betId=$betId status=$($cancel.status)"

    $stillExecutable = $null
    for ($attempt = 1; $attempt -le 10; $attempt++) {
        $stillExecutable = Get-CurrentOrder $MarketId $betId
        if (!$stillExecutable -or [string]$stillExecutable.status -ne 'EXECUTABLE') { break }
        Start-Sleep -Milliseconds 500
    }
    if ($stillExecutable -and [string]$stillExecutable.status -eq 'EXECUTABLE') {
        throw "Bet $betId remains EXECUTABLE after cancellation."
    }
    Write-Output "CANCEL_VERIFIED betId=$betId"
}
catch {
    Write-Error $_
    throw
}
