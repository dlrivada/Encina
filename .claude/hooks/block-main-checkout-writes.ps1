# PreToolUse hook (Write|Edit|NotebookEdit and Bash|PowerShell), scoped to the frontmatter of the writing
# worker agents (issue-worker, mechanical-fixer, docs-writer). Two rules (#1181):
#
# 1. Main-checkout guard. A worker writes only inside a worktree under $CLAUDE_PROJECT_DIR\.claude\worktrees\.
#    Workers wrote into the main checkout three times through relative paths in [IO.File] calls, which .NET
#    resolves against the process directory (the main checkout), not against the worktree.
#    - Write/Edit/NotebookEdit: the file_path (notebook_path) is denied when it resolves inside the main
#      checkout but outside .claude\worktrees\. Paths outside the project (for example %TEMP%) are allowed.
#    - Bash/PowerShell: the write targets that _write-targets.ps1 recognises (see the list there) are resolved
#      against the directory each statement runs in; a target inside the main checkout but outside the
#      worktrees is denied. git subcommands that change the working tree or the index (add, am, apply,
#      checkout, cherry-pick, clean, commit, merge, mv, pull, rebase, reset, restore, revert, rm, stash other
#      than list/show, switch) are denied when their directory (`git -C <dir>`, else the cwd) is the main
#      checkout. A target that depends on a variable or a subexpression cannot be resolved: the call is
#      allowed, and when it runs from the main checkout the hook adds a warning to Claude's context
#      (hookSpecificOutput.additionalContext) and to the user (systemMessage).
#    - `dotnet run <file>.cs` / `pwsh`/`powershell -File <file>.ps1`: the hook reads the named script's own
#      text and denies when it both references src/ or tests/ and contains a file-write API
#      (_write-targets.ps1, Test-ScriptHasWriteApi/Test-ScriptReferencesPath) while the statement runs from
#      the main checkout — the same #1159 vector (a relative path resolving against the process directory),
#      reached through a launched script instead of an inline command. A script path the hook cannot resolve,
#      or cannot read, is allowed for a worker (it already writes only in its own worktree by protocol; a
#      false block on every unreadable script would cost more than it catches). This only partially closes the
#      gap, since it is a text heuristic, not an execution of the script (#1181).
#    When $CLAUDE_PROJECT_DIR is itself a worktree, the main checkout is the part before \.claude\worktrees\.
#
# 2. Edit tool only for source files. Repo files with a source extension ($SourceExtensions below) are never
#    written with Set-Content, Add-Content, Out-File, Tee-Object, New-Item -Value, [IO.File] writes or
#    redirection; a PowerShell -replace corrupted six files in #1159. Denied: such a write whose target
#    resolves inside the project (main checkout or any worktree) outside an `artifacts` folder; and such a
#    write to an unresolved target when the command also uses -replace / .Replace( / [regex]::Replace and names
#    a repository source path (a file with a source extension inside the project, or a project directory
#    listed together with a source extension, as in `Get-ChildItem src -Filter *.cs | ... -replace ... |
#    Set-Content $_`). An unresolved target in a command that names no repository path is allowed.
#
# Comments are ignored. Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself
# allows the call (fail open).

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')
    . (Join-Path $PSScriptRoot '_repo-paths.ps1')
    . (Join-Path $PSScriptRoot '_write-targets.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $projectDir = [string]$env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($projectDir)) { exit 0 }
    $layout = Get-RepoLayout $projectDir
    if ($null -eq $layout) { exit 0 }

    $SourceExtensions = 'cs|csx|csproj|props|targets|sln|slnx|json|ya?ml|md|txt|sql|ps1|psm1|psd1|sh|editorconfig|xml|config|resx|razor|cshtml'
    $sourcePattern = "\.($SourceExtensions)$"

    function Write-MainCheckoutBlock([string]$What, [string]$Path) {
        [Console]::Error.WriteLine("Blocked: $What targets the main checkout ($Path), not a worktree. Workers write only inside their own worktree under $($layout.Worktrees); use the worktree's absolute path (see .claude/agents/issue-worker.md, Protocol; #1181). Relative paths in [IO.File] calls resolve against the process directory, which is usually the main checkout.")
        exit 2
    }

    function Write-SourceEditBlock([string]$Target) {
        [Console]::Error.WriteLine("Blocked: repo source files (.cs, .csproj, .props, .targets, .sln/.slnx, .json, .yml, .md, .txt such as PublicAPI files, .sql, .ps1, .sh, .xml, .config, .editorconfig, .razor, .cshtml, ...) are edited only with the Edit or Write tools, never with PowerShell -replace, Set-Content, Add-Content, Out-File, Tee-Object, [IO.File] writes or redirection ($Target). A PowerShell replace corrupted six files in #1159 (see .claude/agents/issue-worker.md, Protocol; #1181). Make the change with the Edit tool.")
        exit 2
    }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $tool = [string]$payload.tool_name

    if ($tool -in 'Write', 'Edit', 'MultiEdit', 'NotebookEdit') {
        $target = [string]$payload.tool_input.file_path
        if ([string]::IsNullOrWhiteSpace($target)) { $target = [string]$payload.tool_input.notebook_path }
        $full = Get-FullPath $target $cwd
        if (Test-MainCheckout $full $layout) { Write-MainCheckoutBlock "this $tool" $full }
        exit 0
    }

    if ($tool -notin 'Bash', 'PowerShell') { exit 0 }
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }
    $bash = $tool -eq 'Bash'

    $clean = Remove-CommandComments -Text $command -Bash:$bash
    $mentionsSource = $clean -match "\.($SourceExtensions)\b"
    $usesReplace = $clean -match '-[ci]?replace\b|\.Replace\(|::Replace\('
    $scan = Get-ShellWrites -Command $command -Bash:$bash -Cwd $cwd

    function Test-RepoFile([string]$Full) {
        return (Test-Under $Full $layout.MainRoot) -and $Full -notmatch '[\\/]artifacts([\\/]|$)'
    }

    # Does the command name a repository source path (see rule 2)?
    function Test-NamesRepoSource {
        foreach ($m in $scan.Mentions) {
            if (-not (Test-RepoFile $m.Full)) { continue }
            if ($m.Full -match $sourcePattern -and $m.Explicit) { return $true }
            if ($mentionsSource -and $m.Explicit -and [string]::IsNullOrEmpty([IO.Path]::GetExtension($m.Full))) { return $true }
        }
        return $false
    }

    foreach ($g in $scan.Git) {
        if (Test-MainCheckout $g.Dir $layout) {
            [Console]::Error.WriteLine("Blocked: 'git $($g.Verb)' would change the main checkout ($($g.Dir)), not a worktree. Run it in your own worktree with 'git -C <worktree absolute path> $($g.Verb) ...' (see .claude/agents/issue-worker.md, Protocol; #1181).")
            exit 2
        }
    }

    # `dotnet run <file>.cs` / `pwsh -File <file>.ps1`: a script path the hook cannot resolve or read is
    # allowed for a worker (see the header comment); only a script the hook can read, that writes src/ or
    # tests/, while the statement runs from the main checkout, is the #1159 vector this closes.
    foreach ($s in $scan.Scripts) {
        if ($null -eq $s.Full -or -not (Test-MainCheckout $cwd $layout)) { continue }
        $text = $null
        try { $text = Get-Content -LiteralPath $s.Full -Raw -ErrorAction Stop } catch { continue }
        if ((Test-ScriptHasWriteApi $text) -and (Test-ScriptReferencesPath $text @('src/', 'src\', 'tests/', 'tests\'))) {
            Write-MainCheckoutBlock "'$($s.Kind)' of '$($s.Full)', which references src/ or tests/ and writes files, while this command" $cwd
        }
    }

    $warnings = [System.Collections.Generic.List[string]]::new()
    foreach ($w in $scan.Writes) {
        if ($null -ne $w.Full) {
            if (Test-MainCheckout $w.Full $layout) { Write-MainCheckoutBlock 'this command writes a file that' $w.Full }
            if ($w.Content -and $w.Full -match $sourcePattern -and (Test-RepoFile $w.Full)) { Write-SourceEditBlock $w.Full }
            continue
        }
        if ($w.Content -and $usesReplace -and (Test-NamesRepoSource)) { Write-SourceEditBlock $w.Target.Value }
        $fromMain = if (-not [string]::IsNullOrEmpty($w.Base)) { Test-MainCheckout $w.Base $layout } else { Test-MainCheckout $cwd $layout }
        if ($fromMain) { $warnings.Add("$($w.What) writes to '$($w.Target.Value)', which the hook cannot resolve (it depends on a variable or a subexpression), and the command runs from the main checkout ($cwd).") }
    }

    if ($warnings.Count -gt 0) {
        $text = 'Warning (block-main-checkout-writes, #1181): ' + ($warnings -join ' ') + ' Make sure the path is absolute and inside your worktree; a relative path lands in the main checkout.'
        @{ systemMessage = $text; hookSpecificOutput = @{ hookEventName = 'PreToolUse'; additionalContext = $text } } | ConvertTo-Json -Compress -Depth 5
    }
    exit 0
}
catch {
    exit 0
}
