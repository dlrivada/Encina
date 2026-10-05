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
    # --- 0. Update-PublishedLinks unit cases (#1817) ----------------------------------------------------------
    . (Join-Path $PSScriptRoot '_audit-lib.ps1')
    $linkRoot = Join-Path $base 'linkroot'
    Write-Text (Join-Path $linkRoot 'docs\knowledge\a\target one.md') "x`n"
    Write-Text (Join-Path $linkRoot 'docs\knowledge\img\pic.png') "x`n"
    $src = "[t](../a/target%20one.md#sec) ![i](../img/pic.png) [ref][r] [b](../a/missing.md)`n[r]: ../a/target%20one.md`n[m](mailto:a@b.c) [s](/site/root.md)`n"
    $u = Update-PublishedLinks $src 'docs/knowledge/issues/1.md' 'docs/knowledge/audits/1/d/x.md' $linkRoot
    Assert-That 'unit: broken link reported with dest file:line' ($u.Errors.Count -eq 1 -and $u.Errors[0] -ceq 'docs/knowledge/audits/1/d/x.md:1 ../a/missing.md') ($u.Errors -join ';')
    Assert-That 'unit: inline, image and reference-definition links are rewritten' ($u.Text.Contains('[t](../../../a/target%20one.md#sec)') -and $u.Text.Contains('![i](../../../img/pic.png)') -and $u.Text.Contains('[r]: ../../../a/target%20one.md')) $u.Text
    Assert-That 'unit: mailto and site-root links stay' ($u.Text -like '*(mailto:a@b.c)*' -and $u.Text -like '*(/site/root.md)*') $u.Text
    $same = Update-PublishedLinks "[t](../a/target%20one.md)`n" 'docs/knowledge/issues/1.md' 'docs/knowledge/issues/1.md' $linkRoot
    Assert-That 'unit: source equal to destination changes nothing' ($same.Errors.Count -eq 0 -and $same.Text -ceq "[t](../a/target%20one.md)`n") $same.Text
    $valid = Update-PublishedLinks "[t](../a/target%20one.md)`n" 'artifacts/knowledge/stages/x.md' 'docs/knowledge/audits/x.md' $linkRoot
    Assert-That 'unit: a link already valid from the destination is kept' ($valid.Errors.Count -eq 0 -and $valid.Text -ceq "[t](../a/target%20one.md)`n") ($valid.Errors -join ';')

    Write-Text (Join-Path $linkRoot 'docs\knowledge\a\foo(1).md') "x`n"
    $adv = Update-PublishedLinks "[![badge](https://img/x.svg)](../a/target%20one.md) [a [b] c](../a/foo(1).md) [![i](../img/pic.png)](../a/nope.md)`n" 'docs/knowledge/issues/1.md' 'docs/knowledge/audits/1/d/x.md' $linkRoot
    Assert-That 'unit: linked image, nested brackets and parentheses are rewritten, the broken outer link is reported' ($adv.Text.Contains('(https://img/x.svg)](../../../a/target%20one.md)') -and $adv.Text.Contains('[a [b] c](../../../a/foo%281%29.md)') -and $adv.Text.Contains('[![i](../../../img/pic.png)](../a/nope.md)') -and $adv.Errors.Count -eq 1 -and $adv.Errors[0].EndsWith(':1 ../a/nope.md')) ($adv.Errors -join ';') + $adv.Text
    $case = Update-PublishedLinks "[c](../A/target%20one.md)`n" 'docs/knowledge/issues/1.md' 'docs/knowledge/audits/1/d/x.md' $linkRoot
    Assert-That 'unit: a link that differs only in case is reported (the link checker is case-sensitive)' ($case.Errors.Count -eq 1) $case.Text
    $fenceInfo = Update-PublishedLinks "``````text`n[x](../a/nope.md)`n``````js`n[y](../a/nope2.md)`n``````n`[z](../a/nope3.md)`n" 'docs/knowledge/issues/1.md' 'docs/knowledge/audits/1/d/x.md' $linkRoot
    Assert-That 'unit: a fence line with an info string does not close the fence' ($fenceInfo.Errors.Count -eq 0) ($fenceInfo.Errors -join ';')

    # Decision 1: footnotes and prose with a colon are not reference definitions.
    $prose = "[^1]: See the thing.`n[HIGH]: this is bad`n[ok]: ../a/target%20one.md `"Title`"`n[nope]: ../a/missing.md`n"
    $p = Update-PublishedLinks $prose 'docs/knowledge/issues/1.md' 'docs/knowledge/audits/1/d/x.md' $linkRoot
    Assert-That 'unit: footnotes and prose with a colon are left alone and do not fail' ($p.Text.StartsWith("[^1]: See the thing.`n[HIGH]: this is bad`n") -and $p.Text.Contains('[ok]: ../../../a/target%20one.md "Title"') -and $p.Errors.Count -eq 1 -and $p.Errors[0].EndsWith(':4 ../a/missing.md')) ($p.Errors -join ';') + $p.Text

    # Decision 2: percent-encoding round-trips to an equivalent, resolvable link.
    Write-Text (Join-Path $linkRoot 'docs\knowledge\a\foo#bar.md') "x`n"
    Write-Text (Join-Path $linkRoot 'docs\knowledge\a\f%25.md') "x`n"
    Write-Text (Join-Path $linkRoot 'docs\knowledge\a\q%3F.md') "x`n"
    Write-Text (Join-Path $linkRoot 'docs\knowledge\a\un(bal.md') "x`n"
    Write-Text (Join-Path $linkRoot 'docs\knowledge\a\un)bal.md') "x`n"
    $enc = Update-PublishedLinks "[a](../a/foo%23bar.md) [b](../a/f%2525.md) [c](../a/q%253F.md) [d](../a/un%28bal.md) [e](../a/un%29bal.md)`n" 'docs/knowledge/issues/1.md' 'docs/knowledge/audits/1/d/x.md' $linkRoot
    Assert-That 'unit: encoded names round-trip to resolvable links' ($enc.Errors.Count -eq 0 -and $enc.Text -ceq "[a](../../../a/foo%23bar.md) [b](../../../a/f%2525.md) [c](../../../a/q%253F.md) [d](../../../a/un%28bal.md) [e](../../../a/un%29bal.md)`n") ($enc.Errors -join ';') + $enc.Text
    $keep = Update-PublishedLinks "[a](../a/foo%23bar.md)`n[b](../a/f%2525.md)`n" 'docs/knowledge/issues/1.md' 'docs/knowledge/issues/2.md' $linkRoot
    Assert-That 'unit: an unchanged relative path keeps the original text byte for byte' ($keep.Text -ceq "[a](../a/foo%23bar.md)`n[b](../a/f%2525.md)`n") $keep.Text

    # Decision 3: stage excerpts lose their relative links (plain text), other links stay.
    $plain = ConvertTo-PlainTextLinks "- [Widget](../../../src/W.cs) [web](https://e.com/x) [top](#top)`n``````text`n[f](../x.md)`n```````n[r]: ../y.md"
    Assert-That 'unit: relative links become plain text, others and fences stay' ($plain -ceq "- Widget (../../../src/W.cs) [web](https://e.com/x) [top](#top)`n``````text`n[f](../x.md)`n```````nr: ../y.md") $plain
    Write-Text (Join-Path $linkRoot 'docs\knowledge\issues\7.md') "---`nx: 1`n---`n"
    Write-Text (Join-Path $linkRoot 'docs\knowledge\audits\7\stages\archivist.md') "## Scope`n- [Widget](../../../../src/W.cs)`n"
    $scopeText = New-DeltaScopeText 7 $linkRoot 'set'
    Assert-That 'unit: New-DeltaScopeText strips relative links from stage excerpts' ($scopeText.Contains('- Widget (../../../../src/W.cs)') -and -not $scopeText.Contains('](../../../../src')) $scopeText

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
    Copy-Item -Recurse (Join-Path $repo '.github\ISSUE_TEMPLATE') (Join-Path $main '.github\ISSUE_TEMPLATE') -Force
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
        if ($n -eq 97) {
            # #1817: links written for docs/knowledge/issues/ (anchor, code fence and absolute URL stay as they are).
            $fence = '```'
            $tick = '`'
            $rec += "`n## Where the knowledge lives`n`n- [ADR index](../../architecture/adr/index.md), [top](#top), [site](https://example.com/a/../b.md)`n- Inline code $tick[x](../../nope.md)$tick is not a link.`n`n$fence`n[fenced](../../nope.md)`n$fence`n"
        }
        Write-Text (Join-Path $main "docs\knowledge\issues\$n.md") $rec
        Write-Text (Join-Path $main "docs\knowledge\audits\issue-$n.md") "# Audit of issue #$n`n`nPUBLISHED RESULT $n`n"
    }
    Write-Text (Join-Path $main 'docs\architecture\adr\index.md') "# ADR index`n"
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

    # --- 3b. delta remediation files never touch the original audit's (F1) ------------------------------------
    $remDir = Join-Path $knowledge 'remediation'
    $origDraft = Join-Path $remDir '99-docs-1-original-draft.md'
    $origManifest = Join-Path $remDir '_manifest-99.json'
    $origInput = Join-Path $remDir '_input-99-docs-1.md'
    Write-Text $origDraft "ORIGINAL DRAFT`n"
    Write-Text $origManifest "{ `"original`": true }`n"
    Write-Text $origInput "ORIGINAL INPUT`n"
    Write-Text (Join-Path $remDir 'opened.csv') "99-docs-1-original-draft.md,https://github.com/dlrivada/Encina/issues/1`n"
    $prep = Invoke-Script 'audit-draft-remediation.ps1' @('-Prepare', '-NoGh')
    Assert-That 'a delta -Prepare exits 0' ($prep.Exit -eq 0) $prep.Text
    Assert-That 'a delta -Prepare leaves the original draft, manifest and input untouched' ((Test-Path $origDraft) -and (Test-Path $origManifest) -and (Test-Path $origInput) -and ((Get-Content $origDraft -Raw) -like 'ORIGINAL DRAFT*') -and ((Get-Content $origManifest -Raw) -like '*original*')) $prep.Text
    Assert-That 'a delta -Prepare writes its own delta-named manifest' (Test-Path (Join-Path $remDir "_manifest-99-$folder.json")) $prep.Text
    Assert-That 'the committed remediation stage is still clean after Prepare' (-not (Git -C $wt status --porcelain -- artifacts/knowledge/stages/remediation.md))

    # --- 4. audit-done -NoPublish -----------------------------------------------------------------------------
    # F2/F3: an unopened delta draft (named with the delta prefix) stops the publication; the original audit's
    # drafts are not looked at, and opened.csv cannot confuse the two.
    $deltaDraftName = "99-$folder-docs-1-fixture.md"
    Write-Text (Join-Path $remDir $deltaDraftName) "DELTA DRAFT`n"
    $dGate = Invoke-Script 'audit-done.ps1' @('-NoPublish')
    Assert-That 'an unopened delta draft stops audit-done (the opened-remediation gate applies to a delta)' ($dGate.Exit -ne 0 -and $dGate.Text -like "*not opened yet*$deltaDraftName*") $dGate.Text
    Assert-That 'the gate names only the delta draft, never the original one' ($dGate.Text -notlike '*99-docs-1-original-draft.md*') $dGate.Text
    Assert-That 'the refused publication left the audit open' ((Test-Path $wt) -and (Test-Path $currentAudit))
    Add-Content (Join-Path $remDir 'opened.csv') "$deltaDraftName,https://github.com/dlrivada/Encina/issues/2"
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
    # #1817: a broken relative link in a published file fails the publish and names file:line and the link.
    Write-Text (Join-Path $stagesDir 'lessons.md') "No lessons.`nSee [gone](../../missing/page.md).`n"
    $dBad = Invoke-Script 'audit-done.ps1' @('-NoPublish')
    Assert-That 'a broken relative link fails the publish with file:line and the link' ($dBad.Exit -ne 0 -and $dBad.Text -like "*docs/knowledge/audits/97/$folder/lessons.md:2 ../../missing/page.md*") $dBad.Text
    Assert-That 'the failed publish pushed and opened nothing and kept the audit open' ((-not ($dBad.Log | Where-Object { $_ -match ' push ' -or $_ -match '^gh ' })) -and (Test-Path $wt) -and (Test-Path $currentAudit))
    Write-Text (Join-Path $stagesDir 'lessons.md') "No lessons recorded across the pipeline stages for #97.`n"
    $d3 = Invoke-Script 'audit-done.ps1' @('-NoPublish')
    Assert-That 'the delta of 97 publishes although the original audit has no stages folder (validator accepts a delta-only audit folder)' ($d3.Exit -eq 0) $d3.Text
    $scope97Pub = (Git -C $main show "knowledge/audit-97-${folder}:docs/knowledge/audits/97/$folder/delta-scope.md") -join "`n"
    Assert-That 'the record link was rewritten for the delta folder' $scope97Pub.Contains('[ADR index](../../../../architecture/adr/index.md)') $scope97Pub
    Assert-That 'anchor, absolute URL, inline code and fenced code links were left untouched' ($scope97Pub.Contains('[top](#top)') -and $scope97Pub.Contains('(https://example.com/a/../b.md)') -and $scope97Pub.Contains('`[x](../../nope.md)`') -and $scope97Pub.Contains('[fenced](../../nope.md)')) $scope97Pub
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
