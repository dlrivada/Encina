# PreToolUse hook (Write|Edit|NotebookEdit), wired in the frontmatter of issue-worker and docs-writer with
# -Agent <name>: a file belongs to the specialist that owns its kind, and the other agents delegate it
# (#1181; categories in _repo-paths.ps1, Get-PathCategory).
#
#   issue-worker  may not edit documentation (docs/**/*.md except docs/plans/**, the images docs pages show, the
#                 root README.md, package READMEs and other .md under src/, CONTRIBUTING.md): spawn docs-writer.
#                 The site's code and data under docs/ (*.js, *.html, *.json, *.yml, ...) are code: the
#                 issue-worker edits them and self-reviews them with adversarial-reviewer.
#                 may not edit changelog.d/**, **/PublicAPI.*.txt, .github/coverage-manifest/**: spawn
#                 mechanical-fixer with the exact lines.
#   docs-writer   allowlist: documentation, README.md and CONTRIBUTING.md anywhere (.github/**/README.md
#                 included), changelog.d/** (fragments the brief asks for) and artifacts/** (its issue files);
#                 never .claude/**. Everything else (src/, tests/, build files, docs/ site code and data) is
#                 denied: spawn mechanical-fixer for an already-decided edit, otherwise report it.
#   others        not restricted (mechanical-fixer is the delegate).
#
# docs/plans/** stays with the issue-worker: an implementation plan is an issue-scoped working document
# produced with the implementation-plan prompt, not published documentation.
#
# The rule applies to the agent named by -Agent; when the hook input names a different agent_type (a nested
# agent that inherited the hook), the call is allowed, since that agent has its own hooks. Paths outside the
# project are allowed. Only the file tools are covered: shell writes to these paths are not (the source-edit
# rule of block-main-checkout-writes.ps1 already blocks content writes to .md/.txt/.json files).
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call.

param([string]$Agent)

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_repo-paths.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    if ([string]$payload.tool_name -notin 'Write', 'Edit', 'MultiEdit', 'NotebookEdit') { exit 0 }

    # agent_id/agent_type: present for a subagent's own tool call, absent for the main session's
    # (https://code.claude.com/docs/en/hooks.md, https://code.claude.com/docs/en/sub-agents.md). A missing
    # $caller falls back to -Agent (this frontmatter hook's own agent); a different, non-empty $caller (a
    # nested agent that inherited the hook) is a mismatch: that agent has its own hooks, so the call is let
    # through here.
    $caller = [string]$payload.agent_type
    if ([string]::IsNullOrWhiteSpace($Agent)) { $Agent = $caller }
    elseif (-not [string]::IsNullOrWhiteSpace($caller) -and $caller -ne $Agent) { exit 0 }

    $projectDir = [string]$env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($projectDir)) { exit 0 }
    $layout = Get-RepoLayout $projectDir
    if ($null -eq $layout) { exit 0 }

    $target = [string]$payload.tool_input.file_path
    if ([string]::IsNullOrWhiteSpace($target)) { $target = [string]$payload.tool_input.notebook_path }
    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $location = Get-RepoLocation (Get-FullPath $target $cwd) $layout
    if ($null -eq $location) { exit 0 }

    $relative = $location.Relative
    $category = Get-PathCategory $relative

    if ($Agent -eq 'issue-worker') {
        if ($category -eq 'docs') {
            [Console]::Error.WriteLine("Blocked: '$relative' is documentation, which docs-writer owns (#1181; .claude/agents/issue-worker.md, Delegation). Spawn docs-writer in the foreground on this worktree with the facts and decisions the page needs, and make no edits to it yourself. docs/plans/** is the only documentation an issue-worker writes.")
            exit 2
        }
        if ($category -in 'changelog', 'publicapi', 'manifest') {
            $kind = @{ changelog = 'a changelog fragment'; publicapi = 'a PublicAPI file'; manifest = 'a coverage manifest' }[$category]
            [Console]::Error.WriteLine("Blocked: '$relative' is $kind; these already-decided edits belong to mechanical-fixer (#1181; .claude/agents/issue-worker.md, Delegation). Spawn mechanical-fixer in the foreground on this worktree with the exact lines to add or change, and make no edits until it returns.")
            exit 2
        }
    }
    elseif ($Agent -eq 'docs-writer') {
        $allowed = $relative -notmatch '^\.claude(/|$)' -and (
            $category -in 'docs', 'changelog' -or
            $relative -match '(^|/)README\.md$' -or
            $relative -match '(^|/)CONTRIBUTING\.md$' -or
            $relative -match '^artifacts/')
        if (-not $allowed) {
            [Console]::Error.WriteLine("Blocked: docs-writer edits only documentation (docs/**/*.md and the images they show, READMEs, CONTRIBUTING.md), changelog fragments and its artifacts/ files; '$relative' is not one of them (#1181; .claude/agents/docs-writer.md, Delegation). Code, tests, .claude/, build files and the site's code and data under docs/ (*.js, *.html, *.json, *.yml such as docs/_config.yml) belong to others: spawn mechanical-fixer for an already-decided edit; otherwise list the change in your report for the orchestrator.")
            exit 2
        }
    }

    exit 0
}
catch {
    exit 0
}
