# Shared GitHub API quota helper (#2568). Dot-source it:  . (Join-Path $PSScriptRoot 'gh-quota.ps1')
#
# Why: the maintainer account has one GraphQL budget (5000 points/hour) shared by every Claude session, watcher and
# script. On 2026-10-21 it reached remaining 0 and every `gh pr|issue|label` call failed, while REST was at 4995/5000.
# Rules for callers:
#   - an operation with a REST equivalent uses REST always (`gh api repos/...`), never `gh pr|issue|label ...`;
#   - an operation that exists only in GraphQL (review threads, Projects v2, auto-merge) calls
#     Wait-GhGraphQLQuota first and Write-GhQuotaUsage after, so it backs off until resetAt instead of failing.
#
# Functions:
#   Get-GhGraphQLQuota [-MaxAgeSeconds 60]   -> { Remaining; ResetAt (UTC DateTime); ReadAtUtc } or $null (unknown)
#   Wait-GhGraphQLQuota -Caller <s> [-MinRemaining 200] [-NoWait]   -> $true when a GraphQL call may proceed
#   Test-GhGraphQLLow [-MinRemaining 200]    -> $true when the known remaining is below the threshold
#   Write-GhQuotaUsage -Caller <s> -Operation <s> [-Api graphql|rest] [-Count 1]   -> usage.csv line
#   Get-GhRepoLabelNames -Repo <owner/name>  -> label names over REST (throws on failure, never cached)
#   Get-GhCheckBuckets -Repo <owner/name> -Sha <sha>   -> check runs + commit statuses of a head sha over REST
#                                              as { Name; Bucket pass|fail|pending|skipping; Url }
#
# The quota is read with `gh api graphql -f query='{ rateLimit { remaining resetAt } }'`, never `gh api rate_limit`
# (its graphql numbers were stale on 2026-10-21). The reading is cached in <repo root>/artifacts/gh-quota/
# graphql-ratelimit.json, shared by every process. A failed read is "unknown" ($null), never zero.
# Overrides for tests: ENCINA_GH_QUOTA_GH (path to a stub gh) and ENCINA_GH_QUOTA_ROOT (root of the artifacts folder).

function Invoke-GhQuotaGh {
    # Runs gh (or the ENCINA_GH_QUOTA_GH stub) and returns @{ Code; Output } with stdout and stderr merged.
    param([Parameter(Mandatory)][string[]]$Arguments)
    $exe = if ($env:ENCINA_GH_QUOTA_GH) { $env:ENCINA_GH_QUOTA_GH } else { 'gh' }
    $global:LASTEXITCODE = 0
    $raw = @(& $exe @Arguments 2>&1)
    $code = $LASTEXITCODE
    # Stdout is what callers parse; Output (stdout + stderr) is what they report. A stray stderr line never breaks parsing.
    $stdout = @($raw | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { [string]$_ })
    [pscustomobject]@{ Code = $code; Stdout = ($stdout -join "`n"); Output = (($raw | ForEach-Object { [string]$_ }) -join "`n") }
}

function Get-GhQuotaDirectory {
    $root = $env:ENCINA_GH_QUOTA_ROOT
    if (-not $root) {
        # The main checkout, so the cache and usage.csv are shared by every worktree and process.
        # Resolved from this script's folder, so it does not depend on the caller's current directory.
        $common = & git -C $PSScriptRoot rev-parse --path-format=absolute --git-common-dir 2>$null
        $root = if ($LASTEXITCODE -eq 0 -and $common) { Split-Path -Parent ([string]$common) } else { (Get-Location).Path }
    }
    $dir = Join-Path $root 'artifacts/gh-quota'
    if (-not (Test-Path -LiteralPath $dir)) { [void](New-Item -ItemType Directory -Force -Path $dir) }
    return $dir
}

function Get-GhGraphQLQuota {
    param([int]$MaxAgeSeconds = 60)
    $cachePath = $null
    try {
        $cachePath = Join-Path (Get-GhQuotaDirectory) 'graphql-ratelimit.json'
        if (Test-Path -LiteralPath $cachePath) {
            $cached = Get-Content -LiteralPath $cachePath -Raw | ConvertFrom-Json
            $readAt = [datetime]::Parse([string]$cached.ReadAtUtc, [cultureinfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal)
            if (((Get-Date).ToUniversalTime() - $readAt).TotalSeconds -lt $MaxAgeSeconds) {
                return [pscustomobject]@{
                    Remaining = [int]$cached.Remaining
                    ResetAt   = [datetime]::Parse([string]$cached.ResetAt, [cultureinfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal)
                    ReadAtUtc = $readAt
                }
            }
        }
    }
    catch { }   # an unreadable cache is a cache miss
    try {
        $r = Invoke-GhQuotaGh @('api', 'graphql', '-f', 'query={ rateLimit { remaining resetAt } }')
        if ($r.Code -ne 0) { return $null }
        $rl = ($r.Stdout | ConvertFrom-Json).data.rateLimit
        if ($null -eq $rl -or $null -eq $rl.remaining) { return $null }
        $reset = [datetime]::Parse([string]$rl.resetAt, [cultureinfo]::InvariantCulture, [Globalization.DateTimeStyles]::AssumeUniversal -bor [Globalization.DateTimeStyles]::AdjustToUniversal)
        $quota = [pscustomobject]@{ Remaining = [int]$rl.remaining; ResetAt = $reset; ReadAtUtc = (Get-Date).ToUniversalTime() }
        try {
            if (-not $cachePath) { $cachePath = Join-Path (Get-GhQuotaDirectory) 'graphql-ratelimit.json' }
            [pscustomobject]@{
                Remaining = $quota.Remaining
                ResetAt   = $quota.ResetAt.ToString('o')
                ReadAtUtc = $quota.ReadAtUtc.ToString('o')
            } | ConvertTo-Json -Compress | Set-Content -LiteralPath $cachePath -Encoding utf8
        }
        catch { }   # a cache that cannot be written only costs one extra call next time
        return $quota
    }
    catch { return $null }
}

function Test-GhGraphQLLow {
    param([int]$MinRemaining = 200)
    $q = Get-GhGraphQLQuota
    return ($null -ne $q -and $q.Remaining -lt $MinRemaining)
}

function Wait-GhGraphQLQuota {
    param(
        [Parameter(Mandatory)][string]$Caller,
        [int]$MinRemaining = 200,
        [switch]$NoWait
    )
    $announced = $false
    $maxAge = 60
    while ($true) {
        $q = Get-GhGraphQLQuota -MaxAgeSeconds $maxAge
        if ($null -eq $q -or $q.Remaining -ge $MinRemaining) { return $true }
        if ($NoWait) { return $false }
        if (-not $announced) {
            [Console]::Error.WriteLine("gh-quota: GraphQL remaining $($q.Remaining) < $MinRemaining; $Caller waits until $($q.ResetAt.ToString('yyyy-MM-dd HH:mm:ss')) UTC")
            $announced = $true
        }
        # Wake once per 60 s to re-check until the window resets (resetAt + 5 s is the latest a new window can start).
        # Never re-check faster than once per 60 s, even when resetAt is closer (the re-read after it sees the new window).
        Start-Sleep -Seconds 60
        $maxAge = 1   # after a sleep the cached reading is stale: read again
    }
}

function Write-GhQuotaUsage {
    param(
        [Parameter(Mandatory)][string]$Caller,
        [Parameter(Mandatory)][string]$Operation,
        [ValidateSet('graphql', 'rest')][string]$Api = 'graphql',
        [int]$Count = 1
    )
    try {
        $path = Join-Path (Get-GhQuotaDirectory) 'usage.csv'
        if (-not (Test-Path -LiteralPath $path)) { Set-Content -LiteralPath $path -Encoding utf8 -Value 'timestampUtc,caller,operation,api,count' }
        $stamp = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        $caller = $Caller.Replace(',', ';'); $op = $Operation.Replace(',', ';')
        Add-Content -LiteralPath $path -Encoding utf8 -Value "$stamp,$caller,$op,$Api,$Count"
    }
    catch { }   # instrumentation never fails the caller
}

function Get-GhCheckBuckets {
    # REST replacement of `gh pr checks` / statusCheckRollup (#2568): the check runs and the combined commit status
    # of one head sha, as { Name; Bucket (pass|fail|pending|skipping); Url }. Throws when a read fails.
    param([Parameter(Mandatory)][string]$Repo, [Parameter(Mandatory)][string]$Sha)
    $buckets = [System.Collections.Generic.List[object]]::new()
    $runs = Invoke-GhQuotaGh @('api', '--paginate', '--slurp', "repos/$Repo/commits/$Sha/check-runs?per_page=100")
    if ($runs.Code -ne 0) { throw "gh api check-runs of $Sha failed (exit $($runs.Code)): $($runs.Output)" }
    foreach ($page in @($runs.Stdout | ConvertFrom-Json)) {
        foreach ($cr in @($page.check_runs)) {
            $bucket = if ($cr.status -ne 'completed') { 'pending' }
            else {
                switch ([string]$cr.conclusion) {
                    { $_ -in 'success', 'neutral' } { 'pass' }
                    'skipped' { 'skipping' }
                    default { 'fail' }   # failure, timed_out, cancelled, action_required, startup_failure, stale
                }
            }
            $buckets.Add([pscustomobject]@{ Name = [string]$cr.name; Bucket = $bucket; Url = [string]$cr.html_url })
        }
    }
    $st = Invoke-GhQuotaGh @('api', "repos/$Repo/commits/$Sha/status?per_page=100")
    if ($st.Code -ne 0) { throw "gh api status of $Sha failed (exit $($st.Code)): $($st.Output)" }
    foreach ($s in @(($st.Stdout | ConvertFrom-Json).statuses)) {
        $bucket = switch ([string]$s.state) { 'success' { 'pass' } 'pending' { 'pending' } default { 'fail' } }   # failure, error
        $buckets.Add([pscustomobject]@{ Name = [string]$s.context; Bucket = $bucket; Url = [string]$s.target_url })
    }
    return $buckets.ToArray()
}

function Get-GhRepoLabelNames {
    param([Parameter(Mandatory)][string]$Repo)
    $r = Invoke-GhQuotaGh @('api', "repos/$Repo/labels", '--paginate', '--jq', '.[].name')
    if ($r.Code -ne 0) { throw "gh api repos/$Repo/labels failed (exit $($r.Code)): $($r.Output)" }
    return @($r.Stdout -split "`r?`n" | Where-Object { $_ -ne '' })
}
