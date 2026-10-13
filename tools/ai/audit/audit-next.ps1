# tools/ai/audit/audit-next.ps1 [-Issue n] (#1345, #2234)
#
# Starts one SPEC-003 audit: the queue discipline that replaces batching issues. Several audits may be open at
# once (#2234, maintainer decision of 2026-10-10), up to tools/ai/audit/pipeline.json maxParallelAudits (2-5,
# default 2), each one individual: its own wia-<n> worktree, its own six stage agents, its own verifier loop and
# its own knowledge pull request, exactly as if the audits ran one after the other. Never a batch: every call
# opens ONE audit.
#
# Refuses when the limit is reached, or when a stray `wia-*` worktree exists that has no open-audit state file
# (a previous audit that was not closed with audit-done.ps1). With no -Issue, takes the first queue entry
# (artifacts/knowledge/audit-queue.txt, in file order) that is not recorded in artifacts/knowledge/progress.csv,
# not open already, and whose scope (the packages under src/ its pre-draft and commits name, Get-IssueScopePackages)
# shares no package with an open audit's scope (Get-AuditScope); an overlapping issue is skipped, stays in the
# queue and is taken by a later call once the overlapping audit has closed (queue order is kept otherwise). That
# overlap rule is what keeps the result identical to a sequential run. -Issue n is only accepted when n is that
# same issue AND gh reports it CLOSED; there is no override to jump the queue.
#
# Creates .claude/worktrees/wia-<n> on a NEW LOCAL branch audit/<n> based on origin/main (never a detached HEAD,
# and the branch is never pushed: audit-done.ps1 deletes it after collecting), writes the audit's state file
# artifacts/knowledge/open-audits/<n>.json (scope and the concurrent audits) and adds <n> to the `concurrent` list
# of every other open audit (the cross-audit duplicate check of the remediation stage reads it). The local-model
# pre-draft (artifacts/knowledge/predraft/<n>.md) of each candidate is made before its scope is read, so the
# pre-draft exists before the archivist stage starts.
#
# -Delta <set> (#1763): starts a DELTA audit instead, a re-check of an audit already published, for the rules
# decided after it ran only (set rules-2026-10: docs rule (a), tests rule (b)). It takes the next of the audited
# issues (the distinct issues of progress.csv, in order) that has no delta in the set's progress file
# (artifacts/knowledge/delta-progress-<set>.csv), is not open and does not overlap an open audit (its scope is the
# `packages:` of its published record) -- or -Issue n, any issue of that list not yet done, open or overlapping --,
# reuses the scope the original audit recorded (the knowledge record and the published archivist/code stage files
# in docs/knowledge, written to artifacts/knowledge/delta-scope.md of the new worktree; no pre-draft, no
# classify-scope), creates the audit worktree as today, records `mode: delta` and `set` in its state file and
# prints the first stage of tools/ai/audit/pipeline-delta.json. Delta audits count toward the same limit.
#   pwsh -NoProfile -File tools/ai/audit/audit-next.ps1 -Delta rules-2026-10 [-Issue n]

#
# No audit forgotten half-way (#2234): the audit is recorded as open (its state file, with startedUtc) the moment
# this script starts it, so it is never handed to a second slot and audit-stage.ps1 -List shows it with its stage
# and the days it has been open. While any open audit is older than 2 days (stale), this script refuses to start
# another one and names it; -Force starts one anyway (an explicit, printed decision).

param([int]$Issue, [string]$Delta = '', [switch]$Force)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$mainRoot = Get-MainRoot $PSScriptRoot
$knowledgeRoot = Get-KnowledgeRoot $mainRoot
$worktreesRoot = Join-Path $mainRoot '.claude\worktrees'

# Two audit-next runs at the same time would both see the same free slot: serialize them.
$lockName = 'Local\Encina.AuditNext.' + (-join ([System.Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($mainRoot.ToLowerInvariant())) | ForEach-Object { $_.ToString('x2') }))
$lock = [System.Threading.Mutex]::new($false, $lockName)
$locked = $false
try { $locked = $lock.WaitOne(30000) } catch [System.Threading.AbandonedMutexException] { $locked = $true }
if (-not $locked) { Write-Error 'audit-next: another audit-next.ps1 is running; try again when it has finished.'; exit 1 }

try {
    try {
        $open = @(Get-OpenAudits $mainRoot)
        $max = Get-MaxParallelAudits (Get-Pipeline $PSScriptRoot)
    }
    catch { Write-Error "audit-next: $($_.Exception.Message)"; exit 1 }
    $openNumbers = @($open | ForEach-Object { [string]$_.issue })
    $openText = if ($open.Count -gt 0) { ($open | ForEach-Object { "#$($_.issue)" }) -join ', ' } else { 'none' }
    if ($open.Count -ge $max) {
        Write-Error "audit-next: $($open.Count) audits are already open ($openText) and pipeline.json maxParallelAudits is $max. Close one with audit-done.ps1 first."
        exit 1
    }
    $stale = @(Get-StaleAudits $open ([DateTime]::UtcNow))
    if ($stale.Count -gt 0) {
        $staleText = ($stale | ForEach-Object { "#$($_.issue) (wia-$($_.issue), started $($_.startedUtc))" }) -join ', '
        if (-not $Force) {
            Write-Error "audit-next: stale audit(s) open for more than $($script:StaleAuditDays) days: $staleText. Finish or close them first (audit-stage.ps1 -List shows their stage), or pass -Force to start another audit anyway."
            exit 1
        }
        Write-Warning "audit-next: -Force: starting another audit although stale audit(s) are open: $staleText."
    }
    $existingWia = @(Get-ChildItem -LiteralPath $worktreesRoot -Directory -Filter 'wia-*' -ErrorAction SilentlyContinue | Where-Object { $_.Name -notmatch '^wia-(\d+)$' -or $openNumbers -notcontains $Matches[1] })
    if ($existingWia.Count -gt 0) {
        $names = ($existingWia | ForEach-Object { $_.Name }) -join ', '
        Write-Error "audit-next: a wia-* worktree exists that is not an open audit ($names; open: $openText). Remove it (git worktree remove) or restore its artifacts/knowledge/open-audits/<n>.json before starting a new audit."
        exit 1
    }
    # The scope of every open audit now (its recorded scope plus what its archivist and code stages named).
    $openScopes = @{}
    foreach ($a in $open) { $openScopes[[string]$a.issue] = @(Get-AuditScope $mainRoot $a) }
    # The open audits a candidate scope overlaps, as '#<m> (<packages>)' texts; empty when it overlaps none.
    function Get-Overlaps([string[]]$Scope) {
        return @(foreach ($a in $open) {
                $shared = @(Get-ScopeOverlap $openScopes[[string]$a.issue] $Scope)
                if ($shared.Count -gt 0) { "#$($a.issue) ($($shared -join ', '))" }
            })
    }
    # Registers the new audit (Register-OpenAudit, _audit-lib.ps1: under the open-audits lock, with the open audits
    # read fresh, so an audit that audit-done.ps1 closed while this run generated a pre-draft or fetched is never
    # re-saved; review F1 of #2234). When registration is refused, the worktree and branch just created are removed.
    function Register-Audit($State) {
        try { return (Register-OpenAudit $mainRoot $State) }
        catch {
            $reason = $_.Exception.Message
            $rmOut = & git -C $mainRoot worktree remove ([string]$State.worktree) --force 2>&1
            $brOut = & git -C $mainRoot branch -D ([string]$State.branch) 2>&1
            Write-Error "audit-next: could not register #$($State.issue): $reason Worktree and branch removed, no audit left half open."
            exit 1
        }
    }

    if ($Delta) {
        $deltaPipelineSource = Get-Pipeline $PSScriptRoot 'pipeline-delta.json'
        if ([string]$deltaPipelineSource.delta.set -ne $Delta) {
            Write-Error "audit-next: unknown delta set '$Delta' (tools/ai/audit/pipeline-delta.json defines '$($deltaPipelineSource.delta.set)')."
            exit 1
        }
        $candidates = @(Get-DeltaCandidates $knowledgeRoot)
        if ($candidates.Count -eq 0) { Write-Error "audit-next: no audited issue in artifacts/knowledge/progress.csv; nothing to re-check."; exit 1 }
        $deltaDone = @(Get-DeltaDone $knowledgeRoot $Delta)
        $fetchOutput = & git -C $mainRoot fetch origin main 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: git fetch origin main failed: $fetchOutput"; exit 1 }
        # The set covers exactly the audits done BEFORE its rules (pipeline-delta.json delta.cutOff): an issue whose
        # published record says its audit ran on or after the cut-off already applied the rules and never enters the
        # queue. A record without an audit date predates the rules. The delta needs the published record on
        # origin/main (its scope source); an issue without one is skipped with a warning. Published stage files are
        # NOT required: without them the scope is built from the record and the published result.
        $cutOff = [string]$deltaPipelineSource.delta.cutOff
        $unfit = [System.Collections.Generic.List[string]]::new()
        $afterCutOff = [System.Collections.Generic.List[string]]::new()
        $recordScopes = @{}
        $known = Get-SrcPackageNames $mainRoot
        $pendingDelta = @($candidates | Where-Object { $deltaDone -notcontains $_ } | Where-Object {
                $recordText = (& git -C $mainRoot show "origin/main:docs/knowledge/issues/$_.md" 2>$null) -join "`n"
                if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($recordText)) { $unfit.Add($_); return $false }
                $auditDate = Get-RecordAuditDate $recordText
                if ($auditDate -and $cutOff -and [string]::CompareOrdinal($auditDate, $cutOff) -ge 0) { $afterCutOff.Add($_); return $false }
                $recordScopes[[string]$_] = @(Get-PackagesFromText $recordText $known)
                $true
            })
        if ($unfit.Count -gt 0) { Write-Warning "audit-next: skipped (no published record docs/knowledge/issues/<n>.md on origin/main): $($unfit -join ', ')." }
        if ($afterCutOff.Count -gt 0) { Write-Host "audit-next: not in the '$Delta' set (audit dated on or after $cutOff): $($afterCutOff -join ', ')." }
        $pendingDelta = @($pendingDelta | Where-Object { $openNumbers -notcontains [string]$_ })
        if ($pendingDelta.Count -eq 0) { Write-Error "audit-next: no audited issue left without a '$Delta' delta that has a published record and is not open already (open: $openText)."; exit 1 }
        if ($Issue) {
            if ($pendingDelta -notcontains [string]$Issue) {
                Write-Error "audit-next: -Issue $Issue is not an audited issue without a '$Delta' delta (pending: $($pendingDelta -join ', '))."
                exit 1
            }
            $overlaps = @(Get-Overlaps $recordScopes[[string]$Issue])
            if ($overlaps.Count -gt 0) { Write-Error "audit-next: -Issue $Issue overlaps the open audit(s) $($overlaps -join ', '); start it after they close (#2234)."; exit 1 }
            $n = $Issue
        }
        else {
            $n = 0
            $skipped = [System.Collections.Generic.List[string]]::new()
            foreach ($candidate in $pendingDelta) {
                $overlaps = @(Get-Overlaps $recordScopes[[string]$candidate])
                if ($overlaps.Count -eq 0) { $n = [int]$candidate; break }
                $skipped.Add("#$candidate overlaps $($overlaps -join ', ')")
            }
            if ($skipped.Count -gt 0) { Write-Host "audit-next: skipped for now (scope overlaps an open audit, #2234): $($skipped -join '; ')." }
            if ($n -eq 0) { Write-Error "audit-next: every pending '$Delta' issue overlaps an open audit ($openText); close one first."; exit 1 }
        }

        $wt = Join-Path $worktreesRoot "wia-$n"
        $branch = "audit/$n"
        $addOutput = & git -C $mainRoot worktree add -b $branch $wt origin/main 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: failed to create worktree $wt on branch $branch`: $addOutput"; exit 1 }

        $scopeText = New-DeltaScopeText $n $wt $Delta
        if ($null -eq $scopeText) {
            $rmOut = & git -C $mainRoot worktree remove $wt --force 2>&1
            $brOut = & git -C $mainRoot branch -D $branch 2>&1
            Write-Error "audit-next: docs/knowledge/issues/$n.md does not exist on origin/main, so the scope the original audit recorded cannot be reused; worktree and branch removed, no audit left open. Publish that audit's record first (audit-done.ps1)."
            exit 1
        }
        New-Item -ItemType Directory -Force (Get-StagesDir $wt) | Out-Null
        Set-Content -LiteralPath (Join-Path $wt 'artifacts\knowledge\delta-scope.md') -Value $scopeText -Encoding utf8

        $null = Register-Audit ([ordered]@{
                issue      = $n
                worktree   = $wt
                branch     = $branch
                startedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
                mode       = 'delta'
                set        = $Delta
                scope      = @($recordScopes[[string]$n])
                concurrent = @($open | ForEach-Object { [int]$_.issue })
            })

        "Delta audit $Delta of #$n (pending deltas: $($pendingDelta.Count); open audits now: $($open.Count + 1) of $max); scope reused from the original audit: $wt\artifacts\knowledge\delta-scope.md"
        $deltaPipeline = $deltaPipelineSource
        $marker = [string]$deltaPipeline.delta.promptMarker
        $nextDelta = Get-NextStage (Get-StagesDir $wt) $wt $deltaPipeline
        if ($null -eq $nextDelta) { "All stages already complete. Run audit-done.ps1 -Issue $n." }
        else {
            $ruleHint = if ($nextDelta.PSObject.Properties['rule']) { "check only rule ($($nextDelta.rule))" } else { 'delta stage' }
            "Next stage: $($nextDelta.stage) (spawn $($nextDelta.agent) -- issue #$n, worktree $wt, branch $branch; the prompt says `"$marker, $ruleHint`")"
        }
        exit 0
    }

    # The pending queue, computed the same way whether -Issue is given or not: the audit-queue.txt entries (in file
    # order) not recorded in progress.csv and not open already. The first one whose scope overlaps no open audit is
    # the one this call opens; -Issue is only ever accepted when it equals that issue AND gh reports it CLOSED
    # (review thread T7); there is no override.
    $queuePath = Join-Path $knowledgeRoot 'audit-queue.txt'
    if (-not (Test-Path -LiteralPath $queuePath)) { Write-Error "audit-next: no queue file at $queuePath."; exit 1 }
    $progressPath = Join-Path $knowledgeRoot 'progress.csv'
    $done = if (Test-Path -LiteralPath $progressPath) { @(Get-Content -LiteralPath $progressPath | ForEach-Object { ($_ -split ',')[0] }) } else { @() }
    $queue = @(Get-Content -LiteralPath $queuePath | Where-Object { $_ -match '\S' } | ForEach-Object { $_.Trim() })
    $pending = @($queue | Where-Object { $done -notcontains $_ -and $openNumbers -notcontains $_ } | Select-Object -Unique)
    if ($pending.Count -eq 0) { Write-Error "audit-next: no pending issue in artifacts/knowledge/audit-queue.txt (open: $openText)."; exit 1 }

    # The pre-draft of each candidate is made before its scope is read (the scope comes partly from it), and before
    # any worktree exists (review thread T9: a failed generator must never leave an audit half open).
    $n = 0
    $scope = @()
    $skipped = [System.Collections.Generic.List[string]]::new()
    foreach ($candidate in $pending) {
        $predraftFile = Join-Path $knowledgeRoot "predraft\$candidate.md"
        if (-not (Test-Path -LiteralPath $predraftFile)) {
            & (Join-Path $PSScriptRoot 'qwen-predraft.ps1') -Issue ([int]$candidate)
            $predraftExit = $LASTEXITCODE
            if ($predraftExit -ne 0 -or -not (Test-Path -LiteralPath $predraftFile)) {
                Write-Error "audit-next: pre-draft generation failed for #$candidate (exit $predraftExit, predraft\$candidate.md present: $(Test-Path -LiteralPath $predraftFile)); no worktree was created and no audit was opened."
                exit 1
            }
        }
        $candidateScope = @(Get-IssueScopePackages $mainRoot ([int]$candidate))
        $overlaps = @(Get-Overlaps $candidateScope)
        if ($overlaps.Count -eq 0) { $n = [int]$candidate; $scope = $candidateScope; break }
        $skipped.Add("#$candidate overlaps $($overlaps -join ', ')")
    }
    if ($skipped.Count -gt 0) { Write-Host "audit-next: skipped for now (scope overlaps an open audit, #2234): $($skipped -join '; ')." }
    if ($Issue -and [string]$Issue -ne [string]$n) {
        $nextText = if ($n -gt 0) { "#$n" } else { 'none (every pending issue overlaps an open audit)' }
        Write-Error "audit-next: -Issue $Issue is not the next pending issue that overlaps no open audit in artifacts/knowledge/audit-queue.txt (next: $nextText)."
        exit 1
    }
    if ($n -eq 0) { Write-Error "audit-next: every pending issue overlaps an open audit ($openText); close one with audit-done.ps1 first."; exit 1 }

    if ($Issue) {
        $stateOutput = & gh issue view $Issue --repo dlrivada/Encina --json state --jq '.state' 2>&1
        if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: gh issue view #$Issue failed: $stateOutput"; exit 1 }
        if (([string]$stateOutput).Trim() -ne 'CLOSED') {
            Write-Error "audit-next: issue #$Issue is not CLOSED (gh reports '$stateOutput')."
            exit 1
        }
    }

    $fetchOutput = & git -C $mainRoot fetch origin main 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: git fetch origin main failed: $fetchOutput"; exit 1 }

    $wt = Join-Path $worktreesRoot "wia-$n"
    $branch = "audit/$n"
    $addOutput = & git -C $mainRoot worktree add -b $branch $wt origin/main 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "audit-next: failed to create worktree $wt on branch $branch`: $addOutput"; exit 1 }

    New-Item -ItemType Directory -Force (Get-StagesDir $wt) | Out-Null
    New-Item -ItemType Directory -Force $knowledgeRoot | Out-Null

    $null = Register-Audit ([ordered]@{
            issue      = $n
            worktree   = $wt
            branch     = $branch
            startedUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
            scope      = @($scope)
            concurrent = @($open | ForEach-Object { [int]$_.issue })
        })

    $classified = & (Join-Path $PSScriptRoot 'classify-scope.ps1') -Issue $n
    "$classified"
    "Audit #$n opened (open audits now: $($open.Count + 1) of $max; scope: $(if ($scope.Count) { $scope -join ', ' } else { 'no src package named' }))."

    $pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')
    $next = Get-NextStage (Get-StagesDir $wt) $wt $pipeline
    if ($null -eq $next) {
        "All stages already complete. Run audit-done.ps1 -Issue $n."
    }
    elseif ($next.agent -match '^issue-|^audit-|^docs-reviewer$') {
        "Next stage: $($next.stage) (spawn $($next.agent) -- issue #$n, worktree $wt, branch $branch)"
    }
    else {
        "Next stage: $($next.stage) (run $($next.agent))"
    }
}
finally {
    if ($locked) { $lock.ReleaseMutex() }
    $lock.Dispose()
}
