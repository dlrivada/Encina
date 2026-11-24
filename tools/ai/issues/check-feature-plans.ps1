# tools/ai/issues/check-feature-plans.ps1 [-Since <utc>] [-GraceHours <n>] (#4255)
#
# AGENTS.md section 11: a [FEATURE] issue is opened together with its implementation plan. This lists every OPEN
# [FEATURE] issue created after -Since (default 2026-11-24T00:00Z, the day the rule started) that has
#   - no docs/plans/*-<issue>.md on origin/main, and
#   - no open pull request whose files include such a plan.
# Exit 0 when none is missing or every missing one is still inside the grace period; exit 1 when any is older than
# -GraceHours (default 24); exit 2 when gh itself fails (nothing was checked).
#
# REST only (gh api), so it works when the GraphQL quota is exhausted, and the default GITHUB_TOKEN is enough, so a
# scheduled workflow can run it.

param(
    [datetime]$Since = [datetime]::Parse('2026-11-24T00:00:00Z', [cultureinfo]::InvariantCulture, [System.Globalization.DateTimeStyles]::AdjustToUniversal),
    [int]$GraceHours = 24,
    [string]$Repo = 'dlrivada/Encina',
    [string]$Branch = 'main'
)

$ErrorActionPreference = 'Stop'
# A bare date such as -Since 2026-11-17 binds with Kind Unspecified and would be read as local time; it means UTC.
if ($Since.Kind -eq [System.DateTimeKind]::Unspecified) { $Since = [datetime]::SpecifyKind($Since, [System.DateTimeKind]::Utc) }
$Since = $Since.ToUniversalTime()

function Invoke-GhRest {
    param([string]$Path, [switch]$Paginate)
    $ghArgs = @('api', $Path)
    if ($Paginate) { $ghArgs += @('--paginate', '--slurp') }
    $out = & gh @ghArgs
    if ($LASTEXITCODE -ne 0) { throw "gh api $Path failed (exit $LASTEXITCODE)" }
    $parsed = ("$($out -join "`n")" | ConvertFrom-Json)
    if ($Paginate) { return @($parsed | ForEach-Object { $_ }) }
    return $parsed
}

try {
    # Open issues (the endpoint also returns pull requests; they carry a pull_request member).
    $features = @(Invoke-GhRest "repos/$Repo/issues?state=open&per_page=100" -Paginate |
            Where-Object { -not $_.pull_request -and $_.title -match '^\[FEATURE\]' -and ([datetime]$_.created_at).ToUniversalTime() -gt $Since })

    if ($features.Count -eq 0) {
        Write-Host "check-feature-plans: no open [FEATURE] issue created after $($Since.ToString('u')); nothing to check"
        exit 0
    }

    # Open pull requests that add or change a plan, keyed by issue number. Read BEFORE the main tree, so a plan PR merged
    # between the two reads is seen in one of them. Detection uses the changed files of every open pull request (title,
    # branch and body are not trusted: a plan PR may omit the issue number). There is no cheap server-side filter on the
    # pulls endpoint for changed paths.
    $planPrByIssue = @{}
    foreach ($pr in @(Invoke-GhRest "repos/$Repo/pulls?state=open&per_page=100" -Paginate)) {
        $files = @(Invoke-GhRest "repos/$Repo/pulls/$($pr.number)/files?per_page=100" -Paginate)
        foreach ($file in $files) {
            if ($file.filename -match '^docs/plans/[^/]+-implementation-plan-(\d+)\.md$' -and -not $planPrByIssue.ContainsKey([int]$Matches[1])) {
                $planPrByIssue[[int]$Matches[1]] = $pr.number
            }
        }
    }

    # Plans on main: docs/plans/*-<n>.md from the recursive tree.
    $tree = Invoke-GhRest "repos/$Repo/git/trees/${Branch}?recursive=1"
    if ($tree.truncated) { throw 'the git tree of main is truncated; cannot list docs/plans' }
    $planNumbers = @{}
    foreach ($entry in $tree.tree) {
        if ($entry.path -match '^docs/plans/[^/]+-implementation-plan-(\d+)\.md$') { $planNumbers[[int]$Matches[1]] = $entry.path }
    }

    $now = [datetime]::UtcNow
    $rows = foreach ($issue in ($features | Sort-Object number)) {
        $n = [int]$issue.number
        $status = $null
        if ($planNumbers.ContainsKey($n)) {
            $status = "plan on main ($($planNumbers[$n]))"
        }
        elseif ($planPrByIssue.ContainsKey($n)) {
            $status = "open plan PR #$($planPrByIssue[$n])"
        }
        $elapsedHours = ($now - ([datetime]$issue.created_at).ToUniversalTime()).TotalHours
        [pscustomobject]@{
            Issue    = $n
            AgeHours = [math]::Round($elapsedHours, 1)
            Plan     = if ($status) { $status } else { 'MISSING' }
            Overdue  = (-not $status) -and ($elapsedHours -gt $GraceHours)
            Title    = $issue.title
        }
    }

    $rows | Format-Table Issue, AgeHours, Plan, Overdue, Title -AutoSize | Out-String -Width 240 | Write-Host
    $missing = @($rows | Where-Object { $_.Plan -eq 'MISSING' })
    $overdue = @($rows | Where-Object { $_.Overdue })
    $summary = "check-feature-plans: $($rows.Count) open [FEATURE] issue(s) since $($Since.ToString('u')); $($missing.Count) without plan; $($overdue.Count) older than $GraceHours h"
    Write-Host $summary
    if ($env:GITHUB_STEP_SUMMARY) {
        $markdown = @('### Feature plan check', '', $summary, '', '| Issue | Age (h) | Plan | Overdue | Title |', '|---|---|---|---|---|')
        foreach ($row in $rows) {
            $markdown += "| #$($row.Issue) | $($row.AgeHours) | $($row.Plan) | $($row.Overdue) | $($row.Title.Replace('|', '\|')) |"
        }
        Add-Content -LiteralPath $env:GITHUB_STEP_SUMMARY -Value $markdown
    }
    if ($overdue.Count -gt 0) {
        Write-Host "check-feature-plans: FAIL - no plan on $Branch and no open plan PR for: $(($overdue | ForEach-Object { '#' + $_.Issue }) -join ' ')"
        exit 1
    }
    exit 0
}
catch {
    Write-Host "check-feature-plans: $($_.Exception.Message)"
    exit 2
}
