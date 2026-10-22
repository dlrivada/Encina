param(
    [string]$Repo = 'dlrivada/Encina',
    [int]$IntervalSeconds = 60,
    [switch]$Once
)

# Polls every open PR of $Repo; emits one line per NEW event:
#   PR #n CHECK-FAIL <name> <url>            (a check in the fail bucket; codecov/* excluded, check-links is
#                                              NOT excluded: ignoring it hid a real failure from 2026-09-23 to
#                                              2026-09-26. Emitted on the FIRST poll too -- a check already
#                                              failing when the monitor starts is actionable immediately; only
#                                              the review-thread baseline stays silent on the first poll.)
#   PR #n NEW-THREAD <author> <path>          (a new unresolved review thread)
#   PR #n MERGED|CLOSED                       (the PR left the open list)
#   WATCHING PRs <numbers> (<n> unresolved threads already known)   (first poll: records the review-thread
#                                              baseline so a re-armed monitor does not re-announce old
#                                              threads)
#   WARN <message>                            (a transient API/network failure; the current state is kept
#                                              and no partial or failed result is ever applied)
#
# Keys use an ordinal (case-sensitive) set: GraphQL node ids can differ only in case.
# The open-PR list comes from paginated REST (`gh api --paginate --slurp`), never the gh PR list subcommand with a limit,
# which silently caps the result at N and can make an omitted PR read as closed.
# -Once does a single poll and exits 0, for verification and tests.

# Quota (#2568): everything here is REST except the review threads, which exist only in GraphQL. Threads are queried
# only when the PR's updated_at changed since its last successful thread read (or on the first poll), after
# Wait-GhGraphQLQuota -NoWait; when the GraphQL quota is low the thread read is skipped (one WARN per reset window)
# and the previous thread state is kept. See docs/engineering/github-api-quota.md.

$ErrorActionPreference = 'Continue'
. (Join-Path $PSScriptRoot 'gh-quota.ps1')
$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$open = [System.Collections.Generic.HashSet[int]]::new()
$threadsReadAt = @{}   # PR number -> updated_at of the PR when its threads were last read successfully
$warnedResetWindow = $null
$quotaSkipped = [System.Collections.Generic.HashSet[int]]::new()   # PRs whose first-poll thread read did not happen (GraphQL low or a failed read): still in the silent baseline
$baselined = [System.Collections.Generic.HashSet[int]]::new()   # PRs whose thread baseline was recorded silently
$checkedSha = @{}      # PR number -> head sha of its last successful check read
$checkPending = @{}    # PR number -> $true when that read still had pending, failed or no checks
$first = $true
$repoParts = $Repo -split '/', 2
$repoOwner = $repoParts[0]
$repoName = $repoParts[1]
while ($true) {
    $rateLimited = $false   # set when a failure of this poll mentions a rate limit: the next poll waits at least 60 s
    try {
        $prsR = Invoke-GhQuotaGh @('api', '--paginate', '--slurp', "repos/$Repo/pulls?state=open&per_page=100")
        if ($prsR.Code -ne 0) { throw "gh api pulls list failed: $($prsR.Output)" }
        $pages = $prsR.Stdout | ConvertFrom-Json
        $prs = @(foreach ($page in @($pages)) { foreach ($p in @($page)) { $p } }) |
            ForEach-Object { [pscustomobject]@{ number = $_.number; title = $_.title; sha = $_.head.sha; updated = [string]$_.updated_at } }
        $nums = @($prs | ForEach-Object { $_.number })
        foreach ($n in @($open)) {
            if ($nums -notcontains $n) {
                $stR = Invoke-GhQuotaGh @('api', "repos/$Repo/pulls/$n")
                if ($stR.Code -ne 0) {
                    if ($stR.Output -match '(?i)rate limit') { $rateLimited = $true }
                    Write-Output "WARN gh api pulls/$n state failed: $($stR.Output)"; continue
                }
                $stJson = $stR.Stdout | ConvertFrom-Json
                if ($stJson.state -eq 'open') { continue }   # still open: only missing from the list (keep watching it)
                $st = if ($stJson.merged) { 'MERGED' } else { 'CLOSED' }
                Write-Output "PR #$n $st"
                [void]$open.Remove($n)
                $threadsReadAt.Remove($n); $checkedSha.Remove($n); $checkPending.Remove($n)
            }
        }
        $existing = 0
        foreach ($p in $prs) {
            $n = $p.number; [void]$open.Add($n)
            # Check runs and statuses (2 REST calls) are read only on the first poll, after the head sha changed, or while
            # the previous read still had pending, failed or no checks; otherwise the previous result stands (no new lines).
            $needChecks = (-not $checkedSha.ContainsKey($n)) -or $checkedSha[$n] -ne $p.sha -or $checkPending[$n]
            if ($needChecks) {
                try {
                    $buckets = @(Get-GhCheckBuckets -Repo $Repo -Sha $p.sha)
                    foreach ($c in $buckets) {
                        if ($c.Bucket -eq 'fail' -and $c.Name -notmatch '^codecov/') {
                            if ($seen.Add("$n|fail|$($c.Name)|$($c.Url)")) { Write-Output "PR #$n CHECK-FAIL $($c.Name) $($c.Url)" }
                        }
                    }
                    $checkedSha[$n] = $p.sha
                    $checkPending[$n] = ($buckets.Count -eq 0) -or (@($buckets | Where-Object { $_.Bucket -in 'pending', 'fail' }).Count -gt 0)   # a failed job can be re-run on the same sha
                }
                catch {
                    if ($_.Exception.Message -match '(?i)rate limit') { $rateLimited = $true }
                    if ($first) { [void]$quotaSkipped.Add($n) }   # its threads are not read this poll: keep the silent baseline for later
                    Write-Output "WARN checks of #$n failed: $($_.Exception.Message)"; continue
                }
            }
            if ($threadsReadAt.ContainsKey($n) -and $threadsReadAt[$n] -eq $p.updated) { continue }
            if (-not (Wait-GhGraphQLQuota -Caller 'watch-open-prs' -MinRemaining 300 -NoWait)) {
                if ($first) { [void]$quotaSkipped.Add($n) }   # its first thread read is still the silent baseline
                $quota = Get-GhGraphQLQuota
                $window = if ($quota) { $quota.ResetAt.ToString('o') } else { 'unknown' }
                if ($warnedResetWindow -ne $window) {
                    $warnedResetWindow = $window
                    Write-Output "WARN GraphQL low ($($quota.Remaining)); review threads of #$n skipped until $($quota.ResetAt.ToString('HH:mm:ss')) UTC (resetAt)"
                }
                continue
            }
            $q = 'query { repository(owner:"' + $repoOwner + '", name:"' + $repoName + '") { pullRequest(number: ' + $n + ') { reviewThreads(first: 100) { nodes { id isResolved comments(first: 1) { nodes { author { login } path } } } } } } }'
            $tR = Invoke-GhQuotaGh @('api', 'graphql', '-f', "query=$q")
            Write-GhQuotaUsage -Caller 'watch-open-prs' -Operation "reviewThreads #$n"
            if ($tR.Code -ne 0) {
                if ($tR.Output -match '(?i)rate limit') { $rateLimited = $true }
                # A failed first read keeps the PR in the silent baseline, so old threads are never announced as NEW-THREAD.
                if ($first) { [void]$quotaSkipped.Add($n) }
                Write-Output "WARN gh api graphql reviewThreads for #$n failed: $($tR.Output)"; continue
            }
            $t = $tR.Stdout | ConvertFrom-Json
            $threadsReadAt[$n] = $p.updated
            # The silent baseline lasts until a PR's FIRST successful thread read, so a read skipped while GraphQL
            # was low never turns old threads into NEW-THREAD lines later.
            $isBaseline = $false
            if (-not $baselined.Contains($n) -and ($first -or $quotaSkipped.Contains($n))) { $isBaseline = $true; [void]$baselined.Add($n) }
            foreach ($th in @($t.data.repository.pullRequest.reviewThreads.nodes | Where-Object { -not $_.isResolved })) {
                if ($seen.Add("$n|thread|$($th.id)")) {
                    if ($isBaseline) { $existing++ } else { Write-Output "PR #$n NEW-THREAD $($th.comments.nodes[0].author.login) $($th.comments.nodes[0].path)" }
                }
            }
        }
        if ($first) { Write-Output "WATCHING PRs $($nums -join ', ') ($existing unresolved threads already known)"; $first = $false }
    }
    catch {
        if ($_.Exception.Message -match '(?i)rate limit') { $rateLimited = $true }
        Write-Output "WARN $($_.Exception.Message)"
    }

    if ($Once) { exit 0 }
    # After a failure that mentions a rate limit the next poll is at least 60 s away, whatever the interval.
    Start-Sleep -Seconds $(if ($rateLimited) { [Math]::Max(60, $IntervalSeconds) } else { $IntervalSeconds })
}
