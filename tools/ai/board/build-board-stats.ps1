<#
.SYNOPSIS
    Builds the maintainer's control board "Week" tab snapshots (#1382).

.DESCRIPTION
    Runs tools/ai/board/session-stats.cs against the local Claude Code transcripts, collects
    GitHub activity (PRs and issues created or closed since -Since) and the local-model ledgers
    from every worktree, merges them with the optional, private session-meta.json and plan.json,
    and writes db-summary.json, db-days.json and db-sessions.json to -OutDir. It never publishes
    anything: the orchestrator uploads the resulting files to the board.

.PARAMETER Since
    ISO date (yyyy-MM-dd) the report starts from. Required.

.PARAMETER OutDir
    Where the outputs (and session-stats.json, github.json, localai.json) are written. Defaults
    to artifacts/board under the repository root.

.PARAMETER ProjectDir
    The Claude Code project folder to read transcripts from. Defaults to the folder Claude Code
    derives from the repository root path (colons and backslashes replaced with a dash), under
    ~/.claude/projects, e.g. D:\Proyectos\Encina -> ~/.claude/projects/D--Proyectos-Encina.

.PARAMETER SessionMeta
    Optional path to a private JSON file of { "<8-char session id prefix>": { title, assessment } }.
    Holds the maintainer's own assessments; never commit its content. When omitted, sessions carry
    no title or assessment.

.PARAMETER Plan
    Optional path to a plan-usage JSON file, copied verbatim into db-summary.json's "plan" field.
    When omitted, "plan" is null.

.EXAMPLE
    pwsh -File tools/ai/board/build-board-stats.ps1 -Since 2026-09-21
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Since,

    [string]$OutDir,

    [string]$ProjectDir,

    [string]$SessionMeta,

    [string]$Plan
)

$ErrorActionPreference = 'Stop'

function Read-Json {
    param([Parameter(Mandatory = $true)][string]$Path)
    Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json -DateKind String
}

$repoRoot = (git -C $PSScriptRoot rev-parse --show-toplevel).Trim()

if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'artifacts\board' }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

# Claude Code transcripts, and the local-model ledgers under .claude/worktrees/*, are keyed by
# the main checkout's path, not a worktree's: in a worktree $repoRoot is the worktree itself, so
# the main checkout is derived from the common .git directory's parent instead.
$gitCommonDir = (git -C $PSScriptRoot rev-parse --path-format=absolute --git-common-dir).Trim()
$mainCheckoutRoot = (Resolve-Path (Join-Path $gitCommonDir '..')).Path.TrimEnd('\')

if (-not $ProjectDir) {
    $claudeProjectName = $mainCheckoutRoot.Replace(':', '-').Replace('\', '-').Replace('/', '-')
    $ProjectDir = Join-Path $HOME ".claude\projects\$claudeProjectName"
}
if (-not (Test-Path -LiteralPath $ProjectDir)) {
    throw "Claude Code project folder not found: $ProjectDir"
}

# 1. Session statistics (active time, prompts, reply latency, tokens per model, agents).
$sinceIsoUtc = "$($Since)T00:00:00Z"
$sessionStatsScript = Join-Path $PSScriptRoot 'session-stats.cs'
$sessionStatsOut = Join-Path $OutDir 'session-stats.json'
dotnet run --file $sessionStatsScript -- $ProjectDir $sinceIsoUtc $sessionStatsOut
if ($LASTEXITCODE -ne 0) { throw "session-stats.cs failed with exit code $LASTEXITCODE" }

# 2. GitHub activity: PRs and issues created or closed since -Since. Every value that flows into
# a gh argument is copied to a plain variable first (a gh argument built from an expression such
# as "$($Since)..." is a known PowerShell/gh parsing trap).
$repoSlug = 'dlrivada/Encina'
$searchCreatedSince = "created:>=$Since"
$searchClosedSince = "closed:>=$Since"

$prsRaw = gh pr list --repo $repoSlug --state all --search $searchCreatedSince --json number,createdAt,mergedAt,state --limit 500 | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "gh pr list failed with exit code $LASTEXITCODE" }
$issuesOpenedRaw = gh issue list --repo $repoSlug --state all --search $searchCreatedSince --json number,createdAt,state --limit 500 | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "gh issue list (opened) failed with exit code $LASTEXITCODE" }
$issuesClosedRaw = gh issue list --repo $repoSlug --state closed --search $searchClosedSince --json number,closedAt --limit 500 | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) { throw "gh issue list (closed) failed with exit code $LASTEXITCODE" }

$prs = @($prsRaw | ForEach-Object { [ordered]@{ number = $_.number; at = $_.createdAt; merged = $_.mergedAt; state = $_.state } })
$issuesOpened = @($issuesOpenedRaw | ForEach-Object { [ordered]@{ number = $_.number; at = $_.createdAt; state = $_.state } })
$issuesClosed = @($issuesClosedRaw | ForEach-Object { [ordered]@{ number = $_.number; at = $_.closedAt } })

$github = [ordered]@{ prs = $prs; issuesOpened = $issuesOpened; issuesClosed = $issuesClosed }
$githubPath = Join-Path $OutDir 'github.json'
$github | ConvertTo-Json -Depth 6 -Compress | Set-Content -Encoding utf8 $githubPath

# 3. Local-model ledgers: every artifacts/local-ai/ledger.csv under the main root and its
# .claude/worktrees/*, aggregated per day (Since is a date, so a string compare against the
# yyyy-MM-dd prefix of each row's timestamp is enough).
$sinceDate = [DateTime]::Parse($Since, [System.Globalization.CultureInfo]::InvariantCulture)
$ledgerPaths = @(Join-Path $mainCheckoutRoot 'artifacts\local-ai\ledger.csv')
$worktreesDir = Join-Path $mainCheckoutRoot '.claude\worktrees'
if (Test-Path -LiteralPath $worktreesDir) {
    $ledgerPaths += Get-ChildItem -LiteralPath $worktreesDir -Directory | ForEach-Object {
        Join-Path $_.FullName 'artifacts\local-ai\ledger.csv'
    }
}

$rows = foreach ($ledgerPath in $ledgerPaths) {
    if (-not (Test-Path -LiteralPath $ledgerPath)) { continue }
    Import-Csv -LiteralPath $ledgerPath
}

# #1593: the local-ai-standin's paid drafts (artifacts/local-ai/standin-ledger.csv, same roots) are their own
# counter (standinCalls, standinTokens, standinCostUsd); they are never added to the free local-model calls.
$standinRows = foreach ($ledgerPath in $ledgerPaths) {
    $standinPath = Join-Path (Split-Path -Parent $ledgerPath) 'standin-ledger.csv'
    if (-not (Test-Path -LiteralPath $standinPath)) { continue }
    Import-Csv -LiteralPath $standinPath
}

function New-LocalDayEntry([string]$Day) {
    return [ordered]@{ day = $Day; calls = 0; promptTokens = 0.0; completion = 0.0; seconds = 0.0; standinCalls = 0; standinTokens = 0.0; standinCostUsd = 0.0 }
}

$localByDay = [ordered]@{}
foreach ($row in $standinRows) {
    $ts = [DateTimeOffset]::Parse($row.timestampUtc, [System.Globalization.CultureInfo]::InvariantCulture)
    if ($ts.UtcDateTime -lt $sinceDate) { continue }
    $day = $ts.ToString('yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture)
    if (-not $localByDay.Contains($day)) { $localByDay[$day] = New-LocalDayEntry $day }
    $entry = $localByDay[$day]
    $entry.standinCalls += 1
    # totalTokens (prompt + completion for CLI rows, the subagent total for agent rows); rows from before that
    # column existed fall back to completionTokens.
    $entry.standinTokens += if ($row.PSObject.Properties['totalTokens'] -and $row.totalTokens) { [double]$row.totalTokens } else { [double]$row.completionTokens }
    if ($row.PSObject.Properties['costUsd'] -and $row.costUsd) { $entry.standinCostUsd += [double]::Parse($row.costUsd, [System.Globalization.CultureInfo]::InvariantCulture) }
}
foreach ($row in $rows) {
    $ts = [DateTimeOffset]::Parse($row.timestampUtc, [System.Globalization.CultureInfo]::InvariantCulture)
    if ($ts.UtcDateTime -lt $sinceDate) { continue }
    $day = $ts.ToString('yyyy-MM-dd', [System.Globalization.CultureInfo]::InvariantCulture)
    if (-not $localByDay.Contains($day)) { $localByDay[$day] = New-LocalDayEntry $day }
    $entry = $localByDay[$day]
    $entry.calls += 1
    $entry.promptTokens += [double]$row.promptTokens
    $entry.completion += [double]$row.completionTokens
    $entry.seconds += [double]$row.seconds
}

$localai = @($localByDay.Values | ForEach-Object {
        $tokPerSec = if ($_.seconds -gt 0) { [math]::Round($_.completion / $_.seconds, 1) } else { $null }
        [ordered]@{ day = $_.day; calls = $_.calls; promptTokens = $_.promptTokens; completion = $_.completion; seconds = $_.seconds; tokPerSec = $tokPerSec; standinCalls = $_.standinCalls; standinTokens = $_.standinTokens; standinCostUsd = [math]::Round($_.standinCostUsd, 4) }
    })
$localaiPath = Join-Path $OutDir 'localai.json'
$localai | ConvertTo-Json -Depth 4 -Compress | Set-Content -Encoding utf8 $localaiPath

# 4. Optional, private inputs.
$meta = if ($SessionMeta) { Read-Json -Path $SessionMeta } else { [pscustomobject]@{} }
$planData = if ($Plan) { Read-Json -Path $Plan } else { $null }

$st = Read-Json -Path $sessionStatsOut
$gh = Read-Json -Path $githubPath
$loc = @(Read-Json -Path $localaiPath)

# 5. db-sessions.json: one row per session, titled and assessed from the optional session-meta.
$sessions = foreach ($s in ($st.sessions | Sort-Object startUtc)) {
    $k = $s.id.Substring(0, 8)
    $m = $meta.$k
    $models = [ordered]@{}
    foreach ($p in $s.models.PSObject.Properties) { $models[$p.Name] = $p.Value }
    $agents = @($s.agents.PSObject.Properties | Sort-Object { $_.Value.total } -Descending | Select-Object -First 5 | ForEach-Object {
            [ordered]@{ name = $_.Name; spawns = $_.Value.spawns; total = $_.Value.total; out = $_.Value.out }
        })
    [ordered]@{
        id = $k; title = $m.title; assessment = $m.assessment
        startUtc = $s.startUtc; endUtc = $s.endUtc; activeMin = $s.activeMin; blocks = $s.blocks
        prompts = $s.prompts; medianReplyMin = $s.medianReplyMin; reviewMin = $s.reviewMin
        awayMin = $s.awayMin; awayCount = $s.awayCount; calls = $s.calls; tools = $s.tools
        spawns = $s.spawns; models = $models; agents = $agents
    }
}
[ordered]@{ sessions = @($sessions) } | ConvertTo-Json -Depth 8 -Compress | Set-Content -Encoding utf8 (Join-Path $OutDir 'db-sessions.json')

# 6. db-days.json: one row per day, tokens by model plus GitHub and local-model activity.
$days = foreach ($d in $st.days) {
    $day = [string]$d.day
    $out = [ordered]@{}
    foreach ($p in $d.models.PSObject.Properties) { $out[$p.Name] = $p.Value.out }
    $l = $loc | Where-Object { $_.day -eq $day }
    [ordered]@{
        day = $day; activeMin = $d.activeMin; reviewMin = $d.reviewMin; awayMin = $d.awayMin
        prompts = $d.prompts; spawns = $d.spawns; firstUtc = $d.firstUtc; lastUtc = $d.lastUtc
        outByModel = $out
        prsOpened = @($gh.prs | Where-Object { ([string]$_.at).StartsWith($day) }).Count
        prsMerged = @($gh.prs | Where-Object { $_.merged -and ([string]$_.merged).StartsWith($day) }).Count
        issuesOpened = @($gh.issuesOpened | Where-Object { ([string]$_.at).StartsWith($day) }).Count
        issuesClosed = @($gh.issuesClosed | Where-Object { ([string]$_.at).StartsWith($day) }).Count
        localCalls = $(if ($l) { $l.calls } else { 0 })
        localTokPerSec = $(if ($l) { $l.tokPerSec } else { $null })
        standinCalls = $(if ($l) { $l.standinCalls } else { 0 })
        standinTokens = $(if ($l) { $l.standinTokens } else { 0 })
    }
}
[ordered]@{ days = @($days) } | ConvertTo-Json -Depth 6 -Compress | Set-Content -Encoding utf8 (Join-Path $OutDir 'db-days.json')

# 7. db-summary.json: totals across the window, tokens per model, agents by total tokens.
$tot = [ordered]@{}
foreach ($s in $st.sessions) {
    foreach ($p in $s.models.PSObject.Properties) {
        $v = $p.Value
        if (-not $tot.Contains($p.Name)) {
            $tot[$p.Name] = [ordered]@{ model = $p.Name; calls = 0; inW = 0; cacheR = 0; out = 0; mainOut = 0; tokps = $null; best = 0 }
        }
        $t = $tot[$p.Name]
        $t.calls += $v.calls; $t.inW += $v.in + $v.cacheW; $t.cacheR += $v.cacheR; $t.out += $v.out; $t.mainOut += $v.mainOut
        if ($v.medianOutTokPerSec -and $v.mainOut -gt $t.best) { $t.tokps = $v.medianOutTokPerSec; $t.best = $v.mainOut }
    }
}
$models = @($tot.Values | ForEach-Object { $_.Remove('best'); $_ } | Sort-Object { $_.out } -Descending)

$ag = [ordered]@{}
foreach ($s in $st.sessions) {
    foreach ($a in $s.agents.PSObject.Properties) {
        if (-not $ag.Contains($a.Name)) { $ag[$a.Name] = [ordered]@{ name = $a.Name; spawns = 0; total = 0; out = 0; models = $a.Value.models } }
        $ag[$a.Name].spawns += $a.Value.spawns; $ag[$a.Name].total += $a.Value.total; $ag[$a.Name].out += $a.Value.out
    }
}

$lc = ($loc | Measure-Object calls -Sum).Sum
$lcomp = ($loc | Measure-Object completion -Sum).Sum
$lsec = ($loc | Measure-Object seconds -Sum).Sum
$sc = ($loc | Measure-Object standinCalls -Sum).Sum
$stok = ($loc | Measure-Object standinTokens -Sum).Sum
$scost = ($loc | Measure-Object standinCostUsd -Sum).Sum

$summary = [ordered]@{
    generatedUtc = $st.generatedUtc
    since = $st.since
    plan = $planData
    totals = [ordered]@{
        sessions = @($st.sessions).Count
        activeMin = ($st.days | Measure-Object activeMin -Sum).Sum
        reviewMin = ($st.days | Measure-Object reviewMin -Sum).Sum
        awayMin = ($st.days | Measure-Object awayMin -Sum).Sum
        prompts = ($st.days | Measure-Object prompts -Sum).Sum
        spawns = ($st.days | Measure-Object spawns -Sum).Sum
        prsCreated = @($gh.prs).Count
        prsMerged = @($gh.prs | Where-Object merged).Count
        prsOpen = @($gh.prs | Where-Object state -eq 'OPEN').Count
        issuesOpened = @($gh.issuesOpened).Count
        issuesOpenedStillOpen = @($gh.issuesOpened | Where-Object state -eq 'OPEN').Count
        issuesClosed = @($gh.issuesClosed).Count
        localCalls = $(if ($lc) { $lc } else { 0 })
        localCompletion = $(if ($lcomp) { $lcomp } else { 0 })
        localTokPerSec = [math]::Round($(if ($lcomp) { $lcomp } else { 0 }) / [math]::Max(1, $(if ($lsec) { $lsec } else { 0 })), 1)
        standinCalls = $(if ($sc) { $sc } else { 0 })
        standinTokens = $(if ($stok) { $stok } else { 0 })
        standinCostUsd = [math]::Round($(if ($scost) { $scost } else { 0 }), 4)
    }
    models = $models
    agents = @($ag.Values | Sort-Object { $_.total } -Descending)
}
$summary | ConvertTo-Json -Depth 6 -Compress | Set-Content -Encoding utf8 (Join-Path $OutDir 'db-summary.json')

Get-ChildItem (Join-Path $OutDir 'db-*.json') | ForEach-Object { "$($_.Name) $($_.Length)" }
