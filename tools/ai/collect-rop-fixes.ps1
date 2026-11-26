<#
.SYNOPSIS
    Prints the sorted set of issues that the pull requests merged into a base branch close.

.DESCRIPTION
    Reads the closed pull requests of a base branch through the REST API (never `gh pr list`, which is
    GraphQL-backed; docs/engineering/github-api-quota.md), keeps the merged ones, and extracts every
    GitHub closing keyword from the PR body (close/closes/closed, fix/fixes/fixed, resolve/resolves/resolved,
    case-insensitive, optional colon, `#N` or `dlrivada/Encina#N`).

    stdout is one `Fixes #N` line per unique issue, sorted numerically ascending, and nothing else. The
    ROP union migration (#3667) uses it to build the body of the Z0 pull request, so a failed API call
    exits non-zero and prints no partial set: a silently incomplete set would leave slice issues open.

.PARAMETER Base
    The base branch whose merged pull requests are read. Default: feat/rop-union-migration.

.PARAMETER Repository
    owner/name of the repository. Default: dlrivada/Encina.

.PARAMETER Limit
    Read only the most recent N merged pull requests (the API returns the newest first). 0 (default) reads all.

.EXAMPLE
    pwsh -NoProfile -File tools/ai/collect-rop-fixes.ps1

.EXAMPLE
    pwsh -NoProfile -File tools/ai/collect-rop-fixes.ps1 -Base main -Limit 20
#>
[CmdletBinding()]
param(
    [string]$Base = 'feat/rop-union-migration',
    [string]$Repository = 'dlrivada/Encina',
    [ValidateRange(0, 100000)]
    [int]$Limit = 0
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# keyword, optional colon, whitespace, then #N, owner/repo#N or the issue URL form.
# GH-N is not matched: this repository does not use it.
$repoEsc = [regex]::Escape($Repository)
$closingPattern = '(?i)(?<![\w-])(?:close[sd]?|fix(?:e[sd])?|resolve[sd]?)\s*:?\s+(?:(?:' + $repoEsc + ')?#|https://github\.com/' + $repoEsc + '/issues/)(?<n>\d+)'

# A mistyped or not yet created base returns an empty list with HTTP 200, so check the branch first.
& gh api ('repos/{0}/branches/{1}' -f $Repository, $Base) --silent
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("collect-rop-fixes: base branch '$Base' not found in $Repository (or the API failed); no set printed.")
    exit 1
}

$uri = 'repos/{0}/pulls?state=closed&base={1}&per_page=100' -f $Repository, [uri]::EscapeDataString($Base)
$jq = '.[] | select(.merged_at != null) | {number: .number, body: .body}'

$issues = [System.Collections.Generic.HashSet[int]]::new()
$read = 0
$rows = & gh api --paginate $uri --jq $jq
if ($LASTEXITCODE -ne 0) {
    [Console]::Error.WriteLine("collect-rop-fixes: gh api failed for base '$Base' (exit $LASTEXITCODE); no set printed.")
    exit 1
}

foreach ($row in $rows) {
    if ([string]::IsNullOrWhiteSpace($row)) { continue }
    $pr = $row | ConvertFrom-Json
    $read++
    $body = if ($null -eq $pr.body) { '' } else { [string]$pr.body }
    foreach ($m in [regex]::Matches($body, $closingPattern)) {
        [void]$issues.Add([int]$m.Groups['n'].Value)
    }
    if ($Limit -gt 0 -and $read -ge $Limit) { break }
}

[Console]::Error.WriteLine("collect-rop-fixes: read $read merged pull request(s) into '$Base', found $($issues.Count) issue(s).")
foreach ($n in ($issues | Sort-Object)) {
    "Fixes #$n"
}
