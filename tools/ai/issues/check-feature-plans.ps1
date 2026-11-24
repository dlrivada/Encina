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

    # Plans on main: docs/plans/*-<n>.md from the recursive tree.
    $tree = Invoke-GhRest "repos/$Repo/git/trees/${Branch}?recursive=1"
    if ($tree.truncated) { throw 'the git tree of main is truncated; cannot list docs/plans' }
    $planNumbers = @{}
    foreach ($entry in $tree.tree) {
        if ($entry.path -match '^docs/plans/[^/]+-(\d+)\.md$') { $planNumbers[[int]$Matches[1]] = $entry.path }
    }

    # Open pull requests that touch a plan for the issue: candidates are the PRs that name the issue number.
    $openPrs = $null
    $now = [datetime]::UtcNow
    $rows = foreach ($issue in ($features | Sort-Object number)) {
        $n = [int]$issue.number
        $status = $null
        if ($planNumbers.ContainsKey($n)) {
            $status = "plan on main ($($planNumbers[$n]))"
        }
        else {
            if ($null -eq $openPrs) { $openPrs = @(Invoke-GhRest "repos/$Repo/pulls?state=open&per_page=100" -Paginate) }
            $needle = "(?<![0-9])$n(?![0-9])"
            foreach ($pr in ($openPrs | Where-Object { "$($_.title) $($_.head.ref) $($_.body)" -match $needle })) {
                $files = @(Invoke-GhRest "repos/$Repo/pulls/$($pr.number)/files?per_page=100" -Paginate)
                if ($files | Where-Object { $_.filename -match "^docs/plans/[^/]+-$n\.md$" }) { $status = "open plan PR #$($pr.number)"; break }
            }
        }
        $ageHours = [math]::Round(($now - ([datetime]$issue.created_at).ToUniversalTime()).TotalHours, 1)
        [pscustomobject]@{
            Issue    = $n
            AgeHours = $ageHours
            Plan     = if ($status) { $status } else { 'MISSING' }
            Overdue  = (-not $status) -and ($ageHours -gt $GraceHours)
            Title    = $issue.title
        }
    }

    $rows | Format-Table Issue, AgeHours, Plan, Overdue, Title -AutoSize | Out-String -Width 240 | Write-Host
    $missing = @($rows | Where-Object { $_.Plan -eq 'MISSING' })
    $overdue = @($rows | Where-Object { $_.Overdue })
    Write-Host "check-feature-plans: $($rows.Count) open [FEATURE] issue(s) since $($Since.ToString('u')); $($missing.Count) without plan; $($overdue.Count) older than $GraceHours h"
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
