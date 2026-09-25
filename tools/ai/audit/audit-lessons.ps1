# tools/ai/audit/audit-lessons.ps1 (#1345)
#
# Collects every stage artifact's '## Lessons for the pipeline' section into stages/lessons.md, one bullet
# per line, each followed by an 'Applied: TODO' line the orchestrator must resolve (the commit/file that
# applied it, or 'not applied' with the reason) before audit-done.ps1 will close the audit.

param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$audit = Get-CurrentAudit $mainRoot
if ($null -eq $audit) { Write-Error 'audit-lessons: no open audit (artifacts/knowledge/current-audit.json not found). Run audit-next.ps1 first.'; exit 1 }

$wt = [string]$audit.worktree
$n = [string]$audit.issue
$stagesDir = Get-StagesDir $wt
$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')

$lines = [System.Collections.Generic.List[string]]::new()
$count = 0
foreach ($stage in $pipeline.stages) {
    $file = Join-Path $stagesDir $stage.artifact
    $section = Get-StageSection $file 'Lessons for the pipeline'
    if (-not $section) { continue }
    foreach ($bullet in ($section -split "`n" | Where-Object { $_ -match '^\s*-\s+\S' })) {
        $text = ($bullet -replace '^\s*-\s*', '').Trim()
        if ($text -in 'none', 'None') { continue }
        $lines.Add("- $text")
        $lines.Add('Applied: TODO')
        $count++
    }
}
if ($count -eq 0) { $lines.Add("No lessons recorded across the pipeline stages for #$n.") }

Set-Content -LiteralPath (Join-Path $stagesDir 'lessons.md') -Encoding utf8 -Value ($lines -join "`n")
"audit-lessons: wrote stages\lessons.md for #$n ($count lesson(s))"
