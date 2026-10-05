# tools/ai/audit/audit-delta-selftest.ps1 (#1763)
#
# Self-test for the delta mode of the SPEC-003 audit pipeline (audit-next.ps1 -Delta, the delta pipeline and the
# delta publication of audit-done.ps1). Builds a throwaway "main" repository with a local bare "origin" that
# holds a published audit of issue 99 (record, audit result, archivist and code stage files) and runs the REAL
# scripts with git and gh replaced by PowerShell functions that record the commands: the git stub forwards to
# the real git except `push` (recorded, never executed), the gh stub only records and prints a fake URL.
# Asserts:
#   1. audit-next -Delta rejects an unknown set, an issue that already has a delta and an issue that is not
#      audited, before creating anything;
#   2. audit-next -Delta rules-2026-10 picks the first audited issue without a delta (skipping the done one and
#      de-duplicating a redone audit), reuses the scope of the original audit (delta-scope.md carries the
#      record's packages and the archivist/code scope lists), creates wia-99 on audit/99, records mode delta and
#      the set in current-audit.json, and prints the first delta stage;
#   3. audit-stage -Next walks the delta pipeline order docs -> tests -> remediation -> verification (never an
#      archivist or code stage) as each stage is committed;
#   4. audit-done -NoPublish publishes exactly docs/knowledge/audits/99/delta-2026-10/{stage files, lessons.md,
#      delta-scope.md} on knowledge/audit-99-delta-2026-10 and leaves the original record and audit result alone;
#   5. a real publish opens the pull request with "Refs #1345", closes the audit, appends the delta progress file
#      and leaves progress.csv and the original archived stages untouched.
# Nothing here can push or open a pull request.
#
# Exit code: 0 if every assertion passes, 1 otherwise.

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$realGit = (Get-Command git -CommandType Application | Select-Object -First 1).Source
$base = Join-Path ([IO.Path]::GetTempPath()) ("audit-delta-selftest-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
$failures = [System.Collections.Generic.List[string]]::new()
$issue = 99
$set = 'rules-2026-10'
$folder = 'delta-2026-10'

function Assert-That([string]$Name, [bool]$Condition, [string]$Detail = '') {
    if ($Condition) { Write-Host "PASS  $Name" }
    else { Write-Host "FAIL  $Name $Detail"; $failures.Add($Name) }
}

function Git { & $realGit @args 2>&1 | ForEach-Object { "$_" } }

function Write-Text([string]$Path, [string]$Text) {
    New-Item -ItemType Directory -Force (Split-Path -Parent $Path) | Out-Null
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}

try {
    $main = Join-Path $base 'main'
    $origin = Join-Path $base 'origin.git'
    $stubs = Join-Path $base 'stubs'
    $log = Join-Path $base 'commands.log'
    New-Item -ItemType Directory -Force $main, $stubs | Out-Null

    Write-Text (Join-Path $stubs 'git-stub.ps1') @'
Add-Content -LiteralPath $env:AUDIT_STUB_LOG -Value ('git ' + ($args -join ' '))
if ($args -contains 'push') { exit 0 }
& $env:AUDIT_STUB_REAL_GIT @args
exit $LASTEXITCODE
'@
    Write-Text (Join-Path $stubs 'gh-stub.ps1') @'
Add-Content -LiteralPath $env:AUDIT_STUB_LOG -Value ('gh ' + ($args -join ' '))
if ($args -contains 'list') { exit 0 }
'https://github.com/dlrivada/Encina/pull/9999'
exit 0
'@

    # --- fixture repository: issue 99 was audited and published -----------------------------------------------
    Git init -q -b main $main | Out-Null
    Git -C $main config user.name 'Selftest' | Out-Null
    Git -C $main config user.email 'selftest@example.invalid' | Out-Null
    Git init -q --bare -b main $origin | Out-Null
    Copy-Item -Recurse (Join-Path $repo 'tools\ai\audit') (Join-Path $main 'tools\ai\audit') -Force
    New-Item -ItemType Directory -Force (Join-Path $main '.github\scripts') | Out-Null
    Copy-Item (Join-Path $repo '.github\scripts\knowledge-records.cs') (Join-Path $main '.github\scripts\knowledge-records.cs') -Force
    Write-Text (Join-Path $main '.gitignore') "artifacts/`n"
    $record = "---`nschema: 2`nnav_exclude: true`nissue: $issue`ntitle: `"[DEBT] Fixture`"`nclosed: 2025-12-22`nstate_reason: completed`noutcome: delivered`ntype: debt`narea: core`nreview: verified`npackages: [Encina.Fixture]`nprs: []`nlinked_prs: []`nremediation: []`nknowledge:`n  - kind: decision`n    statement: `"A statement.`"`n    current: `"yes`"`n    sources:`n      - `"paraphrase: fixture (issue #1, 2025-12-22)`"`n    destinations:`n      - kind: adr`n        status: done`n        target: `"fixture`"`naudit:`n  checklist: 1`n  date: 2026-09-20`n  verdict: findings-tracked`n  record: `"docs/knowledge/audits/issue-$issue.md`"`n---`n`nORIGINAL RECORD`n"
    Write-Text (Join-Path $main "docs\knowledge\issues\$issue.md") $record
    Write-Text (Join-Path $main "docs\knowledge\audits\issue-$issue.md") "# Audit of issue #$issue`n`nORIGINAL RESULT`n"
    Write-Text (Join-Path $main "docs\knowledge\audits\$issue\stages\archivist.md") "## Scope`n- src/Encina.Fixture/Widget.cs`n- tests/Encina.UnitTests/Fixture/WidgetTests.cs`n## Findings`n- none`n"
    Write-Text (Join-Path $main "docs\knowledge\audits\$issue\stages\code.md") "## Findings`n- none`n"
    # 97: an early audit with a published record and result but NO published stage files (its scope comes from the
    # record); 96: audited on 2026-10-06, after the set's cut-off (never in the queue); 95: no published record.
    foreach ($spec in @(@(97, '2026-09-20'), @(96, '2026-10-06'))) {
        $n = $spec[0]
        $rec = $record.Replace("issue: $issue", "issue: $n").Replace('date: 2026-09-20', "date: $($spec[1])").Replace("issue-$issue.md", "issue-$n.md").Replace('Encina.Fixture', "Encina.Early$n").Replace('ORIGINAL RECORD', "RECORD $n")
        Write-Text (Join-Path $main "docs\knowledge\issues\$n.md") $rec
        Write-Text (Join-Path $main "docs\knowledge\audits\issue-$n.md") "# Audit of issue #$n`n`nPUBLISHED RESULT $n`n"
    }
    Git -C $main add -A | Out-Null
    Git -C $main commit -q -m 'fixture main' | Out-Null
    Git -C $main remote add origin $origin | Out-Null
    Git -C $main push -q origin main | Out-Null

    # Audit history: 98 audited and already re-checked, 99 audited twice (a redo), 97 not in the list at all.
    $knowledge = Join-Path $main 'artifacts\knowledge'
    Write-Text (Join-Path $knowledge 'progress.csv') "issue,status,findings_blocker,findings_major,findings_minor,remediation_opened,notes`n98,done,,,,0,`"`"`n$issue,done,,,,1,`"first`"`n$issue,done,,,,2,`"redo`"`n97,done,,,,0,`"`"`n96,done,,,,0,`"`"`n95,done,,,,0,`"`"`n"
    $deltaProgress = Join-Path $knowledge "delta-progress-$set.csv"
    Write-Text $deltaProgress "98,done,0,`n"
    $progressBefore = Get-Content (Join-Path $knowledge 'progress.csv') -Raw
    $currentAudit = Join-Path $knowledge 'current-audit.json'
    $wt = Join-Path $main '.claude\worktrees\wia-99'

    $env:AUDIT_STUB_LOG = $log
    $env:AUDIT_STUB_REAL_GIT = $realGit
    function Invoke-Script([string]$Script, [string[]]$ScriptArgs) {
        if (Test-Path $log) { Remove-Item $log -Force }
        $command = "function git { & '$stubs\git-stub.ps1' @args }; function gh { & '$stubs\gh-stub.ps1' @args }; & '$main\tools\ai\audit\$Script' $($ScriptArgs -join ' '); exit `$LASTEXITCODE"
        $out = & pwsh -NoProfile -Command $command 2>&1 | ForEach-Object { "$_" }
        return @{ Exit = $LASTEXITCODE; Text = ($out -join "`n"); Log = $(if (Test-Path $log) { @(Get-Content $log) } else { @() }) }
    }

    # --- 1. rejections create nothing -------------------------------------------------------------------------
    $r = Invoke-Script 'audit-next.ps1' @('-Delta', 'rules-1999-01')
    Assert-That 'an unknown delta set is refused' ($r.Exit -ne 0 -and $r.Text -like '*unknown delta set*') $r.Text
    $r = Invoke-Script 'audit-next.ps1' @('-Delta', $set, '-Issue', '98')
    Assert-That 'an issue that already has a delta is refused' ($r.Exit -ne 0 -and $r.Text -like '*not an audited issue without*') $r.Text
    $r = Invoke-Script 'audit-next.ps1' @('-Delta', $set, '-Issue', '5')
    Assert-That 'an issue that was never audited is refused' ($r.Exit -ne 0) $r.Text
    Assert-That 'the refusals created no audit and no worktree' ((-not (Test-Path $currentAudit)) -and (-not (Test-Path $wt)))

    # --- 2. audit-next -Delta ---------------------------------------------------------------------------------
    $r = Invoke-Script 'audit-next.ps1' @('-Delta', $set)
    Assert-That 'audit-next -Delta exits 0' ($r.Exit -eq 0) $r.Text
    Assert-That 'it picked 99, the first audited issue without a delta' ($r.Text -like "*Delta audit $set of #99*") $r.Text
    $audit = Get-Content $currentAudit -Raw | ConvertFrom-Json
    Assert-That 'current-audit.json records mode delta and the set' ($audit.mode -eq 'delta' -and $audit.set -eq $set -and $audit.issue -eq $issue -and $audit.branch -eq 'audit/99') ($audit | ConvertTo-Json -Compress)
    Assert-That 'the audit worktree is on audit/99' ((Test-Path $wt) -and (Git -C $wt rev-parse --abbrev-ref HEAD) -ceq 'audit/99')
    $scopeFile = Join-Path $wt 'artifacts\knowledge\delta-scope.md'
    $scope = if (Test-Path $scopeFile) { Get-Content $scopeFile -Raw } else { '' }
    Assert-That 'the scope of the original audit was reused (record packages, archivist scope list)' ($scope -like '*Encina.Fixture*' -and $scope -like '*src/Encina.Fixture/Widget.cs*' -and $scope -like '*WidgetTests.cs*') $scope
    Assert-That 'no pre-draft was generated and no classify-scope ran' (-not (Test-Path (Join-Path $knowledge 'predraft')))
    Assert-That 'it prints the first delta stage with the delta marker' ($r.Text -like '*Next stage: docs (spawn docs-reviewer*delta: rules-2026-10, check only rule (a)*') $r.Text
    $r2 = Invoke-Script 'audit-next.ps1' @('-Delta', $set)
    Assert-That 'a second audit-next is refused while the delta audit is open' ($r2.Exit -ne 0 -and $r2.Text -like '*already open*') $r2.Text

    # --- 3. the delta pipeline order ---------------------------------------------------------------------------
    $stagesDir = Join-Path $wt 'artifacts\knowledge\stages'
    $order = @()
    $expectedOrder = @('docs', 'tests', 'remediation', 'verification')
    foreach ($stage in $expectedOrder) {
        $next = (Invoke-Script 'audit-stage.ps1' @('-Next')).Text
        $order += $(if ($next -match 'Next stage: (\w+)') { $Matches[1] } else { $next })
        $def = (Get-Content (Join-Path $wt 'tools\ai\audit\pipeline-delta.json') -Raw | ConvertFrom-Json).stages | Where-Object { $_.stage -eq $stage }
        $text = if ($stage -eq 'verification') { "Verdict: PASS`n## Verified claims`nFixture.`n" } else { "## Findings`n- none`n## Lessons for the pipeline`n- none`n" }
        Write-Text (Join-Path $stagesDir $def.artifact) $text
        Git -C $wt add -f "artifacts/knowledge/stages/$($def.artifact)" | Out-Null
        Git -C $wt commit -q -m "audit #99: $stage stage" -m "Stage: $stage" | Out-Null
    }
    Assert-That 'audit-stage -Next walks docs, tests, remediation, verification' (($order -join ',') -ceq ($expectedOrder -join ',')) ($order -join ',')
    Assert-That 'audit-stage -Next reports every stage complete at the end' (((Invoke-Script 'audit-stage.ps1' @('-Next')).Text) -like '*All stages complete*')
    Write-Text (Join-Path $stagesDir 'lessons.md') "No lessons recorded across the pipeline stages for #99.`n"

    # --- 4. audit-done -NoPublish -----------------------------------------------------------------------------
    $d1 = Invoke-Script 'audit-done.ps1' @('-NoPublish')
    Assert-That 'audit-done -NoPublish exits 0' ($d1.Exit -eq 0) $d1.Text
    $branch = "knowledge/audit-99-$folder"
    $diff = @(Git -C $main diff --name-status origin/main $branch | ForEach-Object { ($_ -replace '\s+', ' ').Trim() } | Sort-Object)
    $expected = @("A docs/knowledge/audits/99/$folder/docs.md", "A docs/knowledge/audits/99/$folder/tests.md", "A docs/knowledge/audits/99/$folder/remediation.md", "A docs/knowledge/audits/99/$folder/verification.md", "A docs/knowledge/audits/99/$folder/lessons.md", "A docs/knowledge/audits/99/$folder/delta-scope.md" | Sort-Object)
    Assert-That 'the publish layout is exactly docs/knowledge/audits/99/delta-2026-10/ (original record and result untouched)' (($diff -join "`n") -ceq ($expected -join "`n")) "`nactual:`n$($diff -join "`n")`nexpected:`n$($expected -join "`n")"
    Assert-That 'the publication commit names the delta' ((Git -C $main log -1 --format=%s $branch) -ceq "docs(knowledge): SPEC-003 delta $set audit of #99")
    Assert-That 'NoPublish shows the push and gh pr create commands and runs neither' (($d1.Text -like "*push -u origin $branch*") -and ($d1.Text -like "*pr create*--head $branch*--body Refs #1345*") -and -not ($d1.Log | Where-Object { $_ -match ' push ' -or $_ -match '^gh ' }))
    Assert-That 'NoPublish keeps the delta audit open' ((Test-Path $wt) -and (Test-Path $currentAudit))

    # --- 5. real publish ---------------------------------------------------------------------------------------
    $d2 = Invoke-Script 'audit-done.ps1' @()
    Assert-That 'the publish exits 0' ($d2.Exit -eq 0) $d2.Text
    $prLine = @($d2.Log | Where-Object { $_ -match '^gh pr create' })
    Assert-That 'gh pr create ran with Refs #1345 for the delta branch' ($prLine.Count -eq 1 -and $prLine[0] -like "*--head $branch*" -and $prLine[0] -like '*--body Refs #1345*' -and $prLine[0] -notlike '*Fixes*') ($d2.Log -join "`n")
    Assert-That 'the delta audit is closed (worktree, branch, current-audit.json)' ((-not (Test-Path $wt)) -and (-not (Test-Path $currentAudit)) -and (-not (Git -C $main branch --list 'audit/99')))
    Assert-That 'the delta progress file records 99 and progress.csv is untouched' (((Get-Content $deltaProgress) -match '^99,done') -and ((Get-Content (Join-Path $knowledge 'progress.csv') -Raw) -ceq $progressBefore))
    Assert-That 'the delta stages were archived next to, not over, the originals' ((Test-Path (Join-Path $knowledge "stages\99-$folder\docs.md")) -and -not (Test-Path (Join-Path $knowledge 'stages\99')))
    Assert-That 'the cut-off and the missing record are reported (96 after the cut-off, 95 skipped with a warning)' (($r.Text -like '*not in the*2026-10-05*96*') -and ($r.Text -like '*skipped*95*')) $r.Text

    # --- 6. an early audit without published stage files (97) ------------------------------------------------
    $r4 = Invoke-Script 'audit-next.ps1' @('-Delta', $set)
    Assert-That 'the next delta is 97, the early audit without stage files (95 and 96 never enter the queue)' ($r4.Exit -eq 0 -and $r4.Text -like "*Delta audit $set of #97*") $r4.Text
    $wt = Join-Path $main '.claude\worktrees\wia-97'
    $stagesDir = Join-Path $wt 'artifacts\knowledge\stages'
    $scopeFile = Join-Path $wt 'artifacts\knowledge\delta-scope.md'
    $scope97 = if (Test-Path $scopeFile) { Get-Content $scopeFile -Raw } else { '' }
    Assert-That 'its scope came from the published record and result, and says so' ($scope97 -like '*Scope source:*only: the original audit has no published stage files*' -and $scope97 -like '*Encina.Early97*' -and $scope97 -like '*PUBLISHED RESULT 97*') $scope97
    foreach ($stage in $expectedOrder) {
        $def = (Get-Content (Join-Path $wt 'tools\ai\audit\pipeline-delta.json') -Raw | ConvertFrom-Json).stages | Where-Object { $_.stage -eq $stage }
        $text = if ($stage -eq 'verification') { "Verdict: PASS`n## Verified claims`nFixture.`n" } else { "## Findings`n- none`n## Lessons for the pipeline`n- none`n" }
        Write-Text (Join-Path $stagesDir $def.artifact) $text
        Git -C $wt add -f "artifacts/knowledge/stages/$($def.artifact)" | Out-Null
        Git -C $wt commit -q -m "audit #97: $stage stage" -m "Stage: $stage" | Out-Null
    }
    Write-Text (Join-Path $stagesDir 'lessons.md') "No lessons recorded across the pipeline stages for #97.`n"
    $d3 = Invoke-Script 'audit-done.ps1' @('-NoPublish')
    Assert-That 'the delta of 97 publishes although the original audit has no stages folder (validator accepts a delta-only audit folder)' ($d3.Exit -eq 0) $d3.Text
    $diff97 = @(Git -C $main diff --name-only origin/main "knowledge/audit-97-$folder")
    $d3b = Invoke-Script 'audit-done.ps1' @()
    Assert-That 'the real publish of 97 then closes the audit' ($d3b.Exit -eq 0 -and -not (Test-Path $currentAudit)) $d3b.Text
    Assert-That 'its publish layout is audits/97/delta-2026-10/ only' ((@($diff97 | Where-Object { $_ -notlike "docs/knowledge/audits/97/$folder/*" }).Count -eq 0) -and $diff97.Count -eq 6) ($diff97 -join "`n")

    $r3 = Invoke-Script 'audit-next.ps1' @('-Delta', $set)
    Assert-That 'with every eligible audited issue done the next audit-next -Delta says so' ($r3.Exit -ne 0 -and $r3.Text -like '*no audited issue left*') $r3.Text
}
finally {
    Set-Location $PSScriptRoot
    if (Test-Path -LiteralPath $base) {
        Get-ChildItem -LiteralPath $base -Recurse -Force -File -ErrorAction SilentlyContinue | ForEach-Object { $_.IsReadOnly = $false }
        Remove-Item -LiteralPath $base -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($failures.Count -gt 0) {
    Write-Host "audit-delta-selftest: $($failures.Count) assertion(s) failed."
    exit 1
}
Write-Host 'audit-delta-selftest: all assertions passed.'
exit 0
