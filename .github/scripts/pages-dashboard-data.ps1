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

The fix has three modes:

  -Mode Read      (publisher, first step of computing its data)
      Writes <Domain>/data/ of the "dashboard-data" branch into -OutDir, so the publisher builds its
      base files (history.json, the previous latest.json) from the durable record and not from the
      live site, which lags behind while a deploy is pending, cancelled or failed. A missing branch
      or a domain the branch does not carry writes nothing and exits 0 (the publisher then falls
      back to the live copy); any other git failure throws (fail closed).

  -Mode Persist   (publisher, before it asks docs.yml to deploy)
      Commits the publisher's freshly computed files for one dashboard to the orphan branch
      "dashboard-data" (layout <domain>/..., like the site). The branch is the durable record of the
      last published data of each dashboard, so a cancelled or failed deploy loses no dashboard data:
      the next deploy, whatever triggered it, reads the branch. Files the publisher leaves out of its overlay
      (guarded, e.g. an empty mutation run) keep their previous committed version; timestamped
      snapshots (yyyy-MM-ddTHHmmssZ.json) of earlier runs are pruned, and the branch keeps a single
      parentless commit replaced with --force-with-lease, so neither the branch nor the site grows
      without bound. A push that loses a race with another publisher is rejected and retried on top
      of the new branch head, so it never drops the other publisher's data.

  -Mode Assemble  (docs.yml deploy job, inside the "pages" concurrency lock)
      Lays the dashboard data over the built site: first the live Pages copy of every managed file
      (a base for dashboards that have not persisted anything yet), then the "dashboard-data" branch
      (always at least as fresh as the live copy). Every file that Jekyll copied from
      docs/<domain>/data (committed latest, history and timestamped snapshots) and the committed
      badges are removed first, except cited-by.json, so they are never published by accident. Any
      failure to read the live site (other than a 404 for a file that dashboard does not publish) or
      the branch fails the deploy, every JSON file of the assembled data (live or from the branch)
      must parse, and every dashboard must end with a latest.json and a history.json.
      cited-by.json is not touched: the build renders it from the docref indexes.

Running inside the lock is what closes the lost-update race: deploys are serialized, and each one
reads the data after the previous deploy and every earlier persist finished.

.PARAMETER Mode
Read, Persist or Assemble.

.PARAMETER Domain
Read and Persist: the dashboard (coverage, benchmarks, load-tests or mutations).

.PARAMETER OutDir
Read only: the directory that receives the files of <Domain>/data/ from the data branch.

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
pwsh .github/scripts/pages-dashboard-data.ps1 -Mode Read -Domain mutations -OutDir base/mutations

.EXAMPLE
pwsh .github/scripts/pages-dashboard-data.ps1 -Mode Persist -Domain mutations -OverlayRoot overlay

.EXAMPLE
pwsh .github/scripts/pages-dashboard-data.ps1 -Mode Assemble -SiteRoot _site
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('Read', 'Persist', 'Assemble')]
    [string] $Mode,

    [ValidateSet('coverage', 'benchmarks', 'load-tests', 'mutations')]
    [string] $Domain,

    [string] $OverlayRoot,

    [string] $OutDir,

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

function New-TempDirectory {
    $path = Join-Path ([IO.Path]::GetTempPath()) "dashboard-data-$([guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path $path | Out-Null
    return $path
}

# Writes the full tree of a commit into an empty directory. git archive reads the object database
# directly, so the sparse checkout of the calling job (docs.yml's deploy job checks out only two
# files) cannot filter the branch content, which a `git worktree add` would inherit.
function Expand-Commit {
    param([string] $Commit, [string] $Destination)
    $archive = Join-Path ([IO.Path]::GetTempPath()) "dashboard-data-$([guid]::NewGuid().ToString('N')).tar"
    try {
        Invoke-Git -Arguments @('archive', '--format=tar', '-o', $archive, $Commit) | Out-Null
        $output = & tar -xf $archive -C $Destination 2>&1
        if ($LASTEXITCODE -ne 0) { throw "tar -xf of $Commit failed: $($output -join ' ')" }
    }
    finally {
        if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive -Force }
    }
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
    $gitDir = [string] ((Invoke-Git -Arguments @('rev-parse', '--absolute-git-dir')).Output | Select-Object -First 1)
    for ($attempt = 1; $attempt -le $maxAttempts; $attempt++) {
        $tree = New-TempDirectory
        $index = Join-Path ([IO.Path]::GetTempPath()) "dashboard-data-$([guid]::NewGuid().ToString('N')).index"
        try {
            # The branch always holds a single parentless commit with the current data (the history
            # of every dashboard lives in its own history.json), so it never grows. Each persist
            # replaces that commit with --force-with-lease: a push that races another publisher is
            # rejected instead of dropping the other publisher's data, and is retried on top of it.
            # The commit is built with plumbing (archive, a private index, write-tree, commit-tree),
            # never a worktree, so the caller's checkout and any sparse-checkout setting play no part.
            $exists = Test-RemoteBranch
            $expected = ''
            if ($exists) {
                Update-RemoteBranchRef
                $expected = [string] ((Invoke-Git -Arguments @('rev-parse', "refs/remotes/origin/$Branch")).Output | Select-Object -First 1)
                Expand-Commit -Commit $expected -Destination $tree
            }
            else {
                Write-Host "Branch $Branch does not exist yet; creating it."
                Set-Content -LiteralPath (Join-Path $tree 'README.md') -Encoding utf8NoBOM -Value @(
                    '# dashboard-data',
                    '',
                    'Last published data of each GitHub Pages dashboard, laid out as on the site',
                    '(`<domain>/data/...`, `<domain>/badge.*`). Written by the publish-* workflows',
                    'and read by the docs.yml deploy job inside the `pages` concurrency lock, so no',
                    'deploy reverts or drops a dashboard''s data (issue #1386). Never edit by hand.'
                )
            }

            $target = Join-Path $tree $Domain
            $targetData = Join-Path $target 'data'
            if (Test-Path -LiteralPath $targetData) {
                Get-ChildItem -LiteralPath $targetData -File |
                    Where-Object { $_.Name -match $SnapshotPattern } |
                    Remove-Item -Force
            }
            Copy-Tree -Source $source -Destination $target

            $env:GIT_INDEX_FILE = $index
            try {
                # --force: no ignore rule of the calling repository may drop a data file.
                $add = & git --git-dir=$gitDir --work-tree=$tree -C $tree -c core.sparseCheckout=false add --all --force . 2>&1
                if ($LASTEXITCODE -ne 0) { throw "git add of the data tree failed: $($add -join ' ')" }
                $treeId = [string] (& git --git-dir=$gitDir write-tree 2>&1 | Select-Object -First 1)
                if ($LASTEXITCODE -ne 0) { throw "git write-tree failed: $treeId" }
            }
            finally {
                Remove-Item Env:GIT_INDEX_FILE -ErrorAction SilentlyContinue
            }

            if ($exists) {
                $currentTreeId = [string] ((Invoke-Git -Arguments @('rev-parse', "$expected^{tree}")).Output | Select-Object -First 1)
                if ($currentTreeId -eq $treeId) {
                    Write-Host "No change for $Domain on $Branch; nothing to push."
                    return
                }
            }
            $commit = [string] ((Invoke-Git -Arguments @(
                        '-c', 'user.name=github-actions[bot]',
                        '-c', 'user.email=github-actions[bot]@users.noreply.github.com',
                        'commit-tree', $treeId, '-m', "dashboard-data: $Domain from run $runId")).Output | Select-Object -First 1)

            # An empty expected value means "the branch must not exist yet".
            # Only a rejection by the lease is retried; any other failure (auth, network, a ruleset)
            # throws at once with git's message.
            $push = Invoke-Git -Arguments @('push', "--force-with-lease=refs/heads/${Branch}:$expected", 'origin', "${commit}:refs/heads/$Branch") -AllowedExitCodes @(0..255)
            if ($push.ExitCode -eq 0) {
                Write-Host "Persisted $Domain to $Branch as $commit (attempt $attempt):"
                Get-ChildItem -LiteralPath $target -Recurse -File |
                    ForEach-Object { '  ' + [IO.Path]::GetRelativePath($tree, $_.FullName).Replace('\', '/') } |
                    Write-Host
                return
            }
            $pushText = $push.Output -join ' '
            if ($pushText -notmatch 'stale info|\[rejected\]|fetch first') {
                throw "git push to $Branch failed with exit code $($push.ExitCode): $pushText"
            }
            Write-Host "Push attempt $attempt of $maxAttempts was rejected by the lease (another publisher moved ${Branch}?): $pushText"
        }
        finally {
            Remove-Item -LiteralPath $tree -Recurse -Force -ErrorAction SilentlyContinue
            Remove-Item -LiteralPath $index -Force -ErrorAction SilentlyContinue
        }
        if ($attempt -lt $maxAttempts) { Start-Sleep -Seconds (5 * $attempt) }
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

# Removes everything Jekyll copied from docs/<domain>/ that this script manages: every file under
# <domain>/data/ except cited-by.json (rendered by the build) and the badges. Committed snapshots
# such as 2026-03-29T101913Z.json would otherwise be published next to the branch's latest one.
function Clear-DomainData {
    param([string] $Root, [string] $Domain)
    $dataDir = Join-Path $Root "$Domain/data"
    if (Test-Path -LiteralPath $dataDir -PathType Container) {
        Get-ChildItem -LiteralPath $dataDir -Recurse -File |
            Where-Object { $_.Name -ne 'cited-by.json' } |
            Remove-Item -Force
    }
    foreach ($file in $RootFiles) {
        $path = Join-Path $Root "$Domain/$file"
        if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
    }
}

# Fails the deploy on the first JSON file of the assembled domain that does not parse, whether it
# came from the live site or from the dashboard-data branch.
function Assert-DomainJsonValid {
    param([string] $Root, [string] $Domain)
    $dataDir = Join-Path $Root "$Domain/data"
    $files = @()
    if (Test-Path -LiteralPath $dataDir -PathType Container) {
        $files += Get-ChildItem -LiteralPath $dataDir -Recurse -File -Filter '*.json'
    }
    $badge = Join-Path $Root "$Domain/badge.json"
    if (Test-Path -LiteralPath $badge) { $files += Get-Item -LiteralPath $badge }
    foreach ($file in $files) {
        try { Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json -Depth 100 | Out-Null }
        catch {
            $relative = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
            throw "Refusing to deploy: $relative is not valid JSON: $($_.Exception.Message)"
        }
    }
}

function Invoke-Assemble {
    if (-not $SiteRoot) { throw 'Assemble needs -SiteRoot.' }
    if (-not (Test-Path -LiteralPath $SiteRoot -PathType Container)) { throw "Site root $SiteRoot does not exist." }
    $SiteRoot = (Resolve-Path -LiteralPath $SiteRoot).Path
    $cacheBuster = if ($env:GITHUB_RUN_ID) { "$($env:GITHUB_RUN_ID)-$($env:GITHUB_RUN_ATTEMPT)" } else { [DateTime]::UtcNow.Ticks }
    $origin = @{}

    foreach ($domain in $Domains) {
        Clear-DomainData -Root $SiteRoot -Domain $domain
        $managed = @($DataFiles | ForEach-Object { "$domain/data/$_" }) + @($RootFiles | ForEach-Object { "$domain/$_" })
        foreach ($relative in $managed) {
            $path = Join-Path $SiteRoot $relative
            if (Save-LiveFile -RelativePath $relative -Destination $path -CacheBuster $cacheBuster) {
                $origin[$relative] = 'live'
            }
        }
    }

    if (Test-RemoteBranch) {
        Update-RemoteBranchRef
        $persisted = New-TempDirectory
        try {
            $head = [string] ((Invoke-Git -Arguments @('rev-parse', "refs/remotes/origin/$Branch")).Output | Select-Object -First 1)
            Expand-Commit -Commit $head -Destination $persisted
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
            Remove-Item -LiteralPath $persisted -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    else {
        Write-Host "Branch $Branch does not exist yet; every dashboard keeps its live data."
    }

    foreach ($domain in $Domains) { Assert-DomainJsonValid -Root $SiteRoot -Domain $domain }

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

function Invoke-Read {
    if (-not $Domain) { throw 'Read needs -Domain.' }
    if (-not $OutDir) { throw 'Read needs -OutDir.' }
    if (-not (Test-RemoteBranch)) {
        Write-Host "Branch $Branch does not exist yet; nothing to read for $Domain."
        return
    }
    Update-RemoteBranchRef
    $head = [string] ((Invoke-Git -Arguments @('rev-parse', "refs/remotes/origin/$Branch")).Output | Select-Object -First 1)
    $persisted = New-TempDirectory
    try {
        Expand-Commit -Commit $head -Destination $persisted
        $source = Join-Path $persisted "$Domain/data"
        if (-not (Test-Path -LiteralPath $source -PathType Container)) {
            Write-Host "$Branch at $head has no $Domain/data; nothing to read."
            return
        }
        Copy-Tree -Source $source -Destination $OutDir
        Write-Host "Read $Domain/data from $Branch at ${head}:"
        Get-ChildItem -LiteralPath $source -File | ForEach-Object { Write-Host "  $($_.Name)" }
    }
    finally {
        Remove-Item -LiteralPath $persisted -Recurse -Force -ErrorAction SilentlyContinue
    }
}

switch ($Mode) {
    'Read' { Invoke-Read }
    'Persist' { Invoke-Persist }
    'Assemble' { Invoke-Assemble }
}
