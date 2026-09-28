# tools/ai/priority/Test-Priority.ps1 — self-test for the #1552 priority pipeline's deterministic
# functions. No gh call, no local-model call: everything here runs against canned data.
#
# Usage: pwsh -NoProfile -File tools/ai/priority/Test-Priority.ps1        (exit 1 when any case fails)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_priority-lib.ps1')

$script:passCount = 0
$script:failCount = 0

function Assert-Equal($Expected, $Actual, [string]$Label) {
    $isNum = { param($v) $v -is [double] -or $v -is [int] -or $v -is [decimal] }
    $bothNumeric = (& $isNum $Expected) -and (& $isNum $Actual)
    # Numeric comparison avoids a culture-dependent string round trip: under a comma-decimal culture,
    # [string]100.0 and a naive re-parse of "100.0" as a thousands-grouped integer would disagree.
    $equal = if ($bothNumeric) { [Math]::Abs([double]$Expected - [double]$Actual) -lt 0.0001 } else { [string]$Expected -eq [string]$Actual }
    if ($equal) {
        $script:passCount++
    }
    else {
        $script:failCount++
        Write-Output "FAIL: $Label — expected '$Expected', got '$Actual'"
    }
}

function Assert-True([bool]$Condition, [string]$Label) {
    if ($Condition) { $script:passCount++ }
    else { $script:failCount++; Write-Output "FAIL: $Label" }
}

# --- Age cap logic ---------------------------------------------------------------------------
$now = [datetime]::Parse('2026-09-28T00:00:00Z').ToUniversalTime()
Assert-Equal 0 (Get-PriorityAgeScore -CreatedAtUtc $now -NowUtc $now) 'age: 0 days open scores 0'
Assert-Equal 50 (Get-PriorityAgeScore -CreatedAtUtc $now.AddDays(-90) -NowUtc $now) 'age: 90 of 180 days scores 50'
Assert-Equal 100 (Get-PriorityAgeScore -CreatedAtUtc $now.AddDays(-180) -NowUtc $now) 'age: exactly 180 days scores 100 (cap boundary)'
Assert-Equal 100 (Get-PriorityAgeScore -CreatedAtUtc $now.AddDays(-400) -NowUtc $now) 'age: 400 days open is capped at 100'

# --- Milestone -> fit mapping (#1552 decision 3, every entry) --------------------------------
$fitCases = [ordered]@{
    'v0.14.0'                 = 100
    'v0.15.0'                 = 90
    'v0.16.0'                 = 85
    'v0.17.0'                 = 80
    'v0.18.0'                 = 75
    'v0.19.0'                 = 70
    'v0.20.0'                 = 65
    'v0.21.0'                 = 60
    'v0.22.0'                 = 55
    'v1.0.0-rc.1'             = 50
}
foreach ($k in $fitCases.Keys) {
    Assert-Equal $fitCases[$k] (Get-PriorityMilestoneFitScore -MilestoneTitle $k) "fit: milestone '$k'"
}
# A milestone title with a trailing description still matches on its leading token.
Assert-Equal 100 (Get-PriorityMilestoneFitScore -MilestoneTitle 'v0.14.0 — Hardening') 'fit: milestone with trailing description'
Assert-Equal 40 (Get-PriorityMilestoneFitScore -MilestoneTitle $null) 'fit: no milestone scores 40'
Assert-Equal 40 (Get-PriorityMilestoneFitScore -MilestoneTitle '') 'fit: empty milestone scores 40'
Assert-Equal 40 (Get-PriorityMilestoneFitScore -MilestoneTitle 'v9.9.9-unknown') 'fit: unrecognized milestone falls back to 40'

# --- Effort checkbox parsing: the three template shapes (#1552 decision 8) -------------------
$effortSmall = "## Effort Estimate`n`n- [x] Small (< 1 hour)`n- [ ] Medium (1-4 hours)`n- [ ] Large (> 4 hours)`n`n## Related Issues`n"
$r = Get-PriorityEffortCheckboxScore -Body $effortSmall
Assert-Equal 100 $r.Score 'effort: Small ticked scores 100'
Assert-True $r.Ticked 'effort: Small ticked reports Ticked=true'

$effortMedium = "## Effort Estimate`n`n- [ ] Small (< 1 hour)`n- [X] Medium (1-4 hours)`n- [ ] Large (> 4 hours)`n"
$r = Get-PriorityEffortCheckboxScore -Body $effortMedium
Assert-Equal 60 $r.Score 'effort: Medium ticked (uppercase X) scores 60'

$effortLarge = "## Effort Estimate`n`n- [ ] Small (< 1 hour)`n- [ ] Medium (1-4 hours)`n- [x] Large (> 4 hours)`n"
$r = Get-PriorityEffortCheckboxScore -Body $effortLarge
Assert-Equal 25 $r.Score 'effort: Large ticked scores 25'

$effortUnticked = "## Effort Estimate`n`n- [ ] Small (< 1 hour)`n- [ ] Medium (1-4 hours)`n- [ ] Large (> 4 hours)`n"
$r = Get-PriorityEffortCheckboxScore -Body $effortUnticked
Assert-Equal '' $r.Score 'effort: nothing ticked falls back to judged (Score is null)'
Assert-True (-not $r.Ticked) 'effort: nothing ticked reports Ticked=false'

$effortNoSection = "## Description`n`nSome text with no Effort Estimate section at all.`n"
$r = Get-PriorityEffortCheckboxScore -Body $effortNoSection
Assert-Equal '' $r.Score 'effort: missing section falls back to judged (Score is null)'
Assert-True (-not $r.Ticked) 'effort: missing section reports Ticked=false'

# --- Dependency-reference parsing (#1552 decisions 1, 8) -------------------------------------
$depBody = 'This is blocked by #100 and also Depends on #200. See #300 for background. Requires #400 too. Needs #500. After #600 lands.'
$targets = Get-PriorityDependencyTargets -Body $depBody -SelfNumber 999
Assert-Equal '100,200,400,500,600' ($targets -join ',') 'dependencies: keyword references collected, mere mention (see #300) excluded'

$selfRefBody = "blocked by #999 and blocked by #700"
$targets2 = Get-PriorityDependencyTargets -Body $selfRefBody -SelfNumber 999
Assert-Equal '700' ($targets2 -join ',') 'dependencies: a self-reference is never counted'

$noDeps = Get-PriorityDependencyTargets -Body '' -SelfNumber 1
Assert-Equal 0 $noDeps.Count 'dependencies: empty body yields no targets'

$partOfBody = 'This feature is part of epic #123 and also part of #456.'
$targets3 = Get-PriorityDependencyTargets -Body $partOfBody -SelfNumber 999
Assert-Equal '123,456' ($targets3 -join ',') 'dependencies: "part of epic #n" and "part of #n" are both counted'

$multiNumberBody = 'Blocked by #100, #101 and #102. See #200 elsewhere.'
$targets4 = Get-PriorityDependencyTargets -Body $multiNumberBody -SelfNumber 999
Assert-Equal '100,101,102' ($targets4 -join ',') 'dependencies: every #n in the same clause after one keyword is counted, capped at the sentence end'
Assert-True (-not ($targets4 -contains 200)) 'dependencies: an unrelated later mention (see #200) in the next sentence is not swept into the earlier clause'

$multiLineBody = "Requires #700, #750`nSee #800 on the next line."
$targets5 = Get-PriorityDependencyTargets -Body $multiLineBody -SelfNumber 999
Assert-Equal '700,750' ($targets5 -join ',') 'dependencies: a clause captures every #n up to a newline, and the newline still excludes the next line'

$decimalBody = 'Requires v2.5 fix, see #100 and #101 for details.'
$targets6 = Get-PriorityDependencyTargets -Body $decimalBody -SelfNumber 999
Assert-Equal '100,101' ($targets6 -join ',') 'dependencies: a decimal like "v2.5" does not end the clause early (its period has no space+capital after it)'

$abbreviationBody = 'Depends on e.g. the auth module, see #100 and #101 for the design.'
$targets7 = Get-PriorityDependencyTargets -Body $abbreviationBody -SelfNumber 999
Assert-Equal '100,101' ($targets7 -join ',') 'dependencies: an abbreviation like "e.g." does not end the clause early'

$twoSentenceBody = 'Requires #900. The next sentence mentions #901 with no keyword.'
$targets8 = Get-PriorityDependencyTargets -Body $twoSentenceBody -SelfNumber 999
Assert-Equal '900' ($targets8 -join ',') 'dependencies: a real sentence break (period, space, capital letter) still ends the clause'

Assert-Equal 0 (Get-PriorityUnblockingScore -DependentCount 0) 'unblocking: 0 dependents scores 0'
Assert-Equal 50 (Get-PriorityUnblockingScore -DependentCount 2) 'unblocking: 2 dependents scores 50'
Assert-Equal 100 (Get-PriorityUnblockingScore -DependentCount 4) 'unblocking: 4 dependents scores 100 (cap boundary)'
Assert-Equal 100 (Get-PriorityUnblockingScore -DependentCount 10) 'unblocking: 10 dependents is capped at 100'

# --- Class assignment (#1552 decision 2) ------------------------------------------------------
Assert-Equal 'epics' (Get-PriorityIssueClass -Title '[EPIC] Something big' -MilestoneTitle 'v0.14.0') 'class: [EPIC] title is not ranked'
Assert-Equal 'post-1.0' (Get-PriorityIssueClass -Title '[FEATURE] Later' -MilestoneTitle 'Post-1.0: Someday') 'class: Post-1.0 milestone is not ranked'
Assert-Equal 'ranked' (Get-PriorityIssueClass -Title '[BUG] Fix it' -MilestoneTitle 'v0.14.0') 'class: everything else with a v0.x milestone is ranked'
Assert-Equal 'ranked' (Get-PriorityIssueClass -Title '[BUG] Fix it' -MilestoneTitle $null) 'class: no milestone is ranked (needs-milestone flag handled by the caller)'

# --- Total and rank computation, tie ordering (#1552 decision 5) ----------------------------
$fullScores = @{ unblocking = 100; importance = 100; regulatory = 100; transversality = 100; method = 100; effort = 100; fit = 100; age = 100 }
Assert-Equal 100.0 (Get-PriorityTotal -Scores $fullScores) 'total: all-100 scores sum to weight total / 100 = 100.0'
$zeroScores = @{ unblocking = 0; importance = 0; regulatory = 0; transversality = 0; method = 0; effort = 0; fit = 0; age = 0 }
Assert-Equal 0.0 (Get-PriorityTotal -Scores $zeroScores) 'total: all-0 scores sum to 0.0'
$onlyImportance = @{ unblocking = 0; importance = 100; regulatory = 0; transversality = 0; method = 0; effort = 0; fit = 0; age = 0 }
Assert-Equal 18.0 (Get-PriorityTotal -Scores $onlyImportance) 'total: importance alone contributes its 18-point weight'
$missingCriterion = @{ unblocking = 100 }
Assert-Equal 18.0 (Get-PriorityTotal -Scores $missingCriterion) 'total: a missing criterion counts as 0'

$older = @{ Number = 1; Total = 50.0; CreatedAtUtc = '2026-01-01T00:00:00Z' }
$newer = @{ Number = 2; Total = 50.0; CreatedAtUtc = '2026-06-01T00:00:00Z' }
$highest = @{ Number = 3; Total = 90.0; CreatedAtUtc = '2026-08-01T00:00:00Z' }
$rankedList = Get-PriorityRankedList -Items @($newer, $older, $highest)
Assert-Equal 3 $rankedList[0].Number 'rank: highest total ranks first'
Assert-Equal 1 $rankedList[1].Number 'rank: a tie is broken by the older issue first'
Assert-Equal 2 $rankedList[2].Number 'rank: a tie leaves the newer issue after the older one'
Assert-Equal 1 $rankedList[0].Rank 'rank: ranks are 1-based'
Assert-Equal 3 $rankedList[2].Rank 'rank: the last item gets the last rank number'

# --- Override application ---------------------------------------------------------------------
$scoresForOverride = @{ unblocking = 0; importance = 20; regulatory = 0; transversality = 0; method = 0; effort = 0; fit = 0; age = 0 }
$overrides = @(
    [pscustomobject]@{ issue = 42; criterion = 'importance'; score = 90; reason = 'maintainer call: security-adjacent' },
    [pscustomobject]@{ issue = 43; criterion = 'importance'; score = 10; reason = 'wrong issue number, must not apply' }
)
$applied = Merge-PriorityOverrides -Scores $scoresForOverride -IssueNumber 42 -Overrides $overrides
Assert-Equal 90 $scoresForOverride['importance'] 'override: applies the override matching the issue number'
Assert-Equal 1 $applied.Count 'override: only the matching override is reported as applied'

$scoresUntouched = @{ importance = 20 }
Merge-PriorityOverrides -Scores $scoresUntouched -IssueNumber 999 -Overrides $overrides | Out-Null
Assert-Equal 20 $scoresUntouched['importance'] 'override: an issue with no matching override is left untouched'

# --- New-ScoredEntry -Resume logic (#1560): only the judged criteria are reused ----------------
# score-issues.ps1 is dot-sourced (invocation name '.') purely for its function definitions —
# the script itself detects that and returns before the -All/-Issue body runs, so this triggers
# no gh call, no collect-issues.ps1 refresh and no local-model call.
. (Join-Path $PSScriptRoot 'score-issues.ps1')

$resumeBody = "## Description`n`nSome debt.`n`n## Effort Estimate`n`n- [ ] Small (< 1 hour)`n- [ ] Medium (1-4 hours)`n- [ ] Large (> 4 hours)`n"
$previousEntryRaw = [ordered]@{
    number       = 1600
    title        = '[DEBT] Old title before resume'
    milestone    = $null
    createdAtUtc = '2026-01-01T00:00:00Z'
    flags        = @('needs-milestone')
    total        = 42.0
    scores       = @{ unblocking = 0; importance = 70; regulatory = 20; transversality = 40; method = 55; effort = 45; fit = 40; age = 10 }
    why          = @{ importance = 'previous importance why'; regulatory = 'previous regulatory why'; transversality = 'previous transversality why'; method = 'previous method why'; effort = 'previous effort why' }
    source       = @{ unblocking = 'deterministic'; importance = 'model'; regulatory = 'model'; transversality = 'model'; method = 'model'; effort = 'model'; fit = 'deterministic'; age = 'deterministic' }
    bodyHash     = 'irrelevant-to-New-ScoredEntry-the-caller-checks-the-hash'
    scoredAtUtc  = '2026-01-01T12:00:00Z'
    judgedAtUtc  = '2026-01-01T12:00:00Z'
}
# Round-trip through JSON so nested property access matches the real scores.json shape
# (PSCustomObject, not Hashtable) that the -All loop reads back into $previous.
$previousEntry = $previousEntryRaw | ConvertTo-Json -Depth 8 | ConvertFrom-Json

$freshIssue = [ordered]@{
    number     = 1600
    title      = '[DEBT] Fresh title after milestone assigned'
    milestone  = 'v0.15.0'
    createdAt  = '2026-06-01T00:00:00Z'
    body       = $resumeBody
    dependents = @(10, 11, 12)
}
$resumed = New-ScoredEntry -IssueRecord $freshIssue -Overrides @() -PreviousJudged $previousEntry
Assert-Equal 'v0.15.0' $resumed.milestone 'resume: fresh milestone replaces the previous (stale) one'
Assert-Equal '[DEBT] Fresh title after milestone assigned' $resumed.title 'resume: title is recomputed from the fresh record'
Assert-Equal 90 $resumed.scores.fit 'resume: fit is recomputed from the fresh milestone'
Assert-Equal 75 $resumed.scores.unblocking 'resume: unblocking is recomputed from the fresh dependents (3 x 25)'
Assert-Equal 0 $resumed.flags.Count 'resume: needs-milestone flag drops once a fresh milestone exists'
Assert-Equal 70 $resumed.scores.importance 'resume: importance is reused from the previous judged entry'
Assert-Equal 'previous importance why' $resumed.why.importance 'resume: importance why is reused from the previous judged entry'
Assert-Equal 'model' $resumed.source.importance 'resume: importance source is reused from the previous judged entry'
Assert-Equal 20 $resumed.scores.regulatory 'resume: regulatory is reused from the previous judged entry'
Assert-Equal 40 $resumed.scores.transversality 'resume: transversality is reused from the previous judged entry'
Assert-Equal 55 $resumed.scores.method 'resume: method is reused from the previous judged entry'
Assert-Equal 45 $resumed.scores.effort 'resume: effort is reused from the previous judged entry when its source was model and the checkbox is unticked'
Assert-Equal 'previous effort why' $resumed.why.effort 'resume: effort why is reused with the score'
Assert-Equal '2026-01-01T12:00:00Z' $resumed.judgedAtUtc 'resume: judgedAtUtc keeps the previous judged timestamp'
Assert-True ($resumed.scoredAtUtc -ne '2026-01-01T12:00:00Z') 'resume: scoredAtUtc is always stamped fresh, never reused'

# A now-ticked effort checkbox wins over the previous judged effort score.
$tickedBody = "## Description`n`nSome debt.`n`n## Effort Estimate`n`n- [x] Small (< 1 hour)`n- [ ] Medium (1-4 hours)`n- [ ] Large (> 4 hours)`n"
$tickedIssue = [ordered]@{
    number     = 1600
    title      = '[DEBT] Fresh title, effort now ticked'
    milestone  = 'v0.15.0'
    createdAt  = '2026-06-01T00:00:00Z'
    body       = $tickedBody
    dependents = @()
}
$resumedTicked = New-ScoredEntry -IssueRecord $tickedIssue -Overrides @() -PreviousJudged $previousEntry
Assert-Equal 100 $resumedTicked.scores.effort 'resume: a newly ticked effort checkbox overrides the previous judged effort score'
Assert-Equal 'Effort Estimate checkbox ticked' $resumedTicked.why.effort 'resume: checkbox effort why replaces the previous judged why'
Assert-Equal 'checkbox' $resumedTicked.source.effort 'resume: checkbox effort source replaces the previous model source'
Assert-Equal 70 $resumedTicked.scores.importance 'resume: importance is still reused even when effort switches to the checkbox'

# An override is still re-applied on top of the resumed judged scores.
$resumeOverrides = @([pscustomobject]@{ issue = 1600; criterion = 'importance'; score = 5; reason = 'maintainer call: deprioritize on resume' })
$resumedOverridden = New-ScoredEntry -IssueRecord $freshIssue -Overrides $resumeOverrides -PreviousJudged $previousEntry
Assert-Equal 5 $resumedOverridden.scores.importance 'resume: an override still applies on top of a resumed judged score'
Assert-Equal 'maintainer call: deprioritize on resume' $resumedOverridden.why.importance 'resume: override why replaces the resumed why'
Assert-Equal 'override' $resumedOverridden.source.importance 'resume: override source replaces the resumed model source'

# --- Invalid model JSON handling (#1552 decision 4/8) ------------------------------------------
$validJson = '{"importance":{"score":80,"why":"x"},"regulatory":{"score":10,"why":"x"},"transversality":{"score":50,"why":"x"},"method":{"score":30,"why":"x"},"effort":{"score":60,"why":"x"}}'
$parsedOk = ConvertFrom-PriorityModelJson -Text $validJson
Assert-True (Test-PriorityModelScores -Parsed $parsedOk) 'model json: a well-formed reply passes validation'

$fence = '```'
$fencedJson = $fence + "json`n" + $validJson + "`n" + $fence
$parsedFenced = ConvertFrom-PriorityModelJson -Text $fencedJson
Assert-True (Test-PriorityModelScores -Parsed $parsedFenced) 'model json: a Markdown code fence around the JSON is stripped'

$garbage = 'Sure! Here is my analysis: importance is high because of reasons.'
$parsedGarbage = ConvertFrom-PriorityModelJson -Text $garbage
Assert-True ($null -eq $parsedGarbage) 'model json: prose instead of JSON parses to null'

$truncatedJson = '{"importance":{"score":80,"why":"x"},"regulatory":{"sc'
$parsedTruncated = ConvertFrom-PriorityModelJson -Text $truncatedJson
Assert-True ($null -eq $parsedTruncated) 'model json: truncated JSON parses to null'

$missingField = '{"importance":{"score":80,"why":"x"},"regulatory":{"score":10,"why":"x"},"transversality":{"score":50,"why":"x"},"method":{"score":30,"why":"x"}}'
$parsedMissing = ConvertFrom-PriorityModelJson -Text $missingField
Assert-True (-not (Test-PriorityModelScores -Parsed $parsedMissing)) 'model json: valid JSON missing the effort key fails score validation (triggers a retry)'

$outOfRange = '{"importance":{"score":800,"why":"x"},"regulatory":{"score":10,"why":"x"},"transversality":{"score":50,"why":"x"},"method":{"score":30,"why":"x"},"effort":{"score":60,"why":"x"}}'
$parsedOor = ConvertFrom-PriorityModelJson -Text $outOfRange
Assert-True (-not (Test-PriorityModelScores -Parsed $parsedOor)) 'model json: a score outside 0-100 fails score validation'

$emptyText = ''
Assert-True ($null -eq (ConvertFrom-PriorityModelJson -Text $emptyText)) 'model json: empty reply parses to null'

# --- Summary ------------------------------------------------------------------------------------
Write-Output "Test-Priority: $script:passCount passed, $script:failCount failed."
if ($script:failCount -gt 0) { exit 1 }
exit 0
