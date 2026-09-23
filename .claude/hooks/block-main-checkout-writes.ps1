# PreToolUse hook (Write|Edit|NotebookEdit and Bash|PowerShell), scoped to the frontmatter of the writing
# worker agents (issue-worker, mechanical-fixer, docs-writer). Two rules (#1181):
#
# 1. Main-checkout guard. A worker writes only inside a worktree under $CLAUDE_PROJECT_DIR\.claude\worktrees\.
#    Workers wrote into the main checkout three times through relative paths in [IO.File] calls, which .NET
#    resolves against the process directory (the main checkout), not against the worktree.
#    - Write/Edit/NotebookEdit: the file_path (notebook_path) is denied when it resolves inside the main
#      checkout but outside .claude\worktrees\. Paths outside the project (for example %TEMP%) are allowed.
#    - Bash/PowerShell: each statement's write targets are resolved against the command's cwd (followed
#      through literal cd / Set-Location / Push-Location; a relative [IO.File] target resolves against the
#      tool call's cwd, where the process runs). Write targets: Set-Content, Add-Content, Out-File, New-Item
#      (-Path or first positional), Copy-Item / Move-Item destination, cp / mv destination and touch in Bash,
#      [IO.File]::Write*/Append*/Create* first argument, and > / >> redirections. A target inside the main
#      checkout but outside the worktrees is denied. git subcommands that change the working tree or the
#      index (add, am, apply, checkout, cherry-pick, clean, commit, merge, mv, pull, rebase, reset, restore,
#      revert, rm, stash other than list/show, switch) are denied when their directory (`git -C <dir>`, else
#      the cwd) is the main checkout. Read-only commands run anywhere; a target that depends on a variable
#      or a subexpression cannot be resolved and is allowed.
#    When $CLAUDE_PROJECT_DIR is itself a worktree, the main checkout is the part before \.claude\worktrees\.
#
# 2. Edit tool only for source files. Repo files with a source extension (.cs, .csproj, .props, .targets,
#    .json, .yml, .yaml, .md, .slnx) are never written with Set-Content, Add-Content, Out-File, [IO.File]
#    writes or redirection; a PowerShell -replace corrupted six files in #1159. Denied: such a write whose
#    literal target resolves inside the project (main checkout or any worktree) outside an `artifacts`
#    folder; and such a write to a variable target in a command that also uses -replace / .Replace( and names
#    a source extension (the `Get-ChildItem *.cs | ... -replace ... | Set-Content $_` pattern).
#
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call
# (fail open).

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $projectDir = [string]$env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($projectDir)) { exit 0 }

    $sep = [IO.Path]::DirectorySeparatorChar
    $ignoreCase = [StringComparison]::OrdinalIgnoreCase

    # Git Bash spells D:\x as /d/x.
    function ConvertTo-NativePath([string]$Path) {
        if ($IsWindows -and $Path -match '^/([A-Za-z])(/|$)') {
            $rest = if ($Path.Length -gt 3) { $Path.Substring(3) } else { '' }
            return "$($Matches[1]):\$rest"
        }
        return $Path
    }

    # Absolute, normalised path; $null when a relative path has no known base.
    function Get-FullPath([string]$Path, [string]$Base) {
        if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
        $p = ConvertTo-NativePath $Path
        if (-not [IO.Path]::IsPathRooted($p)) {
            if ([string]::IsNullOrWhiteSpace($Base)) { return $null }
            $p = Join-Path (ConvertTo-NativePath $Base) $p
        }
        return [IO.Path]::GetFullPath($p)
    }

    function Test-Under([string]$Path, [string]$Root) {
        return ($Path.TrimEnd($sep) + $sep).StartsWith($Root.TrimEnd($sep) + $sep, $ignoreCase)
    }

    $project = Get-FullPath $projectDir $null
    $marker = "$sep.claude$($sep)worktrees$sep"
    $at = $project.IndexOf($marker, $ignoreCase)
    $mainRoot = if ($at -ge 0) { $project.Substring(0, $at) } else { $project.TrimEnd($sep) }
    $worktrees = $mainRoot + $marker

    function Test-MainCheckout([string]$Path) {
        return $null -ne $Path -and (Test-Under $Path $mainRoot) -and -not (Test-Under $Path $worktrees)
    }

    function Write-MainCheckoutBlock([string]$What, [string]$Path) {
        [Console]::Error.WriteLine("Blocked: $What targets the main checkout ($Path), not a worktree. Workers write only inside their own worktree under $worktrees; use the worktree's absolute path (see .claude/agents/issue-worker.md, Protocol; #1181). Relative paths in [IO.File] calls resolve against the process directory, which is usually the main checkout.")
        exit 2
    }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $tool = [string]$payload.tool_name

    if ($tool -in 'Write', 'Edit', 'MultiEdit', 'NotebookEdit') {
        $target = [string]$payload.tool_input.file_path
        if ([string]::IsNullOrWhiteSpace($target)) { $target = [string]$payload.tool_input.notebook_path }
        $full = Get-FullPath $target $cwd
        if (Test-MainCheckout $full) { Write-MainCheckoutBlock "this $tool" $full }
        exit 0
    }

    if ($tool -notin 'Bash', 'PowerShell') { exit 0 }
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }
    $bash = $tool -eq 'Bash'

    $sourceExtension = '\.(cs|csproj|props|targets|json|ya?ml|md|slnx)$'
    $mentionsSource = $command -match '\.(cs|csproj|props|targets|json|ya?ml|md|slnx)\b'
    $usesReplace = $command -match '-[ci]?replace\b|\.Replace\('

    function Write-SourceEditBlock([string]$Target) {
        [Console]::Error.WriteLine("Blocked: repo source files (.cs, .csproj, .props, .targets, .json, .yml, .md, .slnx) are edited only with the Edit or Write tools, never with PowerShell -replace, Set-Content, Add-Content, Out-File, [IO.File] writes or redirection ($Target). A PowerShell replace corrupted six files in #1159 (see .claude/agents/issue-worker.md, Protocol; #1181). Make the change with the Edit tool.")
        exit 2
    }

    # Value of a named parameter (-Path x, -Path:x) or of the positional argument at $Position.
    $valueParameters = @('-value', '-encoding', '-itemtype', '-type', '-name', '-stream', '-filter', '-include', '-exclude', '-width', '-inputobject', '-credential', '-delimiter', '-path', '-literalpath', '-lp', '-pspath', '-filepath', '-destination', '-target', '-t')
    function Get-ArgumentToken($Tokens, [int]$From, [string[]]$Names, [int]$Position) {
        $positional = 0
        for ($i = $From; $i -lt $Tokens.Count; $i++) {
            $t = $Tokens[$i]
            if (-not $t.Quoted -and $t.Value.Length -gt 1 -and $t.Value.StartsWith('-')) {
                $name = $t.Value.ToLowerInvariant()
                $inline = $null
                $colon = $name.IndexOf(':')
                if ($colon -gt 0) { $inline = New-CommandToken $t.Value.Substring($colon + 1) $t.Quoted $t.Dynamic; $name = $name.Substring(0, $colon) }
                if ($Names -contains $name) {
                    if ($null -ne $inline) { return $inline }
                    if ($i + 1 -lt $Tokens.Count) { return $Tokens[$i + 1] }
                    return $null
                }
                if ($null -eq $inline -and $valueParameters -contains $name) { $i++ }
                continue
            }
            if ($positional -eq $Position) { return $t }
            $positional++
        }
        return $null
    }

    $contentCmdlets = @('set-content', 'add-content', 'ac', 'out-file')
    $newItemCmdlets = @('new-item', 'ni')
    $copyCmdlets = @('copy-item', 'move-item', 'cpi', 'mi', 'copy', 'move')
    $gitMutating = @('add', 'am', 'apply', 'checkout', 'cherry-pick', 'clean', 'commit', 'merge', 'mv', 'pull', 'rebase', 'reset', 'restore', 'revert', 'rm', 'stash', 'switch')

    $statements = Split-CommandStatements -Text $command -Bash:$bash
    $current = $cwd

    foreach ($tokens in $statements) {
        # cd / Set-Location / Push-Location: a variable target makes the directory unknown.
        if ($tokens.Count -ge 2 -and -not $tokens[0].Quoted -and $tokens[0].Value -in 'cd', 'Set-Location', 'sl', 'chdir', 'Push-Location', 'pushd') {
            $t = $tokens[1]
            if ($t.Value -in '-LiteralPath', '-Path') { $t = if ($tokens.Count -ge 3) { $tokens[2] } else { $null } }
            $current = if ($null -eq $t -or $t.Dynamic) { $null } else { Get-FullPath $t.Value $current }
            continue
        }

        # Each write: kind (content = text written into the file), target token, base directory.
        $writes = [System.Collections.Generic.List[object]]::new()
        $k = Resolve-Executable $tokens
        $name = if ($k -ge 0 -and -not $tokens[$k].Dynamic) { $tokens[$k].Value.ToLowerInvariant() } else { '' }

        if ($contentCmdlets -contains $name) {
            $writes.Add(@{ Content = $true; Target = (Get-ArgumentToken $tokens ($k + 1) @('-path', '-literalpath', '-lp', '-pspath', '-filepath') 0); Base = $current })
        }
        elseif ($newItemCmdlets -contains $name) {
            $writes.Add(@{ Content = $false; Target = (Get-ArgumentToken $tokens ($k + 1) @('-path', '-literalpath', '-lp', '-pspath') 0); Base = $current })
        }
        elseif ($copyCmdlets -contains $name -or (-not $bash -and $name -in 'cp', 'mv')) {
            $writes.Add(@{ Content = $false; Target = (Get-ArgumentToken $tokens ($k + 1) @('-destination') 1); Base = $current })
        }
        elseif ($bash -and $name -in 'cp', 'mv', 'touch') {
            $operands = @($tokens | Select-Object -Skip ($k + 1) | Where-Object { $_.Quoted -or -not $_.Value.StartsWith('-') })
            if ($name -eq 'touch') { foreach ($o in $operands) { $writes.Add(@{ Content = $false; Target = $o; Base = $current }) } }
            elseif ($operands.Count -ge 2) { $writes.Add(@{ Content = $false; Target = $operands[-1]; Base = $current }) }
        }
        elseif ($name -in 'git', 'git.exe') {
            $dir = $current
            $j = $k + 1
            while ($j -lt $tokens.Count -and -not $tokens[$j].Quoted -and $tokens[$j].Value.StartsWith('-')) {
                $opt = $tokens[$j].Value
                if ($opt -ceq '-C' -and $j + 1 -lt $tokens.Count) {
                    $dir = if ($tokens[$j + 1].Dynamic) { $null } else { Get-FullPath $tokens[$j + 1].Value $dir }
                    $j += 2; continue
                }
                if ($opt -ceq '-c' -or $opt -in '--git-dir', '--work-tree', '--namespace', '--exec-path') { $j += 2; continue }
                $j++
            }
            $verb = if ($j -lt $tokens.Count) { $tokens[$j].Value } else { $null }
            $readOnlyStash = $verb -eq 'stash' -and $j + 1 -lt $tokens.Count -and $tokens[$j + 1].Value -in 'list', 'show'
            if ($gitMutating -ccontains $verb -and -not $readOnlyStash -and (Test-MainCheckout $dir)) {
                [Console]::Error.WriteLine("Blocked: 'git $verb' would change the main checkout ($dir), not a worktree. Run it in your own worktree with 'git -C <worktree absolute path> $verb ...' (see .claude/agents/issue-worker.md, Protocol; #1181).")
                exit 2
            }
        }

        foreach ($t in $tokens) {
            if ($t.Quoted) { continue }
            # [IO.File]::WriteAllText('path', ...): the tokenizer keeps the call's argument text in the token.
            $io = [regex]::Match($t.Value, '^\[(System\.)?IO\.File\]::(Write|Append|Create)\w*\s*(?<arg>.*)$', 'IgnoreCase')
            if ($io.Success) {
                $arg = [regex]::Match($io.Groups['arg'].Value, '^(?<q>[''"])(?<p>[^''"]*)\k<q>')
                $literal = $arg.Success -and -not ($arg.Groups['q'].Value -eq '"' -and $arg.Groups['p'].Value.Contains('$'))
                $target = if ($literal) { New-CommandToken $arg.Groups['p'].Value $true $false } else { New-CommandToken $io.Groups['arg'].Value $false $true }
                $writes.Add(@{ Content = $true; Target = $target; Base = $cwd })
                continue
            }
            # > file, >> file, 2> file, *> file (not 2>&1, > $null, > /dev/null, > nul).
            $redirect = [regex]::Match($t.Value, '^(\d|\*)?>>?(?<t>.*)$')
            if ($redirect.Success) {
                $value = $redirect.Groups['t'].Value
                $target = if ($value) { New-CommandToken $value $false $t.Dynamic } else {
                    $index = $tokens.IndexOf($t)
                    if ($index + 1 -lt $tokens.Count) { $tokens[$index + 1] } else { $null }
                }
                if ($null -ne $target -and -not $target.Value.StartsWith('&') -and $target.Value -notin '$null', '/dev/null', 'nul') {
                    $writes.Add(@{ Content = $true; Target = $target; Base = $current })
                }
            }
        }

        foreach ($w in $writes) {
            if ($null -eq $w.Target) { continue }
            if ($w.Target.Dynamic) {
                if ($w.Content -and $usesReplace -and $mentionsSource) { Write-SourceEditBlock $w.Target.Value }
                continue
            }
            $full = Get-FullPath $w.Target.Value $w.Base
            if ($null -eq $full) { continue }
            if (Test-MainCheckout $full) { Write-MainCheckoutBlock 'this command writes a file that' $full }
            if ($w.Content -and $full -match $sourceExtension -and (Test-Under $full $mainRoot) -and $full -notmatch '[\\/]artifacts[\\/]') {
                Write-SourceEditBlock $full
            }
        }
    }
    exit 0
}
catch {
    exit 0
}
