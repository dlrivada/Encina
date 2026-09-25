param(
    [string]$Root = (Split-Path -Parent (Split-Path -Parent $PSScriptRoot)),
    [int]$IntervalSeconds = 120,
    [int]$StallMinutes = 20,
    [switch]$Once
)

# Polls the worktrees under <Root>\.claude\worktrees\ and reports when one stops changing. No token cost:
# no gh call, no model call, only git and the filesystem. Emits:
#   WATCHING worktrees: <names>                        (first poll; "none" when there are no worktrees)
#   STALLED <name> <branch> <minutes>m since <last change utc>   (once, when a fingerprint has not
#                                                                  changed for StallMinutes)
#   RESUMED <name>                                       (once, when a previously STALLED worktree's
#                                                          fingerprint changes again)
#   REMOVED <name>                                       (when a previously seen worktree disappears)
#   WARN <message>                            (a transient git failure; the previous state is kept and no
#                                              REMOVED/STALLED/RESUMED is derived from a partial result)
#
# A worktree's fingerprint is its HEAD sha + `git status --porcelain` + a SHA256 of `git diff HEAD` + the
# newest LastWriteTimeUtc across the files under <worktree>\artifacts and the files `git status --porcelain`
# lists: an agent that keeps editing tracked files (an ` M <file>` status with HEAD unchanged) must never
# read as stalled even when none of those edits land under artifacts\. -Once does a single poll and exits 0,
# for verification and tests.

$ErrorActionPreference = 'Continue'

function Get-Worktrees([string]$Root) {
    # Trailing separator so a sibling directory whose name merely starts with "worktrees" (e.g.
    # .claude\worktrees-backup\) is never matched as a segment-boundary prefix.
    $prefix = ((Join-Path $Root '.claude\worktrees') -replace '\\', '/') + '/'
    $lines = & git -C $Root worktree list --porcelain 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git worktree list failed: $lines" }
    $result = @{}
    $path = $null
    $branch = $null
    foreach ($line in $lines) {
        if ($line -like 'worktree *') {
            $path = $line.Substring('worktree '.Length)
        }
        elseif ($line -like 'branch *') {
            $branch = $line.Substring('branch '.Length) -replace '^refs/heads/', ''
        }
        elseif ([string]::IsNullOrWhiteSpace($line)) {
            if ($path) {
                $normalized = $path -replace '\\', '/'
                if ($normalized.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
                    $name = Split-Path -Leaf $path
                    $result[$name] = [pscustomobject]@{ Path = $path; Branch = $branch }
                }
            }
            $path = $null; $branch = $null
        }
    }
    if ($path) {
        $normalized = $path -replace '\\', '/'
        if ($normalized.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
            $name = Split-Path -Leaf $path
            $result[$name] = [pscustomobject]@{ Path = $path; Branch = $branch }
        }
    }
    return $result
}

function Get-Fingerprint([string]$WtPath) {
    $head = & git -C $WtPath rev-parse HEAD 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git rev-parse HEAD failed in ${WtPath}: $head" }
    $status = & git -C $WtPath status --porcelain 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git status --porcelain failed in ${WtPath}: $status" }
    $diffOutput = & git -C $WtPath diff HEAD 2>&1
    if ($LASTEXITCODE -ne 0) { throw "git diff HEAD failed in ${WtPath}: $diffOutput" }

    $diffText = ($diffOutput -join "`n")
    $diffHash = ''
    if ($diffText) {
        $sha256 = [System.Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha256.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($diffText))
            $diffHash = [Convert]::ToHexString($bytes)
        }
        finally {
            $sha256.Dispose()
        }
    }

    $newest = [DateTime]::MinValue
    $found = $false
    $artifactsDir = Join-Path $WtPath 'artifacts'
    if (Test-Path -LiteralPath $artifactsDir) {
        $latest = Get-ChildItem -LiteralPath $artifactsDir -Recurse -File -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if ($latest) { $newest = $latest.LastWriteTimeUtc; $found = $true }
    }
    # Also consider the mtime of every file `git status --porcelain` lists (tracked edits, renames,
    # untracked files), not only artifacts\: an agent editing a tracked file elsewhere in the tree must move
    # the fingerprint too.
    foreach ($line in @($status)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        $relative = $line.Substring([Math]::Min(3, $line.Length)).Trim().Trim('"')
        if ($relative -match '^(.+) -> (.+)$') { $relative = $Matches[2] }
        if (-not $relative) { continue }
        $full = Join-Path $WtPath ($relative -replace '/', '\')
        if (Test-Path -LiteralPath $full -PathType Leaf) {
            $mtime = (Get-Item -LiteralPath $full).LastWriteTimeUtc
            if ($mtime -gt $newest) { $newest = $mtime; $found = $true }
        }
    }
    $newestText = if ($found) { $newest.ToString('o') } else { '' }

    return "$head|$($status -join "`n")|$diffHash|$newestText"
}

# name -> @{ Fingerprint; LastChangeUtc; Stalled; Branch }
$state = @{}
$first = $true

while ($true) {
    try {
        $current = Get-Worktrees $Root
        $now = (Get-Date).ToUniversalTime()

        foreach ($name in @($state.Keys)) {
            if (-not $current.ContainsKey($name)) {
                Write-Output "REMOVED $name"
                $state.Remove($name)
            }
        }

        foreach ($name in $current.Keys) {
            $wt = $current[$name]
            $fingerprint = Get-Fingerprint $wt.Path
            if (-not $state.ContainsKey($name)) {
                $state[$name] = @{ Fingerprint = $fingerprint; LastChangeUtc = $now; Stalled = $false; Branch = $wt.Branch }
                continue
            }
            $entry = $state[$name]
            $entry.Branch = $wt.Branch
            if ($fingerprint -ne $entry.Fingerprint) {
                $entry.Fingerprint = $fingerprint
                $entry.LastChangeUtc = $now
                if ($entry.Stalled) {
                    $entry.Stalled = $false
                    Write-Output "RESUMED $name"
                }
            }
            elseif (-not $entry.Stalled) {
                $minutes = [int](($now - $entry.LastChangeUtc).TotalMinutes)
                if ($minutes -ge $StallMinutes) {
                    $entry.Stalled = $true
                    $lastChange = $entry.LastChangeUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
                    Write-Output "STALLED $name $($entry.Branch) ${minutes}m since $lastChange"
                }
            }
        }

        if ($first) {
            $names = @($current.Keys | Sort-Object)
            $list = if ($names.Count -eq 0) { 'none' } else { $names -join ', ' }
            Write-Output "WATCHING worktrees: $list"
            $first = $false
        }
    }
    catch {
        Write-Output "WARN $($_.Exception.Message)"
    }

    if ($Once) { exit 0 }
    Start-Sleep -Seconds $IntervalSeconds
}
