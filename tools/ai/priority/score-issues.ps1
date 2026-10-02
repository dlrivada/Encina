# tools/ai/priority/score-issues.ps1 (#1552)
#
# Scores every ranked open issue against the 8 weighted criteria and writes
# artifacts/priority/scores.json (full) and artifacts/priority/board.json (trimmed, for the
# control board). Two modes:
#
#   -All                Full recompute: refreshes artifacts/priority/issues.json via
#                        collect-issues.ps1 (unless -NoCollect), scores every 'ranked' issue, and
#                        replaces scores.json/board.json.
#   -Issue <n>           Single-issue mode for a newly opened issue: fetches issue <n> with gh,
#                        scores it alone, and merges/re-ranks it into the existing scores.json
#                        (which must already exist from a previous -All run).
#
# Other switches:
#   -Resume              Skip re-scoring an issue whose body hash matches its previous scores.json
#                         entry; carry the previous scores forward instead.
#   -Limit <k>            Score only the first k ranked issues (collection order) — for samples.
#   -NoCollect            Skip the collect-issues.ps1 refresh in -All mode; reuse the cached
#                         issues.json (for tests and repeated sample runs against the same data).
#
# Usage:
#   pwsh -NoProfile -File tools/ai/priority/score-issues.ps1 -All [-Limit 15] [-Resume] [-NoCollect]
#   pwsh -NoProfile -File tools/ai/priority/score-issues.ps1 -Issue 1552

[CmdletBinding(DefaultParameterSetName = 'All')]
param(
    [Parameter(ParameterSetName = 'All')]
    [switch]$All,

    [Parameter(ParameterSetName = 'Single', Mandatory)]
    [int]$Issue,

    [Parameter(ParameterSetName = 'All')]
    [switch]$Resume,

    [Parameter(ParameterSetName = 'All')]
    [int]$Limit = 0,

    [Parameter(ParameterSetName = 'All')]
    [switch]$NoCollect,

    [string]$Repo = 'dlrivada/Encina',
    [string]$Root = (git rev-parse --show-toplevel),
    [string]$IssuesFile,
    [string]$ScoresFile,
    [string]$BoardFile,
    [string]$OverridesFile,
    [string]$RubricFile
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_priority-lib.ps1')

function Get-BodyHash([string]$Body) {
    $bytes = [System.Text.Encoding]::UTF8.GetBytes([string]$Body)
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { return [Convert]::ToHexString($sha.ComputeHash($bytes)) } finally { $sha.Dispose() }
}

function Get-PriorityOverrides {
    if (Test-Path $OverridesFile) {
        $raw = Get-Content $OverridesFile -Raw
        if ([string]::IsNullOrWhiteSpace($raw)) { return @() }
        return @(ConvertFrom-Json -InputObject $raw)
    }
    return @()
}

function Invoke-JudgedScores {
    <#
        .SYNOPSIS
        One local-model call for importance/regulatory/transversality/method/effort, retried once
        on invalid JSON. Returns @{ Parsed = <object-or-$null>; Error = <string-or-$null> }.
    #>
    param([Parameter(Mandatory)]$IssueRecord)

    $bodyExcerpt = [string]$IssueRecord.body
    if ($bodyExcerpt.Length -gt 4000) { $bodyExcerpt = $bodyExcerpt.Substring(0, 4000) }
    $labels = ($IssueRecord.labels -join ', ')
    $milestone = if ($IssueRecord.milestone) { $IssueRecord.milestone } else { '(none)' }
    $inputText = "Title: $($IssueRecord.title)`nLabels: $labels`nMilestone: $milestone`n`nBody (first 4000 chars):`n$bodyExcerpt"
    $inputFile = Join-Path $modelInDir "$($IssueRecord.number).md"
    Set-Content -Path $inputFile -Value $inputText -Encoding utf8

    $outFile = Join-Path $modelOutDir "$($IssueRecord.number).json"
    $ledgerFile = Join-Path $Root 'artifacts/local-ai/ledger.csv'
    $lastError = $null
    for ($attempt = 1; $attempt -le 2; $attempt++) {
        # Any exception in this per-issue read/parse path (the model call, Get-Content on its
        # output, or JSON parsing) must not abort the whole -All batch: caught here, it is treated
        # exactly like an invalid reply — retried once, then the issue is marked unscored with the
        # error while the batch continues to the next issue.
        try {
            $log = & dotnet run $localAiScript -- --task "priority-$($IssueRecord.number)" --brief $RubricFile --input $inputFile --out $outFile --ledger $ledgerFile --max-tokens 1024 2>&1
            if ($LASTEXITCODE -eq 3) {
                [Console]::Error.WriteLine("local model switched off and the stand-in is unavailable ($($log -join ' ')); stopping the whole run (#1593)")
                exit 3
            }
            if ($LASTEXITCODE -ne 0) {
                $lastError = "local-ai-ask.cs exited $LASTEXITCODE`: $log"
                continue
            }
            $raw = Get-Content $outFile -Raw
            $parsed = ConvertFrom-PriorityModelJson -Text $raw
            if (Test-PriorityModelScores -Parsed $parsed) {
                return @{ Parsed = $parsed; Error = $null }
            }
            $lastError = "invalid or incomplete JSON from the model (attempt $attempt): $($raw.Substring(0, [Math]::Min(200, $raw.Length)))"
        }
        catch {
            $lastError = "exception in the per-issue model read/parse path (attempt $attempt): $($_.Exception.Message)"
        }
    }
    return @{ Parsed = $null; Error = $lastError }
}

function New-ScoredEntry {
    <#
        .SYNOPSIS
        Builds one scored entry from a fresh issue record. Every field is recomputed from
        $IssueRecord (title, milestone, flags, unblocking, fit, age, the effort checkbox and
        overrides) except the five model-judged criteria, which are taken from $PreviousJudged
        when it is supplied (#1560's -Resume fix).

        .PARAMETER PreviousJudged
        The previous scores.json entry for this same issue number, only when its bodyHash matched
        the current body and it was not itself 'unscored' (the caller decides that; this function
        does not compare hashes). When supplied: importance/regulatory/transversality/method are
        always reused verbatim (score, why, source) — no model call for them. Effort is reused only
        when $PreviousJudged.source.effort is 'model'; the effort checkbox (recomputed fresh below)
        still wins over any reused value; and if effort is neither ticked nor previously model-
        sourced (it was 'override' or 'checkbox'), the pre-override/pre-checkbox model value is not
        retained anywhere in the previous entry, so a fresh model call is needed for effort alone.
        Pass $null (the default) to always score from the model, matching the pre-#1560 behavior.
    #>
    param(
        [Parameter(Mandatory)]$IssueRecord,
        [array]$Overrides,
        $PreviousJudged
    )
    $flags = @()
    if (-not $IssueRecord.milestone) { $flags += 'needs-milestone' }

    $effortCk = Get-PriorityEffortCheckboxScore -Body $IssueRecord.body

    # A model call is needed unless every judged criterion can come from $PreviousJudged: the four
    # non-effort criteria always can when $PreviousJudged is given; effort can only when the
    # checkbox is unticked AND the previous entry's effort was itself model-sourced.
    $needsModel = $true
    if ($PreviousJudged) {
        $needsModel = (-not $effortCk.Ticked) -and ([string]$PreviousJudged.source.effort -ne 'model')
    }

    $judged = $null
    if ($needsModel) {
        $judged = Invoke-JudgedScores -IssueRecord $IssueRecord
        if ($null -eq $judged.Parsed) {
            return [ordered]@{
                number   = $IssueRecord.number
                title    = $IssueRecord.title
                milestone = $IssueRecord.milestone
                flags    = $flags
                error    = $judged.Error
                unscored = $true
            }
        }
    }

    if ($PreviousJudged) {
        $importanceScore = [double]$PreviousJudged.scores.importance
        $importanceWhy = [string]$PreviousJudged.why.importance
        $importanceSrc = [string]$PreviousJudged.source.importance
        $regulatoryScore = [double]$PreviousJudged.scores.regulatory
        $regulatoryWhy = [string]$PreviousJudged.why.regulatory
        $regulatorySrc = [string]$PreviousJudged.source.regulatory
        $transversalityScore = [double]$PreviousJudged.scores.transversality
        $transversalityWhy = [string]$PreviousJudged.why.transversality
        $transversalitySrc = [string]$PreviousJudged.source.transversality
        $methodScore = [double]$PreviousJudged.scores.method
        $methodWhy = [string]$PreviousJudged.why.method
        $methodSrc = [string]$PreviousJudged.source.method
    }
    else {
        $importanceScore = [double]$judged.Parsed.importance.score
        $importanceWhy = [string]$judged.Parsed.importance.why
        $importanceSrc = 'model'
        $regulatoryScore = [double]$judged.Parsed.regulatory.score
        $regulatoryWhy = [string]$judged.Parsed.regulatory.why
        $regulatorySrc = 'model'
        $transversalityScore = [double]$judged.Parsed.transversality.score
        $transversalityWhy = [string]$judged.Parsed.transversality.why
        $transversalitySrc = 'model'
        $methodScore = [double]$judged.Parsed.method.score
        $methodWhy = [string]$judged.Parsed.method.why
        $methodSrc = 'model'
    }

    if ($effortCk.Ticked) {
        $effortScore = [double]$effortCk.Score
        $effortWhy = 'Effort Estimate checkbox ticked'
        $effortSrc = 'checkbox'
    }
    elseif ($PreviousJudged -and [string]$PreviousJudged.source.effort -eq 'model') {
        $effortScore = [double]$PreviousJudged.scores.effort
        $effortWhy = [string]$PreviousJudged.why.effort
        $effortSrc = 'model'
    }
    else {
        $effortScore = [double]$judged.Parsed.effort.score
        $effortWhy = [string]$judged.Parsed.effort.why
        $effortSrc = 'model'
    }

    # judgedAtUtc records when the five model-judged criteria were last actually produced (by the
    # model, independent of the effort checkbox/override path above), as opposed to scoredAtUtc
    # below, which always reflects this run. A resumed entry keeps the previous judgedAtUtc; a
    # freshly-judged one (no previous entry, or the effort-only re-judge branch above) stamps now.
    # ConvertFrom-Json (reading scores.json back for -Resume) auto-parses an ISO-8601-looking
    # string into a [datetime], so this re-formats through [datetime] regardless of whether the
    # source value arrived as that type or as a plain string (e.g. in this function's own tests).
    $judgedAtUtc = if ($PreviousJudged -and $PreviousJudged.judgedAtUtc) {
        ([datetime]$PreviousJudged.judgedAtUtc).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    }
    elseif ($PreviousJudged -and $PreviousJudged.scoredAtUtc) {
        # Entries scored before #1560 added judgedAtUtc have no such field; scoredAtUtc is the
        # closest approximation available for them.
        ([datetime]$PreviousJudged.scoredAtUtc).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    }
    else {
        (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    }

    $scores = @{
        unblocking     = Get-PriorityUnblockingScore -DependentCount ($IssueRecord.dependents.Count)
        importance     = $importanceScore
        regulatory     = $regulatoryScore
        transversality = $transversalityScore
        method         = $methodScore
        effort         = $effortScore
        fit            = Get-PriorityMilestoneFitScore -MilestoneTitle $IssueRecord.milestone
        age            = Get-PriorityAgeScore -CreatedAtUtc ([datetime]$IssueRecord.createdAt)
    }
    $why = @{
        importance     = $importanceWhy
        regulatory     = $regulatoryWhy
        transversality = $transversalityWhy
        method         = $methodWhy
        effort         = $effortWhy
    }
    $source = @{
        unblocking     = 'deterministic'
        importance     = $importanceSrc
        regulatory     = $regulatorySrc
        transversality = $transversalitySrc
        method         = $methodSrc
        effort         = $effortSrc
        fit            = 'deterministic'
        age            = 'deterministic'
    }

    $applied = Merge-PriorityOverrides -Scores $scores -IssueNumber $IssueRecord.number -Overrides $Overrides
    foreach ($o in $applied) {
        $why[[string]$o.criterion] = [string]$o.reason
        $source[[string]$o.criterion] = 'override'
    }

    return [ordered]@{
        number       = $IssueRecord.number
        title        = $IssueRecord.title
        milestone    = $IssueRecord.milestone
        createdAtUtc = $IssueRecord.createdAt
        flags        = $flags
        total        = Get-PriorityTotal -Scores $scores
        scores       = $scores
        why          = $why
        source       = $source
        bodyHash     = Get-BodyHash -Body $IssueRecord.body
        scoredAtUtc  = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        judgedAtUtc  = $judgedAtUtc
    }
}

function ConvertTo-BoardEntry([hashtable]$Entry) {
    $trimmedWhy = @{}
    if ($Entry.why) {
        foreach ($k in $Entry.why.Keys) {
            $v = [string]$Entry.why[$k]
            if ($v.Length -gt 140) { $v = $v.Substring(0, 140) }
            $trimmedWhy[$k] = $v
        }
    }
    $copy = $Entry.Clone()
    $copy.why = $trimmedWhy
    $copy.Remove('bodyHash')
    return $copy
}

function Get-TopCriteria([hashtable]$Scores, [int]$Take = 2) {
    if (-not $Scores) { return @() }
    $weighted = foreach ($k in $script:PriorityWeights.Keys) {
        [pscustomobject]@{ Criterion = $k; Contribution = $script:PriorityWeights[$k] * [double]$Scores[$k] }
    }
    return @($weighted | Sort-Object Contribution -Descending | Select-Object -First $Take | ForEach-Object { $_.Criterion })
}

# Guards the -All/-Issue execution below so this script can be dot-sourced (invocation name '.')
# for its function definitions alone -- Test-Priority.ps1 does this to exercise New-ScoredEntry's
# -Resume logic (#1560) without a gh call, a collect-issues.ps1 refresh, or a local-model call.
if ($MyInvocation.InvocationName -eq '.') { return }

if (-not $IssuesFile) { $IssuesFile = Join-Path $Root 'artifacts/priority/issues.json' }
if (-not $ScoresFile) { $ScoresFile = Join-Path $Root 'artifacts/priority/scores.json' }
if (-not $BoardFile) { $BoardFile = Join-Path $Root 'artifacts/priority/board.json' }
if (-not $OverridesFile) { $OverridesFile = Join-Path $PSScriptRoot 'overrides.json' }
if (-not $RubricFile) { $RubricFile = Join-Path $PSScriptRoot 'rubric.md' }
$localAiScript = Join-Path $Root 'tools/ai/local-ai-ask.cs'
$modelOutDir = Join-Path $Root 'artifacts/priority/model-out'
$modelInDir = Join-Path $Root 'artifacts/priority/model-in'
New-Item -ItemType Directory -Force -Path $modelOutDir | Out-Null
New-Item -ItemType Directory -Force -Path $modelInDir | Out-Null

$overrides = Get-PriorityOverrides

if ($PSCmdlet.ParameterSetName -eq 'All') {
    if (-not $NoCollect) {
        & (Join-Path $PSScriptRoot 'collect-issues.ps1') -Out $IssuesFile -Repo $Repo | Write-Output
    }
    if (-not (Test-Path $IssuesFile)) { throw "No issues file at '$IssuesFile'; run without -NoCollect first." }
    $data = Get-Content $IssuesFile -Raw | ConvertFrom-Json
    $allIssues = @($data.issues)

    $previous = @{}
    if ($Resume -and (Test-Path $ScoresFile)) {
        $prevData = Get-Content $ScoresFile -Raw | ConvertFrom-Json
        foreach ($r in @($prevData.ranked)) { $previous[[int]$r.number] = $r }
    }

    $epics = New-Object System.Collections.Generic.List[object]
    $post10 = New-Object System.Collections.Generic.List[object]
    $ranked = New-Object System.Collections.Generic.List[object]
    $unscored = New-Object System.Collections.Generic.List[object]

    $rankedCandidates = New-Object System.Collections.Generic.List[object]
    foreach ($i in $allIssues) {
        $class = Get-PriorityIssueClass -Title $i.title -MilestoneTitle $i.milestone
        switch ($class) {
            'epics' { $epics.Add([ordered]@{ number = $i.number; title = $i.title; milestone = $i.milestone }) }
            'post-1.0' { $post10.Add([ordered]@{ number = $i.number; title = $i.title; milestone = $i.milestone }) }
            default { $rankedCandidates.Add($i) }
        }
    }

    if ($Limit -gt 0) { $rankedCandidates = @($rankedCandidates | Select-Object -First $Limit) }

    foreach ($i in $rankedCandidates) {
        $num = [int]$i.number
        $hash = Get-BodyHash -Body $i.body
        $previousJudged = $null
        if ($Resume -and $previous.ContainsKey($num) -and $previous[$num].bodyHash -eq $hash -and -not $previous[$num].unscored) {
            $previousJudged = $previous[$num]
        }
        # New-ScoredEntry already turns a bad model reply into an 'unscored' entry (see
        # Invoke-JudgedScores above); this catches anything else in the per-issue path (a
        # deterministic-score helper throwing on an unexpected value, disk I/O, ...) so one bad
        # issue never aborts the rest of the -All batch.
        try {
            $entry = New-ScoredEntry -IssueRecord $i -Overrides $overrides -PreviousJudged $previousJudged
        }
        catch {
            $entry = [ordered]@{
                number    = $num
                title     = $i.title
                milestone = $i.milestone
                flags     = @()
                error     = "unhandled exception scoring this issue: $($_.Exception.Message)"
                unscored  = $true
            }
        }
        if ($entry.unscored) {
            $unscored.Add($entry)
            Write-Output "issue #$num`: UNSCORED ($($entry.error))"
        }
        else {
            $ranked.Add($entry)
            $resumedNote = if ($previousJudged) { ', resumed judged scores' } else { '' }
            Write-Output "issue #$num`: total=$($entry.total)$resumedNote"
        }
    }

    # .ToArray(), not @($ranked): wrapping a List[object] of hashtables with the @() array
    # subexpression operator throws "Argument types do not match" in this PowerShell version.
    $rankedArr = Get-PriorityRankedList -Items $ranked.ToArray()

    $scoresOut = [ordered]@{
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        weights        = $script:PriorityWeights
        ranked         = $rankedArr
        epics          = $epics.ToArray()
        post10         = $post10.ToArray()
        unscored       = $unscored.ToArray()
    }
    $outDir = Split-Path -Parent $ScoresFile
    if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
    $scoresOut | ConvertTo-Json -Depth 8 | Set-Content -Path $ScoresFile -Encoding utf8

    $boardOut = [ordered]@{
        generatedAtUtc = $scoresOut.generatedAtUtc
        weights        = $scoresOut.weights
        ranked         = @($rankedArr | ForEach-Object { ConvertTo-BoardEntry ([hashtable]$_) })
        epics          = $scoresOut.epics
        post10         = $scoresOut.post10
        unscored       = $scoresOut.unscored
    }
    $boardOut | ConvertTo-Json -Depth 8 | Set-Content -Path $BoardFile -Encoding utf8

    Write-Output "scored $($rankedArr.Count) ranked, $($epics.Count) epics, $($post10.Count) post-1.0, $($unscored.Count) unscored -> $ScoresFile"
}
else {
    # -Issue <n>: single-issue mode for a newly opened issue.
    if (-not (Test-Path $ScoresFile)) { throw "No scores.json at '$ScoresFile'; run -All first." }
    $ghJson = gh issue view $Issue --repo $Repo --json number,title,labels,milestone,createdAt,body 2>&1
    if ($LASTEXITCODE -ne 0) { throw "gh issue view $Issue failed: $ghJson" }
    $ghIssue = $ghJson | ConvertFrom-Json

    $dependents = @()
    if (Test-Path $IssuesFile) {
        $cached = (Get-Content $IssuesFile -Raw | ConvertFrom-Json).issues | Where-Object { [int]$_.number -eq $Issue }
        if ($cached) { $dependents = @($cached.dependents) }
    }

    $issueRecord = [ordered]@{
        number     = $ghIssue.number
        title      = $ghIssue.title
        labels     = @($ghIssue.labels | ForEach-Object { $_.name })
        milestone  = if ($ghIssue.milestone) { $ghIssue.milestone.title } else { $null }
        createdAt  = $ghIssue.createdAt
        body       = $ghIssue.body
        dependents = $dependents
    }

    $class = Get-PriorityIssueClass -Title $issueRecord.title -MilestoneTitle $issueRecord.milestone
    if ($class -ne 'ranked') {
        Write-Output "issue #$Issue is class '$class' — not ranked."
        return
    }

    $entry = New-ScoredEntry -IssueRecord $issueRecord -Overrides $overrides
    $scoresData = Get-Content $ScoresFile -Raw | ConvertFrom-Json
    $rankedList = New-Object System.Collections.Generic.List[object]
    foreach ($r in @($scoresData.ranked)) {
        if ([int]$r.number -ne $Issue) {
            $h = @{}
            $r.psobject.Properties | ForEach-Object { $h[$_.Name] = $_.Value }
            $rankedList.Add($h)
        }
    }
    if ($entry.unscored) {
        Write-Output "issue #$Issue`: UNSCORED ($($entry.error))"
        $unscoredList = New-Object System.Collections.Generic.List[object]
        foreach ($u in @($scoresData.unscored)) { $unscoredList.Add($u) }
        $unscoredList.Add($entry)
        $scoresData | Add-Member -NotePropertyName unscored -NotePropertyValue $unscoredList.ToArray() -Force
    }
    else {
        $rankedList.Add($entry)
    }

    # .ToArray(), not @($rankedList): see the -All branch's identical note above.
    $rankedArr = Get-PriorityRankedList -Items $rankedList.ToArray()
    $scoresOut = [ordered]@{
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        weights        = $script:PriorityWeights
        ranked         = $rankedArr
        epics          = @($scoresData.epics)
        post10         = @($scoresData.post10)
        unscored       = @($scoresData.unscored)
    }
    $scoresOut | ConvertTo-Json -Depth 8 | Set-Content -Path $ScoresFile -Encoding utf8

    $boardOut = [ordered]@{
        generatedAtUtc = $scoresOut.generatedAtUtc
        weights        = $scoresOut.weights
        ranked         = @($rankedArr | ForEach-Object { ConvertTo-BoardEntry ([hashtable]$_) })
        epics          = $scoresOut.epics
        post10         = $scoresOut.post10
        unscored       = $scoresOut.unscored
    }
    $boardOut | ConvertTo-Json -Depth 8 | Set-Content -Path $BoardFile -Encoding utf8

    if (-not $entry.unscored) {
        $mine = $rankedArr | Where-Object { [int]$_.number -eq $Issue }
        $top = Get-TopCriteria -Scores ([hashtable]$mine.scores) -Take 2
        Write-Output "issue #$Issue`: rank $($mine.Rank) of $($rankedArr.Count), total $($mine.Total) (top criteria: $($top -join ', '))"
    }
}
