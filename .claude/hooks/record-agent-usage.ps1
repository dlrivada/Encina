# SubagentStop hook (settings.json, no matcher so every subagent of every session and worktree is covered, #2734):
# appends one row to the main checkout's artifacts/agent-usage/ledger.csv when a subagent stops, so recording no
# longer depends on the agent or the lane session remembering to do it by hand.
#
# Row: timestampUtc,agent,task,model,subagentTokens,notes
#   agent           the hook input's agent_type
#   task            issue-<n> from the first "Issue #<n>" in the agent's transcript (the brief's first line); else the
#                   worktree folder name of the input's cwd; else session-<first 8 of session_id>
#   model           the model of the agent's last assistant message
#   subagentTokens  the figure of the completion notice: input + cache creation + cache read + output tokens of the
#                   agent's LAST assistant message (verified against a notice: 51571 = 2 + 3981 + 46584 + 1004)
#   notes           "hook-recorded agent-<agent_id>"
# The ledger is the main checkout's (CLAUDE_PROJECT_DIR is a worktree in a lane session: the layout resolves its
# main checkout), or $env:ENCINA_AGENT_USAGE_LEDGER when set (tests). A stop whose agent_id and token figure are
# already in the ledger is not recorded twice. The hook never blocks and never fails the stop: any problem exits 0.

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_repo-paths.ps1')
    . (Join-Path $PSScriptRoot '_read-payload.ps1')
    $payload = Read-HookStdin | ConvertFrom-Json
    $agentId = [string]$payload.agent_id
    $agentType = [string]$payload.agent_type
    if ([string]::IsNullOrWhiteSpace($agentId) -or [string]::IsNullOrWhiteSpace($agentType)) { exit 0 }

    $candidates = [System.Collections.Generic.List[string]]::new()
    if ($payload.agent_transcript_path) { $candidates.Add([string]$payload.agent_transcript_path) }
    if ($payload.transcript_path) {
        $parent = [string]$payload.transcript_path
        $dir = Split-Path -Parent $parent
        $session = [IO.Path]::GetFileNameWithoutExtension($parent)
        $candidates.Add((Join-Path $dir "$session\subagents\agent-$agentId.jsonl"))
        $candidates.Add((Join-Path $dir "subagents\agent-$agentId.jsonl"))
    }
    $transcript = $candidates | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf } | Select-Object -First 1
    if (-not $transcript) { exit 0 }

    $ledger = $env:ENCINA_AGENT_USAGE_LEDGER
    if ([string]::IsNullOrWhiteSpace($ledger)) {
        $layout = if ($env:CLAUDE_PROJECT_DIR) { Get-RepoLayout $env:CLAUDE_PROJECT_DIR } else { $null }
        if ($null -eq $layout) { exit 0 }
        $ledger = Join-Path $layout.MainRoot 'artifacts\agent-usage\ledger.csv'
    }

    $task = $null
    $model = ''
    $tokens = $null
    $firstUserSeen = $false
    foreach ($line in [IO.File]::ReadLines($transcript)) {
        # The task comes from the brief only (the first user line), never from a later tool result.
        if (-not $firstUserSeen -and $line.Contains('"type":"user"')) {
            $firstUserSeen = $true
            $m = [regex]::Match($line, '(?i)issue #(?<n>\d+)')
            if ($m.Success) { $task = "issue-$($m.Groups['n'].Value)" }
        }
        if (-not $line.Contains('"usage"') -or -not $line.Contains('"assistant"')) { continue }
        try { $entry = $line | ConvertFrom-Json -Depth 64 } catch { continue }
        $usage = $entry.message.usage
        if ($entry.message.role -ne 'assistant' -or $null -eq $usage) { continue }
        # Synthetic messages (rate limit, error) carry model <synthetic> and all-zero usage: not a real turn.
        if ([string]$entry.message.model -eq '<synthetic>') { continue }
        $turn = [long]$usage.input_tokens + [long]$usage.cache_creation_input_tokens + [long]$usage.cache_read_input_tokens + [long]$usage.output_tokens
        if ($turn -le 0) { continue }
        $tokens = $turn
        if ($entry.message.model) { $model = [string]$entry.message.model }
    }
    if ($null -eq $tokens) { exit 0 }

    if (-not $task) {
        $cwd = Get-FullPath ([string]$payload.cwd) $null
        $layout = if ($env:CLAUDE_PROJECT_DIR) { Get-RepoLayout $env:CLAUDE_PROJECT_DIR } else { $null }
        $location = if ($null -ne $cwd -and $null -ne $layout) { Get-RepoLocation $cwd $layout } else { $null }
        if ($null -ne $location -and $location.InWorktree) { $task = Split-Path -Leaf $location.Root }
        else { $task = 'session-' + ([string]$payload.session_id).Substring(0, [Math]::Min(8, ([string]$payload.session_id).Length)) }
    }

    $clean = { param([string]$v) ($v -replace '[,"\r\n]', ' ').Trim() }
    $row = '{0},{1},{2},{3},{4},"hook-recorded agent-{5}"' -f (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ'), (& $clean $agentType), (& $clean $task), (& $clean $model), $tokens, (& $clean $agentId)

    New-Item -ItemType Directory -Force (Split-Path -Parent $ledger) | Out-Null
    # Concurrent stops append to the same file: open it exclusively and retry briefly.
    for ($attempt = 0; $attempt -lt 20; $attempt++) {
        try {
            $stream = [IO.File]::Open($ledger, [IO.FileMode]::OpenOrCreate, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            try {
                $reader = [IO.StreamReader]::new($stream, [Text.UTF8Encoding]::new($false), $true, 1024, $true)
                $existing = $reader.ReadToEnd()
                $writer = [IO.StreamWriter]::new($stream, [Text.UTF8Encoding]::new($false), 1024, $true)
                $isNew = $existing.Length -eq 0
                if ($isNew) { $writer.Write("timestampUtc,agent,task,model,subagentTokens,notes`n") }
                elseif (-not $existing.EndsWith("`n")) { $writer.Write("`n") }
                $duplicate = $existing.Contains("agent-$agentId`"") -and $existing.Contains(",$tokens,`"hook-recorded agent-$agentId`"")
                if (-not $duplicate) { $writer.Write($row + "`n") }
                $writer.Flush()
            }
            finally { $stream.Dispose() }
            break
        }
        catch [System.IO.IOException] { Start-Sleep -Milliseconds 100 }
    }
    exit 0
}
catch {
    exit 0
}
