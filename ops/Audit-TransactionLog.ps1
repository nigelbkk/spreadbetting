param(
    [string]$LogPath = "C:\Development\SXTrader\logs\transactions.csv",
    [string]$StatePath = "C:\SpreadTraderAudit\audit-state.json",
    [string]$AuditLogPath = "C:\SpreadTraderAudit\audit.log",
    [int]$ContextLines = 3
)

$ErrorActionPreference = "Stop"

function Write-AuditLog {
    param([string]$Message)

    $dir = Split-Path -Parent $AuditLogPath
    if ($dir) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    Add-Content -Path $AuditLogPath -Value "[$((Get-Date).ToString("o"))] $Message"
}

function Send-Telegram {
    param([string]$Text)

    $token = $env:TELEGRAM_BOT_TOKEN
    $chatId = $env:TELEGRAM_CHAT_ID

    if ([string]::IsNullOrWhiteSpace($token) -or [string]::IsNullOrWhiteSpace($chatId)) {
        Write-AuditLog "Telegram not configured. TELEGRAM_BOT_TOKEN or TELEGRAM_CHAT_ID is missing."
        return
    }

    Invoke-RestMethod `
        -Uri "https://api.telegram.org/bot$token/sendMessage" `
        -Method Post `
        -Body @{
            chat_id = $chatId
            text = $Text
        } | Out-Null
}

function Get-State {
    if (Test-Path $StatePath) {
        try {
            return Get-Content -Raw -Path $StatePath | ConvertFrom-Json
        }
        catch {
            Write-AuditLog "State file was unreadable; starting fresh. $($_.Exception.Message)"
        }
    }

    return [pscustomobject]@{
        LogPath = $LogPath
        LastLength = 0
        LastWriteUtc = ""
    }
}

function Save-State {
    param([long]$Length, [datetime]$LastWriteUtc)

    $dir = Split-Path -Parent $StatePath
    if ($dir) {
        New-Item -ItemType Directory -Force -Path $dir | Out-Null
    }

    [pscustomobject]@{
        LogPath = $LogPath
        LastLength = $Length
        LastWriteUtc = $LastWriteUtc.ToString("o")
    } | ConvertTo-Json | Set-Content -Path $StatePath
}

function Get-NewLogText {
    param($State, [System.IO.FileInfo]$File)

    $previousLength = 0
    if ($null -ne $State.LastLength) {
        $previousLength = [long]$State.LastLength
    }
    $currentLength = $File.Length

    if ($currentLength -lt $previousLength) {
        Write-AuditLog "Log appears to have reset. Previous length=$previousLength current length=$currentLength"
        $previousLength = 0
    }

    if ($currentLength -eq $previousLength) {
        return ""
    }

    $stream = [System.IO.File]::Open($File.FullName, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $stream.Seek($previousLength, [System.IO.SeekOrigin]::Begin) | Out-Null
        $reader = New-Object System.IO.StreamReader($stream)
        return $reader.ReadToEnd()
    }
    finally {
        if ($reader) {
            $reader.Dispose()
        }
        $stream.Dispose()
    }
}

function Find-Problems {
    param([string[]]$Lines)

    $patterns = @(
        "DispatcherUnhandledException",
        "UnhandledException",
        "UnobservedTaskException",
        "\[PNL LOOP ERROR\]",
        "\[BOOK LOOP ERROR\]",
        "UI THREAD VIOLATION",
        "REAL CONCURRENCY",
        "INCONSISTENCY",
        "MISSING IN UI",
        "Exception",
        "failed",
        "Duplicate partial",
        "Existing partial fragment observed",
        "Remainder cancelled after partial match",
        "Older Pd observed"
    )

    $matches = New-Object System.Collections.Generic.List[string]

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $line = $Lines[$i]
        foreach ($pattern in $patterns) {
            if ($line -match $pattern) {
                $start = [Math]::Max(0, $i - $ContextLines)
                $end = [Math]::Min($Lines.Count - 1, $i + $ContextLines)
                $context = ($Lines[$start..$end] -join "`n")
                $matches.Add("Pattern: $pattern`n$context")
                break
            }
        }
    }

    return $matches
}

try {
    if (!(Test-Path $LogPath)) {
        Write-AuditLog "Log file not found: $LogPath"
        Send-Telegram "SpreadTrader audit: log file not found: $LogPath"
        exit 1
    }

    $file = Get-Item $LogPath
    $state = Get-State
    $newText = Get-NewLogText -State $state -File $file

    Save-State -Length $file.Length -LastWriteUtc $file.LastWriteTimeUtc

    if ([string]::IsNullOrWhiteSpace($newText)) {
        Write-AuditLog "No new log content."
        exit 0
    }

    $lines = $newText -split "`r?`n" | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
    $problems = Find-Problems -Lines $lines

    if ($problems.Count -eq 0) {
        Write-AuditLog "Scanned $($lines.Count) new lines. No problems found."
        exit 0
    }

    $maxSections = 8
    $body = ($problems | Select-Object -First $maxSections) -join "`n`n---`n`n"
    $more = if ($problems.Count -gt $maxSections) { "`n`n...and $($problems.Count - $maxSections) more matches." } else { "" }
    $message = "SpreadTrader audit found $($problems.Count) suspicious log match(es) in $LogPath.`n`n$body$more"

    if ($message.Length -gt 3900) {
        $message = $message.Substring(0, 3900) + "`n...(truncated)"
    }

    Write-AuditLog "Scanned $($lines.Count) new lines. Found $($problems.Count) suspicious matches."
    Send-Telegram $message
}
catch {
    $err = "SpreadTrader audit failed: $($_.Exception.Message)"
    Write-AuditLog "$err`n$($_.ScriptStackTrace)"
    Send-Telegram $err
    exit 1
}
