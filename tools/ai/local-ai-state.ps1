<#
.SYNOPSIS
    The switch for the maintainer's local model (llama-server), and the stand-in ledger writer (#1593).

.DESCRIPTION
    State file: <main checkout>\artifacts\local-ai\state.json, {"state":"off","reason":"...","sinceUtc":"..."};
    an absent file means ON. The main checkout is the parent of `git rev-parse --path-format=absolute
    --git-common-dir` run from this script's own folder, so every worktree resolves the same file.

    ONLY THE MAIN SESSION runs -Off and -On, on the maintainer's order. Workers, specialists and scripts only
    read the state (-Status, or tools/ai/local-ai-ask.cs, which reads it first).

    While the state is OFF, tools/ai/local-ai-ask.cs drafts through the stand-in engine (the Claude Code CLI,
    haiku, low effort) and records each call in artifacts/local-ai/standin-ledger.csv. When the CLI is not
    available it exits with code 3; the caller then spawns the local-ai-standin agent through the Agent tool
    and records that spawn with -RecordStandin.

.PARAMETER Off
    Switch the local model off. Requires -Reason.
.PARAMETER Reason
    Why it is off (for example "hardware temperature").
.PARAMETER On
    Switch the local model on (removes the state file).
.PARAMETER Status
    Print ON, or OFF with the reason and the since-UTC time. The default action.
.PARAMETER StateFile
    Override the state file path (tests).
.PARAMETER RecordStandin
    Append one row of the local-ai-standin agent route to standin-ledger.csv: needs -Task, -OutFile, -Tokens.
.PARAMETER Task
    Task name for -RecordStandin.
.PARAMETER OutFile
    The draft's output file, written exactly as it was passed to the stand-in. The ledger lives in
    <directory of OutFile>\..\standin-ledger.csv (the artifacts\local-ai folder of the same root).
.PARAMETER Tokens
    The agent's subagent_tokens (stored as completionTokens; promptTokens stays empty).
.PARAMETER Seconds
    The agent's duration in seconds (optional).
#>
[CmdletBinding()]
param(
    [switch]$Off,
    [string]$Reason,
    [switch]$On,
    [switch]$Status,
    [string]$StateFile,
    [switch]$RecordStandin,
    [string]$Task,
    [string]$OutFile,
    [long]$Tokens,
    [double]$Seconds
)

$ErrorActionPreference = 'Stop'

function Get-StateFilePath {
    if ($StateFile) { return [IO.Path]::GetFullPath($StateFile) }
    $common = (& git -C $PSScriptRoot rev-parse --path-format=absolute --git-common-dir 2>$null)
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($common)) { throw 'git rev-parse --git-common-dir failed; cannot resolve the main checkout' }
    $mainRoot = Split-Path -Parent ([string]$common).Trim()
    return Join-Path $mainRoot 'artifacts\local-ai\state.json'
}

$modes = @($Off.IsPresent, $On.IsPresent, $Status.IsPresent, $RecordStandin.IsPresent) | Where-Object { $_ }
if ($modes.Count -gt 1) { throw 'Use exactly one of -Off, -On, -Status, -RecordStandin.' }

if ($RecordStandin) {
    if (-not $Task -or -not $OutFile -or $PSBoundParameters.ContainsKey('Tokens') -eq $false) { throw '-RecordStandin needs -Task, -OutFile and -Tokens.' }
    $ledger = [IO.Path]::GetFullPath((Join-Path (Split-Path -Parent ([IO.Path]::GetFullPath($OutFile))) '..\standin-ledger.csv'))
    New-Item -ItemType Directory -Force (Split-Path -Parent $ledger) | Out-Null
    if (-not (Test-Path -LiteralPath $ledger)) {
        Add-Content -LiteralPath $ledger -Value 'timestampUtc,task,promptTokens,completionTokens,seconds,tokensPerSecond,outFile,costUsd'
    }
    $secs = if ($PSBoundParameters.ContainsKey('Seconds')) { $Seconds.ToString('F1', [Globalization.CultureInfo]::InvariantCulture) } else { '' }
    $file = if ($OutFile -match '[",\n]') { '"' + ($OutFile -replace '"', '""') + '"' } else { $OutFile }
    $row = '{0},{1},,{2},{3},,{4},' -f [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ'), $Task, $Tokens, $secs, $file
    Add-Content -LiteralPath $ledger -Value $row
    "recorded: $row -> $ledger"
    return
}

$path = Get-StateFilePath

if ($Off) {
    if ([string]::IsNullOrWhiteSpace($Reason)) { throw '-Off needs -Reason <text>.' }
    New-Item -ItemType Directory -Force (Split-Path -Parent $path) | Out-Null
    $state = [ordered]@{ state = 'off'; reason = $Reason.Trim(); sinceUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ') }
    Set-Content -LiteralPath $path -Value ($state | ConvertTo-Json -Compress) -Encoding utf8
    "OFF: $($state.reason) (since $($state.sinceUtc))"
    return
}

if ($On) {
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    'ON'
    return
}

# -Status (default)
if (-not (Test-Path -LiteralPath $path)) { 'ON'; return }
$raw = Get-Content -LiteralPath $path -Raw
$s = $raw | ConvertFrom-Json
# ConvertFrom-Json turns the ISO timestamp into a local DateTime; print the text as stored.
$since = [regex]::Match($raw, '"sinceUtc"\s*:\s*"(?<v>[^"]*)"').Groups['v'].Value
if ([string]$s.state -eq 'off') { "OFF: $($s.reason) (since $since)" } else { 'ON' }
