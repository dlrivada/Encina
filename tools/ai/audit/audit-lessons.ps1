# tools/ai/audit/audit-lessons.ps1 (#1345)
#
# Collects every stage artifact's '## Lessons for the pipeline' section into stages/lessons.md, one bullet
# per line, each followed by an 'Applied: TODO' line the orchestrator must resolve (the commit/file that
# applied it, or 'not applied' with the reason) before audit-done.ps1 will close the audit.

#
# #2234: -Issue <n> names the audit when several are open (optional from its wia-<n> worktree or with one open
# audit; Resolve-OpenAudit, _audit-lib.ps1). Each audit has its own lessons.md in its own worktree.

param([int]$Issue)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
try { $audit = Resolve-OpenAudit $mainRoot $Issue $PSScriptRoot }
catch { Write-Error "audit-lessons: $($_.Exception.Message)"; exit 1 }

$wt = [string]$audit.worktree
$n = [string]$audit.issue
$stagesDir = Get-StagesDir $wt
$pipeline = Get-AuditPipeline $audit

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
