param(
    [string]$Repo = 'dlrivada/Encina',
    [string]$Csv = (Join-Path (git rev-parse --show-toplevel) 'artifacts\local-ai\issues\classification.csv'),
    [string]$Out = (Join-Path (git rev-parse --show-toplevel) 'artifacts\local-ai\issues\classification-final.csv'),
    [switch]$DryRun
)

# Applies the maintainer-approved P0-P3 classification as GitHub labels.
# Maintainer decisions (2026-09-22): example issues are P1; EPIC containers carry no priority label.

$ErrorActionPreference = 'Continue'
$labels = @{
    P0 = @{ name = 'p0-mandatory';   color = 'B60205'; desc = 'Backlog priority P0: mandatory for 1.0 (SPEC-000, ENCINA-1.0-RECONCILIATION section 4)' }
    P1 = @{ name = 'p1-recommended'; color = 'D93F0B'; desc = 'Backlog priority P1: recommended for 1.0, deferrable with an explicit reason' }
    P2 = @{ name = 'p2-post-1.0';    color = '0E8A16'; desc = 'Backlog priority P2: post-1.0, must not block the first stable release' }
    P3 = @{ name = 'p3-obsolete';    color = 'BFBFBF'; desc = 'Backlog priority P3: obsolete, duplicate or superseded; close or mark as replaced' }
}
$existing = gh label list --repo $Repo --limit 300 --json name --jq '.[].name'
foreach ($k in $labels.Keys) {
    $l = $labels[$k]
    if ($existing -notcontains $l.name) {
        if (-not $DryRun) { gh label create $l.name --repo $Repo --color $l.color --description $l.desc | Out-Null }
        Write-Output "label created: $($l.name)"
    }
}

$rows = Import-Csv $Csv
$final = foreach ($r in $rows) {
    $p = $r.priority
    $note = ''
    if ($r.title -like '`[EPIC`]*') { $p = ''; $note = 'epic container: no priority label' }
    elseif ($r.title -like '`[FEATURE`] Examples:*') { if ($p -ne 'P1') { $note = "maintainer: examples are P1 (was $p)" }; $p = 'P1' }
    [pscustomobject]@{ number = $r.number; priority = $p; confidence = $r.confidence; milestone = $r.milestone; title = $r.title; reason = $r.reason; note = $note }
}
$final | Export-Csv -Path $Out -NoTypeInformation -Encoding utf8
Write-Output ("final: " + (($final | Where-Object priority -ne '' | Group-Object priority | Sort-Object Name | ForEach-Object { "$($_.Name)=$($_.Count)" }) -join ' ') + " skipped(epic)=" + @($final | Where-Object priority -eq '').Count)

$done = 0; $failed = @()
foreach ($f in ($final | Where-Object priority -ne '')) {
    $label = $labels[$f.priority].name
    if ($DryRun) { continue }
    $r = gh issue edit $f.number --repo $Repo --add-label $label 2>&1
    if ($LASTEXITCODE -ne 0) { $failed += "#$($f.number): $r" } else { $done++ }
    if ($done % 50 -eq 0 -and $done -gt 0) { Write-Output "applied $done" }
}
Write-Output "applied=$done failed=$($failed.Count)"
$failed | ForEach-Object { Write-Output $_ }
