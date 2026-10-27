# PreToolUse hook (Bash|PowerShell): validates `gh issue create` against .github/ISSUE_TEMPLATE.
#
# CLAUDE.md, "Issue Body Format (MANDATORY)": an issue title starts with its template prefix ([BUG], [DEBT],
# ...) and the body uses that template's level-2 headers verbatim (case included) and in order. The templates
# are read at run time, so the hook never drifts from them.
#
# #1410: a `--body-file` call must also show the body was drafted by the free local model
# (tools/ai/local-ai-ask.cs), never paid tokens, before the header check even runs. #1593: while the local
# model is switched off, the local-ai-standin agent's drafts count the same: a row of
# artifacts/local-ai/standin-ledger.csv (same columns, same 24-hour window, same roots) is evidence exactly like
# a row of ledger.csv. One of:
#   (a) the file's first line is `<!-- local-draft: <path> -->` (<path> relative to the repository root, or
#       absolute) where <path> exists AND a row of a local-ai/ledger.csv from the last 24 hours (the
#       repository root's own, or any .claude/worktrees/*/artifacts/local-ai/ledger.csv) names <path> as its
#       outFile (paths compared normalised: slash direction, relative/absolute, case);
#   (b) the body file itself IS such a recent ledger outFile (no pointer comment needed — this is the plain
#       "--out <this file>" case, e.g. a worker's issue file copied verbatim to a scratchpad body file); or
#   (c) the body file's content matches (once its own leading `<!-- ... -->` header is stripped, the same way
#       open-remediation.ps1 strips it before calling `gh`) a file under artifacts/knowledge/remediation/,
#       whose OWN header names the SAME --title, written in the last 24 hours — the SPEC-003 remediation
#       pipeline's own drafts, whose `--body-file` is always a stripped $env:TEMP copy, never the ledger
#       outFile itself, so (a)/(b) alone would not recognise it. The title check keeps one legitimately
#       drafted remediation file from being replayed under a different title within the 24-hour window.
# An explicit, logged opt-out is also accepted: a first line `<!-- local-draft: none, reason: <text> -->` with
# a non-empty reason, appended to <repository root>\artifacts\local-ai\opt-outs.log. Only checked on
# `--body-file`, per #1410's own decision; `--body`/inline text and a dynamic title/body-file are left to the
# existing header check alone (unchanged from before #1410 — a known, accepted gap, not evaluated further
# here; see docs/knowledge/issues/1410.md).
#
# Only the arguments of the `gh issue create` statement itself are read, never the rest of the command line.
# Allowed without the body checks: issues on another repository (-R/--repo), --web, --template, calls without a
# body, and titles or bodies that come from a variable or a subexpression without visible headers.
#
# #1926: before any of those body skips, every `gh issue create` on this repository (except --web/--template)
# must also carry an existing, open --milestone (the last one counts; list cached 12 hours, a failed lookup
# denies), the template's default label and one priority label (an [EPIC] needs none); labels compare
# case-insensitively. A variable title is judged on the milestone only
# (its prefix is unknown, so the label checks are skipped and the message says so).
#
# #2470 (decision register): the needs-decision label marks a Dilemma, a choice with two or more valid options
# that only the maintainer can make. An issue created (`gh issue create`) or edited (`gh issue edit
# --add-label`) with that label must contain a Dilemma entry marker in its body (a line starting with "Dilemma", or a "Class: Dilemma" field; negated text does not count; the Dilemma entries of the
# decision register; the decision-extractor agent produces them). For an edit without --body/--body-file the
# existing body is read with `gh api` (ENCINA_ISSUE_GH overrides gh in tests); when it cannot be read the call
# is denied. A variable label, title or body cannot be read and is not judged. Headers inside
# fenced code blocks do not count. Exit code 2 blocks the call and shows stderr to Claude; any failure of the
# hook itself allows the call.
#
# #2823: issue creation through `gh api` gets the same checks. REST `POST repos/dlrivada/Encina/issues` is the
# sanctioned route when the GraphQL quota is exhausted (`gh issue create` itself uses GraphQL), with the same draft
# and template rules: gh api repos/dlrivada/Encina/issues -X POST -f 'title=[DEBT] ...' -F body=@<drafted file>
# -f 'labels[]=<label>' (once per label, not comma-split) -F milestone=<number>.
#   - Creation: method POST (-X/--method, value case-insensitive, leading '=' stripped, the last one wins) or an
#     implicit POST (a -f/-F/--raw-field/--field/--input option with no explicit GET), on the issues endpoint
#     (normalised: scheme and host, query, fragment, URL escapes, slashes, case; `repositories/<id>/issues`; the
#     quoted {owner}/{repo} placeholder; variable owner/repo segments as wildcards). A wholly variable endpoint is
#     judged only as a POST with a title= or body= field. Sub-paths (issues/<n>, /comments, /labels, sub_issues,
#     dependencies/blocked_by), other repositories and a --hostname other than github.com are not judged.
#   - Fields: -f is literal, -F reads @file (@- the piped literal); with --input <file>.json the title, body,
#     labels and milestone are the JSON's and -f/-F are ignored. The milestone is a NUMBER checked against the cache.
#   - Local-draft evidence (#1410) applies to a file-backed body (-F body=@file, or the JSON body of --input
#     file.json, judged on its first line); an inline body keeps the `gh issue create --body` parity (known gap).
#   - Fail closed: an unreadable title or body (variable, missing file, unpiped stdin, malformed JSON, none given)
#     is denied, naming what could not be read.
#   - GraphQL: `gh api graphql` whose query (-f/-F query=, -F query=@file, --input) contains createIssue is always
#     denied; an unreadable query is allowed so other GraphQL operations keep working.
# The milestone cache (artifacts/issue-hygiene/milestones-v2.txt) holds `<state><TAB><number><TAB><title>` lines; the
# old milestones.txt is ignored.

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')

    . (Join-Path $PSScriptRoot '_read-payload.ps1')
    $payload = Read-HookStdin | ConvertFrom-Json
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command) -or $command -notmatch '\bgh\b') { exit 0 }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $statements = Split-CommandStatements -Text $command -Bash:($payload.tool_name -eq 'Bash')

    # Get-RepoRoot, Get-Templates and Get-Headers live in _issue-templates.ps1 (shared with the #1980 spawn
    # gate). Get-RepoRoot is also used by the #1410 local-draft evidence check below, so both agree on the same
    # root (and the same set of candidate ledger/remediation locations).
    # A missing or broken helper denies (it must not fall into the outer catch, which allows): without it no
    # `gh issue create` could be checked at all.
    try { . (Join-Path $PSScriptRoot '_issue-templates.ps1') }
    catch {
        [Console]::Error.WriteLine("Blocked: check-issue-template.ps1 could not load its shared helper _issue-templates.ps1 ($($_.Exception.Message)), so this gh command cannot be checked. Restore the file.")
        exit 2
    }

    # #1410 local-draft evidence -----------------------------------------------------------------------------

    # $Root itself, plus every .claude/worktrees/<name> directory under it — a worker drafts and records its
    # ledger line in its own worktree, never the main checkout's.
    function Get-CandidateRoots([string]$Root) {
        $roots = [System.Collections.Generic.List[string]]::new()
        if (-not $Root) { return $roots }
        $roots.Add($Root)
        $wtDir = Join-Path $Root '.claude/worktrees'
        if (Test-Path -LiteralPath $wtDir) {
            foreach ($d in (Get-ChildItem -LiteralPath $wtDir -Directory -ErrorAction SilentlyContinue)) { $roots.Add($d.FullName) }
        }
        return , $roots
    }

    # Parses a ledger timestampUtc value ("yyyy-MM-ddTHH:mm:ssZ") as UTC regardless of the host's own locale
    # or timezone.
    function ConvertFrom-LedgerTimestamp([string]$Value) {
        $dt = [datetime]::MinValue
        $styles = [System.Globalization.DateTimeStyles]::AdjustToUniversal -bor [System.Globalization.DateTimeStyles]::AssumeUniversal
        if (-not [datetime]::TryParse($Value, [System.Globalization.CultureInfo]::InvariantCulture, $styles, [ref]$dt)) { return $null }
        return $dt
    }

    function Get-RecentLedgerRows([string]$Root, [datetime]$Since) {
        $rows = [System.Collections.Generic.List[object]]::new()
        # ledger.csv (the local model) and standin-ledger.csv (the local-ai-standin agent, #1593) carry the
        # same columns and count the same way as evidence.
        foreach ($ledgerName in 'ledger.csv', 'standin-ledger.csv') {
            $ledger = Join-Path $Root "artifacts/local-ai/$ledgerName"
            if (-not (Test-Path -LiteralPath $ledger)) { continue }
            $parsed = $null
            try { $parsed = Import-Csv -LiteralPath $ledger } catch { continue }
            foreach ($row in $parsed) {
                $ts = ConvertFrom-LedgerTimestamp ([string]$row.timestampUtc)
                if ($null -eq $ts -or $ts -lt $Since) { continue }
                $rows.Add($row)
            }
        }
        return $rows
    }

    # A path normalised for comparison: resolved to a full, lowercase, forward-slash path. $Value is resolved
    # against $Root when it is not itself rooted (a ledger outFile is normally written relative to the root the
    # process ran from; a pointer comment's path is relative to the repository root, AGENTS.md/decision 1).
    function Resolve-NormalizedPath([string]$Value, [string]$Root) {
        if ([string]::IsNullOrWhiteSpace($Value)) { return $null }
        $v = $Value.Trim()
        try {
            $full = if ([IO.Path]::IsPathRooted($v)) { $v } else { Join-Path $Root $v }
            return ([IO.Path]::GetFullPath($full)).ToLowerInvariant().Replace('\', '/')
        }
        catch { return $null }
    }

    # Case (b): $TargetPath (already an absolute, resolved path) is itself a ledger outFile from the last 24
    # hours, in the root's own ledger or any worktree's.
    function Test-LedgerOutFileMatch([string]$TargetPath, [string]$Root, [datetime]$Since) {
        $targetNorm = ([IO.Path]::GetFullPath($TargetPath)).ToLowerInvariant().Replace('\', '/')
        foreach ($candidateRoot in (Get-CandidateRoots $Root)) {
            foreach ($row in (Get-RecentLedgerRows $candidateRoot $Since)) {
                $rowNorm = Resolve-NormalizedPath ([string]$row.outFile) $candidateRoot
                if ($rowNorm -and $rowNorm -eq $targetNorm) { return $true }
            }
        }
        return $false
    }

    # Case (a): a `<!-- local-draft: <path> -->` pointer. <path> must exist AND be a recent ledger outFile.
    function Test-PointerEvidence([string]$PointerValue, [string]$Root, [datetime]$Since) {
        $v = $PointerValue.Trim()
        $resolved = $null
        try {
            $resolved = if ([IO.Path]::IsPathRooted($v)) { $v } else { Join-Path $Root $v }
            $resolved = [IO.Path]::GetFullPath($resolved)
        }
        catch { return $false }
        if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) { return $false }
        return (Test-LedgerOutFileMatch $resolved $Root $Since)
    }

    # Case (c): tools/ai/audit/open-remediation.ps1 copies a drafted remediation file's body (its own leading
    # `<!-- title: ...; labels: ...; milestone: ... -->` header stripped, exactly the regex below) into a
    # $env:TEMP file before calling `gh issue create --body-file`, so neither (a) nor (b) can recognise it —
    # that temp copy is never itself a ledger outFile, and it carries no pointer comment. Recognised instead by
    # content AND title together: a recent (written in the last 24 hours) file under
    # artifacts/knowledge/remediation/ whose own header-stripped content matches $BodyText exactly (line endings
    # and surrounding whitespace ignored) AND whose own header `title:` line equals $TitleText (the `gh issue
    # create --title` value). The title check stops one drafted remediation file from being copied verbatim
    # into a different issue's body-file within the same 24-hour window — a body-only match would otherwise let
    # any later, unrelated `gh issue create` reuse one legitimately drafted file's evidence indefinitely (a
    # real remediation draft is used exactly once, by open-remediation.ps1, for its own title).
    function Test-RemediationDraftMatch([string]$BodyText, [string]$TitleText, [string]$Root, [datetime]$Since) {
        $normalizedBody = ($BodyText -replace "`r`n", "`n").Trim()
        $wantedTitle = $TitleText.Trim()
        foreach ($candidateRoot in (Get-CandidateRoots $Root)) {
            $dir = Join-Path $candidateRoot 'artifacts/knowledge/remediation'
            if (-not (Test-Path -LiteralPath $dir)) { continue }
            foreach ($f in (Get-ChildItem -LiteralPath $dir -Filter '*.md' -File -ErrorAction SilentlyContinue)) {
                if ($f.LastWriteTimeUtc -lt $Since) { continue }
                $raw = $null
                try { $raw = Get-Content -LiteralPath $f.FullName -Raw } catch { continue }
                $titleMatch = [regex]::Match($raw, '(?m)^title:\s*(?<t>.+?)\s*$')
                if (-not $titleMatch.Success -or $titleMatch.Groups['t'].Value.Trim() -ne $wantedTitle) { continue }
                $stripped = ([regex]::Replace($raw, '(?s)^\s*<!--.*?-->\s*', '') -replace "`r`n", "`n").Trim()
                if ($stripped -eq $normalizedBody) { return $true }
            }
        }
        return $false
    }

    function Format-CsvField([string]$Value) {
        if ($Value -match '[",\n]') { return '"' + ($Value -replace '"', '""') + '"' }
        return $Value
    }

    # A first-line opt-out with a non-empty reason is logged and accepted unconditionally; the reason is not
    # otherwise judged (#1410 decision 2: "server down" is one legitimate reason among others, never the only
    # one accepted).
    function Test-OptOut([string]$FirstLine, [string]$Path, [string]$Root) {
        $m = [regex]::Match($FirstLine, '^<!--\s*local-draft:\s*none\s*,\s*reason:\s*(?<reason>.*?)\s*-->$')
        if (-not $m.Success) { return $null }
        $reason = $m.Groups['reason'].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($reason)) { return $false }
        # The log write is best-effort: a failure here (a read-only artifacts/ folder, a locked file) must
        # still allow the call the opt-out itself grants -- it must never fall through to the outer try/catch,
        # whose exit-0-on-any-failure is meant for the hook's OWN failures, not for masking a real refusal
        # elsewhere in this function as an accidental allow.
        try {
            $logPath = Join-Path $Root 'artifacts/local-ai/opt-outs.log'
            New-Item -ItemType Directory -Force (Split-Path -Parent $logPath) | Out-Null
            if (-not (Test-Path -LiteralPath $logPath)) { Set-Content -LiteralPath $logPath -Encoding utf8 -Value 'timestampUtc,bodyFile,reason' }
            $line = "$([DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')),$(Format-CsvField $Path),$(Format-CsvField $reason)"
            Add-Content -LiteralPath $logPath -Value $line
        }
        catch { }
        return $true
    }

    # Decides whether $Path/$Body (a `gh issue create --body-file` target, created with --title $TitleText)
    # shows local-model drafting or a logged opt-out. $Root is the repository root Get-RepoRoot resolved for
    # this statement; when it cannot be resolved at all the check is skipped (fails open, exactly like the
    # template lookup below when no .github/ISSUE_TEMPLATE is found).
    function Test-LocalDraftCompliance([string]$Path, [string]$Body, [string]$TitleText, [string]$Root) {
        if (-not $Root) { return $true }
        $since = (Get-Date).ToUniversalTime().AddHours(-24)
        $firstLine = ((($Body -split "`r?`n") | Select-Object -First 1)); if ($null -eq $firstLine) { $firstLine = '' }
        $firstLine = $firstLine.Trim()

        $optOut = Test-OptOut $firstLine $Path $Root
        if ($null -ne $optOut) { return $optOut }

        $pointer = [regex]::Match($firstLine, '^<!--\s*local-draft:\s*(?<path>(?!none\b).+?)\s*-->$')
        if ($pointer.Success -and (Test-PointerEvidence $pointer.Groups['path'].Value $Root $since)) { return $true }

        if (Test-LedgerOutFileMatch $Path $Root $since) { return $true }

        return (Test-RemediationDraftMatch $Body $TitleText $Root $since)
    }

    # #1926 issue hygiene: an issue is opened complete or not at all (maintainer rule of 2026-10-06). A
    # `gh issue create` needs an existing milestone, the template's default label and one priority label
    # (an [EPIC] needs no priority). Project membership is not checked here (the active token cannot write
    # projects); no workflow writes to the project: the caller adds the issue (open-issue skill step 4,
    # open-remediation.ps1) and tools/ai/issues/check-project-membership.ps1 finds the ones still missing.
    $PriorityLabels = @('p0-mandatory', 'p1-recommended', 'p2-post-1.0')

    # Milestones of the repository: a cache file (ENCINA_MILESTONES_CACHE, default
    # <root>/artifacts/issue-hygiene/milestones-v2.txt) younger than 12 hours, otherwise a `gh api` lookup that
    # refreshes it. Returns $null when the lookup fails, so the caller denies instead of allowing.
    # Every milestone, open and closed, as `<state><TAB><number><TAB><title>` lines (#2823: the REST route
    # sends the milestone by number, `gh issue create` by title). The cache file was renamed from
    # milestones.txt (two columns) so an old file is never read by this reader; a line that does not have the
    # three columns (a stale two-column file named through ENCINA_MILESTONES_CACHE) is dropped, and a cache
    # with no valid line is refreshed.
    function ConvertFrom-MilestoneLine([string]$Line) {
        $parts = $Line -split "`t", 3
        if ($parts.Count -ne 3 -or $parts[0] -notmatch '^(?:open|closed)$' -or $parts[1] -notmatch '^\d+$' -or [string]::IsNullOrWhiteSpace($parts[2])) { return $null }
        return [pscustomobject]@{ State = $parts[0]; Number = [int]$parts[1]; Title = $parts[2] }
    }

    function Get-MilestoneRecords([string]$Root) {
        $cache = if ($env:ENCINA_MILESTONES_CACHE) { $env:ENCINA_MILESTONES_CACHE } elseif ($Root) { Join-Path $Root 'artifacts/issue-hygiene/milestones-v2.txt' } else { $null }
        if ($cache -and (Test-Path -LiteralPath $cache) -and ((Get-Item -LiteralPath $cache).LastWriteTimeUtc -gt (Get-Date).ToUniversalTime().AddHours(-12))) {
            $cached = @(Get-Content -LiteralPath $cache -Encoding utf8 | Where-Object { $_.Trim() } | ForEach-Object { ConvertFrom-MilestoneLine $_ } | Where-Object { $_ })
            if ($cached.Count -gt 0) { return $cached }
        }
        $gh = if ($env:ENCINA_ISSUE_GH) { $env:ENCINA_ISSUE_GH } else { 'gh' }
        $saved = [Console]::OutputEncoding
        try {
            [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
            $lines = @(& $gh api 'repos/dlrivada/Encina/milestones?state=all&per_page=100' --paginate --jq '.[] | .state + "\t" + (.number | tostring) + "\t" + .title' 2>$null | ForEach-Object { "$_".TrimEnd() } | Where-Object { $_ })
            if ($LASTEXITCODE -ne 0 -or $lines.Count -eq 0) { return $null }
        }
        catch { return $null }
        finally { [Console]::OutputEncoding = $saved }
        $records = @($lines | ForEach-Object { ConvertFrom-MilestoneLine $_ } | Where-Object { $_ })
        if ($records.Count -eq 0) { return $null }
        if ($cache) {
            try {
                New-Item -ItemType Directory -Force (Split-Path -Parent $cache) | Out-Null
                [IO.File]::WriteAllLines($cache, [string[]]@($records | ForEach-Object { "$($_.State)`t$($_.Number)`t$($_.Title)" }), [Text.UTF8Encoding]::new($false))
            }
            catch { }
        }
        return $records
    }

    # The default-label and priority-label items missing from $Labels, shared by `gh issue create` (the
    # --label wording) and the REST route (the labels[] wording, #2823). $Labels are already split into names.
    function Get-MissingLabelItems([string[]]$Labels, [string]$Prefix, [string]$DefaultLabel, [bool]$Rest) {
        $missing = [System.Collections.Generic.List[string]]::new()
        $labelWord = if ($Rest) { "-f 'labels[]=" } else { '--label ' }
        $labelEnd = if ($Rest) { "'" } else { '' }
        if ($DefaultLabel -and $Labels -inotcontains $DefaultLabel) { $missing.Add("$labelWord$DefaultLabel$labelEnd (the default label of the $Prefix template)") }
        # #2470: needs-decision is no longer mandatory for a [FEATURE] or [SPIKE] (the 2026-10-06 rule produced
        # 105 labels with 0 real dilemmas). It is allowed only when the body names a Dilemma (see
        # Test-HasNeedsDecision and Test-HasDilemma), so it marks only choices the maintainer must make.
        if ($Prefix -ne '[EPIC]' -and -not ($Labels | Where-Object { $PriorityLabels -icontains $_ })) {
            $missing.Add($(if ($Rest) { "one priority label: " + (($PriorityLabels | ForEach-Object { "-f 'labels[]=$_'" }) -join ' | ') } else { "one priority label: --label $($PriorityLabels -join ' | ')" }))
        }
        return , $missing
    }

    # Returns the list of missing metadata items for one `gh issue create` statement (empty when complete).
    # A dynamic (variable) milestone or label value cannot be read, so it counts as present. With a variable
    # title ($TitleKnown false) the prefix is unknown: the default-label, needs-decision and priority checks
    # are skipped (an [EPIC] title is exempt from the priority and a variable title may be one); the milestone
    # is still checked. Labels compare case-insensitively like GitHub; the last --milestone wins like gh.
    function Get-MissingMetadata($Options, [bool]$TitleKnown, [string]$Prefix, [string]$DefaultLabel, [string]$Root) {
        $missing = [System.Collections.Generic.List[string]]::new()

        # No @() around Get-OptionValues: it already returns the array wrapped (`, @(...)`), and @() would nest it.
        $milestone = Get-OptionValues $Options @('-m', '--milestone')
        if ($milestone.Count -eq 0) { $missing.Add('--milestone "<existing milestone title>"') }
        elseif (-not $milestone[-1].Dynamic) {
            $wanted = [string]$milestone[-1].Value
            $records = Get-MilestoneRecords $Root
            if ($null -eq $records) { $missing.Add("milestone '$wanted' could not be verified (the milestone lookup failed: run gh auth status, or seed artifacts/issue-hygiene/milestones-v2.txt)") }
            else {
                $found = $records | Where-Object { $_.Title -ceq $wanted } | Select-Object -First 1
                if ($null -eq $found) { $missing.Add("milestone '$wanted' is not an existing milestone title (gh api repos/dlrivada/Encina/milestones; a milestone created in the last 12 hours needs artifacts/issue-hygiene/milestones-v2.txt deleted to refresh the cache)") }
                elseif ($found.State -ne 'open') { $missing.Add("milestone '$wanted' is closed; pick an open milestone") }
            }
        }

        $labelValues = Get-OptionValues $Options @('-l', '--label')
        if ($TitleKnown -and -not ($labelValues | Where-Object { $_.Dynamic })) {
            $labels = @($labelValues | ForEach-Object { $_.Value -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
            foreach ($item in (Get-MissingLabelItems $labels $Prefix $DefaultLabel $false)) { $missing.Add($item) }
        }
        return , $missing
    }

    $LocalDraftTail = "must show it was drafted by the free local model, or by the local-ai-standin agent while the local model is switched off (CLAUDE.md, Model routing; AGENTS.md Sec.2/local-ai-task skill) -- one of: a first line '<!-- local-draft: <path> -->' where <path> exists and a local-ai/ledger.csv or local-ai/standin-ledger.csv row from the last 24 hours names it as outFile; the body file itself being such a recent ledger outFile; or a first line '<!-- local-draft: none, reason: <text> -->' opt-out with a non-empty reason (logged to artifacts/local-ai/opt-outs.log). See the local-ai-task skill."
    $LocalDraftMessage = "Blocked: gh issue create's --body-file $LocalDraftTail"
    $RestLocalDraftMessage = "Blocked: a REST issue creation (gh api repos/dlrivada/Encina/issues) with a file-backed body (-F body=@<file>, or the body of --input <file>.json judged on its first line) $LocalDraftTail"

    # Options of `gh issue create` that take a value, so their values are never parsed as options.
    $issueValueOptions = @('-t', '--title', '-b', '--body', '-F', '--body-file', '-R', '--repo', '-l', '--label', '-m', '--milestone', '-a', '--assignee', '-p', '--project', '-T', '--template', '--recover')

    # #2470: needs-decision marks a Dilemma. $LabelOptions are the option values that carry labels; true when
    # one of the static values names needs-decision (a variable value cannot be read and is skipped, but a
    # literal needs-decision next to it is still judged).
    function Test-HasNeedsDecision($LabelOptions) {
        $labels = @($LabelOptions | Where-Object { -not $_.Dynamic } | ForEach-Object { $_.Value -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
        return ($labels -icontains 'needs-decision')
    }

    # A Dilemma entry marker: a line that starts with "Dilemma" (optionally after a list bullet, heading or bold,
    # e.g. "Dilemma D1: ...", "### Dilemma 1") or a "Class: Dilemma" field. Negated text ("No Dilemma", "no
    # Dilemma here") and "Dilemma: none" do not match.
    function Test-HasDilemma([string]$Body) {
        if ($Body -match '(?im)^\s*(?:[-*+>]\s+|#{1,6}\s+)*\**\s*class\s*\**\s*:\s*\**\s*dilemma\b') { return $true }
        return ($Body -match '(?im)^\s*(?:[-*+>]\s+|#{1,6}\s+)*\**\s*dilemma\b(?!\s*:?\s*(?:none|n/?a|no)\b)')
    }

    # The template check shared by `gh issue create` and the REST route (#2823): the message of the first
    # violation (a known template prefix, every template header present, in template order), or $null when the
    # title and body comply or no template can be read at all.
    function Get-TemplateViolation([string]$TitleText, [string]$Body, [string]$Root) {
        $templates = Get-Templates $Root
        if ($templates.Count -eq 0) { return $null }

        $prefix = [regex]::Match($TitleText, '^\[[A-Z]+\]')
        if (-not $prefix.Success -or -not $templates.ContainsKey($prefix.Value)) {
            $known = ($templates.Keys | Sort-Object) -join ', '
            return "Blocked: issue title '$TitleText' must start with a template prefix ($known). See CLAUDE.md, Issue Templates; normalise [TECH-DEBT] to [DEBT], [TESTING] to [TEST], [ARCHITECTURE]/[DECISION]/[REVIEW] to [SPIKE]."
        }
        $template = $templates[$prefix.Value]
        $bodyHeaders = Get-Headers $Body

        $missing = @($template.Headers | Where-Object { $bodyHeaders -cnotcontains $_ })
        if ($missing.Count -gt 0) {
            return "Blocked: a $($prefix.Value) issue uses the headers of .github/ISSUE_TEMPLATE/$($template.File) verbatim and in order (CLAUDE.md, Issue Body Format). Missing: $($missing -join ' | '). Read the template, fill every section (tick the checkboxes that apply) and retry. The open-issue skill describes the procedure."
        }

        $positions = @($template.Headers | ForEach-Object { $bodyHeaders.IndexOf($_) })
        for ($i = 1; $i -lt $positions.Count; $i++) {
            if ($positions[$i] -lt $positions[$i - 1]) {
                return "Blocked: the headers of .github/ISSUE_TEMPLATE/$($template.File) must appear in template order; '$($template.Headers[$i])' comes before '$($template.Headers[$i - 1])'."
            }
        }
        return $null
    }

    $DilemmaMessage = "Blocked: an issue labelled needs-decision must contain a Dilemma entry marker in its body: a line starting with 'Dilemma' (for example 'Dilemma D1: ...') or a 'Class: Dilemma' field; the word inside a sentence, 'No Dilemma' or 'Dilemma: none' do not count (#2470). The label marks a choice with two or more valid options, each better in a concrete named scenario, that only the maintainer can make; a settled or trivial choice carries its citation instead and no label. Run the decision-extractor agent on the issue (.claude/agents/decision-extractor.md), keep only the Dilemma entries in the body (a 'Dilemma D<n>' entry with the options and the scenario in which each wins) and retry; when no Dilemma exists, drop the needs-decision label."

    # Options of `gh issue edit` that take a value.
    $editValueOptions = @('-t', '--title', '-b', '--body', '-F', '--body-file', '-R', '--repo', '--add-label', '--remove-label', '-m', '--milestone', '--add-assignee', '--remove-assignee', '--add-project', '--remove-project')

    # #2823: issue creation through `gh api` ---------------------------------------------------------------------
    # Options of `gh api` that take a value, so a value (`-X POST`, `-p preview`) is never an endpoint candidate;
    # data options switch gh from GET to POST when no explicit method is given.
    $apiValueOptions = @('-X', '--method', '-H', '--header', '-f', '--raw-field', '-F', '--field', '-q', '--jq', '-t', '--template', '--input', '--hostname', '--cache', '-p', '--preview')
    $apiDataOptions = @('-f', '--raw-field', '-F', '--field', '--input')
    # Stands for a variable inside an endpoint (a wildcard when the endpoint is matched).
    $Wild = [string][char]1
    # Stands for one variable segment (a $( ... ) between slashes).
    $WildSegment = [string][char]2

    # Every non-option token of a `gh api` statement: each one is an endpoint candidate.
    function Get-ApiPositionals($Tokens, [int]$From) {
        $positionals = [System.Collections.Generic.List[object]]::new()
        for ($k = $From; $k -lt $Tokens.Count; $k++) {
            $v = [string]$Tokens[$k].Value
            if ($v.StartsWith('-')) {
                if ($v -eq '-' -or $v -eq '--') { continue }
                if (-not ($v.StartsWith('--') -and $v.Contains('=')) -and $apiValueOptions -ccontains $v) { $k++ }
                continue
            }
            $positionals.Add($Tokens[$k])
        }
        return , $positionals
    }

    # What an endpoint token names: 'issues' (the issues collection of dlrivada/Encina, by name or by repository
    # id: creation), 'graphql', 'dynamic' (an endpoint that is wholly a variable) or $null (anything else:
    # sub-paths such as issues/<n>, /labels, /comments, sub_issues, dependencies/blocked_by are not creation).
    # The path is normalised first: scheme and host, query, fragment, URL escapes, leading and trailing slash,
    # case. A variable owner or repository segment is a wildcard; {owner}/{repo} stand for the current repository.
    function Get-ApiEndpointKind($Token) {
        $v = [string]$Token.Value
        # A token with a $( ... ) holds only the inner text of the subexpression, so a segment that still has a
        # '$' or '(' is a variable SEGMENT (a wildcard for that segment only); a token that is wholly one
        # subexpression (no slash) is a wholly variable endpoint.
        if ($Token.Subexpression -and -not $v.Contains('/')) { return 'dynamic' }
        $urlMatch = [regex]::Match($v, '^https?://(?<h>[^/]+)/(?<r>.*)$', 'IgnoreCase')
        if ($urlMatch.Success) {
            if ($urlMatch.Groups['h'].Value -ine 'api.github.com') { return $null }
            $v = $urlMatch.Groups['r'].Value
        }
        $v = ($v -split '[?#]', 2)[0]
        try { $v = [Uri]::UnescapeDataString($v) } catch { }
        $v = $v.Trim('/')
        if ($Token.Subexpression) { $v = (($v -split '/') | ForEach-Object { if ($_ -match '[$(]') { $WildSegment } else { $_ } }) -join '/' }
        elseif ($Token.Dynamic) { $v = [regex]::Replace($v, '\$(?:\{[^}]*\}|[\w:?]+)', $Wild) }
        $v = [regex]::Replace($v, '\{(?:owner|repo)\}', $Wild, 'IgnoreCase')
        $lower = $v.ToLowerInvariant()

        if ($lower.Contains($Wild) -or $lower.Contains($WildSegment)) {
            if (($lower.Replace($Wild, '').Replace($WildSegment, '').Replace('/', '')).Length -eq 0) { return 'dynamic' }
            if ($lower -notmatch '(?:^|/)issues$') { return $null }
            $pattern = '^' + (([regex]::Escape($lower).Replace([regex]::Escape($Wild), '.*').Replace([regex]::Escape($WildSegment), '[^/]+'))) + '$'
            if ('repos/dlrivada/encina/issues' -match $pattern -or 'repositories/1/issues' -match $pattern) { return 'issues' }
            return $null
        }
        if ($lower -eq 'repos/dlrivada/encina/issues' -or $lower -match '^repositories/\d+/issues$') { return 'issues' }
        if ($lower -eq 'graphql') { return 'graphql' }
        return $null
    }

    # True when gh sends the request as POST: -X/--method POST (the last one counts, value case-insensitive,
    # a leading '=' stripped), or no explicit method and a data option (gh defaults to POST once a parameter is
    # added); an explicit GET puts the fields in the query string. A method that is a variable counts as POST
    # only with a data option, so a bare `-X $m` listing is not judged.
    function Test-ApiPost($Options) {
        $explicit = $null
        foreach ($o in $Options) {
            if (@('-X', '--method') -ccontains $o.Name -and $null -ne $o.Value) {
                $explicit = if ($o.Value.Dynamic) { $null } else { ([string]$o.Value.Value).TrimStart('=').Trim().ToUpperInvariant() }
            }
        }
        if ($explicit) { return ($explicit -eq 'POST') }
        return (Test-OptionPresent $Options $apiDataOptions)
    }

    # The -f/--raw-field (literal) and -F/--field (typed: @file reads a file, @- stdin) fields of a `gh api`
    # call, in order, as Key/Value/Dynamic/Typed.
    function Get-ApiFields($Options) {
        $fields = [System.Collections.Generic.List[object]]::new()
        foreach ($o in $Options) {
            if ($null -eq $o.Value) { continue }
            $typed = @('-F', '--field') -ccontains $o.Name
            if (-not ($typed -or @('-f', '--raw-field') -ccontains $o.Name)) { continue }
            $text = [string]$o.Value.Value
            if ($text.StartsWith('=')) { $text = $text.Substring(1) }
            $eq = $text.IndexOf('=')
            if ($eq -lt 1) { continue }
            $fields.Add([pscustomobject]@{ Key = $text.Substring(0, $eq); Value = $text.Substring($eq + 1); Dynamic = [bool]$o.Value.Dynamic; Typed = $typed })
        }
        return , $fields
    }

    # The text a statement pipes into gh when it is one literal (`'<json>' | gh api ...`, `Write-Output '<text>' |
    # gh api ...`); $null otherwise.
    function Get-PipedLiteral($Tokens) {
        $src = $Tokens[0].PipeSource
        if ($null -eq $src -or $src.Count -eq 0) { return $null }
        if ($src.Count -eq 1 -and $src[0].Literal) { return [string]$src[0].Value }
        if ($src.Count -eq 2 -and @('echo', 'write-output') -contains ([string]$src[0].Value).ToLowerInvariant() -and $src[1].Literal) { return [string]$src[1].Value }
        return $null
    }

    function New-Resolved([bool]$Ok, [string]$Text, [string]$Path, [string]$Why) {
        [pscustomobject]@{ Ok = $Ok; Text = $Text; Path = $Path; Why = $Why }
    }

    # The text of one field: its literal, or for a typed @file the file's content (@- the piped literal).
    function Resolve-ApiFieldValue($Field, [string]$Cwd, $PipeText) {
        if ($Field.Dynamic) { return New-Resolved $false $null $null 'its value is a variable or a subexpression' }
        if ($Field.Typed -and $Field.Value.StartsWith('@')) {
            $p = $Field.Value.Substring(1)
            if ($p -eq '-') {
                if ($null -ne $PipeText) { return New-Resolved $true $PipeText $null $null }
                return New-Resolved $false $null $null 'it reads stdin (@-) and the piped text is not one literal'
            }
            $path = Resolve-CommandPath $p $Cwd
            if (-not $path) { return New-Resolved $false $null $null "the file '$p' does not exist or is a variable path (create it in an earlier call)" }
            try { return New-Resolved $true ([IO.File]::ReadAllText($path)) $path $null }
            catch { return New-Resolved $false $null $null "the file '$p' cannot be read" }
        }
        return New-Resolved $true $Field.Value $null $null
    }

    # The JSON object of `--input <file>` (the last one counts; `-` is the piped literal): Ok/Json/Path/Why.
    function Read-ApiInput($Options, [string]$Cwd, $PipeText) {
        $inputValues = Get-OptionValues $Options @('--input')
        if ($inputValues.Count -eq 0) { return $null }
        $t = $inputValues[-1]
        if ($t.Dynamic) { return [pscustomobject]@{ Ok = $false; Json = $null; Path = $null; Why = 'the --input value is a variable or a subexpression' } }
        $path = $null
        if ([string]$t.Value -eq '-') {
            if ($null -eq $PipeText) { return [pscustomobject]@{ Ok = $false; Json = $null; Path = $null; Why = '--input - reads stdin and the piped text is not one literal' } }
            $text = $PipeText
        }
        else {
            $path = Resolve-CommandPath ([string]$t.Value) $Cwd
            if (-not $path) { return [pscustomobject]@{ Ok = $false; Json = $null; Path = $null; Why = "the --input file '$($t.Value)' does not exist (create it in an earlier call)" } }
            try { $text = [IO.File]::ReadAllText($path) }
            catch { return [pscustomobject]@{ Ok = $false; Json = $null; Path = $null; Why = "the --input file '$($t.Value)' cannot be read" } }
        }
        try { $json = $text | ConvertFrom-Json -ErrorAction Stop }
        catch { return [pscustomobject]@{ Ok = $false; Json = $null; Path = $null; Why = 'the --input JSON is malformed' } }
        if ($json -isnot [pscustomobject]) { return [pscustomobject]@{ Ok = $false; Json = $null; Path = $null; Why = 'the --input JSON is not an object' } }
        return [pscustomobject]@{ Ok = $true; Json = $json; Path = $path; Why = $null }
    }

    function Get-JsonString($Json, [string]$Name) {
        $p = $Json.PSObject.Properties[$Name]
        if ($null -ne $p -and $p.Value -is [string]) { return [string]$p.Value }
        return $null
    }

    # What a REST issue creation carries: Title/Body (null when unreadable, with TitleWhy/BodyWhy), BodyPath (the
    # file behind a file-backed body), Labels (one entry per labels[] value, NOT comma-split), Milestone.
    function Get-RestCreation($Options, [string]$Cwd, $PipeText) {
        $c = [pscustomobject]@{ Title = $null; TitleWhy = $null; Body = $null; BodyWhy = $null; BodyPath = $null; Labels = [System.Collections.Generic.List[string]]::new(); LabelsDynamic = $false; MilestonePresent = $false; MilestoneDynamic = $false; Milestone = $null }
        $inputInfo = Read-ApiInput $Options $Cwd $PipeText
        if ($null -ne $inputInfo) {
            # With --input the -f/-F fields go to the query string: title, body, labels and milestone are the JSON's.
            if (-not $inputInfo.Ok) { $c.TitleWhy = $inputInfo.Why; $c.BodyWhy = $inputInfo.Why; return $c }
            $j = $inputInfo.Json
            $c.Title = Get-JsonString $j 'title'; if ($null -eq $c.Title) { $c.TitleWhy = 'the --input JSON has no string "title"' }
            $c.Body = Get-JsonString $j 'body'; if ($null -eq $c.Body) { $c.BodyWhy = 'the --input JSON has no string "body"' } else { $c.BodyPath = $inputInfo.Path }
            $lp = $j.PSObject.Properties['labels']
            if ($null -ne $lp -and $null -ne $lp.Value) {
                foreach ($l in @($lp.Value)) {
                    if ($l -is [string]) { $c.Labels.Add($l) }
                    elseif ($null -ne $l.PSObject.Properties['name']) { $c.Labels.Add([string]$l.name) }
                }
            }
            $mp = $j.PSObject.Properties['milestone']
            if ($null -ne $mp -and $null -ne $mp.Value) { $c.MilestonePresent = $true; $c.Milestone = [string]$mp.Value }
            return $c
        }

        $title = $null; $body = $null; $milestone = $null
        foreach ($f in (Get-ApiFields $Options)) {
            switch -CaseSensitive ($f.Key) {
                'title' { $title = $f }
                'body' { $body = $f }
                'milestone' { $milestone = $f }
                'labels[]' {
                    if ($f.Dynamic) { $c.LabelsDynamic = $true }
                    else { $r = Resolve-ApiFieldValue $f $Cwd $PipeText; if ($r.Ok) { $c.Labels.Add($r.Text) } else { $c.LabelsDynamic = $true } }
                }
            }
        }
        if ($null -eq $title) { $c.TitleWhy = 'there is no title= field' }
        else { $r = Resolve-ApiFieldValue $title $Cwd $PipeText; if ($r.Ok) { $c.Title = $r.Text } else { $c.TitleWhy = $r.Why } }
        if ($null -eq $body) { $c.BodyWhy = 'there is no body= field' }
        else { $r = Resolve-ApiFieldValue $body $Cwd $PipeText; if ($r.Ok) { $c.Body = $r.Text; $c.BodyPath = $r.Path } else { $c.BodyWhy = $r.Why } }
        if ($null -ne $milestone) {
            $c.MilestonePresent = $true
            if ($milestone.Dynamic) { $c.MilestoneDynamic = $true } else { $c.Milestone = $milestone.Value }
        }
        return $c
    }

    # The missing metadata of a REST creation, like Get-MissingMetadata but with a milestone NUMBER and labels[].
    function Get-MissingRestMetadata($Creation, [string]$Prefix, [string]$DefaultLabel, [string]$Root) {
        $missing = [System.Collections.Generic.List[string]]::new()
        if (-not $Creation.MilestonePresent) { $missing.Add("-F milestone=<existing open milestone number> (the REST route sends the number, not the title)") }
        elseif (-not $Creation.MilestoneDynamic) {
            $wanted = [string]$Creation.Milestone
            if ($wanted -notmatch '^\d+$') { $missing.Add("milestone '$wanted' is not a number (the REST route takes the milestone NUMBER: -F milestone=<number>)") }
            else {
                $records = Get-MilestoneRecords $Root
                if ($null -eq $records) { $missing.Add("milestone $wanted could not be verified (the milestone lookup failed: run gh auth status, or seed artifacts/issue-hygiene/milestones-v2.txt)") }
                else {
                    $found = $records | Where-Object { $_.Number -eq [int]$wanted } | Select-Object -First 1
                    if ($null -eq $found) { $missing.Add("milestone number $wanted is not an existing milestone (gh api repos/dlrivada/Encina/milestones; a milestone created in the last 12 hours needs artifacts/issue-hygiene/milestones-v2.txt deleted to refresh the cache)") }
                    elseif ($found.State -ne 'open') { $missing.Add("milestone number $wanted is closed; pick an open milestone") }
                }
            }
        }
        if (-not $Creation.LabelsDynamic) {
            $labels = @($Creation.Labels | ForEach-Object { $_.Trim() } | Where-Object { $_ })
            foreach ($item in (Get-MissingLabelItems $labels $Prefix $DefaultLabel $true)) { $missing.Add($item) }
        }
        return , $missing
    }

    # Judges one REST issue creation like `gh issue create` (#2823): unreadable title or body denies, then the
    # metadata, the local-draft evidence of a file-backed body, the needs-decision Dilemma and the template.
    # Returns the message of the first violation, or $null.
    function Get-RestCreationViolation($Creation, [string]$Root) {
        $unread = @()
        if ($null -eq $Creation.Title) { $unread += "the title ($($Creation.TitleWhy))" }
        if ($null -eq $Creation.Body) { $unread += "the body ($($Creation.BodyWhy))" }
        if ($unread.Count -gt 0) {
            return "Blocked: a REST issue creation (gh api repos/dlrivada/Encina/issues) is judged like gh issue create and fails closed when it cannot be read (#2823). Could not read: $($unread -join ' | '). Type the title literally in single quotes (-f 'title=[DEBT] ...'), pass the body as -F body=@<file> with the file created in an earlier call (or put title, body, labels and milestone in --input <file>.json); a variable, a missing file or stdin cannot be checked."
        }

        $prefix = [regex]::Match($Creation.Title, '^\[[A-Z]+\]').Value
        $defaultLabel = ''
        if ($prefix) { $templates = Get-Templates $Root; if ($templates.ContainsKey($prefix)) { $defaultLabel = $templates[$prefix].DefaultLabel } }
        $missingMeta = Get-MissingRestMetadata $Creation $prefix $defaultLabel $Root
        if ($missingMeta.Count -gt 0) {
            return "Blocked: an issue is opened complete or not at all (#1926), also over REST (#2823). Missing: $($missingMeta -join ' | '). Add them to the gh api call (-f 'labels[]=<label>' once per label, -F milestone=<number>); the open-issue skill has the full checklist (project add, parent, blocked-by follow the create)."
        }

        if ($Creation.BodyPath -and -not (Test-LocalDraftCompliance $Creation.BodyPath $Creation.Body $Creation.Title $Root)) { return $RestLocalDraftMessage }

        # Labels are not comma-split on this route: a comma is part of a REST label name.
        if (($Creation.Labels -icontains 'needs-decision') -and -not (Test-HasDilemma $Creation.Body)) { return $DilemmaMessage }

        return (Get-TemplateViolation $Creation.Title $Creation.Body $Root)
    }

    # GraphQL `createIssue` (#2823): the mutation takes label and milestone node ids and the repository id, so
    # it cannot be checked by name; it is denied. $true when the query of this `gh api graphql` call (inline
    # -f/-F query=, -F query=@file, or the query of --input) contains createIssue; an unreadable query is not
    # judged, so other GraphQL operations keep working.
    function Test-GraphQlCreateIssue($Options, [string]$Cwd, $PipeText) {
        $inputInfo = Read-ApiInput $Options $Cwd $PipeText
        if ($null -ne $inputInfo) {
            if (-not $inputInfo.Ok) { return $false }
            $q = Get-JsonString $inputInfo.Json 'query'
            return ($null -ne $q -and $q.Contains('createIssue'))
        }
        $query = $null
        foreach ($f in (Get-ApiFields $Options)) { if ($f.Key -ceq 'query') { $query = $f } }
        if ($null -eq $query) { return $false }
        # An inline query is read even when it also holds a variable: its literal text still names the mutation.
        if (-not ($query.Typed -and $query.Value.StartsWith('@'))) { return $query.Value.Contains('createIssue') }
        $r = Resolve-ApiFieldValue $query $Cwd $PipeText
        return ($r.Ok -and $r.Text.Contains('createIssue'))
    }

    $GraphQlMessage = "Blocked: a GraphQL createIssue mutation through gh api graphql skips the issue checks (local-draft evidence #1410, template headers, milestone and labels #1926; #2823), and its labels and milestone are node ids that cannot be checked. Create the issue with gh issue create, or, when the GraphQL quota is exhausted, with the REST route that gets the same checks: gh api repos/dlrivada/Encina/issues -X POST -f 'title=[PREFIX] ...' -F body=@<drafted file> -f 'labels[]=<label>' -F milestone=<number>."

    foreach ($tokens in $statements) {
        $cwd = Update-WorkingDirectory $tokens $cwd

        # #2470: `gh issue edit <n> --add-label needs-decision` is judged on the body the issue will have: the
        # --body/--body-file of this call, otherwise the existing body read from GitHub.
        $editAt = Find-Invocation $tokens 'gh' @('issue', 'edit')
        if ($editAt -ge 0) {
            $eOptions = Get-CommandOptions $tokens $editAt $editValueOptions
            $eRepo = Get-OptionValues $eOptions @('-R', '--repo')
            if ($eRepo.Count -gt 0 -and ($eRepo[0].Dynamic -or $eRepo[0].Value -ne 'dlrivada/Encina')) { continue }
            if (-not (Test-HasNeedsDecision (Get-OptionValues $eOptions @('--add-label')))) { continue }

            $eBody = $null
            $eBodyFile = Get-OptionValues $eOptions @('-F', '--body-file')
            $eBodyInline = Get-OptionValues $eOptions @('-b', '--body')
            if ($eBodyFile.Count -gt 0) {
                if ($eBodyFile[0].Dynamic -or $eBodyFile[0].Value -eq '-') { continue }
                $ePath = Resolve-CommandPath $eBodyFile[0].Value $cwd
                try {
                    if (-not $ePath) { throw [IO.FileNotFoundException]::new('body file') }
                    $eBody = [IO.File]::ReadAllText($ePath)
                }
                catch {
                    [Console]::Error.WriteLine("Blocked: the --body-file '$($eBodyFile[0].Value)' cannot be read to check the needs-decision label (#2470: it needs a Dilemma entry in the body). Create the file in an earlier call, or pass the body inline.")
                    exit 2
                }
            }
            elseif ($eBodyInline.Count -gt 0) {
                if ($eBodyInline[0].Dynamic) { continue }
                $eBody = $eBodyInline[0].Value
            }
            else {
                $number = $null
                for ($k = $editAt; $k -lt $tokens.Count; $k++) {
                    if (-not $tokens[$k].Dynamic -and $tokens[$k].Value -match '^(?:.*/issues/)?(\d+)$') { $number = $Matches[1]; break }
                    if ($tokens[$k].Value.StartsWith('-') -and $editValueOptions -ccontains $tokens[$k].Value) { $k++ }
                }
                if (-not $number) {
                    [Console]::Error.WriteLine("Blocked: the issue number of this gh issue edit is a variable, so the body cannot be read to check the needs-decision label (#2470: it needs a Dilemma entry in the body). Type the issue number literally, or pass the body in the same call with --body or --body-file.")
                    exit 2
                }
                $gh =if ($env:ENCINA_ISSUE_GH) { $env:ENCINA_ISSUE_GH } else { 'gh' }
                $saved = [Console]::OutputEncoding
                try {
                    [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
                    $existing = @(& $gh api "repos/dlrivada/Encina/issues/$number" --jq .body 2>$null)
                    if ($LASTEXITCODE -ne 0) { $existing = $null }
                }
                catch { $existing = $null }
                finally { [Console]::OutputEncoding = $saved }
                if ($null -eq $existing) {
                    [Console]::Error.WriteLine("Blocked: the body of issue #$number could not be read to check the needs-decision label (#2470: it needs a 'Dilemma' in the body). Retry, or pass the body in the same call with --body-file.")
                    exit 2
                }
                $eBody = $existing -join "`n"
            }
            if (-not (Test-HasDilemma $eBody)) {
                [Console]::Error.WriteLine($DilemmaMessage)
                exit 2
            }
            continue
        }

        # #2823: `gh api` creating an issue (REST POST on the issues endpoint) gets the checks of `gh issue create`;
        # a GraphQL createIssue mutation is denied.
        $apiAt = Find-Invocation $tokens 'gh' @('api')
        if ($apiAt -ge 0) {
            $aOptions = Get-CommandOptions $tokens $apiAt $apiValueOptions
            $aHost = Get-OptionValues $aOptions @('--hostname')
            if ($aHost.Count -gt 0 -and -not $aHost[-1].Dynamic -and ([string]$aHost[-1].Value) -ine 'github.com') { continue }

            # Assigned first: the helpers return their list wrapped, which a direct pipe would not enumerate.
            $positionals = Get-ApiPositionals $tokens $apiAt
            $kinds = @($positionals | ForEach-Object { Get-ApiEndpointKind $_ } | Where-Object { $_ })
            $pipeText = Get-PipedLiteral $tokens
            if ($kinds -contains 'issues') { $judgeRest = Test-ApiPost $aOptions }
            elseif ($kinds -contains 'graphql') {
                if (Test-GraphQlCreateIssue $aOptions $cwd $pipeText) {
                    [Console]::Error.WriteLine($GraphQlMessage)
                    exit 2
                }
                continue
            }
            elseif ($kinds -contains 'dynamic') {
                # A wholly variable endpoint is judged only when it is a POST that carries a title or a body, so
                # tool shapes such as `repos/$Repo/labels` and `repos/$Repo/milestones/$n` stay allowed.
                $aFields = Get-ApiFields $aOptions
                $judgeRest = (Test-ApiPost $aOptions) -and @($aFields | Where-Object { $_.Key -ceq 'title' -or $_.Key -ceq 'body' }).Count -gt 0
                if (-not $judgeRest -and (Test-OptionPresent $aOptions @('--input'))) {
                    $aInput = Read-ApiInput $aOptions $cwd $pipeText
                    $judgeRest = (Test-ApiPost $aOptions) -and $null -ne $aInput -and $aInput.Ok -and ($null -ne $aInput.Json.PSObject.Properties['title'] -or $null -ne $aInput.Json.PSObject.Properties['body'])
                }
            }
            else { continue }
            if (-not $judgeRest) { continue }

            $restRoot = Get-RepoRoot $cwd
            try { $restViolation = Get-RestCreationViolation (Get-RestCreation $aOptions $cwd $pipeText) $restRoot }
            catch { $restViolation = "Blocked: a REST issue creation (gh api repos/dlrivada/Encina/issues) could not be judged ($($_.Exception.GetType().Name)), so it fails closed (#2823). Retry with a literal title, -F body=@<file> and -F milestone=<number>." }
            if ($restViolation) {
                [Console]::Error.WriteLine($restViolation)
                exit 2
            }
            continue
        }

        $at = Find-Invocation $tokens 'gh' @('issue', 'create')
        if ($at -lt 0) { continue }

        $options = Get-CommandOptions $tokens $at $issueValueOptions
        $repo = Get-OptionValues $options @('-R', '--repo')
        if ($repo.Count -gt 0 -and ($repo[0].Dynamic -or $repo[0].Value -ne 'dlrivada/Encina')) { continue }
        if (Test-OptionPresent $options @('-w', '--web', '-T', '--template')) { continue }

        $title = Get-OptionValues $options @('-t', '--title')
        if ($title.Count -eq 0) { continue }

        $root = Get-RepoRoot $cwd

        # #1926: the metadata check needs no body, so it runs before any skip below (a variable title or body
        # file, `--body-file -`, no body). A variable title is judged on milestone and priority only.
        $metaPrefix = ''; $metaDefaultLabel = ''
        if (-not $title[0].Dynamic) {
            $metaPrefix = [regex]::Match($title[0].Value, '^\[[A-Z]+\]').Value
            $metaTemplates = Get-Templates $root
            if ($metaPrefix -and $metaTemplates.ContainsKey($metaPrefix)) { $metaDefaultLabel = $metaTemplates[$metaPrefix].DefaultLabel }
        }
        $missingMeta = Get-MissingMetadata $options (-not $title[0].Dynamic) $metaPrefix $metaDefaultLabel $root
        if ($missingMeta.Count -gt 0) {
            $dynamicNote = if ($title[0].Dynamic) { ' The title is a variable, so the default-label and priority checks were not judged; type the title literally to have them checked.' } else { '' }
            [Console]::Error.WriteLine("Blocked: an issue is opened complete or not at all (#1926). Missing: $($missingMeta -join ' | '). Add them to the gh issue create call; the open-issue skill has the full checklist (project add, parent, blocked-by follow the create).$dynamicNote")
            exit 2
        }

        if ($title[0].Dynamic) {
            # #2470: a variable title does not hide the needs-decision check when the body is readable.
            if (Test-HasNeedsDecision (Get-OptionValues $options @('-l', '--label'))) {
                $dBody = $null
                $dFile = Get-OptionValues $options @('-F', '--body-file')
                $dInline = Get-OptionValues $options @('-b', '--body')
                if ($dFile.Count -gt 0) {
                    if (-not $dFile[0].Dynamic -and $dFile[0].Value -ne '-') {
                        $dPath = Resolve-CommandPath $dFile[0].Value $cwd
                        try {
                            if (-not $dPath) { throw [IO.FileNotFoundException]::new('body file') }
                            $dBody = [IO.File]::ReadAllText($dPath)
                        }
                        catch {
                            [Console]::Error.WriteLine("Blocked: the --body-file '$($dFile[0].Value)' cannot be read to check the needs-decision label (#2470: it needs a Dilemma entry in the body).")
                            exit 2
                        }
                    }
                }
                elseif ($dInline.Count -gt 0 -and -not $dInline[0].Dynamic) { $dBody = $dInline[0].Value }
                if ($null -ne $dBody -and -not (Test-HasDilemma $dBody)) {
                    [Console]::Error.WriteLine($DilemmaMessage)
                    exit 2
                }
            }
            continue
        }
        $titleText = $title[0].Value

        $body = $null
        $bodyFile = Get-OptionValues $options @('-F', '--body-file')
        $bodyInline = Get-OptionValues $options @('-b', '--body')
        if ($bodyFile.Count -gt 0) {
            if ($bodyFile[0].Dynamic -or $bodyFile[0].Value -eq '-') { continue }
            $path = Resolve-CommandPath $bodyFile[0].Value $cwd
            if (-not $path) {
                if (Test-HasNeedsDecision (Get-OptionValues $options @('-l', '--label'))) {
                    [Console]::Error.WriteLine("Blocked: the --body-file '$($bodyFile[0].Value)' cannot be read to check the needs-decision label (#2470: it needs a Dilemma entry in the body). Create the file in an earlier call.")
                    exit 2
                }
                continue
            }
            $body = [IO.File]::ReadAllText($path)
            if (-not (Test-LocalDraftCompliance $path $body $titleText $root)) {
                [Console]::Error.WriteLine($LocalDraftMessage)
                exit 2
            }
        }
        elseif ($bodyInline.Count -gt 0) {
            $body = $bodyInline[0].Value
            if ($bodyInline[0].Dynamic -and (Get-Headers $body).Count -eq 0) { continue }
        }
        else { continue }

        # #2470: needs-decision only with a Dilemma in the body.
        if ((Test-HasNeedsDecision (Get-OptionValues $options @('-l', '--label'))) -and -not (Test-HasDilemma $body)) {
            [Console]::Error.WriteLine($DilemmaMessage)
            exit 2
        }

        $violation = Get-TemplateViolation $titleText $body $root
        if ($violation) {
            [Console]::Error.WriteLine($violation)
            exit 2
        }
    }
    exit 0
}
catch {
    exit 0
}
