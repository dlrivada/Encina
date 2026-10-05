# PostToolUse hook (PowerShell|Bash and Agent|Task), wired in .claude/settings.json (#1732): after an event
# that changes the private control board it reminds the orchestrator to update the work/flow/audits
# collections now, or to let the 30-minute reconciler (tools/ai/board/reconcile-board.ps1) do it.
#
# Events: a shell command that ran `gh pr create`, `gh pr merge`, `audit-done.ps1` or `audit-commit-stage.ps1`;
# an Agent spawn of issue-worker or docs-writer. The hook NEVER blocks: it always exits 0, and a malformed
# payload or any failure of the hook itself produces no output.

param()

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $tool = [string]$payload.tool_name
    $event = $null
    if ($tool -in 'Bash', 'PowerShell') {
        $command = [string]$payload.tool_input.command
        $m = [regex]::Match($command, '(?i)\bgh(?:\.exe)?\s+pr\s+(?<verb>create|merge)\b|(?<script>audit-done|audit-commit-stage)\.ps1')
        if ($m.Success) { $event = if ($m.Groups['verb'].Success) { "gh pr $($m.Groups['verb'].Value)" } else { "$($m.Groups['script'].Value).ps1" } }
    }
    elseif ($tool -in 'Agent', 'Task') {
        $agent = [string]$payload.tool_input.subagent_type
        if ($agent -in 'issue-worker', 'docs-writer') { $event = "a $agent spawn" }
    }
    if ($event) {
        $context = "Board: update work/flow/audits for $event now (or let the 30-minute reconciler do it)"
        @{ hookSpecificOutput = @{ hookEventName = 'PostToolUse'; additionalContext = $context } } | ConvertTo-Json -Compress
    }
}
catch { }
exit 0
