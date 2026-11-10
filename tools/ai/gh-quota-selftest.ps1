# Self-test of gh-quota.ps1 (#2568). No network: a stub gh answers from environment variables.
#   pwsh -NoProfile -File tools/ai/gh-quota-selftest.ps1
$ErrorActionPreference = 'Stop'
$work = Join-Path ([IO.Path]::GetTempPath()) ("gh-quota-selftest-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $work | Out-Null
$stub = Join-Path $work 'gh-stub.ps1'
Set-Content -LiteralPath $stub -Encoding utf8 -Value @'
Add-Content -LiteralPath (Join-Path $env:ENCINA_GH_QUOTA_ROOT 'stub-calls.log') -Value ($args -join ' ')
$joined = $args -join ' '
if ($joined -like 'api graphql*') {
    if ($env:STUB_GRAPHQL -eq 'fail') { 'API rate limit already exceeded'; exit 1 }
    $reset = (Get-Date).ToUniversalTime().AddMinutes(30).ToString('yyyy-MM-ddTHH:mm:ssZ')
    '{"data":{"rateLimit":{"remaining":' + $env:STUB_REMAINING + ',"resetAt":"' + $reset + '"}}}'
    exit 0
}
if ($joined -like 'api repos/*/labels*') {
    if ($env:STUB_LABELS -eq 'fail') { 'HTTP 502: Bad Gateway'; exit 1 }
    "bug`nenhancement`narea-testing`n"
    exit 0
}
exit 1
'@
$env:ENCINA_GH_QUOTA_GH = $stub
$env:ENCINA_GH_QUOTA_ROOT = $work
. (Join-Path $PSScriptRoot 'gh-quota.ps1')

$script:failed = 0
function Test-Case([string]$Name, [scriptblock]$Check) {
    try { if (& $Check) { "PASS $Name" } else { $script:failed++; "FAIL $Name" } }
    catch { $script:failed++; "FAIL $Name ($($_.Exception.Message))" }
}
function Get-StubCallCount { $log = Join-Path $work 'stub-calls.log'; if (Test-Path $log) { @(Get-Content $log | Where-Object { $_ -like 'api graphql*' }).Count } else { 0 } }
function Reset-Quota { Remove-Item (Join-Path $work 'artifacts') -Recurse -Force -ErrorAction SilentlyContinue; Remove-Item (Join-Path $work 'stub-calls.log') -ErrorAction SilentlyContinue }

try {
    # cache hit: the second read inside MaxAgeSeconds makes no call
    Reset-Quota; $env:STUB_GRAPHQL = 'ok'; $env:STUB_REMAINING = '4000'
    $a = Get-GhGraphQLQuota
    $b = Get-GhGraphQLQuota
    Test-Case 'cache hit: one gh call serves two reads' { $a.Remaining -eq 4000 -and $b.Remaining -eq 4000 -and (Get-StubCallCount) -eq 1 -and $b.ResetAt -gt (Get-Date).ToUniversalTime() }
    Test-Case 'cache file lives under artifacts/gh-quota' { Test-Path (Join-Path $work 'artifacts/gh-quota/graphql-ratelimit.json') }

    # cache expiry: MaxAgeSeconds 0 forces a new call and sees the new value
    $env:STUB_REMAINING = '3000'
    $c = Get-GhGraphQLQuota -MaxAgeSeconds 0
    Test-Case 'cache expiry: an expired cache is read again' { $c.Remaining -eq 3000 -and (Get-StubCallCount) -eq 2 }

    # low quota with -NoWait returns $false at once; enough quota returns $true
    Reset-Quota; $env:STUB_REMAINING = '50'
    $sw = [Diagnostics.Stopwatch]::StartNew()
    $low = Wait-GhGraphQLQuota -Caller 'selftest' -MinRemaining 200 -NoWait
    $sw.Stop()
    Test-Case 'low quota with -NoWait returns false without sleeping' { $low -eq $false -and $sw.Elapsed.TotalSeconds -lt 10 }
    Test-Case 'Test-GhGraphQLLow is true below the threshold' { (Test-GhGraphQLLow -MinRemaining 200) -eq $true }
    Test-Case 'Test-GhGraphQLLow is false at or above the threshold' { (Test-GhGraphQLLow -MinRemaining 50) -eq $false }
    Reset-Quota; $env:STUB_REMAINING = '4000'
    Test-Case 'enough quota proceeds' { (Wait-GhGraphQLQuota -Caller 'selftest' -MinRemaining 200) -eq $true }

    # unknown quota (the read fails) is not zero: it proceeds
    Reset-Quota; $env:STUB_GRAPHQL = 'fail'
    Test-Case 'unknown quota reads as $null' { $null -eq (Get-GhGraphQLQuota) }
    Test-Case 'unknown quota proceeds' { (Wait-GhGraphQLQuota -Caller 'selftest' -NoWait) -eq $true }
    Test-Case 'unknown quota is not low' { (Test-GhGraphQLLow) -eq $false }
    $env:STUB_GRAPHQL = 'ok'

    # #3329: stale and low cached readings
    function Write-QuotaCache([int]$Remaining, [datetime]$ResetAt, [datetime]$ReadAt) {
        $dir = Join-Path $work 'artifacts/gh-quota'; New-Item -ItemType Directory -Force -Path $dir | Out-Null
        @{ Remaining = $Remaining; ResetAt = $ResetAt.ToUniversalTime().ToString('o'); ReadAtUtc = $ReadAt.ToUniversalTime().ToString('o') } |
            ConvertTo-Json -Compress | Set-Content -LiteralPath (Join-Path $dir 'graphql-ratelimit.json') -Encoding utf8
    }
    $nowUtc = (Get-Date).ToUniversalTime()
    Reset-Quota; $env:STUB_GRAPHQL = 'ok'; $env:STUB_REMAINING = '4000'
    Write-QuotaCache -Remaining 0 -ResetAt $nowUtc.AddMinutes(-5) -ReadAt $nowUtc
    $s1 = Get-GhGraphQLQuota -MaxAgeSeconds 60
    Test-Case 'a fresh reading whose resetAt has passed is not trusted' { $s1.Remaining -eq 4000 -and $s1.Source -eq 'live' -and (Get-StubCallCount) -eq 1 }
    Reset-Quota
    Write-QuotaCache -Remaining 0 -ResetAt $nowUtc.AddMinutes(20) -ReadAt $nowUtc.AddMinutes(5)
    $s2 = Get-GhGraphQLQuota -MaxAgeSeconds 60
    Test-Case 'a reading dated in the future is not trusted' { $s2.Source -eq 'live' -and $s2.Remaining -eq 4000 }
    Reset-Quota
    Write-QuotaCache -Remaining 59 -ResetAt $nowUtc.AddMinutes(20) -ReadAt $nowUtc
    Test-Case 'a low cached reading is confirmed live: -NoWait proceeds when the real quota is high' { (Wait-GhGraphQLQuota -Caller 'selftest' -NoWait) -eq $true -and (Get-StubCallCount) -eq 1 }
    Reset-Quota
    Write-QuotaCache -Remaining 59 -ResetAt $nowUtc.AddMinutes(20) -ReadAt $nowUtc
    Test-Case 'Test-GhGraphQLLow confirms a low cached reading live' { (Test-GhGraphQLLow) -eq $false }
    Reset-Quota; $env:STUB_REMAINING = '10'
    Write-QuotaCache -Remaining 59 -ResetAt $nowUtc.AddMinutes(20) -ReadAt $nowUtc
    Test-Case 'a low cached reading confirmed low refuses with -NoWait' { (Wait-GhGraphQLQuota -Caller 'selftest' -NoWait) -eq $false }
    Reset-Quota; $env:STUB_GRAPHQL = 'fail'
    Write-QuotaCache -Remaining 59 -ResetAt $nowUtc.AddMinutes(20) -ReadAt $nowUtc
    Test-Case 'a failed confirming read keeps the low cached reading (inconclusive, not recovery)' { (Wait-GhGraphQLQuota -Caller 'selftest' -NoWait) -eq $false }
    $env:STUB_GRAPHQL = 'ok'; $env:STUB_REMAINING = '4000'

    # #3329: rate-limit kinds and back-off
    Reset-Quota
    Test-Case 'a non rate-limit failure is kind none and makes no quota call' { (Get-GhRateLimitKind -Text 'HTTP 502: Bad Gateway') -eq 'none' -and (Get-StubCallCount) -eq 0 }
    Test-Case 'a rate-limit refusal with a healthy quota is secondary' { (Get-GhRateLimitKind -Text 'HTTP 403: API rate limit already exceeded') -eq 'secondary' }
    Test-Case 'the secondary-limit wording is secondary with a healthy quota' { (Get-GhRateLimitKind -Text 'You have exceeded a secondary rate limit') -eq 'secondary' }
    $env:STUB_REMAINING = '3'
    Test-Case 'a rate-limit refusal with an exhausted quota is primary' { (Get-GhRateLimitKind -Text 'API rate limit already exceeded') -eq 'primary' }
    $env:STUB_GRAPHQL = 'fail'
    Test-Case 'a rate-limit refusal when the quota read fails too is primary' { (Get-GhRateLimitKind -Text 'API rate limit already exceeded') -eq 'primary' }
    $env:STUB_GRAPHQL = 'ok'; $env:STUB_REMAINING = '4000'
    $env:ENCINA_GH_QUOTA_BACKOFF_SECONDS = '0'
    Write-QuotaCache -Remaining 4000 -ResetAt $nowUtc.AddMinutes(20) -ReadAt $nowUtc
    $slept = Invoke-GhRateLimitBackoff -Caller 'selftest' -Kind secondary -Attempt 2
    Test-Case 'back-off drops the cached reading and returns the seconds slept' { $slept -eq 0 -and -not (Test-Path (Join-Path $work 'artifacts/gh-quota/graphql-ratelimit.json')) }
    Remove-Item Env:ENCINA_GH_QUOTA_BACKOFF_SECONDS

    # usage.csv
    Reset-Quota
    Write-GhQuotaUsage -Caller 'selftest' -Operation 'reviewThreads #7'
    Write-GhQuotaUsage -Caller 'selftest' -Operation 'labels' -Api rest -Count 3
    $usage = @(Get-Content (Join-Path $work 'artifacts/gh-quota/usage.csv'))
    Test-Case 'usage.csv has the header and one line per call' { $usage.Count -eq 3 -and $usage[0] -eq 'timestampUtc,caller,operation,api,count' }
    Test-Case 'usage.csv line carries caller, operation, api and count' { $usage[1] -match '^\d{4}-\d\d-\d\dT\d\d:\d\d:\d\dZ,selftest,reviewThreads #7,graphql,1$' -and $usage[2] -match ',selftest,labels,rest,3$' }

    # labels over REST
    $env:STUB_LABELS = 'ok'
    $labels = Get-GhRepoLabelNames -Repo 'dlrivada/Encina'
    Test-Case 'label names are parsed from the REST output' { $labels.Count -eq 3 -and $labels[0] -eq 'bug' -and $labels[2] -eq 'area-testing' }
    $env:STUB_LABELS = 'fail'
    Test-Case 'a label listing failure throws' { try { [void](Get-GhRepoLabelNames -Repo 'dlrivada/Encina'); $false } catch { $_.Exception.Message -match '502' } }
}
finally {
    Remove-Item Env:ENCINA_GH_QUOTA_GH, Env:ENCINA_GH_QUOTA_ROOT, Env:STUB_GRAPHQL, Env:STUB_REMAINING, Env:STUB_LABELS, Env:ENCINA_GH_QUOTA_BACKOFF_SECONDS -ErrorAction SilentlyContinue
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
}
if ($script:failed -gt 0) { "FAILED: $($script:failed) case(s)"; exit 1 }
'gh-quota selftest: all cases passed'
