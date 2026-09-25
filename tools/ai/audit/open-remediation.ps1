# tools/ai/audit/open-remediation.ps1 -Issue <n> (#1345; moved from the unversioned artifacts/knowledge/)
#
# Opens the remediation drafts collected for one audited issue (artifacts/knowledge/remediation/<n>-*.md).
# Drafts carry an HTML-comment header with title/labels/milestone; unknown labels are dropped; bugs default
# to Hardening.

param([Parameter(Mandatory)][int]$Issue)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$root = Get-MainRoot $PSScriptRoot
$dir = Join-Path $root 'artifacts\knowledge\remediation'
$opened = Join-Path $dir 'opened.csv'
$labels = gh label list --repo dlrivada/Encina --limit 400 --json name --jq '.[].name'
$ms = gh api repos/dlrivada/Encina/milestones --jq '.[].title'
foreach ($f in Get-ChildItem $dir -Filter "$Issue-*.md") {
    if ((Test-Path $opened) -and (Select-String -Path $opened -SimpleMatch $f.Name -Quiet)) { continue }
    $raw = Get-Content -Raw $f.FullName
    $h = ([regex]::Match($raw, '(?s)<!--(.*?)-->')).Groups[1].Value
    $title = ([regex]::Match($h, 'title:\s*(.+)')).Groups[1].Value.Trim()
    if (-not $title) { Write-Warning "no title in $($f.Name)"; continue }
    $lab = @((([regex]::Match($h, 'labels:\s*(.+)')).Groups[1].Value -split ',\s*') | ForEach-Object { $_.Trim() } | Where-Object { $labels -contains $_ })
    if (-not $lab) { $lab = @(if ($title.StartsWith('[BUG]')) { 'bug' } elseif ($title.StartsWith('[TEST]')) { 'area-testing' } else { 'technical-debt' }) }
    $m = ([regex]::Match($h, 'milestone:\s*(.+)')).Groups[1].Value.Trim()
    if (-not ($ms -contains $m)) { $m = if ($title.StartsWith('[BUG]')) { 'v0.14.0 — Hardening' } else { '' } }
    $body = ($raw -replace '(?s)^\s*<!--.*?-->\s*', '').Trim()
    $bf = Join-Path $env:TEMP "rem-$($f.BaseName).md"; Set-Content $bf $body -Encoding utf8
    $a = @('issue', 'create', '--repo', 'dlrivada/Encina', '--title', $title, '--body-file', $bf)
    if ($m) { $a += @('--milestone', $m) }
    foreach ($l in $lab) { $a += @('--label', $l) }
    $url = & gh @a
    if ($LASTEXITCODE -ne 0 -or -not $url -or $url -notmatch '^https://github.com/') {
        Write-Error "open-remediation: gh issue create failed for $($f.Name) (exit $LASTEXITCODE, url: '$url')"
        exit 1
    }
    Add-Content $opened "$($f.Name),$url"
    "$url  $title"
}
