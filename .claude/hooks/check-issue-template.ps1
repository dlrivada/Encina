# PreToolUse hook (Bash|PowerShell): validates `gh issue create` against .github/ISSUE_TEMPLATE.
#
# CLAUDE.md, "Issue Body Format (MANDATORY)": an issue title starts with its template prefix ([BUG], [DEBT],
# ...) and the body uses that template's level-2 headers verbatim (case included) and in order. The templates
# are read at run time, so the hook never drifts from them.
#
# #1410: a `--body-file` call must also show the body was drafted by the free local model
# (tools/ai/local-ai-ask.cs), never paid tokens, before the header check even runs. One of:
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
# Allowed without checks: issues on another repository (-R/--repo), --web, --template, calls without a body,
# and titles or bodies that come from a variable or a subexpression without visible headers. Headers inside
# fenced code blocks do not count. Exit code 2 blocks the call and shows stderr to Claude; any failure of the
# hook itself allows the call.

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command) -or $command -notmatch '\bgh\b') { exit 0 }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $statements = Split-CommandStatements -Text $command -Bash:($payload.tool_name -eq 'Bash')

    # The repository root: walked up from $StartDir looking for .github/ISSUE_TEMPLATE, falling back to
    # $env:CLAUDE_PROJECT_DIR. Shared by the template lookup and the #1410 local-draft evidence check below,
    # so both agree on the same root (and the same set of candidate ledger/remediation locations).
    function Get-RepoRoot([string]$StartDir) {
        $root = $StartDir
        while ($root -and -not (Test-Path -LiteralPath (Join-Path $root '.github/ISSUE_TEMPLATE'))) {
            $parent = Split-Path -Parent $root
            if (-not $parent -or $parent -eq $root) { $root = $null; break }
            $root = $parent
        }
        if (-not $root -and $env:CLAUDE_PROJECT_DIR) { $root = $env:CLAUDE_PROJECT_DIR }
        return $root
    }

    function Get-Templates([string]$Root) {
        $templates = @{}
        if (-not $Root) { return $templates }
        $dir = Join-Path $Root '.github/ISSUE_TEMPLATE'
        if (-not (Test-Path -LiteralPath $dir)) { return $templates }
        foreach ($file in Get-ChildItem -LiteralPath $dir -Filter '*.md') {
            $lines = Get-Content -LiteralPath $file.FullName
            $titleLine = $lines | Where-Object { $_ -match '^title:\s*"(\[[A-Z]+\])' } | Select-Object -First 1
            if (-not $titleLine) { continue }
            $prefix = [regex]::Match($titleLine, '\[[A-Z]+\]').Value
            $templates[$prefix] = @{ File = $file.Name; Headers = @($lines | Where-Object { $_ -cmatch '^## \S' } | ForEach-Object { $_.Trim() }) }
        }
        return $templates
    }

    function Get-Headers([string]$Body) {
        $headers = [System.Collections.Generic.List[string]]::new()
        $fence = $null
        foreach ($line in ($Body -split "`r?`n")) {
            if ($fence) {
                if ($line -match "^\s{0,3}$([regex]::Escape($fence.Char)){$($fence.Length),}\s*$") { $fence = $null }
                continue
            }
            # CommonMark: a backtick fence's info string cannot contain a backtick (that line is inline code).
            $open = [regex]::Match($line, '^\s{0,3}(?<f>`{3,}|~{3,})(?<info>.*)$')
            if ($open.Success -and $open.Groups['f'].Value[0] -eq '`' -and $open.Groups['info'].Value.Contains('`')) { $open = [System.Text.RegularExpressions.Match]::Empty }
            if ($open.Success) { $fence = @{ Char = [string]$open.Groups['f'].Value[0]; Length = $open.Groups['f'].Value.Length }; continue }
            if ($line -cmatch '^## \S') { $headers.Add($line.Trim()) }
        }
        return , $headers
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
        $ledger = Join-Path $Root 'artifacts/local-ai/ledger.csv'
        if (-not (Test-Path -LiteralPath $ledger)) { return $rows }
        $parsed = $null
        try { $parsed = Import-Csv -LiteralPath $ledger } catch { return $rows }
        foreach ($row in $parsed) {
            $ts = ConvertFrom-LedgerTimestamp ([string]$row.timestampUtc)
            if ($null -eq $ts -or $ts -lt $Since) { continue }
            $rows.Add($row)
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

    $LocalDraftMessage = "Blocked: gh issue create's --body-file must show it was drafted by the free local model (CLAUDE.md, Model routing; AGENTS.md Sec.2/local-ai-task skill) -- one of: a first line '<!-- local-draft: <path> -->' where <path> exists and a local-ai/ledger.csv row from the last 24 hours names it as outFile; the body file itself being such a recent ledger outFile; or a first line '<!-- local-draft: none, reason: <text> -->' opt-out with a non-empty reason (logged to artifacts/local-ai/opt-outs.log). See the local-ai-task skill."

    # Options of `gh issue create` that take a value, so their values are never parsed as options.
    $issueValueOptions = @('-t', '--title', '-b', '--body', '-F', '--body-file', '-R', '--repo', '-l', '--label', '-m', '--milestone', '-a', '--assignee', '-p', '--project', '-T', '--template', '--recover')

    foreach ($tokens in $statements) {
        $cwd = Update-WorkingDirectory $tokens $cwd
        $at = Find-Invocation $tokens 'gh' @('issue', 'create')
        if ($at -lt 0) { continue }

        $options = Get-CommandOptions $tokens $at $issueValueOptions
        $repo = Get-OptionValues $options @('-R', '--repo')
        if ($repo.Count -gt 0 -and ($repo[0].Dynamic -or $repo[0].Value -ne 'dlrivada/Encina')) { continue }
        if (Test-OptionPresent $options @('-w', '--web', '-T', '--template')) { continue }

        $title = Get-OptionValues $options @('-t', '--title')
        if ($title.Count -eq 0 -or $title[0].Dynamic) { continue }
        $titleText = $title[0].Value

        $root = Get-RepoRoot $cwd

        $body = $null
        $bodyFile = Get-OptionValues $options @('-F', '--body-file')
        $bodyInline = Get-OptionValues $options @('-b', '--body')
        if ($bodyFile.Count -gt 0) {
            if ($bodyFile[0].Dynamic -or $bodyFile[0].Value -eq '-') { continue }
            $path = Resolve-CommandPath $bodyFile[0].Value $cwd
            if (-not $path) { continue }
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

        $templates = Get-Templates $root
        if ($templates.Count -eq 0) { continue }

        $prefix = [regex]::Match($titleText, '^\[[A-Z]+\]')
        if (-not $prefix.Success -or -not $templates.ContainsKey($prefix.Value)) {
            $known = ($templates.Keys | Sort-Object) -join ', '
            [Console]::Error.WriteLine("Blocked: issue title '$titleText' must start with a template prefix ($known). See CLAUDE.md, Issue Templates; normalise [TECH-DEBT] to [DEBT], [TESTING] to [TEST], [ARCHITECTURE]/[DECISION]/[REVIEW] to [SPIKE].")
            exit 2
        }
        $template = $templates[$prefix.Value]
        $bodyHeaders = Get-Headers $body

        $missing = @($template.Headers | Where-Object { $bodyHeaders -cnotcontains $_ })
        if ($missing.Count -gt 0) {
            [Console]::Error.WriteLine("Blocked: a $($prefix.Value) issue uses the headers of .github/ISSUE_TEMPLATE/$($template.File) verbatim and in order (CLAUDE.md, Issue Body Format). Missing: $($missing -join ' | '). Read the template, fill every section (tick the checkboxes that apply) and retry. The open-issue skill describes the procedure.")
            exit 2
        }

        $positions = @($template.Headers | ForEach-Object { $bodyHeaders.IndexOf($_) })
        for ($i = 1; $i -lt $positions.Count; $i++) {
            if ($positions[$i] -lt $positions[$i - 1]) {
                [Console]::Error.WriteLine("Blocked: the headers of .github/ISSUE_TEMPLATE/$($template.File) must appear in template order; '$($template.Headers[$i])' comes before '$($template.Headers[$i - 1])'.")
                exit 2
            }
        }
    }
    exit 0
}
catch {
    exit 0
}
