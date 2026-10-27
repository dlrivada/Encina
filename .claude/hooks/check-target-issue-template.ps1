# PreToolUse hook (Agent|Task, orchestrator): no issue-worker or docs-writer starts on an issue whose body does
# not follow its template (#1980; maintainer decision of 2026-10-07: a non-template issue is rewritten to its
# template when it is picked up, even months after it was opened, and nothing moves forward without that).
#
# Applies only when tool_input.subagent_type is issue-worker or docs-writer (every other agent type passes),
# except an issue-worker spawning its own docs-writer, which names no issue. The
# target issue is the "Issue #<n>" that opens the first non-empty line of the prompt (the worker-brief fixed part starts with it). The title
# and body are read with `gh api repos/dlrivada/Encina/issues/<n> --jq '{title: .title, body: .body}'` (REST, #2568; the executable can be
# overridden with ENCINA_ISSUE_GH, as in check-issue-template.ps1, so tests stub it); the template is picked by
# the title prefix and read at run time from .github/ISSUE_TEMPLATE. The body must contain every level-2 header
# of the template verbatim (case included) and in order. Closed issues are checked the same way.
#
# Second gate, size (#2900; EPIC #2845 working rule: a ready sub-issue is one worker PR). After the template
# check the hook denies (1) an issue whose title starts with [EPIC] (an EPIC is never briefed), (2) a brief
# without a line "Size: <files> files, <lines> lines", (3) a declared size over $MaxFiles files or $MaxLines
# lines or below 1 file, and (4) an issue whose "## Component Affected" (infrastructure template) or
# "## Location" (technical-debt template) section lists, in its "- **File(s)**" bullet only, more distinct
# backticked paths than $MaxFiles or than the brief declares. The hook cannot measure the real diff; the declaration is the contract and review
# (adversarial-reviewer) compares it with git diff --shortstat.
#
# Fails closed: no "Issue #<n>", a gh failure, an unknown title prefix, an unreadable template or a missing
# shared helper all deny. Exit code 2 blocks the call and shows stderr to Claude; an unreadable hook payload
# (not JSON) allows, since the agent type cannot be judged.

$ErrorActionPreference = 'Stop'

# Worker PR limits, EPIC #2845 "Child Issues" working rule ("about 15 files or fewer, about 1500 lines or fewer")
# turned into hard limits (#2900).
$MaxFiles = 15
$MaxLines = 1500

try {
    . (Join-Path $PSScriptRoot '_read-payload.ps1')
}
catch {
    [Console]::Error.WriteLine("Blocked: the spawn gate of #1980 could not load _read-payload.ps1 ($($_.Exception.Message)), so it cannot tell which agent is spawned. Restore it; the gate fails closed.")
    exit 2
}

try {
    $payload = $null
    try { $payload = Read-HookStdin | ConvertFrom-Json } catch { exit 0 }
    if ($null -eq $payload -or [string]$payload.tool_name -notin 'Agent', 'Task') { exit 0 }
    $subagent = [string]$payload.tool_input.subagent_type
    if ($subagent -notin 'issue-worker', 'docs-writer') { exit 0 }
    $prompt = [string]$payload.tool_input.prompt
    # The target issue is named on the first non-empty line of the brief ("Issue #<n>", worker-brief skill).
    $firstLine = [string]($prompt -split "`r?`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
    $found = [regex]::Match($firstLine, '^\s*Issue #(?<n>\d+)')

    # The one legitimate subagent spawn: an issue-worker handing its documentation to docs-writer. It passes
    # only while the prompt names no issue; a prompt that names one is gated like any other. Any other
    # subagent (general-purpose, Plan, ...) spawning a worker is gated like the main session.
    if ([string]$payload.agent_type -eq 'issue-worker' -and $subagent -eq 'docs-writer' -and $prompt -cnotmatch 'Issue #\d+') { exit 0 }

    if (-not $found.Success) {
        [Console]::Error.WriteLine("Blocked: the brief for a $subagent must start with 'Issue #<n>' on its first line, naming the target issue (worker-brief skill), so the template gate of #1980 can check that issue's body. Add it and spawn again.")
        exit 2
    }
    $number = $found.Groups['n'].Value

    # The templates helper is loaded only for spawns subject to the gate, so a missing helper denies those and
    # leaves every other agent type alone.
    try { . (Join-Path $PSScriptRoot '_issue-templates.ps1') }
    catch {
        [Console]::Error.WriteLine("Blocked: the spawn gate of #1980 could not load _issue-templates.ps1 ($($_.Exception.Message)), so issue #$number cannot be checked against its template. Restore it; the gate fails closed.")
        exit 2
    }

    # gh runs in a job bounded below the settings.json hook timeout (30 s): a hook that times out is a
    # non-blocking error, which would let a hung gh through. A timeout denies.
    $gh = if ($env:ENCINA_ISSUE_GH) { $env:ENCINA_ISSUE_GH } else { 'gh' }
    $limit = 15
    # Clamped to 1-25 s so the hook always ends before the 30 s hook timeout.
    if ($env:ENCINA_GH_TIMEOUT_SECONDS -match '^\d{1,3}$') { $limit = [Math]::Min(25, [Math]::Max(1, [int]$env:ENCINA_GH_TIMEOUT_SECONDS)) }
    $issue = $null
    $timedOut = $false
    $job = $null
    try {
        $job = Start-Job -ArgumentList $gh, $number -ScriptBlock {
            param($ghExe, $n)
            try { [Console]::OutputEncoding = [Text.UTF8Encoding]::new($false) } catch { }
            # REST read (#2568): the GraphQL-backed `issue view` fails when the GraphQL quota is exhausted, and this gate fails closed.
            $out = (& $ghExe api "repos/dlrivada/Encina/issues/$n" --jq '{title: .title, body: .body}' 2>$null) -join "`n"
            [pscustomobject]@{ Code = $LASTEXITCODE; Out = $out }
        }
        if (Wait-Job $job -Timeout $limit) {
            $result = Receive-Job $job -ErrorAction Stop
            if ($result.Code -eq 0 -and $result.Out) { $issue = $result.Out | ConvertFrom-Json }
        }
        else { $timedOut = $true }
    }
    catch { $issue = $null }
    finally { if ($job) { Remove-Job $job -Force -ErrorAction SilentlyContinue } }
    if ($timedOut) {
        [Console]::Error.WriteLine("Blocked: gh did not answer within $limit s reading issue #$number, so its body cannot be checked against its template (#1980; a hung gh must not let the spawn through). Run gh auth status and spawn again.")
        exit 2
    }
    if ($null -eq $issue -or $null -eq $issue.title) {
        [Console]::Error.WriteLine("Blocked: issue #$number could not be read with gh (gh api repos/dlrivada/Encina/issues/$number), so its body cannot be checked against its template (#1980). Run gh auth status and spawn again.")
        exit 2
    }

    $title = [string]$issue.title
    $prefix = [regex]::Match($title, '^\[[A-Z]+\]')
    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $root = Get-RepoRoot $cwd
    if (-not $root) { $root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot) }
    $templates = Get-Templates $root
    if ($templates.Count -eq 0) {
        [Console]::Error.WriteLine("Blocked: no issue template could be read from $root/.github/ISSUE_TEMPLATE, so issue #$number cannot be checked (#1980).")
        exit 2
    }
    if (-not $prefix.Success -or -not $templates.ContainsKey($prefix.Value)) {
        $known = ($templates.Keys | Sort-Object) -join ', '
        [Console]::Error.WriteLine("Blocked: issue #$number has the title '$title', which does not start with a template prefix ($known). Rewrite it to its template (title prefix and body) before the brief (#1980).")
        exit 2
    }

    $template = $templates[$prefix.Value]
    $bodyHeaders = Get-Headers ([string]$issue.body)
    $missing = @($template.Headers | Where-Object { $bodyHeaders -cnotcontains $_ })
    $outOfOrder = [System.Collections.Generic.List[string]]::new()
    $last = -1
    foreach ($header in $template.Headers) {
        $position = $bodyHeaders.IndexOf($header)
        if ($position -lt 0) { continue }
        if ($position -lt $last) { $outOfOrder.Add($header) } else { $last = $position }
    }

    if ($missing.Count -gt 0 -or $outOfOrder.Count -gt 0) {
        $problems = [System.Collections.Generic.List[string]]::new()
        if ($missing.Count -gt 0) { $problems.Add("missing: $($missing -join ' | ')") }
        if ($outOfOrder.Count -gt 0) { $problems.Add("out of order: $($outOfOrder -join ' | ')") }
        [Console]::Error.WriteLine("Blocked: issue #$number ($prefix) does not follow .github/ISSUE_TEMPLATE/$($template.File) -- $($problems -join '; '). Rewrite the body to the template (headers verbatim and in order, content preserved and re-checked against today's code) before the brief; no issue moves forward without it (maintainer decision 2026-10-07, #1980).")
        exit 2
    }

    # Size gate (#2900), second check of this hook. The title is already in memory, so no second gh call.
    if ($prefix.Value -eq '[EPIC]') {
        [Console]::Error.WriteLine("Blocked: issue #$number is an [EPIC] ($title); an EPIC is never briefed; brief one of its ready sub-issues (EPIC #2845 working rule, #2900).")
        exit 2
    }
    $size = [regex]::Match($prompt, '(?m)^[ \t]*Size:[ \t]*(?<files>\d+)[ \t]+files?,[ \t]*(?<lines>\d+)[ \t]+lines?\b')
    if (-not $size.Success) {
        [Console]::Error.WriteLine("Blocked: the brief for a $subagent must state its size on a line `"Size: <files> files, <lines> lines`" (worker-brief skill, section 1 'Size rule'); a ready sub-issue is one worker PR: at most $MaxFiles files and $MaxLines changed lines (#2900).")
        exit 2
    }
    $declaredFiles = [bigint]::Parse($size.Groups['files'].Value)
    $declaredLines = [bigint]::Parse($size.Groups['lines'].Value)
    if ($declaredFiles -lt 1 -or $declaredFiles -gt $MaxFiles -or $declaredLines -gt $MaxLines) {
        [Console]::Error.WriteLine("Blocked: the brief for issue #$number declares $declaredFiles files and $declaredLines lines; a worker PR is at least 1 file, at most $MaxFiles files and $MaxLines changed lines. Decompose the issue into sub-issues of one package or provider each (EPIC #2845 working rule), then brief the sub-issue (#2900).")
        exit 2
    }

    # The paths the issue itself lists tie the declaration to the issue text, so the brief alone cannot understate
    # the size. Only the "- **File(s)**" bullet of "## Component Affected" / "## Location" is read, from its first
    # line to the next top-level "- **" bullet; paths in other bullets ("Who does what", "Environment") and other
    # sections are references and are not counted. An issue without that bullet skips this check. Counted: every
    # backticked dir/.../name.ext on the bullet's first line, and the backticked path that opens a nested list item
    # (numbered or "-"/"*"); a path inside a nested item's prose, with a line suffix, spaces, a wildcard or no
    # extension is not counted (the count can undercount, it is not inflated by mentions). The bullet is matched
    # as "- **File(s)**" at column 0 (the template spelling); another spelling skips the check.
    $fileSection = [regex]::Match([string]$issue.body, '(?ms)^##[ \t]+(?:Component Affected|Location)[ \t]*\r?\n(?<s>.*?)(?=^##[ \t]|\z)')
    $fileBullet = if ($fileSection.Success) { [regex]::Match($fileSection.Groups['s'].Value, '(?ms)^-[ \t]+\*\*File\(s\)\*\*(?<b>.*?)(?=^-[ \t]+\*\*|\z)') } else { $null }
    if ($null -ne $fileBullet -and $fileBullet.Success) {
        $paths = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
        $pathToken = '`(?<p>[^`\s*<>]+/[^`\s*<>/]*\.[A-Za-z0-9]+)`'
        $bulletLines = @($fileBullet.Groups['b'].Value.Replace('\', '/') -split "`r?`n")
        foreach ($token in [regex]::Matches($bulletLines[0], $pathToken)) { [void]$paths.Add($token.Groups['p'].Value) }
        foreach ($line in ($bulletLines | Select-Object -Skip 1)) {
            $item = [regex]::Match($line, '^[ \t]+(?:[-*]|\d+\.)[ \t]+' + $pathToken)
            if ($item.Success) { [void]$paths.Add($item.Groups['p'].Value) }
        }
        if ($paths.Count -gt $MaxFiles) {
            [Console]::Error.WriteLine("Blocked: the issue lists $($paths.Count) files; decompose it (a worker PR is at most $MaxFiles files, EPIC #2845 working rule, #2900).")
            exit 2
        }
        if ($declaredFiles -lt $paths.Count) {
            [Console]::Error.WriteLine("Blocked: the brief declares $declaredFiles files but the issue lists $($paths.Count); declare the real size or decompose the issue (#2900).")
            exit 2
        }
    }
    exit 0
}
catch {
    [Console]::Error.WriteLine("Blocked: the spawn gate of #1980 failed unexpectedly ($($_.Exception.Message)); it fails closed.")
    exit 2
}
