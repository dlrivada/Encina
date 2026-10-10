# Self-test of reconcile-board.ps1 (#1732): fixtures for the board export, a stubbed gh and git, and the audit
# files, proving the reconciliation rules, version pinning, idempotence and the 50-write split. Exits 1 on any
# failure. Usage: pwsh -NoProfile -File tools/ai/board/reconcile-board-selftest.ps1

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'reconcile-board.ps1')

$script:failed = 0
$script:total = 0
function Assert-That([bool]$Condition, [string]$Label) {
    $script:total++
    if (-not $Condition) { $script:failed++ }
    "{0} {1}" -f $(if ($Condition) { 'PASS' } else { 'FAIL' }), $Label
}

$work = Join-Path ([IO.Path]::GetTempPath()) ("board-selftest-" + [guid]::NewGuid().ToString('N'))
$main = Join-Path $work 'repo'
New-Item -ItemType Directory -Force -Path (Join-Path $main 'artifacts' 'knowledge') | Out-Null
$wtRoot = Join-Path $main '.claude' 'worktrees'

# ---- stubs: functions shadow the gh / git executables for the functions called below
# Global so the stubs also work when the script under test runs as a child script (scope modifiers are dynamic).
$global:openPrs = @(
    @{ number = 201; title = 'Open PR with card'; headRefName = 'fix/a-101'; body = 'Fixes #101'; isDraft = $false; createdAt = '2026-10-04T08:00:00Z'; author = @{ login = 'dlrivada'; is_bot = $false }; closingIssuesReferences = @() },
    @{ number = 210; title = 'New PR without card'; headRefName = 'fix/new-300'; body = 'Fixes #300'; isDraft = $false; createdAt = '2026-10-05T07:00:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @(@{ number = 300 }) },
    @{ number = 211; title = 'Bump x'; headRefName = 'dependabot/x'; body = 'Fixes #301'; isDraft = $false; createdAt = '2026-10-05T07:00:00Z'; author = @{ login = 'app/dependabot'; is_bot = $true }; closingIssuesReferences = @() },
    @{ number = 212; title = 'No issue'; headRefName = 'docs/y'; body = 'Just docs'; isDraft = $true; createdAt = '2026-10-05T07:00:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 213; title = 'Draft in progress'; headRefName = 'fix/d-107'; body = 'Fixes #107'; isDraft = $true; createdAt = '2026-10-05T07:00:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 234; title = 'Implementation after plan'; headRefName = 'fix/i-412'; body = 'Fixes #412'; isDraft = $false; createdAt = '2026-10-05T07:30:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 236; title = 'PR on a blocked card'; headRefName = 'fix/bl-114'; body = 'Fixes #114'; isDraft = $false; createdAt = '2026-10-05T07:30:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 237; title = 'Draft on a queued card'; headRefName = 'fix/q-115'; body = 'Fixes #115'; isDraft = $true; createdAt = '2026-10-05T07:30:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 221; title = 'Flow ready to review'; headRefName = 'fix/b-401'; body = 'Closes #401'; isDraft = $false; createdAt = '2026-10-05T06:00:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() }
)
$global:mergedPrs = @(
    @{ number = 200; title = 'Merged'; headRefName = 'fix/m-100'; body = 'Fixes #100'; mergedAt = '2026-10-05T09:00:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 230; title = 'Plan only'; headRefName = 'docs/plan-410'; body = 'Refs #410'; mergedAt = '2026-09-20T09:00:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 233; title = 'Plan card PR'; headRefName = 'docs/plan-411'; body = 'Refs #411'; mergedAt = '2026-09-20T09:00:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 235; title = 'Implementation merged'; headRefName = 'fix/m-413'; body = 'Fixes #413'; mergedAt = '2026-10-05T10:45:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 231; title = 'One of two'; headRefName = 'fix/one-108'; body = 'Fixes #108'; mergedAt = '2026-09-20T09:00:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() },
    @{ number = 220; title = 'Merged flow'; headRefName = 'fix/f-400'; body = 'Fixes #400'; mergedAt = '2026-10-05T10:30:00Z'; author = @{ login = 'dlrivada' }; closingIssuesReferences = @() }
)
function gh {
    $a = $args -join ' '
    $global:LASTEXITCODE = 0
    if ($a -match '^pr list .*--state open') { return ConvertTo-Json -InputObject $global:openPrs -Depth 8 }
    if ($a -match '^pr list .*--state merged') { return ConvertTo-Json -InputObject $global:mergedPrs -Depth 8 }
    if ($a -match '^issue list .*--state closed') { return '[{"number":402,"closedAt":"2026-10-04T12:00:00Z","stateReason":"COMPLETED"}]' }
    if ($a -match '^issue view 404 .*--json state') { return '{"state":"CLOSED","stateReason":"NOT_PLANNED","closedAt":"2026-06-01T00:00:00Z"}' }
    if ($a -match '^issue view \d+ .*--json state') { return '{"state":"OPEN","stateReason":null,"closedAt":null}' }
    if ($a -match '^pr view 232 ') { return '{"number":232,"title":"Abandoned","headRefName":"x","body":"Fixes #405","state":"CLOSED","mergedAt":null,"createdAt":"2026-10-01T00:00:00Z","author":{"login":"dlrivada"},"closingIssuesReferences":[]}' }
    if ($a -match '^pr view 204 ') { return '{"number":204,"title":"Older","headRefName":"x","body":"Fixes #104","state":"MERGED","mergedAt":"2026-09-01T00:00:00Z","createdAt":"2026-08-30T00:00:00Z","author":{"login":"dlrivada"},"closingIssuesReferences":[]}' }
    if ($a -match '^issue view (\d+) ') { return "{`"title`":`"Issue $($Matches[1]) title`"}" }
    $global:LASTEXITCODE = 1
    return "unexpected gh call: $a"
}
function git {
    $a = $args -join ' '
    $global:LASTEXITCODE = 0
    if ($a -match 'worktree list --porcelain') {
        return @("worktree $main", 'HEAD aaa', 'branch refs/heads/main', '',
            "worktree $wtRoot\w102", 'HEAD bbb', 'branch refs/heads/fix/w-102', '',
            "worktree $wtRoot\w105", 'HEAD ccc', 'branch refs/heads/fix/w-105', '')
    }
    if ($a -match 'w102 rev-list --count') { return '3' }
    if ($a -match 'rev-list --count') { return '0' }
    $global:LASTEXITCODE = 1
    return "unexpected git call: $a"
}

# ---- fixtures
$export = Join-Path $work 'export'
function Save-Doc([string]$Coll, [string]$Id, $Data) {
    $d = Join-Path $export $Coll
    New-Item -ItemType Directory -Force -Path $d | Out-Null
    [IO.File]::WriteAllText((Join-Path $d "$Id.json"), (ConvertTo-Json $Data -Depth 8), [Text.UTF8Encoding]::new($false))
}
function New-Card($Issues, $Status, $Pr, $Worktree, $Kind = 'worker') {
    @{ agent = 'issue-worker'; endedUtc = $null; issues = $Issues; kind = $Kind; note = 'seed'; pr = $Pr; startedUtc = '2026-10-03T10:00:00Z'; status = $Status; title = "Card $($Issues -join ',')"; worktree = $Worktree }
}
Save-Doc 'work' '100' (New-Card @(100) 'running' 200 'w100')
Save-Doc 'work' '101' (New-Card @(101) 'pr-open' 201 'w101')
Save-Doc 'work' '102' (New-Card @(102) 'queued' $null 'w102')
Save-Doc 'work' '103' (New-Card @(103) 'running' $null 'w103')
Save-Doc 'work' '104' (New-Card @(104) 'pr-open' 204 $null)
Save-Doc 'work' '105-done' (New-Card @(105) 'done' $null 'w105')
Save-Doc 'work' '106-orch' (New-Card @(106) 'running' $null '' 'orchestrator')
$fresh = New-Card @(110) 'running' $null 'w110'
$fresh.startedUtc = '2026-10-05T10:30:00Z'
Save-Doc 'work' '110' $fresh
Save-Doc 'work' '107'(New-Card @(107) 'running' 213 'w107')
Save-Doc 'work' '108' (New-Card @(108, 109) 'pr-open' 231 'w108')
Save-Doc 'flow' '410' @{ issue = 410; lane = 'urgent'; note = 'plan merged'; pr = 230; stage = 'implementation'; status = 'ready'; title = 'Flow plan PR'; updatedUtc = '2026-10-03T00:00:00Z' }
$planCard = New-Card @(411) 'pr-open' 233 'w411'
$planCard.kind = 'plan'
Save-Doc 'work' '411' $planCard
Save-Doc 'work' '412' (New-Card @(412) 'pr-open' 234 'w412')
Save-Doc 'work' '114' (New-Card @(114) 'blocked' 236 'w114')
Save-Doc 'work' '115' (New-Card @(115) 'queued' 237 'w115')
Save-Doc 'flow' '412' @{ issue = 412; lane = 'urgent'; note = 'plan merged'; pr = 230; stage = 'implementation'; status = 'ready'; title = 'Flow after plan, impl open'; updatedUtc = '2026-10-03T00:00:00Z' }
Save-Doc 'flow' '413' @{ issue = 413; lane = 'urgent'; note = 'plan merged'; pr = 230; stage = 'implementation'; status = 'ready'; title = 'Flow after plan, impl merged'; updatedUtc = '2026-10-03T00:00:00Z' }
Save-Doc 'flow' '404' @{ issue = 404; lane = 'pilot'; note = 'n'; pr = $null; stage = 'intake'; status = 'ready'; title = 'Flow old not-planned'; updatedUtc = '2026-05-01T00:00:00Z' }
Save-Doc 'flow' '405' @{ issue = 405; lane = 'pilot'; note = 'n'; pr = 232; stage = 'review'; status = 'in-progress'; title = 'Flow PR abandoned'; updatedUtc = '2026-10-03T00:00:00Z' }
Save-Doc 'flow' '400' @{ issue = 400; lane = 'urgent'; note = 'n'; pr = $null; stage = 'implementation'; status = 'in-progress'; title = 'Flow merged'; updatedUtc = '2026-10-03T00:00:00Z' }
Save-Doc 'flow' '401' @{ issue = 401; lane = 'urgent'; note = 'n'; pr = $null; stage = 'intake'; status = 'ready'; title = 'Flow open PR'; updatedUtc = '2026-10-03T00:00:00Z' }
Save-Doc 'flow' '402' @{ issue = 402; lane = 'pilot'; note = 'n'; pr = $null; stage = 'implementation'; status = 'in-progress'; title = 'Flow closed'; updatedUtc = '2026-10-03T00:00:00Z' }
Save-Doc 'flow' '403' @{ issue = 403; lane = 'pilot'; note = 'n'; pr = $null; stage = 'intake'; status = 'ready'; title = 'Flow untouched'; updatedUtc = '2026-10-03T00:00:00Z' }
Save-Doc 'audits' '5' @{ issue = 5; note = ''; opened = @(); outcome = 'delivered'; pipeline = 'legacy'; status = 'in-progress'; title = 'Audit five' }
Save-Doc 'audits' '6' @{ issue = 6; note = ''; opened = @(); outcome = 'delivered'; pipeline = 'legacy'; status = 'closed'; title = 'Audit six' }
Save-Doc 'meta' 'board' @{ current = 6; pipeline = 'v2'; status = 'hand written'; updatedUtc = '2026-10-01T00:00:00Z' }

[IO.File]::WriteAllText((Join-Path $main 'artifacts' 'knowledge' 'progress.csv'), "issue,status,findings_blocker,findings_major,findings_minor,remediation_opened,notes`n5,done,,,,1,`"five`"`n6,done,,,,0,`"`"`n7,done,,,,2,`"seven`"`n", [Text.UTF8Encoding]::new($false))
# #2234: two open audits, one still in the pre-#2234 current-audit.json, one in its own open-audits/<n>.json.
[IO.File]::WriteAllText((Join-Path $main 'artifacts' 'knowledge' 'current-audit.json'), '{"issue":8,"worktree":"wia-8","startedUtc":"2026-10-05T08:00:00Z"}', [Text.UTF8Encoding]::new($false))
New-Item -ItemType Directory -Force (Join-Path $main 'artifacts' 'knowledge' 'open-audits') | Out-Null
[IO.File]::WriteAllText((Join-Path $main 'artifacts' 'knowledge' 'open-audits' '9.json'), '{"issue":9,"worktree":"wia-9","startedUtc":"2026-10-05T09:30:00Z","scope":[],"concurrent":[8]}', [Text.UTF8Encoding]::new($false))

$now = [datetime]::Parse('2026-10-05T11:00:00Z').ToUniversalTime()
$docs = Read-BoardExport $export
$facts = Get-BoardFacts $docs 'o/r' $main $now 14
$changes = Get-BoardChanges $docs $facts $now
function Find-Change([string]$Key) { $changes | Where-Object { "$($_.Collection)/$($_.Id)" -eq $Key } | Select-Object -First 1 }

try {
    # ---- work cards
    $c = Find-Change 'work/100'
    Assert-That ($c.Data.status -eq 'merged' -and $c.Data.endedUtc -eq '2026-10-05T09:00:00Z') 'merged PR: card becomes merged with the merge time'
    Assert-That ($null -eq (Find-Change 'work/101')) 'open PR on a pr-open card: no write'
    $c = Find-Change 'work/102'
    Assert-That ($c.Data.status -eq 'running' -and $null -eq $c.Data.endedUtc) 'worktree ahead of main and no PR: card running'
    $c = Find-Change 'work/103'
    Assert-That ($c.Data.status -eq 'stopped' -and $c.Data.note -match 'no worktree and no PR' -and $c.Data.endedUtc -eq '2026-10-05T11:00:00Z') 'running card with no worktree and no PR: stopped with a note'
    $c = Find-Change 'work/104'
    Assert-That ($c.Data.status -eq 'merged' -and $c.Data.endedUtc -eq '2026-09-01T00:00:00Z') 'PR outside the 14-day window is resolved with gh pr view'
    Assert-That ($null -eq (Find-Change 'work/107')) 'draft PR on a running card: the card stays running'
    Assert-That ($null -eq (Find-Change 'work/110')) 'running card started 30 minutes ago with no worktree yet: left alone (race)'
    Assert-That ($null -eq (Find-Change 'work/108')) 'card grouping two issues is not finished by a PR closing only one'
    Assert-That ($null -eq (Find-Change 'flow/410')) 'merged plan PR (Refs only) does not finish the flow front'
    Assert-That ($null -eq (Find-Change 'work/105-done')) 'done card is left alone'
    Assert-That ($null -eq (Find-Change 'work/106-orch')) 'running orchestrator card without worktree is not stopped'
    $c = Find-Change 'work/300'
    Assert-That ($c -and -not $c.Exists -and $c.Data.status -eq 'pr-open' -and $c.Data.pr -eq 210 -and $c.Data.issues[0] -eq 300) 'open PR closing an issue and without card: new card'
    Assert-That ($null -eq (Find-Change 'work/301') -and $null -eq (Find-Change 'work/pr-211') -and $null -eq (Find-Change 'work/pr-212')) 'bot PR and PR without a closed issue get no card'
    # ---- flow
    $c = Find-Change 'flow/400'
    Assert-That ($c.Data.status -eq 'merged' -and $c.Data.stage -eq 'done' -and $c.Data.updatedUtc -eq '2026-10-05T10:30:00Z' -and $c.Data.pr -eq 220) 'flow front with a merged PR: merged / done'
    $c = Find-Change 'flow/401'
    Assert-That ($c.Data.status -eq 'in-progress' -and $c.Data.stage -eq 'review' -and $c.Data.pr -eq 221) 'flow front with an open PR: in-progress / review'
    $c = Find-Change 'flow/402'
    Assert-That ($c.Data.status -eq 'closed' -and $c.Data.stage -eq 'close-out') 'flow front whose issue closed without PR: closed / close-out'
    Assert-That ($null -eq (Find-Change 'flow/403')) 'untouched flow front: no write'
    $c = Find-Change 'flow/404'
    Assert-That ($c.Data.status -eq 'closed' -and $c.Data.stage -eq 'not-planned' -and $c.Data.updatedUtc -eq '2026-06-01T00:00:00Z') 'flow front whose issue closed long ago as not planned: closed / not-planned'
    $c = Find-Change 'flow/405'
    Assert-That ($c.Data.status -eq 'stopped' -and $c.Data.note -match 'closed without merging') 'flow front whose PR closed unmerged: stopped with a note'
    $c = Find-Change 'flow/412'
    Assert-That ($c.Data.status -eq 'in-progress' -and $c.Data.stage -eq 'review' -and $c.Data.pr -eq 234) 'front after a merged plan PR advances to review when the Fixes PR opens'
    $c = Find-Change 'flow/413'
    Assert-That ($c.Data.status -eq 'merged' -and $c.Data.stage -eq 'done' -and $c.Data.pr -eq 235) 'front after a merged plan PR is done when the Fixes PR merges'
    $c = Find-Change 'work/411'
    Assert-That ($c.Data.status -eq 'merged' -and $c.Data.endedUtc -eq '2026-09-20T09:00:00Z') 'plan card is finished by its own merged plan PR'
    Assert-That ($null -eq (Find-Change 'work/114')) 'hand-set blocked card is not overwritten by an open PR'
    Assert-That ((Find-Change 'work/115').Data.status -eq 'running') 'queued card with a draft PR becomes running, not pr-open'
    # ---- audits
    Assert-That ((Find-Change 'audits/5').Data.status -eq 'closed') 'audit done in progress.csv: closed'
    Assert-That ($null -eq (Find-Change 'audits/6')) 'closed audit unchanged: no write'
    $c = Find-Change 'audits/7'
    Assert-That ($c -and -not $c.Exists -and $c.Data.status -eq 'closed' -and $c.Data.title -eq 'Issue 7 title') 'audit missing from the board: created from progress.csv with the issue title'
    Assert-That ($null -eq $c.Data.outcome -and $null -eq $c.Data.pipeline -and $c.Data.note -match 'remediation issues opened 2') 'created audit invents no outcome or pipeline; counts come from progress.csv'
    $c = Find-Change 'audits/8'
    Assert-That ($c -and $c.Data.status -eq 'open' -and $c.Data.openedUtc -eq '2026-10-05T08:00:00Z') 'current-audit.json (pre-#2234): open audit created'
    $c = Find-Change 'audits/9'
    Assert-That ($c -and $c.Data.status -eq 'open' -and $c.Data.openedUtc -eq '2026-10-05T09:30:00Z') 'open-audits/9.json (#2234): a second open audit created with its own start time'
    # ---- meta
    $c = Find-Change 'meta/board'
    Assert-That ($c.Data.status -match 'Audits #8 #9 open\.' -and $c.Data.status -match 'Open PRs: #201 #210 #211 #212 \(draft\) #213 \(draft\) #221 #234 #236 #237 \(draft\)\.') 'meta.status lists both open audits and the open PRs'
    Assert-That ($c.Data.status -match 'Merged last 48h: #200 #220 #235\.' -and $c.Data.current -eq 8 -and (@($c.Data.openAudits) -join ',') -eq '8,9' -and $c.Data.updatedUtc -eq '2026-10-05T11:00:00Z') 'meta.status lists recent merges; current (lowest open), openAudits and updatedUtc set'
    Assert-That ($c.Data.status -match ' Notes: hand written$') 'first run keeps the hand-written status behind the Notes marker'
    $noAudit = $facts.Clone(); $noAudit.OpenAudits = @()
    $cNo = (Get-BoardChanges $docs $noAudit $now | Where-Object { $_.Collection -eq 'meta' }).Data
    Assert-That ($cNo.Contains('current') -and $null -eq $cNo.current) 'meta.current is cleared when no audit is open'
    Assert-That ($null -eq ($changes | Where-Object { $_.Collection -notin 'work', 'flow', 'audits', 'meta' })) 'no other collection is touched'

    # ---- versions
    $pins = @{}
    foreach ($ch in $changes | Where-Object { $_.Exists }) { $pins["$($ch.Collection)/$($ch.Id)"] = 7 }
    $pins.Remove('work/103')
    $pins['work/100'] = 4
    $batch = ConvertTo-BatchFiles $changes $pins 'set' 'update' 50
    $all = @($batch.Files | ForEach-Object { $_ })
    Assert-That ($batch.Skipped -contains 'work/103' -and $batch.Skipped.Count -eq 1) 'existing doc without a version is skipped, never written unpinned'
    Assert-That ((@($all | Where-Object { $_.op -eq 'update' -and $_.if_version -gt 0 }).Count) -eq (@($all | Where-Object { $_.op -eq 'update' }).Count)) 'every update carries if_version'
    $creates = @($all | Where-Object { $_.op -eq 'set' })
    Assert-That (@($creates | Where-Object { $_.Contains('if_version') }).Count -eq 0 -and $creates.Count -eq 5) "creates ($($creates.Count): $(($creates | ForEach-Object { "$($_.collection)/$($_.doc_id)" }) -join ',')) carry no if_version (work/300, work/401 for PR 221, audits/7, audits/8, audits/9)"
    Assert-That (($all | Where-Object { $_.collection -eq 'work' -and $_.doc_id -eq '100' }).if_version -eq 4) 'the version comes from the sidecar'
    $w100 = $all | Where-Object { $_.collection -eq 'work' -and $_.doc_id -eq '100' }
    Assert-That ((@($w100.data.Keys | Sort-Object) -join ',') -eq 'endedUtc,status') "update carries only the changed fields (got $(@($w100.data.Keys | Sort-Object) -join ','))"

    # ---- idempotence: apply the changes, a second run writes nothing
    foreach ($ch in $changes) { $docs["$($ch.Collection)/$($ch.Id)"] = @{ Collection = $ch.Collection; Id = $ch.Id; Data = (Copy-Data $ch.Data) } }
    $facts2 = Get-BoardFacts $docs 'o/r' $main $now 14
    $again = Get-BoardChanges $docs $facts2 $now
    Assert-That ($again.Count -eq 0) "second run on the reconciled board writes nothing (got $($again.Count): $(($again | ForEach-Object { "$($_.Collection)/$($_.Id)" }) -join ', '))"

    # ---- the 50-write split
    $many = 1..120 | ForEach-Object { @{ Collection = 'work'; Id = "n$_"; Data = @{ n = $_ }; Exists = $false } }
    $split = ConvertTo-BatchFiles $many @{} 'set' 'update' 50
    Assert-That ($split.Files.Count -eq 3 -and $split.Files[0].Count -eq 50 -and $split.Files[1].Count -eq 50 -and $split.Files[2].Count -eq 20 -and $split.Count -eq 120) '120 writes split into 50 + 50 + 20'
    $one = ConvertTo-BatchFiles ($many | Select-Object -First 50) @{} 'set' 'update' 50
    Assert-That ($one.Files.Count -eq 1) 'exactly 50 writes stay in one file'

    # ---- the script's main block, end to end with the stubs (bare -Out name, split, stale files, safety abort)
    $versionsFile = Join-Path $work 'versions.json'
    $sidecar = @{ 'meta/board' = 3 }
    foreach ($ch in $changes | Where-Object { $_.Exists -and "$($_.Collection)/$($_.Id)" -ne 'work/103' }) { $sidecar["$($ch.Collection)/$($ch.Id)"] = 2 }
    [IO.File]::WriteAllText($versionsFile, (ConvertTo-Json $sidecar), [Text.UTF8Encoding]::new($false))
    $outDir = Join-Path $work 'out'
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    [IO.File]::WriteAllText((Join-Path $outDir 'batch-009.json'), '[]')
    $scriptPath = Join-Path $PSScriptRoot 'reconcile-board.ps1'
    Push-Location $outDir
    try {
        $lines = @(& $scriptPath -CurrentDir $export -Versions $versionsFile -Out 'batch.json' -Repo 'o/r' -MainRoot $main -NowUtc '2026-10-05T11:00:00Z' -MaxWrites 5)
    }
    finally { Pop-Location }
    $made = @(Get-ChildItem $outDir -Filter 'batch*.json' | Sort-Object Name | ForEach-Object Name)
    Assert-That ($lines[0] -match '^WRITES (\d+) SKIPPED 1$' -and $made.Count -eq [math]::Ceiling([int]$Matches[1] / 5) -and $made[0] -eq 'batch-001.json') "main block: bare -Out name resolved, split into numbered files, stale batch-009 removed ($($lines[0]); $($made -join ','))"
    $first = Get-Content (Join-Path $outDir 'batch-001.json') -Raw | ConvertFrom-Json
    Assert-That (@($first).Count -eq 5 -and $first[0].op -in 'set', 'update') 'main block: a batch file holds at most -MaxWrites writes in the ArtifactData shape'
    $broken = Join-Path $work 'export-broken'
    Copy-Item -Recurse $export $broken
    Remove-Item -Recurse -Force (Join-Path $broken 'flow')
    $threw = $false
    try { & $scriptPath -CurrentDir $broken -Versions $versionsFile -Out (Join-Path $outDir 'never.json') -Repo 'o/r' -MainRoot $main | Out-Null } catch { $threw = $_.Exception.Message -match 'incomplete board export' }
    Assert-That ($threw -and -not (Test-Path (Join-Path $outDir 'never.json'))) 'main block: an export without flow/ aborts with an error and writes nothing'

    # ---- dry run: lists the drift, writes and deletes nothing; no -Out and no -DryRun is an error
    $snapshot = { @(Get-ChildItem $outDir -File | Sort-Object Name | ForEach-Object { "$($_.Name):$((Get-FileHash $_.FullName).Hash)" }) -join '|' }
    [IO.File]::WriteAllText((Join-Path $outDir 'batch-009.json'), '[]')
    $before = & $snapshot
    $dry = @(& $scriptPath -CurrentDir $export -Versions $versionsFile -DryRun -Out (Join-Path $outDir 'batch.json') -Repo 'o/r' -MainRoot $main -NowUtc '2026-10-05T11:00:00Z')
    $probe = $changes[0]
    $driftLines = @($dry | Where-Object { $_ -like 'DRIFT *' })
    $skipLines = @($driftLines | Where-Object { $_ -like '*(would skip: no version)' })
    Assert-That (@($driftLines -match "^DRIFT $([regex]::Escape("$($probe.Collection)/$($probe.Id)")): \S").Count -eq 1) "dry run: a DRIFT line names $($probe.Collection)/$($probe.Id) with its changed fields"
    Assert-That ($dry[-1] -eq "DRY-RUN: $($driftLines.Count) documents drift ($($skipLines.Count) would be skipped), nothing written") "dry run: summary line ($($dry[-1]))"
    Assert-That ($dry -contains 'DRY-RUN: -Out ignored') 'dry run: -Out together with -DryRun prints that -Out is ignored'
    Assert-That ((& $snapshot) -eq $before) 'dry run with -Out into the seeded out directory (stale batch-009.json present): file set and hashes unchanged'
    $null = $lines[0] -match '^WRITES (\d+) SKIPPED (\d+)$'
    $realWrites = [int]$Matches[1]; $realSkipped = [int]$Matches[2]
    Assert-That ($driftLines.Count -eq @($changes).Count) "dry run: DRIFT lines ($($driftLines.Count)) equal the changes a real run computes ($(@($changes).Count))"
    Assert-That ($skipLines.Count -eq $realSkipped -and ($driftLines.Count - $skipLines.Count) -eq $realWrites) "dry run: would-skip count ($($skipLines.Count)) equals the real SKIPPED ($realSkipped) and drift minus skipped equals the real WRITES ($realWrites)"
    Assert-That (@($skipLines | Where-Object { $_ -like 'DRIFT work/103:*' }).Count -eq 1) 'dry run: the document without a version (work/103) is the one marked would skip'
    $threw = $false
    try { & $scriptPath -CurrentDir $export -Versions $versionsFile -Repo 'o/r' -MainRoot $main | Out-Null } catch { $threw = $_.Exception.Message -match '-Out is required' }
    Assert-That $threw 'main block: neither -Out nor -DryRun fails with a clear error'
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}

"{0} checks, {1} failed" -f $script:total, $script:failed
exit ([int]($script:failed -gt 0))
