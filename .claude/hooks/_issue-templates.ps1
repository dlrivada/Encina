# Shared by check-issue-template.ps1 (gh issue create) and check-target-issue-template.ps1 (agent spawn gate,
# #1980): the repository root lookup, the issue templates read at run time from .github/ISSUE_TEMPLATE, and the
# level-2 header extraction of an issue body. One copy, so the two hooks never disagree on what a template is.

# The repository root: walked up from $StartDir looking for .github/ISSUE_TEMPLATE, falling back to
# $env:CLAUDE_PROJECT_DIR.
function Get-RepoRoot([string]$StartDir) {
    $root = $StartDir
    while ($root -and -not (Test-Path -LiteralPath (Join-Path $root '.github/ISSUE_TEMPLATE'))) {
        $parent = Split-Path -Parent $root
        if (-not $parent -or $parent -eq $root) { $root = $null; break }
        $root = $parent
    }
    if (-not $root -and $env:CLAUDE_PROJECT_DIR) { $root = $env:CLAUDE_PROJECT_DIR }
    return $root
}

# Title prefix ([BUG], [DEBT], ...) -> @{ File; DefaultLabel; Headers }, from the template files themselves.
function Get-Templates([string]$Root) {
    $templates = @{}
    if (-not $Root) { return $templates }
    $dir = Join-Path $Root '.github/ISSUE_TEMPLATE'
    if (-not (Test-Path -LiteralPath $dir)) { return $templates }
    foreach ($file in Get-ChildItem -LiteralPath $dir -Filter '*.md') {
        $lines = Get-Content -LiteralPath $file.FullName
        $titleLine = $lines | Where-Object { $_ -match '^title:\s*"(\[[A-Z]+\])' } | Select-Object -First 1
        if (-not $titleLine) { continue }
        $prefix = [regex]::Match($titleLine, '\[[A-Z]+\]').Value
        $labelLine = $lines | Where-Object { $_ -match '^labels:\s*(.+?)\s*$' } | Select-Object -First 1
        $defaultLabel = if ($labelLine -and $labelLine -match '^labels:\s*"?(?<l>[^",]+)') { $Matches['l'].Trim() } else { '' }
        $templates[$prefix] = @{ File = $file.Name; DefaultLabel = $defaultLabel; Headers = @($lines | Where-Object { $_ -cmatch '^## \S' } | ForEach-Object { $_.Trim() }) }
    }
    return $templates
}

# The level-2 headers of an issue body, in order; headers inside fenced code blocks do not count.
function Get-Headers([string]$Body) {
    $headers = [System.Collections.Generic.List[string]]::new()
    $fence = $null
    foreach ($line in ($Body -split "`r?`n")) {
        if ($fence) {
            if ($line -match "^\s{0,3}$([regex]::Escape($fence.Char)){$($fence.Length),}\s*$") { $fence = $null }
            continue
        }
        # CommonMark: a backtick fence's info string cannot contain a backtick (that line is inline code).
        $open = [regex]::Match($line, '^\s{0,3}(?<f>`{3,}|~{3,})(?<info>.*)$')
        if ($open.Success -and $open.Groups['f'].Value[0] -eq '`' -and $open.Groups['info'].Value.Contains('`')) { $open = [System.Text.RegularExpressions.Match]::Empty }
        if ($open.Success) { $fence = @{ Char = [string]$open.Groups['f'].Value[0]; Length = $open.Groups['f'].Value.Length }; continue }
        if ($line -cmatch '^## \S') { $headers.Add($line.Trim()) }
    }
    return , $headers
}
