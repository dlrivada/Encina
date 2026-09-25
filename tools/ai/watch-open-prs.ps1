param(
    [string]$Repo = 'dlrivada/Encina',
    [int]$IntervalSeconds = 60,
    [switch]$Once
)

# Polls every open PR of $Repo; emits one line per NEW event:
#   PR #n CHECK-FAIL <name> <url>            (a check newly in the fail bucket; codecov/* excluded,
#                                              check-links is NOT excluded: ignoring it hid a real
#                                              failure from 2026-09-23 to 2026-09-26)
#   PR #n NEW-THREAD <author> <path>          (a new unresolved review thread)
#   PR #n MERGED|CLOSED                       (the PR left the open list)
#   WATCHING PRs <numbers> (<n> unresolved threads already known)   (first poll: records what already
#                                              exists so a re-armed monitor does not re-announce old threads)
#   WARN <message>                            (a transient API/network failure; polling continues)
#
# Keys use an ordinal (case-sensitive) set: GraphQL node ids can differ only in case.
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
        $prs = gh pr list --repo $Repo --state open --limit 50 --json number,title | ConvertFrom-Json
        $nums = @($prs | ForEach-Object { $_.number })
        foreach ($n in @($open)) {
            if ($nums -notcontains $n) {
                $st = gh pr view $n --repo $Repo --json state --jq .state 2>$null
                Write-Output "PR #$n $st"
                [void]$open.Remove($n)
            }
        }
        $existing = 0
        foreach ($p in $prs) {
            $n = $p.number; [void]$open.Add($n)
            $j = gh pr view $n --repo $Repo --json statusCheckRollup | ConvertFrom-Json
            foreach ($c in @($j.statusCheckRollup)) {
                if ($c.conclusion -in 'FAILURE', 'TIMED_OUT', 'CANCELLED' -and $c.name -notmatch '^codecov/') {
                    if ($seen.Add("$n|fail|$($c.name)|$($c.detailsUrl)") -and -not $first) { Write-Output "PR #$n CHECK-FAIL $($c.name) $($c.detailsUrl)" }
                }
            }
            $q = 'query { repository(owner:"' + $repoOwner + '", name:"' + $repoName + '") { pullRequest(number: ' + $n + ') { reviewThreads(first: 100) { nodes { id isResolved comments(first: 1) { nodes { author { login } path } } } } } } }'
            $t = gh api graphql -f query=$q 2>$null | ConvertFrom-Json
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
