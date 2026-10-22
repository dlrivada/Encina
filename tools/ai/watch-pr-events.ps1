param(
    [int]$Pr = 1093,
    [string]$Repo = 'dlrivada/Encina',
    [string]$Me = 'dlrivada',
    [switch]$Once   # a single poll, then exit (verification and tests)
)

# REST only (#2568): no `gh pr ...` subcommand (they use GraphQL); see docs/engineering/github-api-quota.md.
. (Join-Path $PSScriptRoot 'gh-quota.ps1')

# Emits one line per actionable event on a pull request:
#   REVIEW-COMMENT  <user> <path>:<line> <first 160 chars>   (new inline comment/reply, not mine)
#   ISSUE-COMMENT   <user> <first 160 chars>                 (new PR comment, not mine)
#   REVIEW          <user> <state>                           (new review submitted)
#   CHECK-FAIL      <name> <url>                             (a check newly in the fail bucket)
#   CHECKS-DONE     pass=<n> fail=<n> skip=<n>               (no pending checks left)
#   PR-<STATE>      <merge sha>                              (MERGED / CLOSED, then exit)

$since = (Get-Date).ToUniversalTime().AddMinutes(-2)
$seenComments = New-Object System.Collections.Generic.HashSet[long]
$seenReviews = New-Object System.Collections.Generic.HashSet[long]
$seenFails = New-Object System.Collections.Generic.HashSet[string]
$doneAnnounced = $false

function Short([string]$s) {
    $t = ($s -replace '\s+', ' ').Trim()
    if ($t.Length -gt 160) { $t = $t.Substring(0, 160) + '...' }
    return $t
}

while ($true) {
    try {
        $iso = $since.ToString('yyyy-MM-ddTHH:mm:ssZ')

        $rc = gh api "repos/$Repo/pulls/$Pr/comments?since=$iso&per_page=100" 2>$null | ConvertFrom-Json
        foreach ($c in $rc) {
            if ($c.user.login -eq $Me) { continue }
            if ($seenComments.Add([long]$c.id)) {
                $line = if ($c.line) { $c.line } else { $c.original_line }
                Write-Output ("REVIEW-COMMENT " + $c.user.login + " " + $c.path + ":" + $line + " " + (Short $c.body))
            }
        }

        $ic = gh api "repos/$Repo/issues/$Pr/comments?since=$iso&per_page=100" 2>$null | ConvertFrom-Json
        foreach ($c in $ic) {
            if ($c.user.login -eq $Me) { continue }
            if ($seenComments.Add([long]$c.id)) {
                Write-Output ("ISSUE-COMMENT " + $c.user.login + " " + (Short $c.body))
            }
        }

        $rv = gh api "repos/$Repo/pulls/$Pr/reviews?per_page=100" 2>$null | ConvertFrom-Json
        foreach ($r in $rv) {
            if ([datetime]$r.submitted_at -lt $since) { continue }
            if ($seenReviews.Add([long]$r.id)) {
                Write-Output ("REVIEW " + $r.user.login + " " + $r.state)
            }
        }

        # REST only (#2568): the PR read gives the head sha and the state; checks come from the sha's check runs and statuses.
        $prJson = gh api "repos/$Repo/pulls/$Pr" 2>$null | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0 -or -not $prJson) { throw 'pull request read failed' }

        $checks = @(Get-GhCheckBuckets -Repo $Repo -Sha $prJson.head.sha)
        if ($checks.Count -gt 0) {
            $relevant = $checks | Where-Object { $_.Name -notmatch '^(Benchmarks|Validate|Determine|Run Benchmarks|Benchmark)' }
            foreach ($k in ($relevant | Where-Object { $_.Bucket -eq 'fail' })) {
                if ($seenFails.Add($k.Name + '|' + $k.Url)) {
                    Write-Output ("CHECK-FAIL " + $k.Name + " " + $k.Url)
                }
            }
            $pending = @($relevant | Where-Object { $_.Bucket -eq 'pending' }).Count
            if ($pending -eq 0 -and -not $doneAnnounced) {
                $pass = @($relevant | Where-Object { $_.Bucket -eq 'pass' }).Count
                $fail = @($relevant | Where-Object { $_.Bucket -eq 'fail' }).Count
                $skip = @($relevant | Where-Object { $_.Bucket -eq 'skipping' }).Count
                Write-Output ("CHECKS-DONE pass=$pass fail=$fail skip=$skip")
                $doneAnnounced = $true
            }
            elseif ($pending -gt 0) {
                $doneAnnounced = $false
            }
        }

        if ($prJson.merged) {
            Write-Output ("PR-MERGED " + $prJson.merge_commit_sha)
            break
        }
        if ($prJson.state -eq 'closed') {
            Write-Output "PR-CLOSED "
            break
        }
    }
    catch {
        # transient API/network failure: keep polling
    }
    if ($Once) { break }
    Start-Sleep -Seconds 45
}
