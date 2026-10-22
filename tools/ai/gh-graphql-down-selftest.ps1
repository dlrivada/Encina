# Proves the #2568 contract: with GraphQL at 0 (every `gh api graphql`, `gh pr`, `gh issue` and `gh label` call
# fails), the in-scope tools still work over REST. No network: a stub `gh` function is defined in a child pwsh.
#   pwsh -NoProfile -File tools/ai/gh-graphql-down-selftest.ps1
$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot
$work = Join-Path ([IO.Path]::GetTempPath()) ("gh-graphql-down-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $work | Out-Null
$callLog = Join-Path $work 'calls.log'

$stubPath = Join-Path $work 'gh-stub.ps1'
Set-Content -LiteralPath $stubPath -Encoding utf8 -Value @'
function global:gh {
    $joined = $args -join ' '
    Add-Content -LiteralPath $env:GQD_CALLS -Value $joined
    $global:LASTEXITCODE = 0
    $down = 'GraphQL: API rate limit already exceeded'
    # Per-name call counter (a file, so it survives across the calls of one child process).
    function Step([string]$name) {
        $f = "$env:GQD_CALLS.$name"; $n = 1
        if (Test-Path -LiteralPath $f) { $n = [int](Get-Content -LiteralPath $f) + 1 }
        Set-Content -LiteralPath $f -Value $n; return $n
    }
    # The rateLimit probe itself answers: remaining 0 (GraphQL down) or plenty (GQD_HIGH), resets in 30 minutes.
    if ($joined -like 'api graphql*rateLimit*') {
        $rem = if ($env:GQD_HIGH) { 4000 } else { 0 }
        '{"data":{"rateLimit":{"remaining":' + $rem + ',"resetAt":"' + (Get-Date).ToUniversalTime().AddMinutes(30).ToString('yyyy-MM-ddTHH:mm:ssZ') + '"}}}'
        return
    }
    if ($env:GQD_HIGH -and $joined -like 'api graphql*reviewThreads*') {
        # First thread query fails, later ones answer with one unresolved (old) thread.
        if ((Step 'threads') -eq 1) { $global:LASTEXITCODE = 1; return 'HTTP 502: boom' }
        return '{"data":{"repository":{"pullRequest":{"reviewThreads":{"nodes":[{"id":"T1","isResolved":false,"comments":{"nodes":[{"author":{"login":"rev"},"path":"a.cs"}]}}]}}}}}'
    }
    # GQD_SETTLED: no check is pending. GQD_LOOP: the loop ends by itself (PR merged on the 2nd read, exit on the 3rd list).
    if ($env:GQD_SETTLED -and $joined -like 'api --paginate --slurp repos/*/commits/abc/check-runs*') {
        return '[{"check_runs":[{"name":"build","status":"completed","conclusion":"success","html_url":"https://x/run/2"}]}]'
    }
    if ($env:GQD_LOOP -and $joined -like 'api repos/*/pulls/7') {
        if ((Step 'pull') -ge 2) { return '{"head":{"sha":"abc"},"state":"closed","merged":true,"merge_commit_sha":"deadbeef"}' }
        return '{"head":{"sha":"abc"},"state":"open","merged":false,"merge_commit_sha":null}'
    }
    if ($env:GQD_LOOP -and $joined -like 'api --paginate --slurp repos/*/pulls?state=open*') {
        if ((Step 'list') -ge 3) { exit 0 }
    }
    if ($joined -like 'api graphql*' -or $args[0] -in 'pr', 'issue', 'label') { $global:LASTEXITCODE = 1; return $down }
    switch -Wildcard ($joined) {
        'api --paginate --slurp repos/*/pulls?state=open*' { return '[[{"number":7,"title":"t","head":{"sha":"abc"},"updated_at":"2026-10-22T00:00:00Z"}]]' }
        'api --paginate --slurp repos/*/commits/abc/check-runs*' {
            return '[{"check_runs":[{"name":"build","status":"completed","conclusion":"failure","html_url":"https://x/run/1"},{"name":"lint","status":"completed","conclusion":"success","html_url":"https://x/run/2"},{"name":"test","status":"in_progress","conclusion":null,"html_url":"https://x/run/3"}]}]'
        }
        'api repos/*/commits/abc/status*' { return '{"state":"failure","statuses":[{"context":"codecov/patch","state":"failure","target_url":"https://x/cov"}]}' }
        'api repos/*/pulls/7/comments*' { return '[]' }
        'api repos/*/issues/7/comments*' { return '[]' }
        'api repos/*/pulls/7/reviews*' { return '[]' }
        'api repos/*/pulls/7' {
            if ($env:GQD_MERGED) { return '{"head":{"sha":"abc"},"state":"closed","merged":true,"merge_commit_sha":"deadbeef"}' }
            return '{"head":{"sha":"abc"},"state":"open","merged":false,"merge_commit_sha":null}'
        }
        'api repos/*/labels*' { return @('bug', 'enhancement', 'technical-debt') }
        'api repos/*/issues/123' { return '{"number":123,"title":"[EPIC] a container","labels":[{"name":"epic"}],"milestone":null,"created_at":"2026-10-01T00:00:00Z","body":"b"}' }
        default { $global:LASTEXITCODE = 1; return "HTTP 404: no stub rule for: $joined" }
    }
}
'@

$failures = 0
function Assert-That([string]$Name, [bool]$Ok, [string]$Detail = '') {
    if ($Ok) { "PASS $Name" } else { $script:failures++; "FAIL $Name $Detail" }
}
function Invoke-Down([string]$ScriptCommand, [hashtable]$Env = @{}) {
    $env:GQD_CALLS = $callLog; $env:ENCINA_GH_QUOTA_ROOT = $work
    foreach ($k in $Env.Keys) { Set-Item "Env:$k" $Env[$k] }
    try {
        $out = & pwsh -NoProfile -Command "& { . '$stubPath'; $ScriptCommand; exit `$LASTEXITCODE }" 2>&1 | ForEach-Object { "$_" }
        return [pscustomobject]@{ Exit = $LASTEXITCODE; Text = ($out -join "`n") }
    }
    finally { foreach ($k in $Env.Keys) { Remove-Item "Env:$k" -ErrorAction SilentlyContinue } }
}
function Get-Calls { if (Test-Path $callLog) { @(Get-Content $callLog) } else { @() } }

try {
    # watch-pr-events: one iteration
    $r = Invoke-Down "& '$here\watch-pr-events.ps1' -Pr 7 -Once"
    Assert-That 'watch-pr-events -Once exits 0 with GraphQL down' ($r.Exit -eq 0) $r.Text
    Assert-That 'watch-pr-events reports the failed check with its html_url' ($r.Text -match 'CHECK-FAIL build https://x/run/1') $r.Text
    Assert-That 'watch-pr-events reports the failed status too' ($r.Text -match 'CHECK-FAIL codecov/patch https://x/cov') $r.Text
    Assert-That 'watch-pr-events does not announce CHECKS-DONE while a check is pending' ($r.Text -notmatch 'CHECKS-DONE') $r.Text
    $r = Invoke-Down "& '$here\watch-pr-events.ps1' -Pr 7 -Once" @{ GQD_MERGED = '1' }
    Assert-That 'watch-pr-events reports PR-MERGED with the merge sha' ($r.Text -match 'PR-MERGED deadbeef') $r.Text
    Assert-That 'watch-pr-events never calls a GraphQL-backed gh subcommand' (@(Get-Calls | Where-Object { $_ -match '^(pr|issue|label) |^api graphql' -and $_ -notmatch 'rateLimit' }).Count -eq 0) ((Get-Calls) -join ' | ')

    # watch-open-prs: -Once
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$here\watch-open-prs.ps1' -Once"
    Assert-That 'watch-open-prs -Once exits 0 with GraphQL down' ($r.Exit -eq 0) $r.Text
    Assert-That 'watch-open-prs reports the failed check' ($r.Text -match 'PR #7 CHECK-FAIL build https://x/run/1') $r.Text
    Assert-That 'watch-open-prs still excludes codecov/*' ($r.Text -notmatch 'codecov') $r.Text
    Assert-That 'watch-open-prs skips the review threads with one WARN line' (@([regex]::Matches($r.Text, 'WARN GraphQL low \(0\); review threads of #7 skipped')).Count -eq 1) $r.Text
    Assert-That 'watch-open-prs makes no thread query while GraphQL is low' (@(Get-Calls | Where-Object { $_ -like 'api graphql*' -and $_ -notlike '*rateLimit*' }).Count -eq 0) ((Get-Calls) -join ' | ')
    Assert-That 'watch-open-prs prints the WATCHING line' ($r.Text -match 'WATCHING PRs 7') $r.Text

    # watch-pr-events: a second poll with an unchanged sha and no pending checks makes no check-runs/status call
    Get-ChildItem "$callLog*" -ErrorAction SilentlyContinue | Remove-Item -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$here\watch-pr-events.ps1' -Pr 7 -IntervalSeconds 1" @{ GQD_LOOP = '1'; GQD_SETTLED = '1' }
    $loopCalls = Get-Calls
    Assert-That 'watch-pr-events polls twice (the PR reads)' (@($loopCalls | Where-Object { $_ -eq 'api repos/dlrivada/Encina/pulls/7' }).Count -eq 2) ($loopCalls -join ' | ')
    Assert-That 'watch-pr-events reads check runs once for an unchanged sha with settled checks' (@($loopCalls | Where-Object { $_ -like '*check-runs*' }).Count -eq 1) ($loopCalls -join ' | ')
    Assert-That 'watch-pr-events reads the commit status once for an unchanged sha with settled checks' (@($loopCalls | Where-Object { $_ -like '*commits/abc/status*' }).Count -eq 1) ($loopCalls -join ' | ')
    Assert-That 'watch-pr-events announces CHECKS-DONE once and then PR-MERGED' ((@([regex]::Matches($r.Text, 'CHECKS-DONE')).Count -eq 1) -and $r.Text -match 'PR-MERGED deadbeef') $r.Text

    # watch-open-prs: a failed first thread query, then a good one, never announces the old thread as NEW-THREAD;
    # settled checks are read once across polls
    Get-ChildItem "$callLog*" -ErrorAction SilentlyContinue | Remove-Item -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $work 'artifacts/gh-quota/graphql-ratelimit.json') -ErrorAction SilentlyContinue   # drop the cached "remaining 0"
    $r = Invoke-Down "& '$here\watch-open-prs.ps1' -IntervalSeconds 1" @{ GQD_LOOP = '1'; GQD_SETTLED = '1'; GQD_HIGH = '1' }
    $loopCalls = Get-Calls
    Assert-That 'watch-open-prs warns about the failed first thread query' ($r.Text -match 'WARN gh api graphql reviewThreads for #7 failed') $r.Text
    Assert-That 'watch-open-prs retried the thread query (two thread queries)' (@($loopCalls | Where-Object { $_ -like 'api graphql*reviewThreads*' }).Count -eq 2) ($loopCalls -join ' | ')
    Assert-That 'watch-open-prs does not announce an old thread as NEW-THREAD after a failed first read' ($r.Text -notmatch 'NEW-THREAD') $r.Text
    Assert-That 'watch-open-prs reads check runs once across polls for an unchanged sha' (@($loopCalls | Where-Object { $_ -like '*check-runs*' }).Count -eq 1) ($loopCalls -join ' | ')

    # Covered elsewhere: the open-remediation label listing by open-remediation-selftest.ps1 (case 1b); the
    # audit-draft-remediation label listing, duplicate search and issue reads by the REST-shaped gh stub rules of
    # .claude/hooks/tests/Test-Hooks.ps1 (-Prepare end to end); the spawn gate by its Test-Hooks cases.

    # apply-priority-labels -DryRun
    $csv = Join-Path $work 'classification.csv'; $outCsv = Join-Path $work 'final.csv'
    Set-Content -LiteralPath $csv -Encoding utf8 -Value @('number,priority,confidence,milestone,title,reason', '5,P1,high,m,"[DEBT] x",r')
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$here\apply-priority-labels.ps1' -Csv '$csv' -Out '$outCsv' -DryRun"
    Assert-That 'apply-priority-labels -DryRun lists labels over REST while GraphQL is down' ($r.Exit -eq 0 -and $r.Text -match 'final: P1=1' -and $r.Text -match 'label created: p0-mandatory') $r.Text
    Assert-That 'apply-priority-labels calls no gh label subcommand' (@(Get-Calls | Where-Object { $_ -like 'label *' }).Count -eq 0) ((Get-Calls) -join ' | ')

    # score-issues -Issue (an EPIC returns right after the read: the read itself is what is proven)
    $scores = Join-Path $work 'scores.json'; Set-Content -LiteralPath $scores -Value '{}'
    $r = Invoke-Down "& '$here\priority\score-issues.ps1' -Issue 123 -Root '$work' -ScoresFile '$scores' -IssuesFile '$work\issues.json'"
    Assert-That 'score-issues -Issue reads the issue over REST while GraphQL is down' ($r.Exit -eq 0 -and $r.Text -match "issue #123 is class 'epics'") $r.Text
}
finally { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue }

if ($failures -gt 0) { "FAILED: $failures case(s)"; exit 1 }
'gh-graphql-down selftest: all cases passed'
