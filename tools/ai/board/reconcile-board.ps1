# Reconciles the private control board (a claude.ai artifact; db collections work/<id>, flow/<issue>,
# audits/<n>, meta/board) with what GitHub, git and the audit progress files say (#1732). Deterministic: no
# model, no network write. It reads, compares and emits ArtifactData "batch" writes; it never applies them.
#
# Inputs
#   -CurrentDir  the folder an ArtifactData "list ... out_dir" call produced: <dir>/work/*.json,
#                <dir>/flow/*.json, <dir>/audits/*.json, <dir>/meta/board.json. Each file is the document's data
#                only. The file name (without .json) is the doc_id.
#   -Versions    a JSON object {"work/1698": 8, "flow/1698": 3, "meta/board": 2, ...}: each EXISTING
#                document's version, which "list" prints only in its tool result text. The caller (whoever runs
#                it by hand) writes it from those results. An existing document missing from the sidecar is
#                SKIPPED with a warning (a write without if_version could overwrite a concurrent manual edit);
#                a new document needs no version. The run still exits 0 so the other writes can be applied.
#   gh / git     open PRs (head branch, number, "Fixes #n" issues), PRs merged in the last 14 days, closed
#                issues, `git worktree list` (+ commits ahead of origin/main), and in the MAIN checkout
#                artifacts/knowledge/open-audits/<n>.json (one per open audit, #2234; a pre-#2234
#                current-audit.json is read too until the audit scripts convert it) and progress.csv.
# Output
#   -Out         a JSON array of writes [{op, collection, doc_id, if_version?, data}], only for documents
#                whose data differ, at most 50 per file (-Out itself when it fits, else <name>-001.json,
#                <name>-002.json ...). The paths are printed one per line after "WRITES <n>".
#   -DryRun      instead of -Out: prints "DRIFT <collection>/<id>: <fields>" per drifting document (with
#                " (would skip: no version)" when a real run would skip it) and "DRY-RUN: <n> documents drift
#                (<k> would be skipped), nothing written"; writes and deletes no file. -Out is ignored
#                (one line says so).
#   -CreateOp / -UpdateOp  the batch operation names for a new / an existing document.
#
# Rules (see tools/ai/board/README.md): work card or flow front with a merged PR -> merged / done with the
# merge time; open PR -> pr-open / in-progress review; a worktree with commits ahead of main and no PR ->
# running; a running worker card with no worktree and no PR -> stopped with a note; an open PR that closes an
# issue and has no card -> new card; audits from the open-audit state files and progress.csv; meta/board.status
# regenerated (meta/board.current is the lowest open audit, meta/board.openAudits all of them). Documents are never deleted; other collections (dash/*, stats/*, gates/*, prio/*) are never
# touched. Dot-sourcing this file (`. reconcile-board.ps1`) defines the functions without running.

param(
    [string]$CurrentDir,
    [string]$Versions,
    [string]$Out,
    [string]$Repo = 'dlrivada/Encina',
    [string]$MainRoot,
    [string]$NowUtc,
    [string]$CreateOp = 'set',
    [string]$UpdateOp = 'update',
    [int]$MergedDays = 14,
    [int]$MaxWrites = 50,
    [switch]$DryRun
)

#requires -Version 7.5
$ErrorActionPreference = 'Stop'
# ISO timestamps must stay strings (PowerShell 7.5+): the default would turn them into local DateTime values.
$PSDefaultParameterValues['ConvertFrom-Json:DateKind'] = 'String'

# ---------------------------------------------------------------- helpers

function ConvertTo-Canonical($Value) {
    if ($null -eq $Value) { return $null }
    if ($Value -is [System.Collections.IDictionary]) {
        $o = [ordered]@{}
        foreach ($k in ($Value.Keys | Sort-Object { [string]$_ } -CaseSensitive)) { $o[[string]$k] = ConvertTo-Canonical $Value[$k] }
        return $o
    }
    if ($Value -is [string]) { return $Value }
    if ($Value -is [System.Collections.IEnumerable]) { return , @($Value | ForEach-Object { ConvertTo-Canonical $_ }) }
    return $Value
}

function ConvertTo-CanonicalJson($Value) { ConvertTo-Json (ConvertTo-Canonical $Value) -Depth 20 -Compress }

function Copy-Data($Data) { (ConvertTo-Json $Data -Depth 20 -Compress) | ConvertFrom-Json -AsHashtable }

function Get-Utc([datetime]$When) { $When.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss'Z'") }

function Invoke-GhJson([string[]]$GhArguments) {
    $global:LASTEXITCODE = 0
    $text = (gh @GhArguments) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw "gh $($GhArguments -join ' ') failed (exit $LASTEXITCODE)" }
    if ([string]::IsNullOrWhiteSpace($text)) { return @() }
    return , @($text | ConvertFrom-Json -AsHashtable)
}

function Get-ClosedIssues($Pr) {
    $set = [System.Collections.Generic.SortedSet[int]]::new()
    foreach ($r in @($Pr.closingIssuesReferences)) { if ($r -and $r.number) { [void]$set.Add([int]$r.number) } }
    foreach ($m in [regex]::Matches([string]$Pr.body, '(?i)\b(?:fix(?:es|ed)?|close[sd]?|resolve[sd]?)\s+#(\d+)')) { [void]$set.Add([int]$m.Groups[1].Value) }
    return , @($set)
}

function ConvertTo-PrFact($Pr) {
    $login = [string]$Pr.author.login
    @{
        number    = [int]$Pr.number
        title     = [string]$Pr.title
        head      = [string]$Pr.headRefName
        isDraft   = [bool]$Pr.isDraft
        createdAt = [string]$Pr.createdAt
        mergedAt  = [string]$Pr.mergedAt
        state     = [string]$Pr.state
        isBot     = ([bool]$Pr.author.is_bot -or $login -like 'app/*' -or $login -like '*[[]bot[]]')
        closes    = Get-ClosedIssues $Pr
    }
}

# ---------------------------------------------------------------- reading the current board

function Read-BoardExport([string]$Dir) {
    $docs = @{}
    foreach ($c in 'work', 'flow', 'audits') {
        $d = Join-Path $Dir $c
        if (-not (Test-Path -LiteralPath $d)) { continue }
        foreach ($f in Get-ChildItem -LiteralPath $d -Filter *.json -File) {
            $docs["$c/$($f.BaseName)"] = @{ Collection = $c; Id = $f.BaseName; Data = (Get-Content -LiteralPath $f.FullName -Raw | ConvertFrom-Json -AsHashtable) }
        }
    }
    $m = Join-Path $Dir 'meta' 'board.json'
    if (Test-Path -LiteralPath $m) { $docs['meta/board'] = @{ Collection = 'meta'; Id = 'board'; Data = (Get-Content -LiteralPath $m -Raw | ConvertFrom-Json -AsHashtable) } }
    return $docs
}

function Test-CardTerminal($Card) { [string]$Card.status -in 'merged', 'done' }

# ---------------------------------------------------------------- gathering facts (gh, git, audit files)

function Get-BoardFacts($Docs, [string]$Repo, [string]$MainRoot, [datetime]$Now, [int]$MergedDays) {
    $since = $Now.AddDays(-$MergedDays).ToString('yyyy-MM-dd')
    $openRaw = Invoke-GhJson @('pr', 'list', '--repo', $Repo, '--state', 'open', '--limit', '300', '--json', 'number,title,headRefName,body,isDraft,createdAt,author,closingIssuesReferences')
    $mergedRaw = Invoke-GhJson @('pr', 'list', '--repo', $Repo, '--state', 'merged', '--search', "merged:>=$since", '--limit', '300', '--json', 'number,title,headRefName,body,mergedAt,author,closingIssuesReferences')
    $closedRaw = Invoke-GhJson @('issue', 'list', '--repo', $Repo, '--state', 'closed', '--search', "closed:>=$since", '--limit', '300', '--json', 'number,closedAt,stateReason')
    $open = @($openRaw | ForEach-Object { $f = ConvertTo-PrFact $_; $f.state = 'OPEN'; $f })
    $merged = @($mergedRaw | ForEach-Object { $f = ConvertTo-PrFact $_; $f.state = 'MERGED'; $f })
    $closedIssues = @{}
    foreach ($i in $closedRaw) { $closedIssues[[string]$i.number] = @{ closedAt = [string]$i.closedAt; reason = [string]$i.stateReason } }
    # The issue of every open flow front, whatever its age: the 14-day list alone would leave old closures open.
    foreach ($d in $Docs.Values | Where-Object { $_.Collection -eq 'flow' -and [string]$_.Data.status -notin 'merged', 'closed' -and $_.Data.issue }) {
        $n = [string]$d.Data.issue
        if ($closedIssues.ContainsKey($n)) { continue }
        $v = (Invoke-GhJson @('issue', 'view', $n, '--repo', $Repo, '--json', 'state,stateReason,closedAt'))[0]
        if ($v -and [string]$v.state -eq 'CLOSED') { $closedIssues[$n] = @{ closedAt = [string]$v.closedAt; reason = [string]$v.stateReason } }
    }

    # Cards and fronts that name a PR which is in neither list (older than the window, or closed unmerged).
    $known = @(($open + $merged) | ForEach-Object { $_.number })
    $extra = @{}
    $wanted = [System.Collections.Generic.SortedSet[int]]::new()
    foreach ($d in $Docs.Values) {
        if ($d.Collection -notin 'work', 'flow' -or -not $d.Data.pr) { continue }
        if ($d.Collection -eq 'work' -and (Test-CardTerminal $d.Data)) { continue }
        if ($d.Collection -eq 'flow' -and [string]$d.Data.status -in 'merged', 'closed') { continue }
        if ([int]$d.Data.pr -notin $known) { [void]$wanted.Add([int]$d.Data.pr) }
    }
    foreach ($n in $wanted) {
        $p = (Invoke-GhJson @('pr', 'view', "$n", '--repo', $Repo, '--json', 'number,title,headRefName,body,isDraft,createdAt,mergedAt,state,author,closingIssuesReferences'))[0]
        if ($p) { $extra[[string]$n] = ConvertTo-PrFact $p }
    }

    # Worktrees under .claude/worktrees with the commits they hold ahead of origin/main.
    $worktrees = @()
    $path = $null; $branch = $null; $prunable = $false
    $flush = {
        if ($path -and $path -match '[\\/]\.claude[\\/]worktrees[\\/]' -and -not $prunable) {
            $ahead = 0
            $global:LASTEXITCODE = 0
            $n = (git -C $path rev-list --count origin/main..HEAD 2>$null) -join ''
            if ($LASTEXITCODE -eq 0 -and $n -match '^\d+$') { $ahead = [int]$n }
            $script:worktreeList += , @{ name = (Split-Path -Leaf $path); path = $path; branch = $branch; ahead = $ahead }
        }
    }
    $script:worktreeList = @()
    foreach ($line in @(git -C $MainRoot worktree list --porcelain) + @('')) {
        if ($line -match '^worktree (.+)$') { $path = $Matches[1]; $branch = $null; $prunable = $false }
        elseif ($line -match '^branch refs/heads/(.+)$') { $branch = $Matches[1] }
        elseif ($line -match '^prunable') { $prunable = $true }
        elseif ($line -eq '') { & $flush; $path = $null }
    }
    $worktrees = $script:worktreeList

    # Audits: the open ones (#2234: artifacts/knowledge/open-audits/<n>.json, one per open audit, plus a pre-#2234
    # current-audit.json not converted yet), and the history.
    $kn = Join-Path $MainRoot 'artifacts' 'knowledge'
    $openAudits = [System.Collections.Generic.List[object]]::new()
    $openFiles = @()
    $od = Join-Path $kn 'open-audits'
    if (Test-Path -LiteralPath $od) { $openFiles += @(Get-ChildItem -LiteralPath $od -Filter '*.json' -File | Sort-Object Name | ForEach-Object { $_.FullName }) }
    $cp = Join-Path $kn 'current-audit.json'
    if (Test-Path -LiteralPath $cp) { $openFiles += $cp }
    foreach ($file in $openFiles) {
        $state = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json -AsHashtable
        if (-not @($openAudits | Where-Object { [int]$_.issue -eq [int]$state.issue }).Count) { $openAudits.Add($state) }
    }
    $openAudits = @($openAudits | Sort-Object { [int]$_.issue })
    $progress = @()
    $pp = Join-Path $kn 'progress.csv'
    if (Test-Path -LiteralPath $pp) { $progress = @(Import-Csv -LiteralPath $pp) }
    $titles = @{}
    $needTitle = @(@($progress | Where-Object { $_.status -eq 'done' } | ForEach-Object { [int]$_.issue }) + @($openAudits | ForEach-Object { [int]$_.issue }) |
            Where-Object { $_ -and -not $Docs.ContainsKey("audits/$_") } | Select-Object -Unique)
    foreach ($n in $needTitle) {
        $t = (Invoke-GhJson @('issue', 'view', "$n", '--repo', $Repo, '--json', 'title'))[0]
        if ($t) { $titles[[string]$n] = [string]$t.title }
    }

    return @{
        OpenPrs = $open; MergedPrs = $merged; ExtraPrs = $extra; ClosedIssues = $closedIssues
        Worktrees = $worktrees; OpenAudits = $openAudits; Progress = $progress; Titles = $titles
    }
}

# ---------------------------------------------------------------- the rules

# The PR a work card or flow front is about: by its pr number, else by the issues it covers (every issue of the
# card must be closed by the PR, so a card that groups several issues is not finished by a PR for one of them).
# A merged PR that does not close every covered issue (a plan or docs PR "Refs #n", or a PR for one issue of a
# group) does not finish the work: the issue search then looks for the implementation PR. Only a plan card
# ($IsPlan) is finished by its own merged plan PR. A closed-unmerged explicit PR also falls back to the search.
function Find-PrFor($Facts, $Pr, [int[]]$Issues, [bool]$IsPlan = $false) {
    $all = @($Facts.OpenPrs) + @($Facts.MergedPrs)
    $hit = $null
    if ($Pr) {
        $hit = $all | Where-Object { $_.number -eq [int]$Pr } | Select-Object -First 1
        if (-not $hit) { $hit = $Facts.ExtraPrs[[string][int]$Pr] }
        if ($hit -and $hit.state -eq 'OPEN') { return $hit }
        $closesAll = $hit -and ($Issues.Count -eq 0 -or @($Issues | Where-Object { $_ -notin $hit.closes }).Count -eq 0)
        if ($hit -and $hit.state -eq 'MERGED' -and ($closesAll -or $IsPlan)) { return $hit }
    }
    if ($Issues.Count -gt 0) {
        # Prefer an open PR (work in progress) over an older merged one.
        foreach ($set in @($Facts.OpenPrs), @($Facts.MergedPrs | Sort-Object { $_.mergedAt } -Descending)) {
            $found = $set | Where-Object { $p = $_; @($Issues | Where-Object { $_ -notin $p.closes }).Count -eq 0 } | Select-Object -First 1
            if ($found) { return $found }
        }
    }
    if ($hit -and $hit.state -eq 'CLOSED') { return $hit }
    return $null
}

function Test-PlanCard($Card) { [string]$Card.kind -in 'plan', 'docs' -or [string]$Card.title -match '(?i)\bplan\b' }

function Add-Note($Data, [string]$Text) {
    $note = [string]$Data.note
    if ($note.Contains($Text)) { return }
    $Data.note = if ($note) { "$note $Text" } else { $Text }
}

function Update-WorkCard($Data, $Facts, [string]$NowText) {
    $Now = ([datetime]$NowText).ToUniversalTime()
    $c = Copy-Data $Data
    if (Test-CardTerminal $c) { return $c }
    $issues = @($c.issues | ForEach-Object { [int]$_ })
    $pr = Find-PrFor $Facts $c.pr $issues (Test-PlanCard $c)
    $wt = $null
    if ($c.worktree) { $wt = $Facts.Worktrees | Where-Object { $_.name -eq $c.worktree } | Select-Object -First 1 }
    if ($pr) {
        if ($c.pr -ne $pr.number) { $c.pr = $pr.number }
        if ($pr.state -eq 'MERGED') {
            $c.status = 'merged'
            $c.endedUtc = $pr.mergedAt
        }
        elseif ($pr.state -eq 'OPEN') {
            # Only a card in flight follows its PR: a hand-set blocked or stopped card stays as set. A draft PR
            # is work in progress (queued -> running; running and pr-open stay).
            if ($c.status -in 'queued', 'running', 'pr-open') {
                if ($pr.isDraft) { if ($c.status -eq 'queued') { $c.status = 'running' } }
                else { $c.status = 'pr-open' }
                $c.endedUtc = $null
            }
        }
        elseif ($c.status -in 'pr-open', 'running', 'queued') {
            $c.status = 'stopped'
            $c.endedUtc = $NowText
            Add-Note $c "Reconciler: PR #$($pr.number) was closed without merging."
        }
        return $c
    }
    if ($wt -and $wt.ahead -gt 0 -and $c.status -in 'queued', 'running') {
        $c.status = 'running'
        $c.endedUtc = $null
        if (-not $c.startedUtc) { $c.startedUtc = $NowText }
        return $c
    }
    # Only after two hours: a freshly spawned worker may not have its worktree yet.
    $stale = -not $c.startedUtc -or ([datetime]$c.startedUtc).ToUniversalTime() -lt $Now.AddHours(-2)
    if ($c.status -eq 'running' -and $c.kind -eq 'worker' -and -not $wt -and $stale) {
        $c.status = 'stopped'
        $c.endedUtc = $NowText
        Add-Note $c 'Reconciler: no worktree and no PR found, marked stopped.'
    }
    return $c
}

function Update-FlowFront($Data, $Facts, [string]$NowText) {
    $f = Copy-Data $Data
    if ([string]$f.status -in 'merged', 'closed') { return $f }
    $pr = Find-PrFor $Facts $f.pr @([int]$f.issue)
    $closed = $Facts.ClosedIssues[[string]$f.issue]
    if ($pr -and $pr.state -eq 'MERGED') {
        $f.status = 'merged'; $f.stage = 'done'; $f.pr = $pr.number; $f.updatedUtc = $pr.mergedAt
    }
    elseif ($pr -and $pr.state -eq 'OPEN' -and $pr.isDraft) {
        # A draft PR is still being implemented: only record the PR.
        if ($f.pr -ne $pr.number) { $f.pr = $pr.number }
    }
    elseif ($pr -and $pr.state -eq 'OPEN') {
        if ($f.status -ne 'in-progress' -or $f.stage -ne 'review' -or $f.pr -ne $pr.number) {
            $f.status = 'in-progress'; $f.stage = 'review'; $f.pr = $pr.number; $f.updatedUtc = $NowText
        }
    }
    elseif ($closed) {
        # The issue closed without a PR that finishes the front (any age): closed, or not-planned.
        $f.status = 'closed'; $f.stage = $(if ($closed.reason -match '(?i)not_?planned') { 'not-planned' } else { 'close-out' }); $f.updatedUtc = $closed.closedAt
    }
    elseif ($pr -and $pr.state -eq 'CLOSED' -and $f.status -ne 'stopped') {
        $f.status = 'stopped'; $f.updatedUtc = $NowText
        Add-Note $f "Reconciler: PR #$($pr.number) was closed without merging."
    }
    return $f
}

function New-WorkCard($Pr, $Facts) {
    $wt = $Facts.Worktrees | Where-Object { $_.branch -eq $Pr.head } | Select-Object -First 1
    @{
        agent = 'issue-worker'; endedUtc = $null; issues = @($Pr.closes); kind = 'worker'
        note = "Card created by the board reconciler from PR #$($Pr.number)."
        pr = $Pr.number; startedUtc = $Pr.createdAt; status = 'pr-open'; title = $Pr.title
        worktree = $(if ($wt) { $wt.name } else { $null })
    }
}

function Update-Audit($Data, [string]$Id, $Facts) {
    $a = Copy-Data $Data
    $row = $Facts.Progress | Where-Object { [string]$_.issue -eq $Id } | Select-Object -First 1
    if ($row -and $row.status -eq 'done' -and $a.status -ne 'closed') { $a.status = 'closed'; $a.stage = 'done' }
    return $a
}

# The open audit numbered $N (#2234: several may be open), or $null.
function Get-FactsOpenAudit($Facts, [int]$N) {
    return @($Facts.OpenAudits | Where-Object { [int]$_.issue -eq $N }) | Select-Object -First 1
}

function New-Audit([string]$Id, $Facts, [bool]$IsOpen) {
    $row = $Facts.Progress | Where-Object { [string]$_.issue -eq $Id } | Select-Object -First 1
    $title = if ($Facts.Titles.ContainsKey($Id)) { $Facts.Titles[$Id] } else { "Audit #$Id" }
    # Nothing is invented: outcome and pipeline come from progress.csv columns of those names, else stay null.
    $columns = if ($row) { @($row.PSObject.Properties.Name) } else { @() }
    $a = @{ issue = [int]$Id; title = $title; opened = @(); outcome = $(if ('outcome' -in $columns) { $row.outcome } else { $null }); pipeline = $(if ('pipeline' -in $columns) { $row.pipeline } else { $null }); note = ''; blockedBy = @() }
    if ($IsOpen) { $a.status = 'open'; $a.stage = 'running'; $a.verdict = $null; $a.openedUtc = [string](Get-FactsOpenAudit $Facts ([int]$Id)).startedUtc }
    else {
        $a.status = 'closed'; $a.stage = 'done'
        $counts = "progress.csv: blockers $($row.findings_blocker), majors $($row.findings_major), minors $($row.findings_minor), remediation issues opened $($row.remediation_opened)."
        $a.note = "Created by the board reconciler. $counts $($row.notes)".Trim()
    }
    return $a
}

function Get-StatusText($Cards, $Facts, [string]$Existing, [datetime]$Now) {
    $openIds = @($Facts.OpenAudits | ForEach-Object { "#$([int]$_.issue)" })
    $audit = if ($openIds.Count -eq 1) { "Audit $($openIds[0]) open." } elseif ($openIds.Count -gt 1) { "Audits $($openIds -join ' ') open." } else {
        $last = @($Facts.Progress | Where-Object { $_.status -eq 'done' } | ForEach-Object { [int]$_.issue } | Sort-Object)[-1]
        "No audit open$(if ($last) { " (last closed: #$last)" })." }
    $prs = @($Facts.OpenPrs | Sort-Object { $_.number } | ForEach-Object { "#$($_.number)$(if ($_.isDraft) { ' (draft)' })" })
    $cut = $Now.AddHours(-48)
    $recent = @($Facts.MergedPrs | Where-Object { $_.mergedAt -and ([datetime]$_.mergedAt).ToUniversalTime() -ge $cut } | Sort-Object { $_.number } | ForEach-Object { "#$($_.number)" })
    if ($recent.Count -gt 12) { $recent = @($recent | Select-Object -Last 12) + @("(+$($recent.Count - 12) older)") }
    $count = { param($s) @($Cards | Where-Object { [string]$_.status -eq $s }).Count }
    $text = "$audit Open PRs: $(if ($prs) { $prs -join ' ' } else { 'none' }). Merged last 48h: $(if ($recent) { $recent -join ' ' } else { 'none' }). Cards: $(& $count 'running') running, $(& $count 'pr-open') pr-open, $(& $count 'queued') queued, $(& $count 'blocked') blocked."
    # Hand-written text survives behind " Notes: ": kept as is when the marker exists; on the first run (an
    # existing status that is not one of ours) the whole old text moves behind the marker.
    $i = $Existing.IndexOf(' Notes: ')
    if ($i -ge 0) { $text += $Existing.Substring($i) }
    elseif ($Existing.Trim() -and $Existing -notmatch '\sCards: \d+ running, \d+ pr-open, \d+ queued, \d+ blocked\.$') { $text += " Notes: $($Existing.Trim())" }
    return $text
}

# Returns the changed documents: @{ Collection; Id; Data; Exists }, deterministic order.
function Get-BoardChanges($Docs, $Facts, [datetime]$Now) {
    $nowText = Get-Utc $Now
    $changes = [System.Collections.Generic.List[object]]::new()
    $cards = @{}
    $add = {
        param($coll, $id, $data, $existing)
        $key = "$coll/$id"
        $exists = $null -ne $existing
        if (-not $exists -or (ConvertTo-CanonicalJson $existing) -ne (ConvertTo-CanonicalJson $data)) {
            $changes.Add(@{ Collection = $coll; Id = $id; Data = $data; Exists = $exists; Old = $existing })
        }
    }
    foreach ($d in ($Docs.Values | Where-Object { $_.Collection -eq 'work' } | Sort-Object { $_.Id })) {
        $new = Update-WorkCard $d.Data $Facts $nowText
        $cards[$d.Id] = $new
        & $add 'work' $d.Id $new $d.Data
    }
    # New cards: open, non-bot PRs that close an issue and no card covers.
    foreach ($pr in ($Facts.OpenPrs | Sort-Object { $_.number })) {
        if ($pr.isBot -or $pr.closes.Count -eq 0) { continue }
        $covered = $cards.Values | Where-Object { [int]$_.pr -eq $pr.number -or (@($_.issues | ForEach-Object { [int]$_ }) | Where-Object { $_ -in $pr.closes }) }
        if ($covered) { continue }
        $id = [string]$pr.closes[0]
        if ($Docs.ContainsKey("work/$id") -or $cards.ContainsKey($id)) { $id = "pr-$($pr.number)" }
        $card = New-WorkCard $pr $Facts
        $cards[$id] = $card
        & $add 'work' $id $card $null
    }
    foreach ($d in ($Docs.Values | Where-Object { $_.Collection -eq 'flow' } | Sort-Object { [int]$_.Id })) {
        & $add 'flow' $d.Id (Update-FlowFront $d.Data $Facts $nowText) $d.Data
    }
    $auditIds = [System.Collections.Generic.SortedSet[int]]::new()
    foreach ($d in $Docs.Values | Where-Object { $_.Collection -eq 'audits' }) { [void]$auditIds.Add([int]$d.Id) }
    foreach ($r in $Facts.Progress | Where-Object { $_.status -eq 'done' }) { [void]$auditIds.Add([int]$r.issue) }
    foreach ($o in @($Facts.OpenAudits)) { [void]$auditIds.Add([int]$o.issue) }
    foreach ($n in $auditIds) {
        $id = [string]$n
        $openAudit = Get-FactsOpenAudit $Facts $n
        $isOpen = $null -ne $openAudit
        if ($Docs.ContainsKey("audits/$id")) {
            $a = Update-Audit $Docs["audits/$id"].Data $id $Facts
            if ($isOpen -and $a.status -ne 'open') { $a.status = 'open'; $a.stage = 'running'; $a.openedUtc = [string]$openAudit.startedUtc }
            & $add 'audits' $id $a $Docs["audits/$id"].Data
        }
        else { & $add 'audits' $id (New-Audit $id $Facts $isOpen) $null }
    }
    if ($Docs.ContainsKey('meta/board')) {
        $m = Copy-Data $Docs['meta/board'].Data
        $status = Get-StatusText $cards.Values $Facts ([string]$m.status) $Now
        if ($status -ne [string]$m.status) { $m.status = $status; $m.updatedUtc = $nowText }
        # #2234: `current` keeps the lowest open audit for the existing board view; `openAudits` lists them all.
        $openNumbers = @($Facts.OpenAudits | ForEach-Object { [int]$_.issue } | Sort-Object)
        $m.current = $(if ($openNumbers.Count -gt 0) { $openNumbers[0] } else { $null })
        $m.openAudits = $openNumbers
        & $add 'meta' 'board' $m $Docs['meta/board'].Data
    }
    return $changes
}

# Pins every write to an existing document with its version, skips the unpinnable ones, splits the batches.
function ConvertTo-BatchFiles($Changes, $VersionMap, [string]$CreateOp, [string]$UpdateOp, [int]$MaxWrites) {
    $writes = [System.Collections.Generic.List[object]]::new()
    $skipped = [System.Collections.Generic.List[string]]::new()
    foreach ($c in $Changes) {
        $key = "$($c.Collection)/$($c.Id)"
        if ($c.Exists) {
            if (-not $VersionMap.ContainsKey($key)) { $skipped.Add($key); continue }
            # "update" merges fields into the stored document, so only the changed top-level fields are sent.
            $delta = [ordered]@{}
            foreach ($k in $c.Data.Keys) {
                if (-not $c.Old.ContainsKey($k) -or (ConvertTo-CanonicalJson $c.Old[$k]) -ne (ConvertTo-CanonicalJson $c.Data[$k])) { $delta[$k] = $c.Data[$k] }
            }
            $writes.Add([ordered]@{ op = $UpdateOp; collection = $c.Collection; doc_id = $c.Id; if_version = [int]$VersionMap[$key]; data = $delta })
        }
        else { $writes.Add([ordered]@{ op = $CreateOp; collection = $c.Collection; doc_id = $c.Id; data = $c.Data }) }
    }
    $files = @()
    for ($i = 0; $i -lt $writes.Count; $i += $MaxWrites) {
        $files += , @($writes.GetRange($i, [Math]::Min($MaxWrites, $writes.Count - $i)))
    }
    return @{ Files = $files; Skipped = $skipped; Count = $writes.Count }
}

# ---------------------------------------------------------------- main

if ($MyInvocation.InvocationName -ne '.') {
    foreach ($p in 'CurrentDir', 'Versions') { if (-not (Get-Variable $p -ValueOnly)) { throw "-$p is required" } }
    if (-not $DryRun -and -not $Out) { throw '-Out is required (or pass -DryRun to list the drift without writing anything)' }
    if (-not (Test-Path -LiteralPath $Versions)) { throw "versions sidecar not found: $Versions (the caller writes it from the ArtifactData list results)" }
    if (-not $MainRoot) {
        $common = (git rev-parse --path-format=absolute --git-common-dir).Trim()
        $MainRoot = Split-Path -Parent $common
    }
    $now = if ($NowUtc) { ([datetime]$NowUtc).ToUniversalTime() } else { [datetime]::UtcNow }
    if ($Out) { $Out = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Out) }
    # An incomplete export would make existing documents look new (and be replaced unpinned): abort first.
    $missing = @('work', 'flow', 'audits') | Where-Object { -not (Test-Path -LiteralPath (Join-Path $CurrentDir $_) -PathType Container) }
    if (-not (Test-Path -LiteralPath (Join-Path $CurrentDir 'meta' 'board.json'))) { $missing += 'meta/board.json' }
    if ($missing) { throw "incomplete board export in ${CurrentDir}: missing $($missing -join ', '); no writes produced" }
    $docs = Read-BoardExport $CurrentDir
    $versionMap = Get-Content -LiteralPath $Versions -Raw | ConvertFrom-Json -AsHashtable
    $facts = Get-BoardFacts $docs $Repo $MainRoot $now $MergedDays
    $changes = Get-BoardChanges $docs $facts $now
    if ($DryRun) {
        if ($Out) { 'DRY-RUN: -Out ignored' }
        $wouldSkip = 0
        foreach ($c in $changes) {
            $fields = if ($c.Exists) { @($c.Data.Keys | Where-Object { -not $c.Old.ContainsKey($_) -or (ConvertTo-CanonicalJson $c.Old[$_]) -ne (ConvertTo-CanonicalJson $c.Data[$_]) }) -join ', ' } else { 'new' }
            $mark = ''
            if ($c.Exists -and -not $versionMap.ContainsKey("$($c.Collection)/$($c.Id)")) { $mark = ' (would skip: no version)'; $wouldSkip++ }
            "DRIFT $($c.Collection)/$($c.Id): $fields$mark"
        }
        "DRY-RUN: $(@($changes).Count) documents drift ($wouldSkip would be skipped), nothing written"
        return
    }
    $batch = ConvertTo-BatchFiles $changes $versionMap $CreateOp $UpdateOp $MaxWrites
    foreach ($s in $batch.Skipped) { [Console]::Error.WriteLine("WARN: $s changed but has no version in the sidecar; skipped (never written unpinned).") }
    $outDir = Split-Path -Parent $Out
    if ($outDir -and -not (Test-Path -LiteralPath $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    # Remove the files of an earlier run so only this run's batches remain.
    $stem = [IO.Path]::GetFileNameWithoutExtension($Out)
    $ext = [IO.Path]::GetExtension($Out)
    foreach ($old in Get-ChildItem -LiteralPath $outDir -File -ErrorAction SilentlyContinue | Where-Object { $_.Name -eq "$stem$ext" -or $_.Name -match "^$([regex]::Escape($stem))-\d{3}$([regex]::Escape($ext))$" }) { Remove-Item -LiteralPath $old.FullName -Force }
    $paths = @()
    for ($i = 0; $i -lt $batch.Files.Count; $i++) {
        $path = if ($batch.Files.Count -eq 1) { $Out } else { Join-Path $outDir ("{0}-{1:D3}{2}" -f $stem, ($i + 1), $ext) }
        [IO.File]::WriteAllText($path,(ConvertTo-Json -InputObject $batch.Files[$i] -Depth 20), [Text.UTF8Encoding]::new($false))
        $paths += $path
    }
    "WRITES $($batch.Count) SKIPPED $($batch.Skipped.Count)"
    $paths
}
