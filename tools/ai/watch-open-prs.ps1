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
# The open-PR list comes from paginated REST (`gh api --paginate --slurp`), never `gh pr list --limit N`,
# which silently caps the result at N and can make an omitted PR read as closed.
# -Once does a single poll and exits 0, for verification and tests.

$ErrorActionPreference = 'Continue'
$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)
$open = [System.Collections.Generic.HashSet[int]]::new()
$first = $true
$repoParts = $Repo -split '/', 2
$repoOwner = $repoParts[0]
$repoName = $repoParts[1]
while ($true) {
    try {
        $prsRaw = gh api --paginate --slurp "repos/$Repo/pulls?state=open&per_page=100" 2>&1
        if ($LASTEXITCODE -ne 0) { throw "gh api pulls list failed: $prsRaw" }
        $pages = $prsRaw | ConvertFrom-Json
        $prs = @(foreach ($page in @($pages)) { foreach ($p in @($page)) { $p } }) |
            ForEach-Object { [pscustomobject]@{ number = $_.number; title = $_.title } }
        $nums = @($prs | ForEach-Object { $_.number })
        foreach ($n in @($open)) {
            if ($nums -notcontains $n) {
                $st = gh pr view $n --repo $Repo --json state --jq .state 2>&1
                if ($LASTEXITCODE -ne 0) { Write-Output "WARN gh pr view #$n state failed: $st"; continue }
                Write-Output "PR #$n $st"
                [void]$open.Remove($n)
            }
        }
        $existing = 0
        foreach ($p in $prs) {
            $n = $p.number; [void]$open.Add($n)
            $jRaw = gh pr view $n --repo $Repo --json statusCheckRollup 2>&1
            if ($LASTEXITCODE -ne 0) { Write-Output "WARN gh pr view #$n statusCheckRollup failed: $jRaw"; continue }
            $j = $jRaw | ConvertFrom-Json
            foreach ($c in @($j.statusCheckRollup)) {
                if ($c.conclusion -in 'FAILURE', 'TIMED_OUT', 'CANCELLED' -and $c.name -notmatch '^codecov/') {
                    if ($seen.Add("$n|fail|$($c.name)|$($c.detailsUrl)")) { Write-Output "PR #$n CHECK-FAIL $($c.name) $($c.detailsUrl)" }
                }
            }
            $q = 'query { repository(owner:"' + $repoOwner + '", name:"' + $repoName + '") { pullRequest(number: ' + $n + ') { reviewThreads(first: 100) { nodes { id isResolved comments(first: 1) { nodes { author { login } path } } } } } } }'
            $tRaw = gh api graphql -f query=$q 2>&1
            if ($LASTEXITCODE -ne 0) { Write-Output "WARN gh api graphql reviewThreads for #$n failed: $tRaw"; continue }
            $t = $tRaw | ConvertFrom-Json
            foreach ($th in @($t.data.repository.pullRequest.reviewThreads.nodes | Where-Object { -not $_.isResolved })) {
                if ($seen.Add("$n|thread|$($th.id)")) {
                    if ($first) { $existing++ } else { Write-Output "PR #$n NEW-THREAD $($th.comments.nodes[0].author.login) $($th.comments.nodes[0].path)" }
                }
            }
        }
        if ($first) { Write-Output "WATCHING PRs $($nums -join ', ') ($existing unresolved threads already known)"; $first = $false }
    }
    catch {
        Write-Output "WARN $($_.Exception.Message)"
    }

    if ($Once) { exit 0 }
    Start-Sleep -Seconds $IntervalSeconds
}
