# Shared by the tools/ai/audit/ scripts and by .claude/hooks/audit-stage-guard.ps1 (#1345): the SPEC-003
# audit pipeline definition and the paths of the audit's persistent state. The pipeline (stage order, agent,
# minimum model and artifact name) is never hard-coded here or in any caller -- it is read from
# tools/ai/audit/pipeline.json, so a reorder or a renamed artifact is a one-file edit.
#
# Audit data is unversioned, in the MAIN checkout (the repository root, resolved as the parent of git's
# common directory -- never the current working directory, which may be inside a `wia-<n>` worktree):
#   artifacts/knowledge/open-audits/<n>.json one per open audit (#2234; see "Open audits" below)
#   artifacts/knowledge/audit-queue.txt      closed issue numbers, ascending, one per line
#   artifacts/knowledge/progress.csv         history of finished audits
#   artifacts/knowledge/issues|audits|remediation|predraft|stages/<n>/   collected outputs
#
# Stage artifacts live in the audit worktree, on a local branch `audit/<n>` (never pushed), under
# artifacts/knowledge/stages/<file>. A stage counts as done only when its artifact file exists AND a commit
# on that branch carries the trailer `Stage: <name>` (audit-commit-stage.ps1 makes that commit) -- an
# artifact written but not committed is not yet "done" for ordering purposes (the maintainer's two
# additions to #1345 phase A).

# The main checkout: the parent of git's common directory (`git rev-parse --git-common-dir`), which for a
# worktree resolves to the MAIN repository's .git folder regardless of $From's own location or the current
# working directory (#1345 decision: audit data is anchored to the checkout, not to cwd).
function Get-MainRoot([string]$From) {
    if ([string]::IsNullOrWhiteSpace($From)) { $From = (Get-Location).Path }
    $common = & git -C $From rev-parse --git-common-dir 2>$null | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($common)) { throw "Get-MainRoot: '$From' is not inside a git repository." }
    # git prints an ABSOLUTE path here for a worktree, relative (usually '.git') for the main checkout.
    # PowerShell's Join-Path (unlike [IO.Path]::Combine) does not special-case a rooted second argument, so
    # it must be handled explicitly or a worktree's common-dir corrupts into '$From\D:\...\.git'.
    $full = if ([IO.Path]::IsPathRooted($common)) { [IO.Path]::GetFullPath($common) } else { [IO.Path]::GetFullPath((Join-Path $From $common)) }
    return (Split-Path -Parent $full)
}

function Get-KnowledgeRoot([string]$MainRoot) { Join-Path $MainRoot 'artifacts\knowledge' }

# ---------------------------------------------------------------------------------------------------------------
# Open audits (#2234). Several audits may run at once, each individually: its own wia-<n> worktree, its own six
# stage agents, its own verifier loop and its own knowledge pull request, exactly as if they ran one after the
# other (maintainer decision of 2026-10-10: never a batch). The limit is pipeline.json maxParallelAudits (2-5,
# default 2). Every open audit has its own state file artifacts/knowledge/open-audits/<n>.json:
#   { issue, worktree, branch, startedUtc, [mode, set], scope: [src packages], concurrent: [issues] }
# `scope` is the set of packages under src/ the audit touches (the overlap rule of audit-next.ps1); `concurrent`
# lists every other audit that was open at some time during this one (the cross-audit duplicate check of
# audit-draft-remediation.ps1 -Prepare and open-remediation.ps1 compares drafts with theirs).
#
# Migration: the single artifacts/knowledge/current-audit.json of the one-audit pipeline is converted into
# open-audits/<n>.json the first time any script reads the open audits (Convert-LegacyCurrentAudit), so an audit
# that was open when #2234 landed keeps working. The hooks read that file too until it is converted.
# ---------------------------------------------------------------------------------------------------------------

function Get-LegacyCurrentAuditPath([string]$MainRoot) { Join-Path (Get-KnowledgeRoot $MainRoot) 'current-audit.json' }

function Get-OpenAuditsDir([string]$MainRoot) { Join-Path (Get-KnowledgeRoot $MainRoot) 'open-audits' }

function Get-OpenAuditPath([string]$MainRoot, [int]$Issue) { Join-Path (Get-OpenAuditsDir $MainRoot) "$Issue.json" }

# Writes the state file of one open audit atomically (temp file next to it, then an overwriting move), so a hook
# reading it concurrently never sees a half-written file.
function Save-OpenAudit([string]$MainRoot, $Audit) {
    $dir = Get-OpenAuditsDir $MainRoot
    New-Item -ItemType Directory -Force $dir | Out-Null
    $path = Get-OpenAuditPath $MainRoot ([int]$Audit.issue)
    $temp = Join-Path $dir ".$([int]$Audit.issue).json.$PID.$([guid]::NewGuid().ToString('N')).tmp"
    try {
        [IO.File]::WriteAllText($temp, ($Audit | ConvertTo-Json -Depth 5), [Text.UTF8Encoding]::new($false))
        [IO.File]::Move($temp, $path, $true)
        $temp = $null
    }
    finally { if ($null -ne $temp -and (Test-Path -LiteralPath $temp)) { Remove-Item -LiteralPath $temp -Force -ErrorAction SilentlyContinue } }
}

function Remove-OpenAudit([string]$MainRoot, [int]$Issue) {
    $path = Get-OpenAuditPath $MainRoot $Issue
    if (Test-Path -LiteralPath $path) { Remove-Item -LiteralPath $path -Force }
}

# An ordered copy of a state object's properties (ConvertFrom-Json gives a PSCustomObject).
function ConvertTo-AuditState($Object) {
    $state = [ordered]@{}
    foreach ($p in $Object.PSObject.Properties) { $state[$p.Name] = $p.Value }
    if (-not $state.Contains('scope') -or $null -eq $state['scope']) { $state['scope'] = @() }
    if (-not $state.Contains('concurrent') -or $null -eq $state['concurrent']) { $state['concurrent'] = @() }
    $state['scope'] = @($state['scope'] | ForEach-Object { [string]$_ })
    $state['concurrent'] = @($state['concurrent'] | ForEach-Object { [int]$_ })
    return [pscustomobject]$state
}

# Converts a pre-#2234 current-audit.json into open-audits/<n>.json (scope from the issue's pre-draft and commits,
# no concurrent audit) and deletes it. An unreadable file throws: nothing is guessed (fail closed).
function Convert-LegacyCurrentAudit([string]$MainRoot) {
    $legacy = Get-LegacyCurrentAuditPath $MainRoot
    if (-not (Test-Path -LiteralPath $legacy)) { return }
    try { $old = Get-Content -LiteralPath $legacy -Raw | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "artifacts/knowledge/current-audit.json (the pre-#2234 open audit) cannot be read: $($_.Exception.Message); repair it so it can be converted into artifacts/knowledge/open-audits/<n>.json." }
    if ([string]$old.issue -notmatch '^\d+$' -or [string]::IsNullOrWhiteSpace([string]$old.worktree)) { throw 'artifacts/knowledge/current-audit.json (the pre-#2234 open audit) has no issue number or worktree; repair it so it can be converted.' }
    $issue = [int]$old.issue
    if (-not (Test-Path -LiteralPath (Get-OpenAuditPath $MainRoot $issue))) {
        $state = ConvertTo-AuditState $old
        if (@($state.scope).Count -eq 0) { $state.scope = @(Get-IssueScopePackages $MainRoot $issue) }
        Save-OpenAudit $MainRoot $state
    }
    Remove-Item -LiteralPath $legacy -Force
    Write-Host "audit: converted artifacts/knowledge/current-audit.json (audit #$issue) into artifacts/knowledge/open-audits/$issue.json (#2234)."
}

# Every open audit, ordered by issue number. Converts a legacy current-audit.json first. A state file that cannot
# be read, or whose issue does not match its name, throws (fail closed: a script never guesses which audits run).
function Get-OpenAudits([string]$MainRoot) {
    Convert-LegacyCurrentAudit $MainRoot
    $dir = Get-OpenAuditsDir $MainRoot
    if (-not (Test-Path -LiteralPath $dir -PathType Container)) { return @() }
    $audits = foreach ($file in @(Get-ChildItem -LiteralPath $dir -Filter '*.json' -File)) {
        try { $raw = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json -ErrorAction Stop }
        catch { throw "the open-audit state file $($file.FullName) cannot be read: $($_.Exception.Message)" }
        if ("$([string]$raw.issue).json" -ne $file.Name -or [string]::IsNullOrWhiteSpace([string]$raw.worktree)) { throw "the open-audit state file $($file.FullName) does not name its own issue and a worktree." }
        ConvertTo-AuditState $raw
    }
    return @($audits | Sort-Object { [int]$_.issue })
}

# The audit number of the wia-<n> worktree that contains $Path, or 0.
function Get-WorktreeAuditNumber([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return 0 }
    $m = [regex]::Match($Path, '(?i)[\\/]\.claude[\\/]worktrees[\\/]wia-(?<n>\d+)(?:[\\/]|$)')
    if ($m.Success) { return [int]$m.Groups['n'].Value }
    return 0
}

# The open audit a script call is about (#2234): -Issue when given; else the wia-<n> worktree the script runs from
# ($ScriptRoot, the worktree's own tools\ai\audit) or the current directory is in; else the only open audit. With
# several audits open and no worktree context, -Issue is required. -Issue that contradicts the worktree context is
# refused. Throws with the reason; never guesses.
function Resolve-OpenAudit([string]$MainRoot, [int]$Issue = 0, [string]$ScriptRoot = '', [string]$Cwd = '') {
    $audits = @(Get-OpenAudits $MainRoot)
    $openText = if ($audits.Count -gt 0) { ($audits | ForEach-Object { "#$($_.issue)" }) -join ', ' } else { 'none' }
    if ($audits.Count -eq 0) { throw 'no open audit (artifacts/knowledge/open-audits/ has no state file). Run audit-next.ps1 first.' }
    if ([string]::IsNullOrWhiteSpace($Cwd)) { $Cwd = (Get-Location).Path }
    $context = @((Get-WorktreeAuditNumber $ScriptRoot), (Get-WorktreeAuditNumber $Cwd) | Where-Object { $_ -gt 0 } | Select-Object -Unique)
    if ($context.Count -gt 1) { throw "the script runs from wia-$($context[0]) but the current directory is wia-$($context[1]); run it from one audit's worktree or from the main checkout with -Issue." }
    $wanted = if ($Issue -gt 0) { $Issue } elseif ($context.Count -eq 1) { [int]$context[0] } else { 0 }
    if ($Issue -gt 0 -and $context.Count -eq 1 -and [int]$context[0] -ne $Issue) { throw "-Issue $Issue contradicts the worktree wia-$($context[0]) this runs from; run it from the main checkout or from wia-$Issue." }
    if ($wanted -eq 0) {
        if ($audits.Count -eq 1) { return $audits[0] }
        throw "$($audits.Count) audits are open ($openText); pass -Issue <n> to name the one this call is about (#2234)."
    }
    $audit = $audits | Where-Object { [int]$_.issue -eq $wanted } | Select-Object -First 1
    if ($null -eq $audit) { throw "#$wanted is not an open audit (open: $openText)." }
    return $audit
}

# No audit may be forgotten half-way (#2234; audit #4 once stayed open for a long time): an open audit older than
# $script:StaleAuditDays days (from its state file's startedUtc) is stale. audit-stage.ps1 -List flags it and
# audit-next.ps1 refuses to start another audit while one exists, unless -Force.
$script:StaleAuditDays = 2

# Whole and fractional days since the audit started, at $NowUtc; a missing or unreadable startedUtc counts as
# stale (a state file that cannot say when it started is exactly the kind that gets forgotten).
function Get-AuditAgeDays($Audit, [DateTime]$NowUtc) {
    # ConvertFrom-Json already turns an ISO 8601 'Z' value into a UTC DateTime.
    if ($Audit.startedUtc -is [DateTime]) { return ($NowUtc - $Audit.startedUtc.ToUniversalTime()).TotalDays }
    $started = [DateTime]::MinValue
    $styles = [Globalization.DateTimeStyles]::AdjustToUniversal -bor [Globalization.DateTimeStyles]::AssumeUniversal
    if (-not [DateTime]::TryParse([string]$Audit.startedUtc, [Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$started)) { return [double]::PositiveInfinity }
    return ($NowUtc - $started).TotalDays
}

# The open audits older than $script:StaleAuditDays days at $NowUtc.
function Get-StaleAudits($Audits, [DateTime]$NowUtc) {
    return @(@($Audits) | Where-Object { (Get-AuditAgeDays $_ $NowUtc) -gt $script:StaleAuditDays })
}

# The parallelism limit (#2234): pipeline.json maxParallelAudits, an integer from 2 to 5; 2 when absent. Any other
# value throws (a typo must not silently open more audits than the maintainer allowed).
function Get-MaxParallelAudits($Pipeline) {
    $property = $Pipeline.PSObject.Properties['maxParallelAudits']
    if ($null -eq $property) { return 2 }
    $value = 0
    if (-not [int]::TryParse([string]$property.Value, [ref]$value) -or $value -lt 2 -or $value -gt 5) {
        throw "pipeline.json maxParallelAudits must be an integer from 2 to 5 (found '$($property.Value)')."
    }
    return $value
}

# ---------------------------------------------------------------------------------------------------------------
# Scope and overlap (#2234). An audit's scope is the set of packages under src/ it touches. Two audits whose
# scopes share a package would read and judge the same code, so their findings and drafts could collide; such an
# issue waits until the other audit closes (audit-next.ps1), which keeps the result identical to a sequential
# run. Package names are the folder names under src/ of the main checkout (Encina, Encina.Messaging, ...).
# ---------------------------------------------------------------------------------------------------------------

# The folder names under $MainRoot\src, as a case-insensitive name -> canonical name map.
function Get-SrcPackageNames([string]$MainRoot) {
    $map = [System.Collections.Generic.Dictionary[string, string]]::new([StringComparer]::OrdinalIgnoreCase)
    $src = Join-Path $MainRoot 'src'
    if (Test-Path -LiteralPath $src -PathType Container) {
        foreach ($d in Get-ChildItem -LiteralPath $src -Directory -ErrorAction SilentlyContinue) { $map[$d.Name] = $d.Name }
    }
    return , $map
}

# The src/ packages a text names: every `src/<Package>/` path, plus the entries of a front-matter or YAML
# `packages:` list (inline `[a, "b"]` or one `- name` per line). Only names that are folders under src/ count.
function Get-PackagesFromText([string]$Text, $Known) {
    $found = [System.Collections.Generic.SortedSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    if ([string]::IsNullOrWhiteSpace($Text)) { return @() }
    foreach ($m in [regex]::Matches($Text, '(?i)(?<![\w.-])src[\\/](?<p>[A-Za-z][\w.]*)[\\/]')) {
        $name = $m.Groups['p'].Value
        if ($Known.ContainsKey($name)) { [void]$found.Add($Known[$name]) }
    }
    $inline = [regex]::Match($Text, '(?m)^packages:[ \t]*\[(?<list>[^\]\r\n]*)\]')
    $names = [System.Collections.Generic.List[string]]::new()
    if ($inline.Success) { foreach ($part in $inline.Groups['list'].Value -split ',') { $names.Add($part.Trim().Trim('"', "'", ' ')) } }
    $block = [regex]::Match($Text, '(?m)^packages:[ \t]*\r?\n(?<items>(?:[ \t]+-[ \t]*\S.*\r?\n?)+)')
    if ($block.Success) { foreach ($line in $block.Groups['items'].Value -split "`r?`n") { if ($line -match '^\s+-\s*(?<v>\S.*?)\s*$') { $names.Add($Matches['v'].Trim('"', "'", ' ')) } } }
    foreach ($name in $names) { if ($name -and $Known.ContainsKey($name)) { [void]$found.Add($Known[$name]) } }
    return @($found)
}

# The src/ packages of one queued issue before its audit starts: the pre-draft's `packages:` list and paths, the
# files of the commits its raw pre-draft input names, and the files of every commit whose message mentions #<n>
# (the same evidence classify-scope.ps1 uses, without network calls). Sorted; empty when nothing names a package.
function Get-IssueScopePackages([string]$MainRoot, [int]$Issue) {
    $known = Get-SrcPackageNames $MainRoot
    $found = [System.Collections.Generic.SortedSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $knowledge = Get-KnowledgeRoot $MainRoot
    $predraft = Join-Path $knowledge "predraft\$Issue.md"
    if (Test-Path -LiteralPath $predraft) { foreach ($p in (Get-PackagesFromText (Get-Content -LiteralPath $predraft -Raw) $known)) { [void]$found.Add($p) } }
    $files = [System.Collections.Generic.List[string]]::new()
    $raw = Join-Path $knowledge "predraft\raw\$Issue.txt"
    if (Test-Path -LiteralPath $raw) {
        $rawText = Get-Content -LiteralPath $raw -Raw
        foreach ($m in [regex]::Matches($rawText, 'COMMIT ([0-9a-f]{8})')) {
            foreach ($f in @(& git -C $MainRoot show --name-only --format= $m.Groups[1].Value 2>$null)) { if ($f) { $files.Add([string]$f) } }
        }
    }
    foreach ($h in @(& git -C $MainRoot log --all --format=%h -E --grep "#$Issue\b" 2>$null)) {
        foreach ($f in @(& git -C $MainRoot show --name-only --format= $h 2>$null)) { if ($f) { $files.Add([string]$f) } }
    }
    foreach ($p in (Get-PackagesFromText (($files | ForEach-Object { "$_" }) -join "`n") $known)) { [void]$found.Add($p) }
    return @($found)
}

# The scope of an open audit now: its recorded scope plus every src/ package its own worktree's knowledge record
# and archivist and code stage files name (the archivist's scope list is more precise than the pre-draft).
function Get-AuditScope([string]$MainRoot, $Audit) {
    $known = Get-SrcPackageNames $MainRoot
    $found = [System.Collections.Generic.SortedSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($p in @($Audit.scope)) { if ($p) { [void]$found.Add([string]$p) } }
    $wt = [string]$Audit.worktree
    foreach ($relative in "artifacts\knowledge\issues\$($Audit.issue).md", 'artifacts\knowledge\stages\archivist.md', 'artifacts\knowledge\stages\code.md', 'artifacts\knowledge\delta-scope.md') {
        $file = Join-Path $wt $relative
        if (Test-Path -LiteralPath $file) { foreach ($p in (Get-PackagesFromText (Get-Content -LiteralPath $file -Raw) $known)) { [void]$found.Add($p) } }
    }
    return @($found)
}

# The remediation drafts of the audits concurrent with $Audit (#2234): every '<m>-*.md' in the main checkout's
# artifacts/knowledge/remediation/ for each m in $Audit.concurrent (open now or closed since), with the draft's
# title and body as one text for the duplicate-evidence rules, and the issue URL and number opened.csv records for
# it ('' and 0 while it is not opened yet). Sorted by audit and file name.
function Get-ConcurrentAuditDrafts([string]$MainRoot, $Audit) {
    $dir = Join-Path (Get-KnowledgeRoot $MainRoot) 'remediation'
    if (-not (Test-Path -LiteralPath $dir -PathType Container)) { return @() }
    $opened = @{}
    $openedCsv = Join-Path $dir 'opened.csv'
    if (Test-Path -LiteralPath $openedCsv) {
        foreach ($line in Get-Content -LiteralPath $openedCsv) {
            $parts = $line -split ',', 2
            if ($parts.Count -eq 2) { $opened[$parts[0].Trim()] = $parts[1].Trim() }
        }
    }
    $self = [int]$Audit.issue
    $drafts = foreach ($m in @(@($Audit.concurrent) | ForEach-Object { [int]$_ } | Where-Object { $_ -gt 0 -and $_ -ne $self } | Sort-Object -Unique)) {
        foreach ($f in @(Get-ChildItem -LiteralPath $dir -Filter "$m-*.md" -File -ErrorAction SilentlyContinue | Sort-Object Name)) {
            $raw = Get-Content -LiteralPath $f.FullName -Raw
            $header = [regex]::Match($raw, '(?s)<!--(.*?)-->').Groups[1].Value
            $title = ([regex]::Match($header, 'title:[ \t]*(.+)')).Groups[1].Value.Trim()
            $body = ($raw -replace '(?s)^\s*<!--.*?-->\s*', '').Trim()
            $url = if ($opened.ContainsKey($f.Name)) { [string]$opened[$f.Name] } else { '' }
            $number = if ($url -match '/issues/(?<n>\d+)\s*$') { [int]$Matches['n'] } else { 0 }
            [pscustomobject]@{ Issue = $m; Name = $f.Name; Path = $f.FullName; TitleAndBody = "$title`n$body"; Url = $url; Number = $number }
        }
    }
    return @($drafts)
}

# The packages two scopes share (case-insensitive), sorted; empty when they do not overlap.
function Get-ScopeOverlap([string[]]$A, [string[]]$B) {
    $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($x in @($A)) { if ($x) { [void]$set.Add($x) } }
    return @(@($B) | Where-Object { $_ -and $set.Contains($_) } | Sort-Object -Unique)
}

function Get-StagesDir([string]$Worktree) { Join-Path $Worktree 'artifacts\knowledge\stages' }

# tools/ai/audit/pipeline.json read from $ToolsAuditDir (normally the audit worktree's own copy, so an audit
# keeps the pipeline version it started with even if main later reorders it; callers pass the worktree's
# tools\ai\audit, or their own $PSScriptRoot when no worktree is known yet, e.g. audit-next.ps1 before the
# worktree exists).
function Get-Pipeline([string]$ToolsAuditDir, [string]$File = 'pipeline.json') {
    $p = Join-Path $ToolsAuditDir $File
    if (-not (Test-Path -LiteralPath $p)) { throw "Get-Pipeline: $File not found at '$p'." }
    return Get-Content -LiteralPath $p -Raw | ConvertFrom-Json
}

# ---------------------------------------------------------------------------------------------------------------
# Delta mode (#1763): a re-check of an audit already published, for the rules decided after it ran, with the
# pipeline tools/ai/audit/pipeline-delta.json instead of pipeline.json. The audit's state file then carries
# `mode = 'delta'` and `set = '<name>'`; every script and hook that reads the pipeline asks the audit which
# file to read (the hooks inline the same one-line decision, since they depend on nothing here).
# ---------------------------------------------------------------------------------------------------------------

# 'pipeline-delta.json' when the audit is a delta audit, 'pipeline.json' otherwise (also for a state file
# written before delta mode existed, which has no `mode`).
function Get-AuditPipelineFile($Audit) {
    $mode = if ($null -ne $Audit -and $null -ne $Audit.PSObject.Properties['mode']) { [string]$Audit.mode } else { '' }
    if ($mode -eq 'delta') { return 'pipeline-delta.json' }
    return 'pipeline.json'
}

# The pipeline of the open audit, read from the audit worktree's own tools\ai\audit (the worktree keeps the
# version it started with).
function Get-AuditPipeline($Audit) {
    return Get-Pipeline (Join-Path ([string]$Audit.worktree) 'tools\ai\audit') (Get-AuditPipelineFile $Audit)
}

# True when the open audit is a delta audit.
function Test-DeltaAudit($Audit) { return (Get-AuditPipelineFile $Audit) -eq 'pipeline-delta.json' }

# The delta progress file of one delta set, in the git-ignored knowledge working area: one `<issue>,done` line
# per issue whose delta audit was published.
function Get-DeltaProgressPath([string]$KnowledgeRoot, [string]$Set) { Join-Path $KnowledgeRoot "delta-progress-$Set.csv" }

# The issues a delta set re-checks, in audit order: the distinct issue numbers of progress.csv (the audits
# #1..#29 that ran before the rules), first occurrence wins (a redone audit appears twice).
function Get-DeltaCandidates([string]$KnowledgeRoot) {
    $progress = Join-Path $KnowledgeRoot 'progress.csv'
    if (-not (Test-Path -LiteralPath $progress)) { return @() }
    $seen = [System.Collections.Generic.HashSet[string]]::new()
    $ordered = [System.Collections.Generic.List[string]]::new()
    foreach ($line in (Get-Content -LiteralPath $progress | Select-Object -Skip 1)) {
        $id = ($line -split ',')[0].Trim()
        if ($id -match '^\d+$' -and $seen.Add($id)) { $ordered.Add($id) }
    }
    return @($ordered)
}

# The issues of the set already done: the delta progress file's first column.
function Get-DeltaDone([string]$KnowledgeRoot, [string]$Set) {
    $p = Get-DeltaProgressPath $KnowledgeRoot $Set
    if (-not (Test-Path -LiteralPath $p)) { return @() }
    return @(Get-Content -LiteralPath $p | Where-Object { $_ -match '\S' } | ForEach-Object { ($_ -split ',')[0].Trim() })
}

# The date (yyyy-MM-dd) of the audit recorded in a knowledge record's front matter (`audit:` block, `date:`),
# or '' when the record has none (the oldest records predate that block).
function Get-RecordAuditDate([string]$RecordText) {
    $m = [regex]::Match($RecordText, '(?m)^audit:[ \t]*\r?\n(?:[ \t]+\S.*\r?\n)*?[ \t]+date:[ \t]*(?<d>\d{4}-\d{2}-\d{2})')
    if ($m.Success) { return $m.Groups['d'].Value }
    return ''
}

# The scope the original audit recorded, as the text of artifacts\knowledge\delta-scope.md: the packages and
# title of the knowledge record docs/knowledge/issues/<n>.md plus, when published, the archivist and code stage
# files under docs/knowledge/audits/<n>/stages/ (their scope lists are the audit's own scope). Read from the
# audit worktree, which is based on origin/main and therefore holds the published knowledge. Returns $null
# when the record does not exist (the delta cannot run without the original audit's record).
function New-DeltaScopeText([int]$Issue, [string]$Worktree, [string]$Set) {
    $record = Join-Path $Worktree "docs\knowledge\issues\$Issue.md"
    if (-not (Test-Path -LiteralPath $record)) { return $null }
    $sb = [System.Text.StringBuilder]::new()
    $null = $sb.Append("# Delta scope of issue #$Issue (set $Set)`n`n")
    $hasStages = Test-Path -LiteralPath (Join-Path $Worktree "docs\knowledge\audits\$Issue\stages")
    $resultFile = Join-Path $Worktree "docs\knowledge\audits\issue-$Issue.md"
    $hasResult = Test-Path -LiteralPath $resultFile
    $null = $sb.Append("Reused from the original audit; do not re-derive it. Rules in this delta: see tools/ai/audit/pipeline-delta.json.`n`n")
    $source = if ($hasStages) { 'the published record and the published stage files of the original audit (docs/knowledge/audits/' + $Issue + '/stages/)' }
    else { 'the published record' + $(if ($hasResult) { " and the published audit result (docs/knowledge/audits/issue-$Issue.md)" } else { '' }) + ' only: the original audit has no published stage files' }
    $null = $sb.Append("**Scope source:** $source.`n`n")
    $null = $sb.Append("## Knowledge record (docs/knowledge/issues/$Issue.md)`n`n")
    $recordText = Get-Content -LiteralPath $record -Raw
    $front = [regex]::Match($recordText, '(?s)\A---\r?\n(?<fm>.*?)\r?\n---')
    $null = $sb.Append('```yaml' + "`n" + $(if ($front.Success) { $front.Groups['fm'].Value } else { $recordText }) + "`n" + '```' + "`n`n")
    if (-not $hasStages) {
        # Without stage files the scope is what the record names (packages, files and knowledge destinations in the
        # front matter above, the "Where the knowledge lives" section) and the result's text.
        $where = [regex]::Match($recordText, '(?ims)^##\s*Where the knowledge lives[^\r\n]*\r?\n(?<body>.*?)(?=^##\s|\z)')
        if ($where.Success) { $null = $sb.Append("## Where the knowledge lives (record)`n`n" + $where.Groups['body'].Value.Trim() + "`n`n") }
        if ($hasResult) { $null = $sb.Append("## Audit result (docs/knowledge/audits/issue-$Issue.md)`n`n" + (Get-Content -LiteralPath $resultFile -Raw).Trim() + "`n`n") }
    }
    foreach ($name in 'archivist.md', 'code.md') {
        $stageFile = Join-Path $Worktree "docs\knowledge\audits\$Issue\stages\$name"
        if (-not (Test-Path -LiteralPath $stageFile)) { continue }
        $text = (Get-Content -LiteralPath $stageFile -Raw)
        $section = [regex]::Match($text, '(?ims)^##\s*(Scope|Files in scope|Scope list)[^\r\n]*\r?\n(?<body>.*?)(?=^##\s|\z)')
        $null = $sb.Append("## From the original $name (docs/knowledge/audits/$Issue/stages/$name)`n`n")
        if ($section.Success) { $null = $sb.Append((ConvertTo-PlainTextLinks $section.Groups['body'].Value.Trim()) + "`n`n") }
        else { $null = $sb.Append("(no Scope section; the whole stage file is the reference)`n`n" + (ConvertTo-PlainTextLinks $text.Trim()) + "`n`n") }
    }
    return $sb.ToString()
}

# Turns the relative Markdown links of $Text into plain text 'text (path)' (fenced code and absolute, site-root and
# anchor links stay), so text copied from a deeper folder cannot be re-resolved against the wrong base (#1817).
function ConvertTo-PlainTextLinks([string]$Text) {
    $lines = $Text.Split("`n")
    $fence = ''
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $wasInFence = $fence -ne ''
        $fence = Get-FenceState $lines[$i] $fence
        if ($wasInFence -or $fence -ne '') { continue }
        $lines[$i] = [regex]::Replace($lines[$i], '!?\[(?<t>[^\]]*)\]\((?<p>(?![a-zA-Z][a-zA-Z0-9+.-]*:|//|/|#)[^()\s]+)\)', '${t} (${p})')
        $lines[$i] = [regex]::Replace($lines[$i], '^\s{0,3}\[(?!\^)(?<t>[^\]]+)\]:\s*(?<p>(?![a-zA-Z][a-zA-Z0-9+.-]*:|//|/|#)\S+)\s*$', '${t}: ${p}')
    }
    return ($lines -join "`n")
}

# True when a commit on the branch checked out at $Worktree carries the trailer 'Stage: <StageName>' AND the
# working-tree copy of $ArtifactRelativePath (forward slashes, relative to $Worktree) has no uncommitted
# changes (`git status --porcelain` for that path is empty). The second half matters: --grep alone only
# proves SOME commit once carried that trailer, not that the file on disk right now is what was committed --
# an artifact edited again after audit-commit-stage.ps1 ran, and never re-committed, must NOT read as done.
function Test-StageCommitted([string]$Worktree, [string]$StageName, [string]$ArtifactRelativePath) {
    $commit = & git -C $Worktree log -1 --grep "Stage: $StageName" --fixed-strings --pretty=format:%H 2>$null
    if ([string]::IsNullOrWhiteSpace(($commit | Select-Object -First 1))) { return $false }
    $dirty = & git -C $Worktree status --porcelain -- $ArtifactRelativePath 2>$null
    return [string]::IsNullOrWhiteSpace(($dirty | Select-Object -First 1))
}

# The first stage (in pipeline order) that is not yet done: its artifact is missing under $StagesDir, or it
# is present but not committed (clean, with a 'Stage: <name>' commit) on the audit branch at $Worktree.
# $null when every stage is done.
function Get-NextStage([string]$StagesDir, [string]$Worktree, $Pipeline) {
    foreach ($stage in $Pipeline.stages) {
        $file = Join-Path $StagesDir $stage.artifact
        $relative = "artifacts/knowledge/stages/$($stage.artifact)"
        $done = (Test-Path -LiteralPath $file) -and (Test-StageCommitted $Worktree $stage.stage $relative)
        if (-not $done) { return $stage }
    }
    return $null
}

# True when the verifier stage's artifact (the pipeline.json stage whose agent is 'audit-verifier', never a
# hard-coded 'verification.md' -- a renamed artifact must not silently stop this check from finding it) exists
# and its first line is exactly 'Verdict: FAIL' -- the one condition that allows re-running an earlier stage
# out of the normal fixed order (audit-stage-guard.ps1). $Pipeline is the object Get-Pipeline returns, passed
# in the same style as Get-NextStage above.
function Test-LastVerdictFail([string]$StagesDir, $Pipeline) {
    $verifierStage = $Pipeline.stages | Where-Object { $_.agent -eq 'audit-verifier' } | Select-Object -First 1
    if ($null -eq $verifierStage) { return $false }
    $file = Join-Path $StagesDir $verifierStage.artifact
    if (-not (Test-Path -LiteralPath $file)) { return $false }
    return (Get-Content -LiteralPath $file -TotalCount 1) -eq 'Verdict: FAIL'
}

# The Unix timestamp (seconds, [long]) of the newest commit on $Worktree's current branch that touched
# $ArtifactRelativePath (forward slashes, relative to $Worktree), or $null when the path has no commit yet.
function Get-ArtifactCommitTime([string]$Worktree, [string]$ArtifactRelativePath) {
    $ts = & git -C $Worktree log -1 --format=%ct -- $ArtifactRelativePath 2>$null | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($ts)) { return $null }
    return [long]$ts
}

# #1555: a stage re-committed (e.g. remediation regenerated after a PASS) AFTER the verifier stage's own last
# commit makes that verdict stale -- the verifier never inspected the new content. Staleness is derived from
# git history alone (no marker file, no new state): the newest commit of every OTHER pipeline stage's artifact
# is compared against the verifier stage's own newest commit. Returns the (single) pipeline.json stage
# definition with the newest such commit, or $null when there is no verifier stage, the verifier has not
# committed yet (nothing to compare against), or no other stage's artifact was committed after it. A tie
# (same second) does NOT count as stale -- it is not distinguishable from the verifier's own commit and is not
# proof the verifier missed the content. Shared by audit-stage-guard.ps1 (allows re-spawning audit-verifier)
# and audit-done.ps1 (refuses to close while this returns non-null).
function Get-StaleStageAfterVerification([string]$Worktree, $Pipeline) {
    $verifierStage = $Pipeline.stages | Where-Object { $_.agent -eq 'audit-verifier' } | Select-Object -First 1
    if ($null -eq $verifierStage) { return $null }
    $verificationRelative = "artifacts/knowledge/stages/$($verifierStage.artifact)"
    $verificationTime = Get-ArtifactCommitTime $Worktree $verificationRelative
    if ($null -eq $verificationTime) { return $null }

    $stale = $null
    $staleTime = $verificationTime
    foreach ($stage in $Pipeline.stages) {
        if ($stage.stage -eq $verifierStage.stage) { continue }
        $relative = "artifacts/knowledge/stages/$($stage.artifact)"
        $stageTime = Get-ArtifactCommitTime $Worktree $relative
        if ($null -ne $stageTime -and $stageTime -gt $staleTime) {
            $stale = $stage
            $staleTime = $stageTime
        }
    }
    return $stale
}

# stages/lessons.md exists and has no unresolved 'Applied: TODO' line -- each '- <lesson>' bullet
# audit-lessons.ps1 writes is followed by an 'Applied: <status>' line the orchestrator must resolve before the
# lesson counts as applied. Returns '' when valid, or the reason it is not; audit-done.ps1 and
# audit-commit-stage.ps1 -Lessons both call this so the check has one definition (#1345 review).
function Test-LessonsResolved([string]$LessonsFile) {
    if (-not (Test-Path -LiteralPath $LessonsFile)) { return 'missing stages\lessons.md (run audit-lessons.ps1)' }
    $lessonsText = Get-Content -LiteralPath $LessonsFile -Raw
    if ($lessonsText -match 'Applied:\s*TODO') { return 'stages\lessons.md still has an unresolved "Applied: TODO" line' }
    return ''
}

# The '## Findings' or '## Lessons for the pipeline' section of a stage artifact, as raw text; '' when the
# file or the section does not exist. $Heading excludes the leading '##'.
function Get-StageSection([string]$Path, [string]$Heading) {
    if (-not (Test-Path -LiteralPath $Path)) { return '' }
    $text = Get-Content -LiteralPath $Path -Raw
    # The backtick before '$(?<body>' keeps it a literal regex group, not a PowerShell subexpression: an
    # unescaped '$(' inside a double-quoted string is evaluated by PowerShell itself before the regex ever
    # sees it.
    $pattern = "(?ms)^##\s*$([regex]::Escape($Heading))\s*`$(?<body>.*?)(?=^##\s|\z)"
    $m = [regex]::Match($text, $pattern)
    if (-not $m.Success) { return '' }
    return $m.Groups['body'].Value.Trim()
}

# Splits one stage's '## Findings' section text (as returned by Get-StageSection) into individual findings
# (#1375). The layout every stage agent (issue-auditor, test-auditor, docs-reviewer) writes: one numbered
# paragraph per finding, starting with "N. **Blocker**", "N. **Major**" or "N. **Minor**" followed by an em
# dash (the regex below matches it with the Unicode dash-punctuation property escape, never a literal byte,
# so this file stays ASCII-only, per PSScriptAnalyzer)
# or a hyphen and the body; continuation lines belong to that finding until the next numbered finding or the
# next '## ' heading. Returns an array of @{ Stage; Id; Severity; Text } (Id is the finding's own number as a
# string, unique within $Stage -- a repeated number within one stage is a malformed artifact, never silently
# overwritten: see the duplicate-id check below). An explicit "- none" section (the convention the stage
# agents use when nothing survives review) yields an empty array, also when prose follows it (ignored with a
# note, #1694). A numbered paragraph whose bold token is not
# one of Blocker/Major/Minor (a typo like **Critical**, or a marker this parser does not know) still starts a
# NEW finding rather than being appended to the previous one or discarded when it is the first line, with
# Severity 'Unknown'. A non-empty section with no recognizable numbered findings at all never yields zero
# silently either: it becomes one finding with Severity 'Unknown' and the whole section as Text, so a stage's
# real findings are never dropped by a formatting drift the parser does not recognize.
function Split-Findings([string]$Stage, [string]$FindingsText) {
    $results = [System.Collections.Generic.List[pscustomobject]]::new()
    $seenIds = [System.Collections.Generic.HashSet[string]]::new()
    $text = if ($null -eq $FindingsText) { '' } else { $FindingsText.Trim() }
    if ([string]::IsNullOrWhiteSpace($text)) { return $results }
    # A section whose FIRST non-empty line is "- none" (or bare "None"; any case, optional trailing period) has no findings;
    # any text after it is prose the stage agent should have put under '## Informational (not findings)', and
    # is ignored with a note (#1694) instead of turning into an Unknown finding.
    $noneMatch = [regex]::Match($text, '(?i)^(?:-\s*)?none\s*\.?[ \t]*(?:\r?\n(?<rest>[\s\S]*))?$')
    # Fail closed: when ANY line of the text after the marker looks like a finding (a numbered item in any style,
    # a bullet or bold line starting with a severity word), the section contradicts itself and falls through to
    # the normal parser (one or more findings, Unknown when unparseable) so no real finding is dropped. Only
    # plain prose is ignored, with a note that is also recorded in $script:SplitFindingsNotes (#1694).
    $findingShaped = '(?im)^[ \t]*(?:[-*+>][ \t]*)*(?:[#(\[]?\d+[.):\]](?=[ \t*])|#\d+(?=[ \t])|[A-Za-z][.)][ \t]+\*\*|[\[(*_]*(?:Blocker|Critical|Major|Minor|Nit|High|Medium|Low|Warning|Severity)\b)'
    if ($noneMatch.Success -and $noneMatch.Groups['rest'].Value -notmatch $findingShaped) {
        if (-not [string]::IsNullOrWhiteSpace($noneMatch.Groups['rest'].Value)) {
            Write-Host "Split-Findings: trailing text after '- none' in $Stage ignored."
            if ($null -eq $script:SplitFindingsNotes) { $script:SplitFindingsNotes = [System.Collections.Generic.List[string]]::new() }
            $script:SplitFindingsNotes.Add("stage ${Stage}: '- none' followed by prose; prose ignored")
        }
        return $results
    }

    function Complete-Finding($Current) {
        if ($null -eq $Current) { return }
        if (-not $seenIds.Add($Current.Id)) {
            throw "Split-Findings: stage '$Stage' has more than one finding numbered '$($Current.Id)' -- each finding's number must be unique within a stage."
        }
        $results.Add([pscustomobject]@{ Stage = $Stage; Id = $Current.Id; Severity = $Current.Severity; Text = ($Current.Lines -join "`n").Trim() })
    }

    # \p{Pd} (Unicode "dash punctuation" category) matches an em dash or a plain hyphen with a pure-ASCII
    # regex escape, never a literal non-ASCII byte in this file's source (PSScriptAnalyzer).
    $startPattern = '^(?<id>\d+)\.\s+\*\*(?<sev>[^*]+)\*\*\s*\p{Pd}\s*(?<body>.*)$'
    $current = $null
    foreach ($line in ($text -split "`r?`n")) {
        $lineMatch = [regex]::Match($line, $startPattern)
        if ($lineMatch.Success) {
            Complete-Finding $current
            $sevRaw = $lineMatch.Groups['sev'].Value
            $severity = if ($sevRaw -in 'Blocker', 'Major', 'Minor') { $sevRaw } else { 'Unknown' }
            $current = [pscustomobject]@{ Id = $lineMatch.Groups['id'].Value; Severity = $severity; Lines = [System.Collections.Generic.List[string]]::new() }
            $current.Lines.Add($lineMatch.Groups['body'].Value)
        }
        elseif ($line -match '^##\s') {
            # The input is already the extracted Findings section, so a '## ' line here means the section
            # boundary was mis-detected upstream; stop rather than absorb the next section into a finding.
            break
        }
        elseif ($null -ne $current) {
            $current.Lines.Add($line)
        }
    }
    Complete-Finding $current

    if ($results.Count -eq 0) {
        $results.Add([pscustomobject]@{ Stage = $Stage; Id = '1'; Severity = 'Unknown'; Text = $text })
    }
    return $results
}

# ---------------------------------------------------------------------------------------------------------------
# Publishing a closed audit to docs/knowledge (#1735), used by audit-done.ps1 and exercised by
# audit-done-selftest.ps1.
#
# The knowledge record, the audit result and the audit's stage files go into docs/knowledge/ through a pull
# request (Refs #1345, never Fixes), so the audit output is version-controlled instead of living only in the
# git-ignored artifacts/knowledge/.
#
# Layout (decision of #1735):
#   artifacts/knowledge/issues/<n>.md        -> docs/knowledge/issues/<n>.md            (replaces an existing record)
#   artifacts/knowledge/audits/issue-<n>.md  -> docs/knowledge/audits/issue-<n>.md      (generated when missing)
#   artifacts/knowledge/stages/<stage>.md    -> docs/knowledge/audits/<n>/stages/<stage>.md
# Remediation drafts are NOT published: they become issues, and the audit result lists their numbers.
#
# The self-test shadows git and gh with PowerShell functions that record the commands and never push or open a
# pull request, so every call below is a plain `& git ...` / `& gh ...`.
# ---------------------------------------------------------------------------------------------------------------

function Invoke-AuditGit([string[]]$ArgList) {
    $global:LASTEXITCODE = 0
    $out = & git @ArgList 2>&1
    return @{ ExitCode = $global:LASTEXITCODE; Output = (($out | ForEach-Object { "$_" }) -join "`n") }
}

function Invoke-AuditGh([string[]]$ArgList) {
    $global:LASTEXITCODE = 0
    $out = & gh @ArgList 2>&1
    return @{ ExitCode = $global:LASTEXITCODE; Output = (($out | ForEach-Object { "$_" }) -join "`n") }
}

# Number of commits on the audit branch that carry the 'Stage: verification' trailer = verification passes.
function Get-AuditPassCount([string]$Worktree, [string]$VerifierStage = 'verification') {
    $r = Invoke-AuditGit @('-C', $Worktree, 'log', '--grep', "Stage: $VerifierStage", '--fixed-strings', '--pretty=format:%H')
    if ($r.ExitCode -ne 0 -or [string]::IsNullOrWhiteSpace($r.Output)) { return 0 }
    return @($r.Output -split "`n" | Where-Object { $_.Trim() }).Count
}

# Fallback when the audit branch is gone (the backfill): the pass number the verification text itself names
# ('Third pass', 'pass-6', 're-verification'); 1 when it names none. A lower bound, not an exact count.
function Get-VerificationPassCountFromText([string]$VerificationFile) {
    if (-not (Test-Path -LiteralPath $VerificationFile)) { return 1 }
    $text = Get-Content -LiteralPath $VerificationFile -Raw
    $ordinals = @{ first = 1; second = 2; third = 3; fourth = 4; fifth = 5; sixth = 6; seventh = 7 }
    $max = 1
    foreach ($m in [regex]::Matches($text, '(?i)\b(first|second|third|fourth|fifth|sixth|seventh)\s+(?:verification\s+)?pass\b')) { $max = [Math]::Max($max, $ordinals[$m.Groups[1].Value.ToLowerInvariant()]) }
    foreach ($m in [regex]::Matches($text, '(?i)\bpass[- ](\d+)\b')) { $max = [Math]::Max($max, [int]$m.Groups[1].Value) }
    if ($max -lt 2 -and $text -match '(?i)re-verification|after the FAIL') { $max = 2 }
    return $max
}

# The remediation drafts of one audit (<n>-*.md) found in the given folders, de-duplicated by file name, with
# the issue URL from opened.csv ('<draft file>,<url>' per line) when open-remediation.ps1 already opened it.
function Get-AuditRemediation([int]$Issue, [string[]]$DraftDirs, [string]$OpenedCsv, [string]$NamePrefix = '') {
    # $NamePrefix: a delta audit (#1763) looks only at its own '<n>-<delta folder>-*.md' drafts.
    $draftPrefix = if ($NamePrefix) { $NamePrefix } else { "$Issue" }
    $opened = @{}
    if ($OpenedCsv -and (Test-Path -LiteralPath $OpenedCsv)) {
        foreach ($line in Get-Content -LiteralPath $OpenedCsv) {
            $parts = $line -split ',', 2
            if ($parts.Count -eq 2) { $opened[$parts[0].Trim()] = $parts[1].Trim() }
        }
    }
    $seen = @{}
    foreach ($dir in $DraftDirs) {
        if (-not (Test-Path -LiteralPath $dir)) { continue }
        foreach ($f in Get-ChildItem -LiteralPath $dir -Filter "$draftPrefix-*.md" -File) {
            if ($seen.ContainsKey($f.Name)) { continue }
            $seen[$f.Name] = $true
        }
    }
    return @($seen.Keys | Sort-Object | ForEach-Object {
            [pscustomobject]@{ Draft = $_; Url = $(if ($opened.ContainsKey($_)) { $opened[$_] } else { '' }) }
        })
}

# The short audit result generated when the pipeline wrote none (it never does for the six-stage pipeline):
# the verdict line and pass count of the verification stage, one line per stage with a link to its stage file,
# the remediation issues and the duplicates the remediation stage noted.
function New-AuditResultSummary([int]$Issue, [string]$StagesDir, $Pipeline, [int]$PassCount, $Remediation, [switch]$PassCountFromText) {
    $verdict = ''
    $verification = $Pipeline.stages | Where-Object { $_.agent -eq 'audit-verifier' } | Select-Object -First 1
    if ($verification) {
        $vFile = Join-Path $StagesDir $verification.artifact
        if (Test-Path -LiteralPath $vFile) { $verdict = (Get-Content -LiteralPath $vFile -TotalCount 1) }
    }
    $sb = [System.Text.StringBuilder]::new()
    $null = $sb.Append("# Audit of issue #$Issue`n`n")
    $null = $sb.Append("<!-- Generated by tools/ai/audit/audit-done.ps1: the pipeline wrote no audit result file for this issue. -->`n`n")
    $passText = if ($PassCountFromText) { "at least $PassCount pass(es); a lower bound read from the verification text, because the audit branch's commit history was not available" } else { "$PassCount pass(es)" }
    $null = $sb.Append("$verdict (independent verification, $passText)`n`n")
    $null = $sb.Append("Record: [issues/$Issue.md](../issues/$Issue.md)`n`n")
    $null = $sb.Append("## Stages`n`n")
    foreach ($stage in $Pipeline.stages) {
        $null = $sb.Append("- $($stage.stage): [$Issue/stages/$($stage.artifact)]($Issue/stages/$($stage.artifact))`n")
    }
    $null = $sb.Append("- lessons: [$Issue/stages/lessons.md]($Issue/stages/lessons.md)`n`n")
    $null = $sb.Append("## Remediation issues`n`n")
    if (@($Remediation).Count -eq 0) { $null = $sb.Append("- none`n") }
    foreach ($r in @($Remediation)) {
        if ($r.Url) { $null = $sb.Append("- $($r.Url) (draft $($r.Draft))`n") }
        else { $null = $sb.Append("- not opened when this audit was published (draft $($r.Draft))`n") }
    }
    $null = $sb.Append("`n## Duplicates noted`n`n")
    $remStage = $Pipeline.stages | Where-Object { $_.stage -eq 'remediation' } | Select-Object -First 1
    $dups = @()
    if ($remStage) {
        $rFile = Join-Path $StagesDir $remStage.artifact
        if (Test-Path -LiteralPath $rFile) {
            # Only the structured lines "<stage> <n> [(Severity)]: ... duplicate of #N": free prose of the stage may
            # describe a match the verifier has since resolved.
            $dups = @(Get-Content -LiteralPath $rFile | Where-Object { $_ -match '^\s*-?\s*\w+ \d+ \([^)]*\):.*\bduplicate of #\d+' })
        }
    }
    if ($dups.Count -eq 0) { $null = $sb.Append("- none`n") }
    foreach ($d in $dups) { $null = $sb.Append("$($d.TrimEnd())`n") }
    return $sb.ToString()
}

# The published record carries the audit's outcome (F3 of the #1766 review): the archivist commits the record
# before any result exists, so audit-done rewrites `audit.verdict` and `audit.record` in the record's `audit:`
# block. Verification PASS with remediation issues is `findings-tracked`, PASS without is `conforms`. Throws when
# the record has no audit block with both keys (nothing is guessed).
function Set-AuditBlockOutcome([string]$RecordText, [int]$Issue, [string]$Verdict) {
    $lines = $RecordText.Split("`n")
    $inAudit = $false; $verdictSet = $false; $recordSet = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        $cr = if ($line.EndsWith("`r")) { "`r" } else { '' }
        if ($line.TrimEnd("`r") -match '^audit:\s*$') { $inAudit = $true; continue }
        if ($inAudit -and $line.Trim("`r").Length -gt 0 -and $line -notmatch '^\s') { $inAudit = $false }
        if (-not $inAudit) { continue }
        if ($line -match '^  verdict:') { $lines[$i] = "  verdict: $Verdict$cr"; $verdictSet = $true }
        elseif ($line -match '^  record:') { $lines[$i] = "  record: `"docs/knowledge/audits/issue-$Issue.md`"$cr"; $recordSet = $true }
    }
    if (-not ($verdictSet -and $recordSet)) { throw "the knowledge record of #$Issue has no 'audit:' block with 'verdict:' and 'record:' lines; the archivist stage must write them." }
    return ($lines -join "`n")
}

# The files to publish: @{ Source = <path or $null>; Content = <text when generated>; Dest = <repo-relative> }.
# Throws when the record is missing.
function Get-AuditPublishPlan([int]$Issue, [string]$AuditWorktree, [string]$StagesDir, $Pipeline, [string]$ResultSummary) {
    $plan = [System.Collections.Generic.List[hashtable]]::new()
    $src = Join-Path $AuditWorktree 'artifacts\knowledge'
    $record = Join-Path $src "issues\$Issue.md"
    if (-not (Test-Path -LiteralPath $record)) { throw "Get-AuditPublishPlan: the knowledge record $record does not exist." }
    $plan.Add(@{ Source = $record; Content = $null; Dest = "docs/knowledge/issues/$Issue.md" })

    $result = Join-Path $src "audits\issue-$Issue.md"
    if (Test-Path -LiteralPath $result) { $plan.Add(@{ Source = $result; Content = $null; Dest = "docs/knowledge/audits/issue-$Issue.md" }) }
    else { $plan.Add(@{ Source = $null; Content = $ResultSummary; Dest = "docs/knowledge/audits/issue-$Issue.md" }) }

    $names = @($Pipeline.stages | ForEach-Object { $_.artifact }) + 'lessons.md'
    foreach ($name in $names) {
        $f = Join-Path $StagesDir $name
        if (Test-Path -LiteralPath $f) { $plan.Add(@{ Source = $f; Content = $null; Dest = "docs/knowledge/audits/$Issue/stages/$name"; LinkBase = "artifacts/knowledge/stages/$name" }) }
    }
    return , $plan
}

# The files to publish for a delta audit (#1763): the delta stage files, lessons and the scope the delta reused,
# all under docs/knowledge/audits/<n>/<DeltaFolder>/. The original record and audit result are never touched.
function Get-DeltaPublishPlan([int]$Issue, [string]$AuditWorktree, [string]$StagesDir, $Pipeline, [string]$DeltaFolder) {
    $plan = [System.Collections.Generic.List[hashtable]]::new()
    $names = @($Pipeline.stages | ForEach-Object { $_.artifact }) + 'lessons.md'
    foreach ($name in $names) {
        $f = Join-Path $StagesDir $name
        if (Test-Path -LiteralPath $f) { $plan.Add(@{ Source = $f; Content = $null; Dest = "docs/knowledge/audits/$Issue/$DeltaFolder/$name"; LinkBase = "artifacts/knowledge/stages/$name" }) }
    }
    # #1817: the scope text is copied from the original record (docs/knowledge/issues/<n>.md), so its relative
    # links were written for that folder.
    # Assumption behind LinkBase: the record and result sections of delta-scope.md come from docs/knowledge/issues and
    # docs/knowledge/audits, which sit at the same depth. The stage excerpts come from audits/<n>/stages, one level
    # deeper, and are expected to carry no relative links: New-DeltaScopeText turns them into plain text.
    $scope = Join-Path $AuditWorktree 'artifacts\knowledge\delta-scope.md'
    if (Test-Path -LiteralPath $scope) { $plan.Add(@{ Source = $scope; Content = $null; Dest = "docs/knowledge/audits/$Issue/$DeltaFolder/delta-scope.md"; LinkBase = "docs/knowledge/issues/$Issue.md" }) }
    if ($plan.Count -eq 0) { throw "Get-DeltaPublishPlan: no delta stage file under $StagesDir." }
    return , $plan
}

# --- Relative links of published files (#1817) --------------------------------------------------------------------
# Text copied from docs/knowledge/issues/<n>.md or a stage file keeps the relative links it was written with, which
# break one folder deeper. Update-PublishedLinks resolves every relative Markdown link against the file's SOURCE
# location (a repo-relative path, only its folder matters) and rewrites it relative to the DESTINATION; a link that
# resolves from neither is reported as an error. Targets are looked up under $Root (a checkout of the repository).
# Skipped: absolute URLs, mailto:, site-root (/x) links, anchor-only links, fenced code and inline code spans.

function Test-RelativeLinkTarget([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) { return $false }
    return ($Path -notmatch '^([a-zA-Z][a-zA-Z0-9+.-]*:|//|/|#)')
}

# The existing full path of $Decoded resolved from the repo-relative folder $BaseDir under $Root, or $null.
function Resolve-LinkTarget([string]$Root, [string]$BaseDir, [string]$Decoded) {
    $rootFull = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    $full = [IO.Path]::GetFullPath((Join-Path $rootFull (($BaseDir + '/' + $Decoded) -replace '/', '\')))
    if (-not $full.StartsWith($rootFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) { return $null }
    if ((Test-Path -LiteralPath $full) -and (Test-ExactCasePath $rootFull $full)) { return $full }
    return $null
}

# Test-Path ignores case on Windows but the link checker (Linux) does not: every segment must match the name on disk.
function Test-ExactCasePath([string]$RootFull, [string]$Full) {
    $current = $RootFull
    foreach ($segment in $Full.Substring($RootFull.Length).Split([char[]]@('\', '/'), [StringSplitOptions]::RemoveEmptyEntries)) {
        $names = [IO.Directory]::EnumerateFileSystemEntries($current) | ForEach-Object { [IO.Path]::GetFileName($_) }
        if ($names -cnotcontains $segment) { return $false }
        $current = Join-Path $current $segment
    }
    return $true
}

# The new target for one link, the same $Target when it needs no change, or $null when it does not resolve.
function Convert-LinkTarget([string]$Target, [string]$SourceDir, [string]$DestDir, [string]$Root) {
    $bare = $Target.Trim('<', '>')
    $cut = $bare.IndexOfAny([char[]]@('#', '?'))
    $path = if ($cut -ge 0) { $bare.Substring(0, $cut) } else { $bare }
    $suffix = if ($cut -ge 0) { $bare.Substring($cut) } else { '' }
    if (-not (Test-RelativeLinkTarget $path)) { return $Target }
    $decoded = [uri]::UnescapeDataString($path)
    $full = Resolve-LinkTarget $Root $SourceDir $decoded
    if (-not $full) {
        # Already valid from the destination: nothing to rewrite.
        if (Resolve-LinkTarget $Root $DestDir $decoded) { return $Target }
        return $null
    }
    $rel = [IO.Path]::GetRelativePath((Join-Path $Root ($DestDir -replace '/', '\')), $full) -replace '\\', '/'
    if ($decoded.EndsWith('/') -and -not $rel.EndsWith('/')) { $rel += '/' }
    # The relative path did not change: keep the original text byte for byte (its own encoding included).
    if ($rel -ceq $decoded) { return $Target }
    $new = (ConvertTo-LinkPath $rel) + $suffix
    return $(if ($Target.StartsWith('<')) { "<$new>" } else { $new })
}

# Percent-encodes the characters that would end or alter a Markdown link path: % # ? ( ) and space.
function ConvertTo-LinkPath([string]$Path) {
    return $Path.Replace('%', '%25').Replace('#', '%23').Replace('?', '%3F').Replace(' ', '%20').Replace('(', '%28').Replace(')', '%29')
}

# Fence state machine: returns the new opener ('' = outside a fence) after $Line.
function Get-FenceState([string]$Line, [string]$Opener) {
    if ($Line -notmatch '^\s{0,3}(?<f>`{3,}|~{3,})') { return $Opener }
    if ($Opener -eq '') { return $Matches['f'] }
    $f = $Matches['f']
    # A closing fence carries no info string.
    if ($f[0] -eq $Opener[0] -and $f.Length -ge $Opener.Length -and $Line -match '^\s{0,3}(`{3,}|~{3,})\s*$') { return '' }
    return $Opener
}

# Rewrites the links of one non-fence line; $Errors collects 'file:line target' for the ones that do not resolve.
function Update-LinkLine([string]$Line, [int]$Number, [string]$SourceDir, [string]$DestDir, [string]$Root, [string]$Label, $Errors) {
    $spans = @([regex]::Matches($Line, '(`+)(.+?)\1') | ForEach-Object { , @($_.Index, ($_.Index + $_.Length)) })
    # The target allows one level of balanced parentheses; the link text allows one level of nested brackets, so a
    # linked image [![a](img)](page) yields two matches (outer link and inner image), both checked.
    $target = '(?<t><[^>]*>|(?:[^()\s]|\([^()\s]*\))*)'
    $patterns = @(
        ('(?<pre>!?\[[^\]]*\]\(\s*)' + $target),
        ('(?<pre>!?\[(?:[^\[\]]|\[[^\[\]]*\])*\]\(\s*)' + $target),
        # A reference definition: not a footnote ([^1]:), and nothing after the target but an optional quoted or
        # parenthesised title, so prose such as "[HIGH]: this is bad" is not mistaken for one.
        ('^(?<pre>\s{0,3}\[(?!\^)[^\]]+\]:\s*)(?<t><[^>]*>|\S+)(?=\s*$|\s+(?:"[^"]*"|''[^'']*''|\([^)]*\))\s*$)'))
    return Update-LinkMatches $Line $patterns $spans $Number $SourceDir $DestDir $Root $Label $Errors
}

# Replaces the target (group 't') of each distinct link matched by $Patterns outside the inline code $Spans.
function Update-LinkMatches([string]$Line, [string[]]$Patterns, $Spans, [int]$Number, [string]$SourceDir, [string]$DestDir, [string]$Root, [string]$Label, $Errors) {
    $sb = [System.Text.StringBuilder]::new()
    $pos = 0
    $seen = [System.Collections.Generic.HashSet[int]]::new()
    $matchesFound = @($Patterns | ForEach-Object { [regex]::Matches($Line, $_) } | Where-Object { $seen.Add($_.Groups['t'].Index) } | Sort-Object { $_.Groups['t'].Index })
    foreach ($m in $matchesFound) {
        $inCode = @($Spans | Where-Object { $m.Index -ge $_[0] -and $m.Index -lt $_[1] }).Count -gt 0
        $target = $m.Groups['t'].Value
        $new = if ($inCode) { $target } else { Convert-LinkTarget $target $SourceDir $DestDir $Root }
        if ($null -eq $new) { $Errors.Add("${Label}:${Number} $target"); $new = $target }
        $t = $m.Groups['t']
        $null = $sb.Append($Line.Substring($pos, $t.Index - $pos)).Append($new)
        $pos = $t.Index + $t.Length
    }
    return $sb.Append($Line.Substring($pos)).ToString()
}

# Returns @{ Text; Errors = @('<dest>:<line> <link>') }.
function Update-PublishedLinks([string]$Text, [string]$SourceRel, [string]$DestRel, [string]$Root) {
    $sourceDir = ($SourceRel -replace '\\', '/') -replace '/?[^/]*$', ''
    $destDir = ($DestRel -replace '\\', '/') -replace '/?[^/]*$', ''
    $errors = [System.Collections.Generic.List[string]]::new()
    $lines = $Text.Split("`n")
    $fence = ''
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $wasInFence = $fence -ne ''
        $fence = Get-FenceState $lines[$i] $fence
        if ($wasInFence -or $fence -ne '') { continue }
        $lines[$i] = Update-LinkLine $lines[$i] ($i + 1) $sourceDir $destDir $Root $DestRel $errors
    }
    return @{ Text = ($lines -join "`n"); Errors = @($errors) }
}

# Applies Update-PublishedLinks to every Markdown file of the plan already written under $Tmp; returns the errors.
function Update-PlanLinks($Plan, [string]$Tmp) {
    $errors = [System.Collections.Generic.List[string]]::new()
    foreach ($item in $Plan) {
        if ($item.Dest -notlike '*.md') { continue }
        $file = Join-Path $Tmp ($item.Dest -replace '/', '\')
        $base = if ($item.ContainsKey('LinkBase') -and $item.LinkBase) { $item.LinkBase } else { $item.Dest }
        $original = [IO.File]::ReadAllText($file)
        $result = Update-PublishedLinks $original $base $item.Dest $Tmp
        foreach ($e in $result.Errors) { $errors.Add($e) }
        if ($result.Text -cne $original) { [IO.File]::WriteAllText($file, $result.Text, [Text.UTF8Encoding]::new($false)) }
    }
    return @($errors)
}

# Builds the publication of one audit. Returns @{ Ok; Message; Branch; PrUrl; Planned }.
#   -NoPublish: prepares the branch locally (layout, validation, commit) and records the push and
#   'gh pr create' commands in Planned without running them.
# On any failure the caller keeps the audit worktree, the audit branch and its open-audit state file; the temporary
# worktree is always removed and a stale local knowledge/audit-<n> branch from an earlier try is replaced.
function Publish-AuditKnowledge {
    param(
        [Parameter(Mandatory)][int]$Issue,
        [Parameter(Mandatory)][string]$MainRoot,
        [Parameter(Mandatory)][string]$AuditWorktree,
        [Parameter(Mandatory)][string]$StagesDir,
        [Parameter(Mandatory)]$Pipeline,
        [string[]]$DraftDirs = @(),
        [string]$OpenedCsv = '',
        [switch]$NoPublish,
        [string]$TempRoot = ([IO.Path]::GetTempPath()),
        [string]$Repo = 'dlrivada/Encina',
        # Delta audit (#1763): the folder under docs/knowledge/audits/<n>/ the delta stage files go to
        # (pipeline-delta.json `delta.folder`) and the set name for the title. Empty = the normal publication.
        [string]$DeltaFolder = '',
        [string]$DeltaSet = ''
    )
    $isDelta = -not [string]::IsNullOrWhiteSpace($DeltaFolder)
    $branch = if ($isDelta) { "knowledge/audit-$Issue-$DeltaFolder" } else { "knowledge/audit-$Issue" }
    $title = if ($isDelta) { "docs(knowledge): SPEC-003 delta $DeltaSet audit of #$Issue" } else { "docs(knowledge): SPEC-003 audit of #$Issue" }
    $planned = [System.Collections.Generic.List[string]]::new()
    $tmp = Join-Path $TempRoot ("audit-publish-$Issue-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
    $created = $false
    $nothingToCommit = $false
    $fail = { param($msg) @{ Ok = $false; Message = $msg; Branch = $branch; PrUrl = ''; Planned = @($planned) } }

    try {
        $remediation = Get-AuditRemediation $Issue $DraftDirs $OpenedCsv -NamePrefix $(if ($isDelta) { "$Issue-$DeltaFolder" } else { '' })
        # The result names the issues the audit opened, so they must exist first (F2 of the #1766 review); the
        # same gate applies to the remediation drafts of a delta audit (#1763).
        $unopened = @($remediation | Where-Object { -not $_.Url })
        if ($unopened.Count -gt 0) {
            return (& $fail "remediation drafts exist that are not opened yet ($(($unopened | ForEach-Object { $_.Draft }) -join ', ')); run open-remediation.ps1 -Issue $Issue first, then audit-done.ps1.")
        }
        if ($isDelta) {
            # A delta never rewrites the original record's audit block or the original result.
            $plan = Get-DeltaPublishPlan $Issue $AuditWorktree $StagesDir $Pipeline $DeltaFolder
        }
        else {
            $passes = Get-AuditPassCount $AuditWorktree
            $fromText = $false
            if ($passes -eq 0) {
                $verifier = $Pipeline.stages | Where-Object { $_.agent -eq 'audit-verifier' } | Select-Object -First 1
                $passes = Get-VerificationPassCountFromText (Join-Path $StagesDir $verifier.artifact)
                $fromText = $true
            }
            $summary = New-AuditResultSummary $Issue $StagesDir $Pipeline $passes $remediation -PassCountFromText:$fromText
            $plan = Get-AuditPublishPlan $Issue $AuditWorktree $StagesDir $Pipeline $summary
            $outcome = if (@($remediation).Count -gt 0) { 'findings-tracked' } else { 'conforms' }
            foreach ($item in $plan) {
                if ($item.Dest -eq "docs/knowledge/issues/$Issue.md") {
                    $item.Content = Set-AuditBlockOutcome (Get-Content -LiteralPath $item.Source -Raw) $Issue $outcome
                    $item.Source = $null
                }
            }
        }

        $r = Invoke-AuditGit @('-C', $MainRoot, 'fetch', 'origin', 'main')
        if ($r.ExitCode -ne 0) { return (& $fail "git fetch origin main failed: $($r.Output)") }

        # A killed earlier run can leave its temporary worktree registered with the branch checked out; prune the
        # ones whose folder is gone, and fail with a clear message when the branch cannot be replaced.
        $null = Invoke-AuditGit @('-C', $MainRoot, 'worktree', 'prune')
        $stale = Invoke-AuditGit @('-C', $MainRoot, 'rev-parse', '--verify', '--quiet', "refs/heads/$branch")
        if ($stale.ExitCode -eq 0) {
            Write-Warning "audit-done: replacing the stale local branch $branch from an earlier publication attempt."
            $del = Invoke-AuditGit @('-C', $MainRoot, 'branch', '-D', $branch)
            if ($del.ExitCode -ne 0) { return (& $fail "cannot delete the stale local branch ${branch}: $($del.Output). A leftover temporary worktree may still have it checked out; list them with 'git worktree list', remove it with 'git worktree remove --force <path>' and run audit-done.ps1 again.") }
        }
        $r = Invoke-AuditGit @('-C', $MainRoot, 'worktree', 'add', '-b', $branch, $tmp, 'origin/main')
        if ($r.ExitCode -ne 0) { return (& $fail "git worktree add $tmp failed: $($r.Output)") }
        $created = $true

        foreach ($item in $plan) {
            $dest = Join-Path $tmp ($item.Dest -replace '/', '\')
            New-Item -ItemType Directory -Force (Split-Path -Parent $dest) | Out-Null
            if ($item.Source) { Copy-Item -LiteralPath $item.Source -Destination $dest -Force }
            else { [IO.File]::WriteAllText($dest, $item.Content, [Text.UTF8Encoding]::new($false)) }
        }

        # #1817: rewrite relative links written for the source folder; a link that resolves nowhere fails the publish.
        $brokenLinks = Update-PlanLinks $plan $tmp
        if ($brokenLinks.Count -gt 0) { return (& $fail "relative Markdown links that do not resolve (file:line link), nothing was published:`n$($brokenLinks -join "`n")") }

        # The whole docs/knowledge tree must validate with the new files in place (schema, audit.record exists,
        # every result and stage folder has its record). The validator of the fresh origin/main checkout is used.
        $script = Join-Path $tmp '.github\scripts\knowledge-records.cs'
        if (-not (Test-Path -LiteralPath $script)) { return (& $fail "$script not found in the publication checkout; cannot validate.") }
        Push-Location $tmp
        try { $check = & dotnet run --file $script -- --check 2>&1 | ForEach-Object { "$_" }; $checkExit = $LASTEXITCODE }
        finally { Pop-Location }
        if ($checkExit -ne 0) { return (& $fail "knowledge-records --check failed on the publication checkout:`n$($check -join "`n")") }

        $r = Invoke-AuditGit @('-C', $tmp, 'add', 'docs/knowledge')
        if ($r.ExitCode -ne 0) { return (& $fail "git add failed: $($r.Output)") }
        # Nothing to commit means origin/main already holds this exact publication (the pull request was merged
        # after an earlier run): count it as published so a retry can close the audit (F6 of the #1766 review).
        $staged = Invoke-AuditGit @('-C', $tmp, 'diff', '--cached', '--quiet')
        if ($staged.ExitCode -eq 0) {
            $nothingToCommit = $true
        }
        else {
            $r = Invoke-AuditGit @('-C', $tmp, 'commit', '-m', $title)
            if ($r.ExitCode -ne 0) { return (& $fail "git commit failed: $($r.Output)") }
        }

        # After a successful publication the temporary worktree goes first, then the local branch (the remote copy
        # is what the pull request uses); a branch that cannot be deleted is a failure with a clear message.
        $complete = {
            param($res)
            $null = Invoke-AuditGit @('-C', $MainRoot, 'worktree', 'remove', $tmp, '--force')
            $del = Invoke-AuditGit @('-C', $MainRoot, 'branch', '-D', $branch)
            if ($del.ExitCode -ne 0) { return (& $fail "published, but the local branch $branch cannot be deleted: $($del.Output). Remove the leftover temporary worktree ('git worktree list', then 'git worktree remove --force <path>') and delete the branch, then run audit-done.ps1 again.") }
            return $res
        }
        if ($nothingToCommit) {
            return (& $complete @{ Ok = $true; Message = 'nothing to publish: origin/main already holds this audit'; Branch = $branch; PrUrl = ''; Planned = @($planned) })
        }

        $pushArgs = @('-C', $tmp, 'push', '-u', 'origin', $branch)
        $prArgs = @('pr', 'create', '--repo', $Repo, '--base', 'main', '--head', $branch, '--title', $title, '--body', 'Refs #1345')
        if ($NoPublish) {
            $planned.Add('git ' + ($pushArgs -join ' '))
            $planned.Add('gh ' + ($prArgs -join ' '))
            return @{ Ok = $true; Message = 'prepared (NoPublish): nothing pushed, no pull request created'; Branch = $branch; PrUrl = ''; Planned = @($planned) }
        }

        # The force-push is intended: knowledge/audit-<n> is a branch this script owns and recreates from
        # origin/main on every run, so a retry after a partial publication replaces the remote copy of the earlier
        # attempt (AGENTS.md section 10 forbids force-pushing only main/master).
        $r = Invoke-AuditGit ($pushArgs + '--force')
        if ($r.ExitCode -ne 0) { return (& $fail "git push failed: $($r.Output)") }
        # A pull request left by an earlier attempt is reused instead of opening a duplicate.
        $existing = Invoke-AuditGh @('pr', 'list', '--repo', $Repo, '--head', $branch, '--state', 'open', '--json', 'url', '--jq', '.[0].url')
        $existingUrl = ($existing.Output -split "`n" | Where-Object { $_ -match '^https://' } | Select-Object -First 1)
        if ($existing.ExitCode -eq 0 -and $existingUrl) {
            return (& $complete @{ Ok = $true; Message = "published $branch (pull request already open)"; Branch = $branch; PrUrl = "$existingUrl"; Planned = @($planned) })
        }
        $r = Invoke-AuditGh $prArgs
        if ($r.ExitCode -ne 0) { return (& $fail "gh pr create failed (the branch $branch is pushed; re-running replaces it): $($r.Output)") }
        $url = ($r.Output -split "`n" | Where-Object { $_ -match '^https://' } | Select-Object -Last 1)
        return (& $complete @{ Ok = $true; Message = "published $branch"; Branch = $branch; PrUrl = "$url"; Planned = @($planned) })
    }
    catch {
        return (& $fail "publication failed: $($_.Exception.Message)")
    }
    finally {
        if ($created) { $null = Invoke-AuditGit @('-C', $MainRoot, 'worktree', 'remove', $tmp, '--force') }
    }
}
