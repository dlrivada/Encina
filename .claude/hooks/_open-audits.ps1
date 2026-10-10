# Shared by audit-stage-guard.ps1 and enforce-path-ownership.ps1 (#2234): the open SPEC-003 audits and the audit
# a stage agent was spawned for. Several audits may be open at once (tools/ai/audit/pipeline.json
# maxParallelAudits); each one is identified by its worktree wia-<n> and has its own state file
# artifacts/knowledge/open-audits/<n>.json in the MAIN checkout. The hooks depend on nothing under tools/, so the
# reading is inlined here rather than taken from tools/ai/audit/_audit-lib.ps1, which writes the same files.
#
# Migration: before #2234 the single open audit lived in artifacts/knowledge/current-audit.json. The scripts
# convert that file into open-audits/<n>.json the first time they run (Convert-LegacyCurrentAudit in
# _audit-lib.ps1); until then the hooks read it as one more open audit, so the audit that was open when #2234
# landed keeps working without a script run first.

# Every open audit of $MainRoot: @{ Audits = <list of state objects, each with an 'issue' string>; Unreadable =
# <list of file names that exist but cannot be read> }. A caller that must decide ownership fails closed on any
# unreadable file (AGENTS.md section 3).
function Get-HookOpenAudits([string]$MainRoot) {
    $audits = [System.Collections.Generic.List[object]]::new()
    $unreadable = [System.Collections.Generic.List[string]]::new()
    $seen = [System.Collections.Generic.HashSet[string]]::new()
    $dir = Join-Path $MainRoot 'artifacts\knowledge\open-audits'
    if (Test-Path -LiteralPath $dir -PathType Container) {
        foreach ($file in @(Get-ChildItem -LiteralPath $dir -Filter '*.json' -File -ErrorAction Stop | Sort-Object Name)) {
            try {
                $state = Get-Content -LiteralPath $file.FullName -Raw | ConvertFrom-Json -ErrorAction Stop
                $issue = [string]$state.issue
                if ($issue -notmatch '^\d+$' -or "$issue.json" -ne $file.Name -or [string]::IsNullOrWhiteSpace([string]$state.worktree)) { $unreadable.Add($file.Name); continue }
                if ($seen.Add($issue)) { $audits.Add($state) }
            }
            catch { $unreadable.Add($file.Name) }
        }
    }
    $legacy = Join-Path $MainRoot 'artifacts\knowledge\current-audit.json'
    if (Test-Path -LiteralPath $legacy) {
        try {
            $state = Get-Content -LiteralPath $legacy -Raw | ConvertFrom-Json -ErrorAction Stop
            $issue = [string]$state.issue
            if ($issue -notmatch '^\d+$' -or [string]::IsNullOrWhiteSpace([string]$state.worktree)) { $unreadable.Add('current-audit.json') }
            elseif ($seen.Add($issue)) { $audits.Add($state) }
        }
        catch { $unreadable.Add('current-audit.json') }
    }
    return [pscustomobject]@{ Audits = $audits; Unreadable = $unreadable }
}

# The open audit whose worktree is $Root (full path, case-insensitive), or $null.
function Get-HookAuditByWorktree($Audits, [string]$Root) {
    if ([string]::IsNullOrWhiteSpace($Root)) { return $null }
    $want = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
    foreach ($a in @($Audits)) {
        try { $have = [IO.Path]::GetFullPath([string]$a.worktree).TrimEnd('\', '/') } catch { continue }
        if ($have -ieq $want) { return $a }
    }
    return $null
}

# The open audit numbered $Issue, or $null.
function Get-HookAuditByIssue($Audits, [string]$Issue) {
    foreach ($a in @($Audits)) { if ([string]$a.issue -eq $Issue) { return $a } }
    return $null
}

# The distinct audit worktree numbers (wia-<n>) named in a text, in order of first appearance.
function Get-WiaNumbers([string]$Text) {
    $numbers = [System.Collections.Generic.List[string]]::new()
    foreach ($m in [regex]::Matches([string]$Text, '(?i)wia-(?<n>\d+)')) {
        $value = $m.Groups['n'].Value
        if (-not $numbers.Contains($value)) { $numbers.Add($value) }
    }
    return , $numbers
}

# The audit worktree numbers the spawn prompt of the subagent behind $Payload names: the text of the FIRST user
# message of the subagent's own transcript (the prompt the orchestrator passed to the Agent tool, which
# audit-stage-guard.ps1 already required to name exactly one wia-<n>). The transcript is agent_transcript_path,
# else <dir of transcript_path>\<session>\subagents\agent-<agent_id>.jsonl, else
# <dir of transcript_path>\subagents\agent-<agent_id>.jsonl (the same lookup require-specialists.ps1 uses).
# Returns $null when the payload names no subagent or its transcript cannot be found or read.
function Get-SpawnPromptWiaNumbers($Payload) {
    $candidates = [System.Collections.Generic.List[string]]::new()
    if ($Payload.agent_transcript_path) { $candidates.Add([string]$Payload.agent_transcript_path) }
    if ($Payload.transcript_path -and $Payload.agent_id) {
        $parent = [string]$Payload.transcript_path
        $dir = Split-Path -Parent $parent
        $session = [IO.Path]::GetFileNameWithoutExtension($parent)
        $candidates.Add((Join-Path $dir "$session\subagents\agent-$($Payload.agent_id).jsonl"))
        $candidates.Add((Join-Path $dir "subagents\agent-$($Payload.agent_id).jsonl"))
    }
    $transcript = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if (-not $transcript) { return $null }
    try {
        foreach ($line in [IO.File]::ReadLines($transcript)) {
            if (-not $line.Contains('"type":"user"')) { continue }
            try { $entry = $line | ConvertFrom-Json -Depth 64 } catch { continue }
            if ($entry.type -ne 'user') { continue }
            $content = $entry.message.content
            $text = if ($content -is [string]) { $content } else { (@($content) | Where-Object { $_ -isnot [string] -and $_.type -eq 'text' } | ForEach-Object { $_.text }) -join "`n" }
            if ([string]::IsNullOrWhiteSpace($text)) { continue }
            # The comma keeps an empty or one-element list a list for the caller (no pipeline unrolling).
            return , (Get-WiaNumbers $text)
        }
    }
    catch { return $null }
    return $null
}
