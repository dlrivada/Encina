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
    if ($env:GQD_SETTLED -and $joined -like 'api repos/*/commits/abc/status*') {
        return '{"state":"success","statuses":[{"context":"codecov/patch","state":"success","target_url":"https://x/cov"}]}'
    }
    # GQD_FAILED: one completed failed check, no pending check, a passing status (a failed job, settled on the sha).
    if ($env:GQD_FAILED -and $joined -like 'api --paginate --slurp repos/*/commits/abc/check-runs*') {
        return '[{"check_runs":[{"name":"build","status":"completed","conclusion":"failure","html_url":"https://x/run/1"}]}]'
    }
    if ($env:GQD_FAILED -and $joined -like 'api repos/*/commits/abc/status*') {
        return '{"state":"success","statuses":[{"context":"codecov/patch","state":"success","target_url":"https://x/cov"}]}'
    }
    if ($env:GQD_LOOP -and $joined -like 'api repos/*/pulls/7') {
        if ((Step 'pull') -ge 2) { return '{"head":{"sha":"abc"},"state":"closed","merged":true,"merge_commit_sha":"deadbeef"}' }
        return '{"head":{"sha":"abc"},"state":"open","merged":false,"merge_commit_sha":null}'
    }
    if ($env:GQD_LOOP -and $joined -like 'api --paginate --slurp repos/*/pulls?state=open*') {
        if ((Step 'list') -ge 3) { exit 0 }
    }
    if ($joined -like 'api graphql*' -or $args[0] -in 'pr', 'issue', 'label', 'project') { $global:LASTEXITCODE = 1; return $down }
    switch -Wildcard ($joined) {
        # --- #2661: the REST forms that replaced the GraphQL-backed pr/issue/label/project subcommands ---
        'api repos/*/pulls?state=all*' { return '[{"number":7,"created_at":"2026-10-21T10:00:00Z","merged_at":"2026-10-21T11:00:00Z","state":"closed"},{"number":6,"created_at":"2026-09-01T10:00:00Z","merged_at":null,"state":"closed"}]' }
        'api repos/*/issues?state=all*' { return '[{"number":50,"created_at":"2026-10-21T10:00:00Z","state":"open"},{"number":51,"created_at":"2026-10-21T10:00:00Z","state":"open","pull_request":{}},{"number":49,"created_at":"2026-09-01T10:00:00Z","state":"closed"}]' }
        'api repos/*/issues?state=closed*--slurp*' { return '[[]]' }
        'api repos/*/issues?state=closed*' { return '[{"number":40,"closed_at":"2026-10-21T12:00:00Z","state":"closed"},{"number":41,"closed_at":"2026-10-21T12:00:00Z","state":"closed","pull_request":{}},{"number":42,"closed_at":"2026-08-01T12:00:00Z","state":"closed"}]' }
        'api repos/*/issues?state=open*--slurp*' { return '[[{"number":1,"title":"Issue 1","html_url":"https://github.com/dlrivada/Encina/issues/1"},{"number":2,"title":"A pull request","html_url":"https://github.com/dlrivada/Encina/pull/2","pull_request":{}}]]' }
        'api repos/*/pulls?state=open*--slurp*' { return '[[{"number":11,"title":"Open PR","head":{"ref":"fix/b-9"},"body":"Fixes #9","draft":false,"created_at":"2026-10-21T00:00:00Z","merged_at":null,"state":"open","user":{"login":"me","type":"User"}}]]' }
        'api repos/*/pulls?state=closed*' { return '[]' }
        'api repos/*/pulls/12' { return '{"number":12,"title":"Old PR","head":{"ref":"x"},"body":"","state":"closed","merged_at":"2026-09-01T00:00:00Z","created_at":"2026-08-30T00:00:00Z","user":{"login":"me","type":"User"}}' }
        'api repos/*/pulls/9' { return '{"number":9,"merged_at":"2026-10-01T00:00:00Z","state":"closed"}' }
        'api repos/*/pulls/9/files*' { return 'src/a.cs' }
        'api repos/*/issues/8' { return '{"title":"Issue 8","state":"closed","state_reason":"completed","closed_at":"2026-10-02T00:00:00Z"}' }
        'api repos/*/issues/9' { return '{"title":"Issue 9","state":"open","state_reason":null,"closed_at":null}' }
        'api repos/*/issues/55*' { return 'closed' }
        'api repos/*/issues/56/timeline*' { return '[[]]' }
        'api repos/*/issues/56/comments*' { return '[[{"user":{"login":"human"},"body":"a human comment"},{"user":{"login":"coderabbitai[bot]"},"body":"bot noise"}]]' }
        'api repos/*/issues/56' { return '{"number":56,"title":"Closed issue 56","state":"closed","closed_at":"2026-10-01T00:00:00Z","labels":[{"name":"bug"}],"body":"the issue body"}' }
        'api -X POST repos/dlrivada/Encina/issues -f*' { return 'https://github.com/dlrivada/Encina/issues/1001' }
        'api -X POST repos/*/labels*' { return '{}' }
        'api repos/*/milestones*' { return "14`tv0.14.0 $([char]0x2014) Hardening" }
        'api repos/*/labels*' { if ($env:GQD_LABELS_ALL) { return @('bug', 'technical-debt', 'area-testing', 'p0-mandatory', 'p1-recommended', 'p2-post-1.0') } return @('bug', 'enhancement', 'technical-debt') }
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

    # watch-pr-events: a failed check on an unchanged sha is re-read on the next poll (a re-run is seen), and its failure is announced once
    Get-ChildItem "$callLog*" -ErrorAction SilentlyContinue | Remove-Item -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$here\watch-pr-events.ps1' -Pr 7 -IntervalSeconds 1" @{ GQD_LOOP = '1'; GQD_FAILED = '1' }
    $loopCalls = Get-Calls
    Assert-That 'watch-pr-events re-reads check runs on an unchanged sha while a check failed (two check-runs calls)' (@($loopCalls | Where-Object { $_ -like '*check-runs*' }).Count -eq 2) ($loopCalls -join ' | ')
    Assert-That 'watch-pr-events announces the same failed check once' (@([regex]::Matches($r.Text, 'CHECK-FAIL build')).Count -eq 1) $r.Text

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

    # watch-open-prs: a failed check on an unchanged sha is re-read on the next poll, and its failure is announced once
    Get-ChildItem "$callLog*" -ErrorAction SilentlyContinue | Remove-Item -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$here\watch-open-prs.ps1' -IntervalSeconds 1" @{ GQD_LOOP = '1'; GQD_FAILED = '1' }
    $loopCalls = Get-Calls
    Assert-That 'watch-open-prs re-reads check runs on an unchanged sha while a check failed (two check-runs calls)' (@($loopCalls | Where-Object { $_ -like '*check-runs*' }).Count -eq 2) ($loopCalls -join ' | ')
    Assert-That 'watch-open-prs announces the same failed check once' (@([regex]::Matches($r.Text, 'CHECK-FAIL build')).Count -eq 1) $r.Text

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

    # --- #2661 -----------------------------------------------------------------------------------------------------
    function Get-GraphQlBackedCalls { @(Get-Calls | Where-Object { $_ -match '^(pr|issue|label|project) ' -or ($_ -like 'api graphql*' -and $_ -notlike '*rateLimit*') }) }

    # apply-priority-labels without -DryRun: label creation and the label edit are REST POSTs
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $csv5 = Join-Path $work 'classification5.csv'; $out5 = Join-Path $work 'final5.csv'
    Set-Content -LiteralPath $csv5 -Encoding utf8 -Value @('number,priority,confidence,milestone,title,reason', '5,P1,high,m,"[DEBT] x",r')
    $r = Invoke-Down "& '$here\apply-priority-labels.ps1' -Csv '$csv5' -Out '$out5'"
    Assert-That 'apply-priority-labels applies the label over REST while GraphQL is down' ($r.Exit -eq 0 -and $r.Text -match 'applied=1 failed=0') $r.Text
    Assert-That 'apply-priority-labels edits through a REST POST on the issue labels' (@(Get-Calls | Where-Object { $_ -eq 'api -X POST repos/dlrivada/Encina/issues/5/labels -f labels[]=p1-recommended' }).Count -eq 1) ((Get-Calls) -join ' | ')
    Assert-That 'apply-priority-labels calls no GraphQL-backed gh subcommand' ((Get-GraphQlBackedCalls).Count -eq 0) ((Get-Calls) -join ' | ')

    # check-project-membership: the open issues are read over REST (the pull request item is dropped); the project
    # read needs GraphQL, so with GraphQL down it reports the quota and exits 2 (the existing failure path)
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$here\issues\check-project-membership.ps1'"
    Assert-That 'check-project-membership reads the open issues over REST and exits 2 on the project read' ($r.Exit -eq 2 -and $r.Text -match 'GraphQL quota is below its threshold' -and @(Get-Calls | Where-Object { $_ -like 'api repos/dlrivada/Encina/issues?state=open*' }).Count -eq 1) ($r.Text + ' | ' + ((Get-Calls) -join ' | '))
    Assert-That 'check-project-membership makes no gh project call while GraphQL is low' ((Get-GraphQlBackedCalls).Count -eq 0) ((Get-Calls) -join ' | ')

    # build-board-stats: the three windows are REST list endpoints (newest first, stopping at -Since)
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $statsOut = Join-Path $work 'board'; $statsProject = Join-Path $work 'claude-project'
    New-Item -ItemType Directory -Force -Path $statsProject | Out-Null
    $r = Invoke-Down "& '$here\board\build-board-stats.ps1' -Since 2026-10-20 -OutDir '$statsOut' -ProjectDir '$statsProject'"
    $gh = if (Test-Path (Join-Path $statsOut 'github.json')) { Get-Content (Join-Path $statsOut 'github.json') -Raw | ConvertFrom-Json } else { $null }
    Assert-That 'build-board-stats reads the PR and issue windows over REST while GraphQL is down' ($r.Exit -eq 0 -and $null -ne $gh) $r.Text
    Assert-That 'build-board-stats keeps only items inside the window, drops pull requests from the issue lists' ($null -ne $gh -and @($gh.prs).Count -eq 1 -and $gh.prs[0].state -eq 'MERGED' -and @($gh.issuesOpened).Count -eq 1 -and $gh.issuesOpened[0].number -eq 50 -and @($gh.issuesClosed).Count -eq 1 -and $gh.issuesClosed[0].number -eq 40) ($gh | ConvertTo-Json -Depth 5 -Compress)
    Assert-That 'build-board-stats calls no GraphQL-backed gh subcommand' ((Get-GraphQlBackedCalls).Count -eq 0) ((Get-Calls) -join ' | ')

    # reconcile-board: open PRs, merged window, closed issues, single issue and PR reads
    $probe = Join-Path $work 'reconcile-probe.ps1'
    Set-Content -LiteralPath $probe -Encoding utf8 -Value @'
param([string]$Here, [string]$Work)
. (Join-Path $Here 'board\reconcile-board.ps1')
$docs = @{
    'flow/8'  = @{ Collection = 'flow'; Id = '8';  Data = @{ status = 'ready'; issue = 8 } }
    'flow/9'  = @{ Collection = 'flow'; Id = '9';  Data = @{ status = 'ready'; issue = 9 } }
    'work/12' = @{ Collection = 'work'; Id = '12'; Data = @{ status = 'pr-open'; pr = 12 } }
}
$facts = Get-BoardFacts $docs 'o/r' $Work ([datetime]'2026-10-22') 14
"OPEN=$(@($facts.OpenPrs).Count):$(@($facts.OpenPrs)[0].number):$(@($facts.OpenPrs)[0].closes -join '+') MERGED=$(@($facts.MergedPrs).Count) CLOSED=$(@($facts.ClosedIssues.Keys) -join '+'):$($facts.ClosedIssues['8'].reason) EXTRA=$($facts.ExtraPrs['12'].state)"
exit 0   # the worktree listing of a non-repository Work folder leaves a native exit code behind
'@
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$probe' -Here '$here' -Work '$work'"
    Assert-That 'reconcile-board reads open PRs, the issue and the older PR over REST while GraphQL is down' ($r.Exit -eq 0 -and $r.Text -match 'OPEN=1:11:9 MERGED=0 CLOSED=8:COMPLETED EXTRA=MERGED') $r.Text
    Assert-That 'reconcile-board calls no GraphQL-backed gh subcommand' ((Get-GraphQlBackedCalls).Count -eq 0) ((Get-Calls) -join ' | ')

    # audit-next -Issue: the issue state is read over REST ("closed", mapped to CLOSED); the run then stops at the
    # missing origin remote, which proves the state check passed
    $fx = Join-Path $work 'fx-audit'
    New-Item -ItemType Directory -Force -Path $fx | Out-Null
    & git init -q -b main $fx
    Copy-Item -Recurse -Force (Join-Path $here 'audit') (Join-Path $fx 'tools\ai\audit')
    Copy-Item -Force (Join-Path $here 'gh-quota.ps1') (Join-Path $fx 'tools\ai\gh-quota.ps1')
    New-Item -ItemType Directory -Force -Path (Join-Path $fx 'artifacts\knowledge\predraft\raw') | Out-Null
    Set-Content -LiteralPath (Join-Path $fx 'artifacts\knowledge\audit-queue.txt') -Value '55'
    Set-Content -LiteralPath (Join-Path $fx 'artifacts\knowledge\predraft\55.md') -Value 'predraft'
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$fx\tools\ai\audit\audit-next.ps1' -Issue 55"
    Assert-That 'audit-next -Issue reads the issue state over REST while GraphQL is down' ($r.Text -notmatch 'is not CLOSED' -and $r.Text -notmatch 'over REST failed' -and $r.Text -match 'git fetch origin main failed' -and @(Get-Calls | Where-Object { $_ -eq 'api repos/dlrivada/Encina/issues/55 --jq .state' }).Count -eq 1) ($r.Text + ' | ' + ((Get-Calls) -join ' | '))
    Assert-That 'audit-next calls no GraphQL-backed gh subcommand' ((Get-GraphQlBackedCalls).Count -eq 0) ((Get-Calls) -join ' | ')

    # classify-scope: the linked PR and its files are read over REST
    New-Item -ItemType Directory -Force -Path (Join-Path $fx 'artifacts\knowledge\predraft\raw') | Out-Null
    Set-Content -LiteralPath (Join-Path $fx 'artifacts\knowledge\predraft\raw\77.txt') -Value 'LINKED PR #9: Fix (state closed)'
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$fx\tools\ai\audit\classify-scope.ps1' -Issue 77"
    Assert-That 'classify-scope reads the merged PR files over REST while GraphQL is down' ($r.Exit -eq 0 -and $r.Text -match '77 total=1 alive=0 class=code-removed') ($r.Text + ' | ' + ((Get-Calls) -join ' | '))
    Assert-That 'classify-scope calls no GraphQL-backed gh subcommand' ((Get-GraphQlBackedCalls).Count -eq 0) ((Get-Calls) -join ' | ')

    # qwen-predraft -Issue: the issue and its comments are read over REST (a stub local-ai-ask ends the run
    # after the input file is written, so no model is called)
    New-Item -ItemType Directory -Force -Path (Join-Path $fx 'tools\ai') | Out-Null
    Set-Content -LiteralPath (Join-Path $fx 'tools\ai\local-ai-ask.cs') -Value 'return 1;'
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$fx\tools\ai\audit\qwen-predraft.ps1' -Issue 56"
    $rawIn = Join-Path $fx 'artifacts\knowledge\predraft\raw\56.txt'
    $rawText = if (Test-Path $rawIn) { Get-Content $rawIn -Raw } else { '' }
    Assert-That 'qwen-predraft reads the issue and its human comments over REST while GraphQL is down' ($rawText -match 'ISSUE #56: Closed issue 56' -and $rawText -match 'the issue body' -and $rawText -match 'a human comment' -and $rawText -notmatch 'bot noise') ($r.Text + ' | ' + $rawText)
    Assert-That 'qwen-predraft calls no GraphQL-backed gh subcommand' ((Get-GraphQlBackedCalls).Count -eq 0) ((Get-Calls) -join ' | ')

    # open-remediation: the issue is created over REST; the project add (GraphQL only) warns with the retry command
    $fo = Join-Path $work 'fx-remediation'
    New-Item -ItemType Directory -Force -Path (Join-Path $fo 'artifacts\knowledge\remediation') | Out-Null
    & git init -q -b main $fo
    Copy-Item -Recurse -Force (Join-Path $here 'audit') (Join-Path $fo 'tools\ai\audit')
    Copy-Item -Force (Join-Path $here 'gh-quota.ps1') (Join-Path $fo 'tools\ai\gh-quota.ps1')
    Set-Content -LiteralPath (Join-Path $fo 'artifacts\knowledge\remediation\3-bug.md') -Value "<!--`ntitle: [BUG] Something is wrong`nlabels: bug`nmilestone:`nkind: bug`n-->`n`n## Description`n`nBUG DESCRIPTION.`n"
    Remove-Item $callLog -ErrorAction SilentlyContinue
    $r = Invoke-Down "& '$fo\tools\ai\audit\open-remediation.ps1' -Issue 3" @{ GQD_LABELS_ALL = '1' }
    $createCalls = @(Get-Calls | Where-Object { $_ -like 'api -X POST repos/dlrivada/Encina/issues -f title=*' })
    Assert-That 'open-remediation creates the issue over REST while GraphQL is down' ($r.Exit -eq 0 -and $createCalls.Count -eq 1 -and $createCalls[0].Contains('-F milestone=14') -and $createCalls[0].Contains('-f labels[]=bug') -and $r.Text -match 'issues/1001') ($r.Text + ' | ' + ((Get-Calls) -join ' | '))
    Assert-That 'open-remediation warns about the project add with the retry command and keeps the row' ($r.Text -match 'gh project item-add failed' -and $r.Text -match 'gh project item-add 1 --owner dlrivada --url https://github.com/dlrivada/Encina/issues/1001' -and (Get-Content (Join-Path $fo 'artifacts\knowledge\remediation\opened.csv') -Raw) -match '3-bug.md,https://github.com/dlrivada/Encina/issues/1001') $r.Text
    Assert-That 'open-remediation calls no GraphQL-backed gh subcommand' ((Get-GraphQlBackedCalls).Count -eq 0) ((Get-Calls) -join ' | ')
}
finally { Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue }

if ($failures -gt 0) { "FAILED: $failures case(s)"; exit 1 }
'gh-graphql-down selftest: all cases passed'
