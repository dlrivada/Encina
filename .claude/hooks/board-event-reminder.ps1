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
        # Only a statement that RUNS the tool counts, not a mention inside another command's arguments.
        . (Join-Path $PSScriptRoot '_command-text.ps1')
        foreach ($tokens in (Split-CommandStatements -Text $command -Bash:($tool -eq 'Bash'))) {
            $at = Find-Invocation $tokens 'gh' @('pr', 'create|merge')
            if ($at -ge 0) { $event = "gh pr $($tokens[$at - 1].Value)"; break }
            # The script is the executable (direct, & or . call) or the -File argument of pwsh/powershell.
            $k = Resolve-Executable $tokens
            if ($k -lt 0) { continue }
            $candidate = $tokens[$k].Value
            if ((Get-ExecutableName $candidate) -in 'pwsh', 'powershell', 'pwsh-preview') {
                $candidate = $null
                for ($j = $k + 1; $j -lt $tokens.Count - 1; $j++) {
                    if (-not $tokens[$j].Quoted -and $tokens[$j].Value -match '^-f(i(l(e)?)?)?$') { $candidate = $tokens[$j + 1].Value; break }
                }
            }
            if ($candidate -and $candidate -match '(?:^|[\\/])(?<script>audit-done|audit-commit-stage)\.ps1$') { $event = "$($Matches['script']).ps1"; break }
        }
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
