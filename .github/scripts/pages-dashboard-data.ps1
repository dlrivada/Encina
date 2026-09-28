<#
.SYNOPSIS
Persists and assembles the GitHub Pages dashboard data (coverage, benchmarks, load-tests, mutations)
so no deploy can revert or drop another dashboard's fresh data (issue #1386).

.DESCRIPTION
GitHub Pages uses build_type "workflow": every deploy replaces the whole site, and docs.yml is the
only deployer (#1381). The dashboard data is never committed to main, so every deploy must carry the
current data of all four dashboards. Two races lost data before this script:

  1. Lost update: docs.yml read the live data at the start of a ~15-minute build and deployed it at
     the end, so a publisher deploy that landed in between was reverted (2026-09-27: docs push runs
     36324402010 and 36324661640 reverted the mutation data that run 36323680322 had deployed).
  2. Pending cancellation: the "pages" concurrency group keeps one pending deploy; a newer one
     cancels it, and a publisher's data overlay was lost with the cancelled deploy.

The fix has two halves, one per mode:

  -Mode Persist   (publisher, before it asks docs.yml to deploy)
      Commits the publisher's freshly computed files for one dashboard to the orphan branch
      "dashboard-data" (layout <domain>/..., like the site). The branch is the durable record of the
      last published data of each dashboard, so a cancelled or failed deploy loses nothing: the next
      deploy, whatever triggered it, reads the branch. Files the publisher leaves out of its overlay
      (guarded, e.g. an empty mutation run) keep their previous committed version; timestamped
      snapshots (yyyy-MM-ddTHHmmssZ.json) of earlier runs are pruned, and the branch keeps a single
      parentless commit replaced with --force-with-lease, so neither the branch nor the site grows
      without bound. A push that loses a race with another publisher is rejected and retried on top
      of the new branch head, so it never drops the other publisher's data.

  -Mode Assemble  (docs.yml deploy job, inside the "pages" concurrency lock)
      Lays the dashboard data over the built site: first the live Pages copy of every managed file
      (a base for dashboards that have not persisted anything yet), then the "dashboard-data" branch
      (always at least as fresh as the live copy). The committed, older copies that Jekyll copied from
      docs/<domain>/data are removed first, so they are never published by accident. Any failure to
      read the live site (other than a 404 for a file that dashboard does not publish) or the branch
      fails the deploy, and every dashboard must end with a latest.json and a history.json.
      cited-by.json is not touched: the build renders it from the docref indexes.

Running inside the lock is what closes the lost-update race: deploys are serialized, and each one
reads the data after the previous deploy and every earlier persist finished.

.PARAMETER Mode
Persist or Assemble.

.PARAMETER Domain
Persist only: the dashboard (coverage, benchmarks, load-tests or mutations).

.PARAMETER OverlayRoot
Persist only: directory holding <Domain>/... with the files the publisher computed.

.PARAMETER SiteRoot
Assemble only: the built site directory the data is laid over.

.PARAMETER RepoRoot
A git checkout whose "origin" remote is the repository (credentials persisted for Persist).

.PARAMETER Branch
The data branch; "dashboard-data" unless testing.

.PARAMETER BaseUrl
Assemble only: the live Pages root.

.EXAMPLE
pwsh .github/scripts/pages-dashboard-data.ps1 -Mode Persist -Domain mutations -OverlayRoot overlay

.EXAMPLE
pwsh .github/scripts/pages-dashboard-data.ps1 -Mode Assemble -SiteRoot _site
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Persist', 'Assemble')]
    [string] $Mode,

    [ValidateSet('coverage', 'benchmarks', 'load-tests', 'mutations')]
    [string] $Domain,

    [string] $OverlayRoot,

    [string] $SiteRoot,

    [string] $RepoRoot = (Get-Location).Path,

    [string] $Branch = 'dashboard-data',

    [string] $BaseUrl = 'https://dlrivada.github.io/Encina/'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$Domains = @('coverage', 'benchmarks', 'load-tests', 'mutations')
# Files under <domain>/data/ that the dashboards publish. cited-by.json is excluded on purpose: the
# docs build renders it from the docref indexes and owns it.
$DataFiles = @('latest.json', 'history.json', 'docref-index.json', 'fingerprints.json', 'benchmark-coverage.json')
$RequiredDataFiles = @('latest.json', 'history.json')
# Files under <domain>/ (the badges referenced from the README).
$RootFiles = @('badge.svg', 'badge.json')
$SnapshotPattern = '^\d{4}-\d{2}-\d{2}T\d{6}Z\.json$'

function Invoke-Git {
    param([string[]] $Arguments, [int[]] $AllowedExitCodes = @(0))
    $output = & git -C $RepoRoot @Arguments 2>&1
    $code = $LASTEXITCODE
    if ($AllowedExitCodes -notcontains $code) {
        throw "git $($Arguments -join ' ') failed with exit code ${code}: $($output -join [Environment]::NewLine)"
    }
    return [pscustomobject]@{ ExitCode = $code; Output = $output }
}

# Returns $true when the branch exists on origin, $false when it does not; throws on any other error.
function Test-RemoteBranch {
    # ls-remote --exit-code returns 2 when no ref matches; anything else non-zero is a real failure.
    $result = Invoke-Git -Arguments @('ls-remote', '--exit-code', '--heads', 'origin', "refs/heads/$Branch") -AllowedExitCodes @(0, 2)
    return $result.ExitCode -eq 0
}

function Update-RemoteBranchRef {
    Invoke-Git -Arguments @('fetch', '--no-tags', '--depth=1', 'origin', "+refs/heads/${Branch}:refs/remotes/origin/$Branch") | Out-Null
}

function Copy-Tree {
    param([string] $Source, [string] $Destination)
    New-Item -ItemType Directory -Force -Path $Destination | Out-Null
    Get-ChildItem -LiteralPath $Source -Force | Copy-Item -Destination $Destination -Recurse -Force
}

function Invoke-Persist {
    if (-not $Domain) { throw 'Persist needs -Domain.' }
    if (-not $OverlayRoot) { throw 'Persist needs -OverlayRoot.' }
    $source = Join-Path $OverlayRoot $Domain
    if (-not (Test-Path -LiteralPath $source -PathType Container) -or
        -not (Get-ChildItem -LiteralPath $source -Recurse -File)) {
        throw "Overlay $source is missing or empty; nothing to persist."
    }

    $runId = if ($env:GITHUB_RUN_ID) { $env:GITHUB_RUN_ID } else { 'local' }
    $maxAttempts = 5
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $worktree = Join-Path ([IO.Path]::GetTempPath()) "dashboard-data-$([guid]::NewGuid().ToString('N'))"
        try {
            # The branch always holds a single parentless commit with the current data (the history
            # of every dashboard lives in its own history.json), so it never grows. Each persist
            # replaces that commit with --force-with-lease: a push that races another publisher is
            # rejected instead of dropping the other publisher's data, and is retried on top of it.
            $exists = Test-RemoteBranch
            $expected = ''
            if ($exists) {
                Update-RemoteBranchRef
                $expected = [string] ((Invoke-Git -Arguments @('rev-parse', "refs/remotes/origin/$Branch")).Output | Select-Object -First 1)
                Invoke-Git -Arguments @('worktree', 'add', '--detach', $worktree, $expected) | Out-Null
            }
            else {
                Write-Host "Branch $Branch does not exist yet; creating it."
                Invoke-Git -Arguments @('worktree', 'add', '--detach', $worktree) | Out-Null
            }
            # --orphan keeps the checked-out files and index; the next commit simply has no parent.
            # A unique local name, so a retry never collides with the previous attempt's branch.
            & git -C $worktree checkout --quiet --orphan "$Branch-$([guid]::NewGuid().ToString('N'))" 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "git checkout --orphan failed in $worktree" }
            if (-not $exists) {
                & git -C $worktree rm -r -f --quiet --ignore-unmatch . 2>&1 | Out-Null
                if ($LASTEXITCODE -ne 0) { throw "git rm failed in $worktree" }
                Get-ChildItem -LiteralPath $worktree -Force | Where-Object Name -ne '.git' | Remove-Item -Recurse -Force
                Set-Content -LiteralPath (Join-Path $worktree 'README.md') -Encoding utf8NoBOM -Value @(
                    '# dashboard-data',
                    '',
                    'Last published data of each GitHub Pages dashboard, laid out as on the site',
                    '(`<domain>/data/...`, `<domain>/badge.*`). Written by the publish-* workflows',
                    'and read by the docs.yml deploy job inside the `pages` concurrency lock, so no',
                    'deploy reverts or drops a dashboard''s data (issue #1386). Never edit by hand.'
                )
            }

            $target = Join-Path $worktree $Domain
            $targetData = Join-Path $target 'data'
            if (Test-Path -LiteralPath $targetData) {
                Get-ChildItem -LiteralPath $targetData -File |
                    Where-Object { $_.Name -match $SnapshotPattern } |
                    Remove-Item -Force
            }
            Copy-Tree -Source $source -Destination $target

            & git -C $worktree add -A 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "git add failed in $worktree" }
            if ($exists) {
                & git -C $worktree diff --cached --quiet $expected
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "No change for $Domain on $Branch; nothing to push."
                    return
                }
                if ($LASTEXITCODE -ne 1) { throw "git diff --cached failed in $worktree" }
            }
            & git -C $worktree -c user.name='github-actions[bot]' -c user.email='github-actions[bot]@users.noreply.github.com' `
                commit --quiet -m "dashboard-data: $Domain from run $runId" 2>&1 | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "git commit failed in $worktree" }

            # An empty expected value means "the branch must not exist yet".
            $push = & git -C $worktree push "--force-with-lease=refs/heads/${Branch}:$expected" origin "HEAD:refs/heads/$Branch" 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "Persisted $Domain to $Branch (attempt $attempt):"
                Get-ChildItem -LiteralPath $target -Recurse -File |
                    ForEach-Object { '  ' + [IO.Path]::GetRelativePath($worktree, $_.FullName) } |
                    Write-Host
                return
            }
            Write-Host "Push attempt $attempt of $maxAttempts was rejected (another publisher moved $Branch?): $($push -join ' ')"
        }
        finally {
            if (Test-Path -LiteralPath $worktree) {
                & git -C $RepoRoot worktree remove --force $worktree 2>&1 | Out-Null
                if (Test-Path -LiteralPath $worktree) { Remove-Item -LiteralPath $worktree -Recurse -Force }
                & git -C $RepoRoot worktree prune 2>&1 | Out-Null
            }
        }
        Start-Sleep -Seconds (5 * $attempt)
    }
    throw "Could not persist $Domain to $Branch after $maxAttempts attempts."
}

# Downloads one live file. Returns $true when written, $false on a 404; throws on anything else.
function Save-LiveFile {
    param([string] $RelativePath, [string] $Destination, [string] $CacheBuster)
    $url = "$($BaseUrl.TrimEnd('/'))/$RelativePath`?v=$CacheBuster"
    $maxAttempts = 3
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $status = 0
        $failure = $null
        $download = [IO.Path]::GetTempFileName()
        try {
            # -OutFile keeps the exact bytes; the body is discarded unless the status is 200.
            $response = Invoke-WebRequest -Uri $url -OutFile $download -PassThru -SkipHttpErrorCheck -TimeoutSec 60 -MaximumRetryCount 0
            $status = [int] $response.StatusCode
        }
        catch {
            $failure = $_.Exception.Message
        }
        if ($status -eq 200) {
            if ($RelativePath.EndsWith('.json')) {
                try { Get-Content -LiteralPath $download -Raw | ConvertFrom-Json -Depth 100 | Out-Null }
                catch {
                    Remove-Item -LiteralPath $download -Force
                    throw "Live $RelativePath is not valid JSON: $($_.Exception.Message)"
                }
            }
            New-Item -ItemType Directory -Force -Path (Split-Path -Parent $Destination) | Out-Null
            Move-Item -LiteralPath $download -Destination $Destination -Force
            return $true
        }
        if (Test-Path -LiteralPath $download) { Remove-Item -LiteralPath $download -Force }
        if ($status -eq 404) { return $false }
        $reason = if ($failure) { $failure } else { "HTTP $status" }
        Write-Host "  live $RelativePath attempt $attempt of ${maxAttempts}: $reason"
        if ($attempt -lt $maxAttempts) { Start-Sleep -Seconds (10 * $attempt) }
    }
    throw "Could not read the live $RelativePath from $BaseUrl; refusing to deploy without it."
}

function Invoke-Assemble {
    if (-not $SiteRoot) { throw 'Assemble needs -SiteRoot.' }
    if (-not (Test-Path -LiteralPath $SiteRoot -PathType Container)) { throw "Site root $SiteRoot does not exist." }
    $SiteRoot = (Resolve-Path -LiteralPath $SiteRoot).Path
    $cacheBuster = if ($env:GITHUB_RUN_ID) { "$($env:GITHUB_RUN_ID)-$($env:GITHUB_RUN_ATTEMPT)" } else { [DateTime]::UtcNow.Ticks }
    $origin = @{}

    foreach ($domain in $Domains) {
        $managed = @($DataFiles | ForEach-Object { "$domain/data/$_" }) + @($RootFiles | ForEach-Object { "$domain/$_" })
        foreach ($relative in $managed) {
            # Drop the committed copy Jekyll brought from docs/: only live or persisted data is published.
            $path = Join-Path $SiteRoot $relative
            if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
            if (Save-LiveFile -RelativePath $relative -Destination $path -CacheBuster $cacheBuster) {
                $origin[$relative] = 'live'
            }
        }
    }

    if (Test-RemoteBranch) {
        Update-RemoteBranchRef
        $persisted = Join-Path ([IO.Path]::GetTempPath()) "dashboard-data-$([guid]::NewGuid().ToString('N'))"
        try {
            Invoke-Git -Arguments @('worktree', 'add', '--detach', $persisted, "refs/remotes/origin/$Branch") | Out-Null
            $head = (Invoke-Git -Arguments @('rev-parse', "refs/remotes/origin/$Branch")).Output
            Write-Host "Applying $Branch at $head over the live data."
            foreach ($domain in $Domains) {
                $source = Join-Path $persisted $domain
                if (-not (Test-Path -LiteralPath $source -PathType Container)) {
                    Write-Host "  $domain has no persisted data yet; the live copy is kept."
                    continue
                }
                Copy-Tree -Source $source -Destination (Join-Path $SiteRoot $domain)
                Get-ChildItem -LiteralPath $source -Recurse -File | ForEach-Object {
                    $origin[[IO.Path]::GetRelativePath($persisted, $_.FullName).Replace('\', '/')] = $Branch
                }
            }
        }
        finally {
            & git -C $RepoRoot worktree remove --force $persisted 2>&1 | Out-Null
            if (Test-Path -LiteralPath $persisted) { Remove-Item -LiteralPath $persisted -Recurse -Force }
            & git -C $RepoRoot worktree prune 2>&1 | Out-Null
        }
    }
    else {
        Write-Host "Branch $Branch does not exist yet; every dashboard keeps its live data."
    }

    $missing = @()
    foreach ($domain in $Domains) {
        Write-Host "${domain}:"
        foreach ($file in $RequiredDataFiles) {
            if (-not (Test-Path -LiteralPath (Join-Path $SiteRoot "$domain/data/$file"))) { $missing += "$domain/data/$file" }
        }
        $origin.Keys | Where-Object { $_.StartsWith("$domain/") } | Sort-Object |
            ForEach-Object { Write-Host "  $_ <- $($origin[$_])" }
        $latest = Join-Path $SiteRoot "$domain/data/latest.json"
        if (Test-Path -LiteralPath $latest) {
            $json = Get-Content -LiteralPath $latest -Raw | ConvertFrom-Json -Depth 100
            $runId = if ($json.PSObject.Properties['runId']) { $json.runId } else { '(none)' }
            $stamp = if ($json.PSObject.Properties['timestamp']) { $json.timestamp } else { '(none)' }
            Write-Host "  latest.json runId=$runId timestamp=$stamp"
        }
    }
    if ($missing.Count -gt 0) {
        throw "Refusing to deploy: no live or persisted copy of $($missing -join ', ')."
    }
}

switch ($Mode) {
    'Persist' { Invoke-Persist }
    'Assemble' { Invoke-Assemble }
}
