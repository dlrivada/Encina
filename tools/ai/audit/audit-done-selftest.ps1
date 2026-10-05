# tools/ai/audit/audit-done-selftest.ps1 (#1735)
#
# Self-test for the publishing half of audit-done.ps1. Builds a throwaway "main" repository with a local bare
# "origin", an open audit (worktree wia-99, branch audit/99, six committed stage files, lessons, a record) and
# runs the REAL audit-done.ps1 three times with git and gh replaced by stubs found first on PATH:
#   1. -NoPublish: asserts exactly the planned layout on the knowledge/audit-99 branch (and that the existing
#      record was replaced), the commands it would run, and that nothing was pushed or closed;
#   2. a publish whose push fails: asserts the audit worktree, the audit/99 branch and current-audit.json are kept
#      and no pull request was created;
#   3. a publish that succeeds (the retry of 2): asserts the pull request is created with "Refs #1345" BEFORE the
#      audit branch is deleted, and the audit is closed.
# The git stub forwards everything to the real git EXCEPT push (recorded, never executed); the gh stub only
# records and prints a fake URL. Nothing here can push or open a pull request.
#
# Exit code: 0 if every assertion passes, 1 otherwise.

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$realGit = (Get-Command git -CommandType Application | Select-Object -First 1).Source
$base = Join-Path ([IO.Path]::GetTempPath()) ("audit-done-selftest-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
$failures = [System.Collections.Generic.List[string]]::new()
$issue = 99

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

    # --- stubs: git.cmd / gh.cmd shims calling PowerShell stubs, found before the real tools on PATH ---------
    Write-Text (Join-Path $stubs 'git-stub.ps1') @'
$line = 'git ' + ($args -join ' ')
Add-Content -LiteralPath $env:AUDIT_STUB_LOG -Value $line
if ($args -contains 'push') {
    if ($env:AUDIT_STUB_FAIL_PUSH -eq '1') { [Console]::Error.WriteLine('stub: push rejected'); exit 1 }
    exit 0
}
& $env:AUDIT_STUB_REAL_GIT @args
exit $LASTEXITCODE
'@
    Write-Text (Join-Path $stubs 'gh-stub.ps1') @'
Add-Content -LiteralPath $env:AUDIT_STUB_LOG -Value ('gh ' + ($args -join ' '))
if ($args -contains 'list') { exit 0 }
'https://github.com/dlrivada/Encina/pull/9999'
exit 0
'@

    # --- fixture repository ---------------------------------------------------------------------------------
    Git init -q -b main $main | Out-Null
    Git -C $main config user.name 'Selftest' | Out-Null
    Git -C $main config user.email 'selftest@example.invalid' | Out-Null
    Git init -q --bare -b main $origin | Out-Null
    Copy-Item -Recurse (Join-Path $repo 'tools\ai\audit') (Join-Path $main 'tools\ai\audit') -Force
    Copy-Item (Join-Path $repo '.github\scripts\knowledge-records.cs') (Join-Path $main '.github\scripts\knowledge-records.cs') -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path (Join-Path $main '.github\scripts\knowledge-records.cs'))) {
        New-Item -ItemType Directory -Force (Join-Path $main '.github\scripts') | Out-Null
        Copy-Item (Join-Path $repo '.github\scripts\knowledge-records.cs') (Join-Path $main '.github\scripts\knowledge-records.cs') -Force
    }
    Write-Text (Join-Path $main '.gitignore') "artifacts/`n"
    $oldRecord = "---`nschema: 1`nnav_exclude: true`nissue: $issue`ntitle: `"[DEBT] Fixture (old fix-PR record)`"`nclosed: 2025-12-22`nstate_reason: completed`noutcome: delivered`ntype: debt`narea: core`nreview: verified`npackages:`nprs:`nlinked_prs:`nremediation:`nknowledge:`naudit:`n  checklist: 1`n  date: 2026-10-05`n  verdict: not-audited`n  record: `"not written yet`"`n---`n`nOLD`n"
    Write-Text (Join-Path $main "docs\knowledge\issues\$issue.md") $oldRecord
    Git -C $main add -A | Out-Null
    Git -C $main commit -q -m 'fixture main' | Out-Null
    Git -C $main remote add origin $origin | Out-Null
    Git -C $main push -q origin main | Out-Null

    $wt = Join-Path $main '.claude\worktrees\wia-99'
    Git -C $main worktree add -q -b "audit/$issue" $wt main | Out-Null
    $pipeline = Get-Content (Join-Path $main 'tools\ai\audit\pipeline.json') -Raw | ConvertFrom-Json
    $stagesDir = Join-Path $wt 'artifacts\knowledge\stages'
    foreach ($stage in $pipeline.stages) {
        $text = if ($stage.stage -eq 'verification') { "Verdict: PASS`n## Verified claims`nSecond pass.`n" } else { "## Findings`n- none`n" }
        Write-Text (Join-Path $stagesDir $stage.artifact) $text
        Git -C $wt add -f "artifacts/knowledge/stages/$($stage.artifact)" | Out-Null
        Git -C $wt commit -q -m "Stage: $($stage.stage)" | Out-Null
    }
    Write-Text (Join-Path $stagesDir 'lessons.md') "# Lessons`n- none`n"
    $record = "---`nschema: 2`nnav_exclude: true`nissue: $issue`ntitle: `"[DEBT] Fixture`"`nclosed: 2025-12-22`nstate_reason: completed`noutcome: delivered`ntype: debt`narea: core`nreview: verified`npackages: [Encina]`nprs: []`nlinked_prs: []`nremediation: [1234]`nknowledge:`n  - kind: decision`n    statement: `"A statement.`"`n    current: `"yes`"`n    sources:`n      - `"paraphrase: fixture (issue #1, 2025-12-22)`"`n    destinations:`n      - kind: adr`n        status: done`n        target: `"fixture`"`naudit:`n  checklist: 1`n  date: 2026-10-05`n  verdict: findings-tracked`n  record: `"docs/knowledge/audits/issue-$issue.md`"`n---`n`nNEW`n"
    Write-Text (Join-Path $wt "artifacts\knowledge\issues\$issue.md") $record
    Write-Text (Join-Path $main 'artifacts\knowledge\remediation\99-fixture-draft.md') "<!-- issue`ntitle: [DEBT] fixture`n-->`nbody`n"
    Write-Text (Join-Path $main 'artifacts\knowledge\remediation\opened.csv') "99-fixture-draft.md,https://github.com/dlrivada/Encina/issues/1234`n"
    $auditJson = @{ issue = $issue; worktree = $wt; branch = "audit/$issue"; startedUtc = '2026-10-05T00:00:00Z' } | ConvertTo-Json
    $currentAudit = Join-Path $main 'artifacts\knowledge\current-audit.json'
    Write-Text $currentAudit $auditJson

    $env:AUDIT_STUB_LOG = $log
    $env:AUDIT_STUB_REAL_GIT = $realGit
    $auditDone = Join-Path $main 'tools\ai\audit\audit-done.ps1'

    # git and gh are PowerShell functions in the child session, so they shadow the real executables for every
    # `& git ...` and `& gh ...` audit-done.ps1 and its helpers make (a .cmd shim would mangle `--pretty=format:%H`).
    function Invoke-AuditDone([string[]]$ExtraArgs) {
        if (Test-Path $log) { Remove-Item $log -Force }
        $command = "function git { & '$stubs\git-stub.ps1' @args }; function gh { & '$stubs\gh-stub.ps1' @args }; & '$auditDone' $($ExtraArgs -join ' '); exit `$LASTEXITCODE"
        $out = & pwsh -NoProfile -Command $command 2>&1 | ForEach-Object { "$_" }
        return @{ Exit = $LASTEXITCODE; Text = ($out -join "`n"); Log = $(if (Test-Path $log) { @(Get-Content $log) } else { @() }) }
    }

    # --- 1. -NoPublish ----------------------------------------------------------------------------------------
    $env:AUDIT_STUB_FAIL_PUSH = '0'
    $r1 = Invoke-AuditDone @('-NoPublish')
    Assert-That 'NoPublish exits 0' ($r1.Exit -eq 0) $r1.Text
    $diff = @(Git -C $main diff --name-status origin/main "knowledge/audit-$issue" | ForEach-Object { ($_ -replace '\s+', ' ').Trim() } | Sort-Object)
    $expected = @("M docs/knowledge/issues/$issue.md", "A docs/knowledge/audits/issue-$issue.md") +
        @($pipeline.stages | ForEach-Object { "A docs/knowledge/audits/$issue/stages/$($_.artifact)" }) +
        @("A docs/knowledge/audits/$issue/stages/lessons.md")
    $expected = @($expected | Sort-Object)
    Assert-That 'NoPublish branch holds exactly the planned layout' (($diff -join "`n") -ceq ($expected -join "`n")) "`nactual:`n$($diff -join "`n")`nexpected:`n$($expected -join "`n")"
    $published = (Git -C $main show "knowledge/audit-${issue}:docs/knowledge/issues/$issue.md") -join "`n"
    Assert-That 'the schema 2 record replaced the existing record' ($published -like '*NEW*' -and $published -notlike '*OLD*')
    $result = (Git -C $main show "knowledge/audit-${issue}:docs/knowledge/audits/issue-$issue.md") -join "`n"
    Assert-That 'the missing audit result was generated (verdict, passes, remediation issue)' ($result -like '*Verdict: PASS*' -and $result -like '*1 pass(es)*' -and $result -like '*issues/1234*') $result
    Assert-That 'the commit message names the audit' ((Git -C $main log -1 --format=%s "knowledge/audit-$issue") -ceq "docs(knowledge): SPEC-003 audit of #$issue")
    Assert-That 'NoPublish shows the push and gh pr create commands' (($r1.Text -like "*push -u origin knowledge/audit-$issue*") -and ($r1.Text -like '*pr create*--head knowledge/audit-99*--body Refs #1345*')) $r1.Text
    Assert-That 'NoPublish never ran push or gh' (-not ($r1.Log | Where-Object { $_ -match ' push ' -or $_ -match '^gh ' }))
    Assert-That 'NoPublish keeps the audit open' ((Test-Path $wt) -and (Test-Path $currentAudit) -and (Git -C $main branch --list "audit/$issue"))
    Assert-That 'NoPublish removed its temporary worktree' (-not ((Git -C $main worktree list) -match 'audit-publish-'))

    # --- 2. publish with a failing push -----------------------------------------------------------------------
    $env:AUDIT_STUB_FAIL_PUSH = '1'
    $r2 = Invoke-AuditDone @()
    Assert-That 'a failed push exits 1' ($r2.Exit -eq 1) $r2.Text
    Assert-That 'a failed push keeps the audit worktree, branch and current-audit.json' ((Test-Path $wt) -and (Test-Path $currentAudit) -and (Git -C $main branch --list "audit/$issue")) $r2.Text
    Assert-That 'a failed push opens no pull request and deletes no branch' (-not ($r2.Log | Where-Object { $_ -match '^gh pr create' -or $_ -match 'branch -D audit/' }))
    Assert-That 'a failed push changed nothing local' (-not (Test-Path (Join-Path $main 'artifacts\knowledge\progress.csv')))

    # --- 3. successful publish (the retry) -------------------------------------------------------------------
    $env:AUDIT_STUB_FAIL_PUSH = '0'
    $r3 = Invoke-AuditDone @()
    Assert-That 'the retry exits 0' ($r3.Exit -eq 0) $r3.Text
    $prIndex = [array]::FindIndex($r3.Log, [Predicate[string]] { param($l) $l -match '^gh pr create' })
    $delIndex = [array]::FindIndex($r3.Log, [Predicate[string]] { param($l) $l -match 'branch -D audit/99' })
    Assert-That 'gh pr create ran with Refs #1345 before the audit branch was deleted' ($prIndex -ge 0 -and $delIndex -gt $prIndex -and $r3.Log[$prIndex] -like '*--body Refs #1345*' -and $r3.Log[$prIndex] -notlike '*Fixes*') ($r3.Log -join "`n")
    Assert-That 'the audit is closed' ((-not (Test-Path $wt)) -and (-not (Test-Path $currentAudit)) -and (-not (Git -C $main branch --list "audit/$issue")))
    Assert-That 'progress.csv records the audit' ((Test-Path (Join-Path $main 'artifacts\knowledge\progress.csv')) -and ((Get-Content (Join-Path $main 'artifacts\knowledge\progress.csv')) -match "^$issue,done"))
}
finally {
    Set-Location $PSScriptRoot
    if (Test-Path -LiteralPath $base) {
        Get-ChildItem -LiteralPath $base -Recurse -Force -File -ErrorAction SilentlyContinue | ForEach-Object { $_.IsReadOnly = $false }
        Remove-Item -LiteralPath $base -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($failures.Count -gt 0) {
    Write-Host "audit-done-selftest: $($failures.Count) assertion(s) failed."
    exit 1
}
Write-Host 'audit-done-selftest: all assertions passed.'
exit 0
