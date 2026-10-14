[CmdletBinding()]
param(
    [Parameter(Mandatory = $false)]
    [string]$MarketId,
    [Parameter(Mandatory = $false)]
    [long]$SelectionId,
    [string]$RpcUrl = 'http://100.76.118.17:15055',
    [switch]$ConfirmLiveOrder,
    [int]$HorseMarketWindowMinutes = 30
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

if (!$MarketId -or !$SelectionId) {
    # Betfair rejects PowerShell's round-trip offset/fraction format here;
    # send strict UTC timestamps as required by the API.
    $from = [DateTime]::UtcNow.AddMinutes(-2).ToString('yyyy-MM-ddTHH:mm:ssZ')
    $to = [DateTime]::UtcNow.AddMinutes($HorseMarketWindowMinutes).ToString('yyyy-MM-ddTHH:mm:ssZ')
    $catalogue = @(Invoke-BetfairRpc 'listMarketCatalogue' @{
        filter = @{
            eventTypeIds = @('7')
            marketTypeCodes = @('WIN')
            marketStartTime = @{ from = $from; to = $to }
        }
        # The relay accepts the stable projections below; marketName is
        # returned by default.  EVENT/MARKET_NAME trigger DSC-0018 on this
        # Betfair endpoint.
        marketProjection = @('MARKET_START_TIME', 'RUNNER_DESCRIPTION')
        sort = 'FIRST_TO_START'
        maxResults = 50
    })
    $candidateIds = @($catalogue | Where-Object { $_.marketId } | ForEach-Object { [string]$_.marketId })
    if ($candidateIds.Count -eq 0) { throw "No horse WIN markets start within $HorseMarketWindowMinutes minutes." }

    $books = @(Invoke-BetfairRpc 'listMarketBook' @{ marketIds = $candidateIds })
    # Market-book responses do not repeat marketStartTime.  Walk the
    # catalogue's FIRST_TO_START order and choose the first matching OPEN book.
    $open = $null
    foreach ($candidate in $catalogue) {
        $book = $books | Where-Object { [string]$_.marketId -eq [string]$candidate.marketId -and [string]$_.status -eq 'OPEN' } | Select-Object -First 1
        if ($book -and @($book.runners).Count -gt 0) { $open = $book; break }
    }
    if (!$open) { throw 'No currently OPEN horse WIN market was found in the near-market window.' }
    $MarketId = [string]$open.marketId
    $SelectionId = [long](@($open.runners) | Select-Object -First 1).selectionId
    Write-Output "SELECTED marketId=$MarketId selectionId=$SelectionId"
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
finally {
    # Never leave a successful smoke-test order working if verification fails.
    if ($betId) {
        try {
            $remaining = Get-CurrentOrder $MarketId $betId
            if ($remaining -and [string]$remaining.status -eq 'EXECUTABLE') {
                Invoke-BetfairRpc 'cancelOrders' @{
                    marketId = $MarketId
                    instructions = @(@{ betId = [UInt64]$betId })
                } | Out-Null
                Write-Output "CLEANUP_CANCEL_REQUESTED betId=$betId"
            }
        }
        catch {
            Write-Error "CLEANUP_FAILED betId=$betId $($_.Exception.Message)"
        }
    }
}
