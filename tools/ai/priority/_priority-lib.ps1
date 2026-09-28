# tools/ai/priority/_priority-lib.ps1 — pure, gh-free and model-free functions for the #1552
# priority pipeline. Dot-source this file from collect-issues.ps1, score-issues.ps1 and
# Test-Priority.ps1 so the deterministic rules are defined exactly once and unit-testable without
# a network call.

# Weights are exactly #1552's table: Unblocking, Importance, Regulatory, Transversality, Method,
# Effort (inverse), Fit with 1.0, Age.
$script:PriorityWeights = [ordered]@{
    unblocking     = 18
    importance     = 18
    regulatory     = 10
    transversality = 12
    method         = 12
    effort         = 14
    fit            = 10
    age            = 6
}

# Milestone title -> Fit-with-1.0 score (#1552 decision 3). Matched on the leading token so a
# milestone title with a trailing description ("v0.14.0 — Hardening") still matches.
$script:PriorityMilestoneFit = [ordered]@{
    'v0.14.0'     = 100
    'v0.15.0'     = 90
    'v0.16.0'     = 85
    'v0.17.0'     = 80
    'v0.18.0'     = 75
    'v0.19.0'     = 70
    'v0.20.0'     = 65
    'v0.21.0'     = 60
    'v0.22.0'     = 55
    'v1.0.0-rc.1' = 50
}
$script:PriorityMilestoneFitDefault = 40

# Keywords that mark a dependency reference (#1552 decision 1); "see #n" is deliberately not one
# of these, so a mere mention is never counted.
$script:PriorityDependencyKeywords = @('blocked by', 'depends on', 'requires', 'after', 'needs')

function Get-PriorityAgeScore {
    <#
        .SYNOPSIS
        Age score: days open / 180 * 100, capped at 100 (#1552 decision 3).
    #>
    param(
        [Parameter(Mandatory)][datetime]$CreatedAtUtc,
        [datetime]$NowUtc = (Get-Date).ToUniversalTime()
    )
    $days = ($NowUtc - $CreatedAtUtc).TotalDays
    if ($days -lt 0) { $days = 0 }
    return [Math]::Round([Math]::Min(100.0, ($days / 180.0) * 100.0), 4)
}

function Get-PriorityMilestoneFitScore {
    <#
        .SYNOPSIS
        Fit-with-1.0 score from a milestone title, per #1552 decision 3's fixed table.
    #>
    param([string]$MilestoneTitle)
    if ([string]::IsNullOrWhiteSpace($MilestoneTitle)) { return $script:PriorityMilestoneFitDefault }
    $token = ($MilestoneTitle.Trim() -split '\s+')[0]
    if ($script:PriorityMilestoneFit.Contains($token)) { return $script:PriorityMilestoneFit[$token] }
    return $script:PriorityMilestoneFitDefault
}

function Get-PriorityUnblockingScore {
    <#
        .SYNOPSIS
        Unblocking score: min(100, 25 x number of distinct open issues that depend on it).
    #>
    param([int]$DependentCount)
    if ($DependentCount -lt 0) { $DependentCount = 0 }
    return [Math]::Min(100, 25 * $DependentCount)
}

function Get-PriorityIssueClass {
    <#
        .SYNOPSIS
        Classifies one issue into epics / post-1.0 / ranked (#1552 decision 2).
    #>
    param(
        [Parameter(Mandatory)][string]$Title,
        [string]$MilestoneTitle
    )
    if ($Title -match '^\s*\[EPIC\]') { return 'epics' }
    if ($MilestoneTitle -and $MilestoneTitle.Trim() -match '^Post-1\.0:') { return 'post-1.0' }
    return 'ranked'
}

function Get-PriorityTemplateSection {
    <#
        .SYNOPSIS
        Returns the body text of one '## Header' section (up to the next '## ' header or the end
        of the body), or $null when the header is not present.
    #>
    param(
        [Parameter(Mandatory)][AllowEmptyString()][string]$Body,
        [Parameter(Mandatory)][string]$Header
    )
    if ([string]::IsNullOrEmpty($Body)) { return $null }
    $pattern = '(?ism)^##\s+' + [regex]::Escape($Header) + '\s*(.*?)(?=^##\s+|\z)'
    # The line above intentionally does NOT use PowerShell string interpolation for the capture
    # group: '(.*?)' is regex syntax, not a PowerShell subexpression, so this must stay single-quoted.
    $m = [regex]::Match($Body, $pattern)
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}

function Get-PriorityEffortCheckboxScore {
    <#
        .SYNOPSIS
        Reads the technical_debt.md "## Effort Estimate" checkbox (#1552 decision 3/8). Returns a
        hashtable @{ Score = <int-or-$null>; Ticked = <bool> }: Score is $null and Ticked is
        $false when the section is missing or no box is ticked (the caller then falls back to a
        judged score), matching the template's three shapes (ticked Small/Medium, ticked Large,
        no section or nothing ticked).
    #>
    param([AllowEmptyString()][string]$Body)
    $result = @{ Score = $null; Ticked = $false }
    if ([string]::IsNullOrEmpty($Body)) { return $result }
    $section = Get-PriorityTemplateSection -Body $Body -Header 'Effort Estimate'
    if (-not $section) { return $result }
    if ($section -match '(?im)^\s*-\s*\[[xX]\]\s*Small\b') { return @{ Score = 100; Ticked = $true } }
    if ($section -match '(?im)^\s*-\s*\[[xX]\]\s*Medium\b') { return @{ Score = 60; Ticked = $true } }
    if ($section -match '(?im)^\s*-\s*\[[xX]\]\s*Large\b') { return @{ Score = 25; Ticked = $true } }
    return $result
}

function Get-PriorityDependencyTargets {
    <#
        .SYNOPSIS
        Distinct issue numbers that $Body references as a dependency ("blocked by #n", "depends on
        #n", "requires #n", "after #n", "needs #n"), excluding $SelfNumber. A bare mention such as
        "see #n" is never counted because it carries none of the keywords.
    #>
    param(
        [AllowEmptyString()][string]$Body,
        [int]$SelfNumber = 0
    )
    $targets = New-Object System.Collections.Generic.HashSet[int]
    if ([string]::IsNullOrEmpty($Body)) { return @() }
    $kw = ($script:PriorityDependencyKeywords -join '|')
    $pattern = "(?im)\b($kw)\b[^\n#]{0,24}#(\d+)"
    foreach ($m in [regex]::Matches($Body, $pattern)) {
        $n = [int]$m.Groups[2].Value
        if ($n -ne $SelfNumber) { [void]$targets.Add($n) }
    }
    return @($targets | Sort-Object)
}

function Get-PriorityTotal {
    <#
        .SYNOPSIS
        Total = sum(weight x score) / 100, rounded to 1 decimal (#1552 decision 5). Missing
        criteria (an unscored issue) score 0.
    #>
    param([Parameter(Mandatory)][hashtable]$Scores)
    $sum = 0.0
    foreach ($k in $script:PriorityWeights.Keys) {
        $s = $Scores[$k]
        if ($null -eq $s) { $s = 0 }
        $sum += $script:PriorityWeights[$k] * [double]$s
    }
    return [Math]::Round($sum / 100.0, 1)
}

function Get-PriorityRankedList {
    <#
        .SYNOPSIS
        Sorts a list of hashtables (each with Total and CreatedAtUtc) by Total desc, ties broken
        by CreatedAtUtc asc (older first, #1552 decision 5), and stamps a 1-based Rank on each.
    #>
    param([Parameter(Mandatory)][array]$Items)
    $sorted = @($Items | Sort-Object -Property `
        @{ Expression = { [double]$_.Total }; Descending = $true }, `
        @{ Expression = { [datetime]$_.CreatedAtUtc }; Descending = $false })
    for ($i = 0; $i -lt $sorted.Count; $i++) {
        $sorted[$i].Rank = $i + 1
    }
    # PowerShell unwraps a single-element array on return, which would turn a one-issue ranked list
    # into a bare Hashtable (whose .Count would then count its KEYS, not issues) — Write-Output
    # -NoEnumerate keeps $sorted an array of exactly $sorted.Count items regardless of that count.
    Write-Output -NoEnumerate $sorted
}

function Merge-PriorityOverrides {
    <#
        .SYNOPSIS
        Applies every override in $Overrides whose 'issue' matches $IssueNumber onto $Scores,
        last writer wins, returning the list of applied override records (#1552 decision 1/overrides.json).
    #>
    param(
        [Parameter(Mandatory)][hashtable]$Scores,
        [Parameter(Mandatory)][int]$IssueNumber,
        [array]$Overrides
    )
    $applied = @()
    foreach ($o in @($Overrides)) {
        if ($null -eq $o) { continue }
        if ([int]$o.issue -ne $IssueNumber) { continue }
        $Scores[[string]$o.criterion] = [double]$o.score
        $applied += $o
    }
    return $applied
}

function ConvertFrom-PriorityModelJson {
    <#
        .SYNOPSIS
        Parses the local model's reply into an object, tolerating a Markdown code fence around the
        JSON. Returns $null (never throws) on anything that is not valid JSON, so the caller can
        retry once and then mark the issue "unscored" (#1552 decision 4/8).
    #>
    param([AllowEmptyString()][string]$Text)
    if ([string]::IsNullOrWhiteSpace($Text)) { return $null }
    $t = $Text.Trim()
    $fenced = [regex]::Match($t, '(?s)```(?:json)?\s*(.*?)\s*```')
    if ($fenced.Success) { $t = $fenced.Groups[1].Value }
    try {
        return $t | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        return $null
    }
}

function Test-PriorityModelScores {
    <#
        .SYNOPSIS
        True when $Parsed (the result of ConvertFrom-PriorityModelJson) has every judged criterion
        with a numeric 0-100 'score' field: importance, regulatory, transversality, method, effort.
    #>
    param($Parsed)
    if ($null -eq $Parsed) { return $false }
    foreach ($k in @('importance', 'regulatory', 'transversality', 'method', 'effort')) {
        $node = $Parsed.$k
        if ($null -eq $node) { return $false }
        $score = $node.score
        if ($null -eq $score) { return $false }
        $d = 0.0
        if (-not [double]::TryParse([string]$score, [ref]$d)) { return $false }
        if ($d -lt 0 -or $d -gt 100) { return $false }
    }
    return $true
}
