# tools/ai/audit/audit-draft-remediation.ps1 (#1345)
#
# The remediation stage: reads the 'Findings' sections of the code, tests and docs stage artifacts and asks
# the free local model (tools/ai/local-ai-ask.cs) to draft one issue file per finding group into
# artifacts/knowledge/remediation/<n>-<slug>.md, using the house issue-template headers verbatim. Writes
# stages/remediation.md listing the drafts (or explaining why there were none). Requires stages/docs.md --
# the docs stage must have already run, even when it found nothing to review.

param()

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$audit = Get-CurrentAudit $mainRoot
if ($null -eq $audit) { Write-Error 'audit-draft-remediation: no open audit (artifacts/knowledge/current-audit.json not found). Run audit-next.ps1 first.'; exit 1 }

$wt = [string]$audit.worktree
$n = [string]$audit.issue
$stagesDir = Get-StagesDir $wt
$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')

function StageFile([string]$Name) {
    $def = $pipeline.stages | Where-Object { $_.stage -eq $Name }
    if ($null -eq $def) { throw "audit-draft-remediation: pipeline.json has no '$Name' stage." }
    return Join-Path $stagesDir $def.artifact
}

$docsFile = StageFile 'docs'
if (-not (Test-Path -LiteralPath $docsFile)) {
    Write-Error "audit-draft-remediation: stages\$(Split-Path -Leaf $docsFile) is missing; run the docs stage before remediation."
    exit 1
}

$codeFindings = Get-StageSection (StageFile 'code') 'Findings'
$testsFindings = Get-StageSection (StageFile 'tests') 'Findings'
$docsFindings = Get-StageSection $docsFile 'Findings'

$groups = [System.Collections.Generic.List[pscustomobject]]::new()
if ($codeFindings) { $groups.Add([pscustomobject]@{ Source = 'code'; Text = $codeFindings }) }
if ($testsFindings) { $groups.Add([pscustomobject]@{ Source = 'tests'; Text = $testsFindings }) }
if ($docsFindings) { $groups.Add([pscustomobject]@{ Source = 'docs'; Text = $docsFindings }) }

$remediationDir = Join-Path $mainRoot 'artifacts\knowledge\remediation'
New-Item -ItemType Directory -Force $remediationDir | Out-Null

$drafts = [System.Collections.Generic.List[string]]::new()

if ($groups.Count -eq 0) {
    "No findings from the code, tests or docs stages for #$n." | Out-Null
}
else {
    $brief = Join-Path $remediationDir "_brief-$n.md"
    Set-Content -LiteralPath $brief -Encoding utf8 -Value @"
You draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #$n of the
Encina .NET library. Use ONLY the input finding text; never invent facts. Output exactly this structure:

<!--
title: [DEBT] <specific title>
labels: technical-debt
milestone:
-->

## Type
- [x] Code smell
## Description
<description>
## Location
<file(s)/package(s)>
## Current Behavior
<current behavior>
## Expected Behavior
<expected behavior>
## Root Cause
<root cause>
## Proposed Fix
<proposed fix>
## Priority
- [x] Medium
## Effort Estimate
- [x] Small
## Related Issues
- #$n
"@

    $i = 0
    foreach ($group in $groups) {
        $i++
        $inputFile = Join-Path $remediationDir "_input-$n-$i.md"
        Set-Content -LiteralPath $inputFile -Encoding utf8 -Value $group.Text
        $slug = "$n-$($group.Source)-$i"
        $outFile = Join-Path $remediationDir "$slug.md"
        Push-Location $mainRoot
        try {
            $askOutput = & dotnet run (Join-Path $mainRoot 'tools\ai\local-ai-ask.cs') -- --task "remediation-$slug" --brief $brief --input $inputFile --out $outFile 2>&1
            $askExit = $LASTEXITCODE
        }
        finally {
            Pop-Location
        }
        if ($askExit -ne 0 -or -not (Test-Path -LiteralPath $outFile)) {
            Write-Error "audit-draft-remediation: local model failed for $slug (exit $askExit, output present: $(Test-Path -LiteralPath $outFile)): $askOutput"
            exit 1
        }
        $drafts.Add("- $slug.md (from the $($group.Source) stage findings)")
    }
}

$lines = [System.Collections.Generic.List[string]]::new()
if ($drafts.Count -eq 0) {
    $lines.Add("No findings from the code, tests or docs stages for #$n; no remediation drafts were written.")
}
else {
    $lines.Add("Remediation drafts for #$n`:")
    $lines.AddRange($drafts)
}
$lines.Add('')
$lines.Add('## Lessons for the pipeline')
$lines.Add('- none')

Set-Content -LiteralPath (StageFile 'remediation') -Encoding utf8 -Value ($lines -join "`n")
"audit-draft-remediation: wrote stages\remediation.md for #$n ($($drafts.Count) draft(s))"
