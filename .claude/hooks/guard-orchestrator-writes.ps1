# PreToolUse hook (Write|Edit|NotebookEdit and Bash|PowerShell), wired in the project .claude/settings.json:
# the main session (the orchestrator) does not edit src/ or tests/, in the main checkout or in any worktree;
# it briefs an issue-worker instead (#1181). A rebase conflict in src/ or tests/ is resolved by an
# issue-worker too, briefed through the worker-brief skill.
#
# Exempt: the governed writing agents, told apart by the hook input's agent_type (issue-worker,
# mechanical-fixer, docs-writer), which carry their own frontmatter hooks. Every other caller is treated like
# the main session: the main session itself (no agent_id) and any other subagent it spawns (general-purpose,
# Plan, claude, ...), so the orchestrator cannot route an edit through an ungoverned agent. Everything outside
# src/** and tests/** stays allowed (specifications, plans, .claude, memory, scratch files, git operations
# that do not write those folders).
#
# Denied:
#   - a Write/Edit/NotebookEdit whose path resolves under <checkout>/src/ or <checkout>/tests/, where
#     <checkout> is the main checkout or a worktree under .claude\worktrees\;
#   - a shell write whose target resolves there (the write detection of _write-targets.ps1, shared with
#     block-main-checkout-writes.ps1, including Invoke-WebRequest/Invoke-RestMethod -OutFile and
#     Start-Process -RedirectStandardOutput/-RedirectStandardError), and an Expand-Archive whose destination is
#     there or is the root of a checkout;
#   - `git checkout [<rev>] -- <paths>` / `git checkout <rev> <paths>` and `git restore` (other than --staged
#     alone) whose pathspecs resolve there or to a checkout root;
#   - `git apply` / `git am` whose patch names a file there, or whose patch cannot be read (stdin, a variable);
#   - `dotnet run <file>.cs` / `dotnet run --file <file>.cs` / `pwsh`/`powershell -File <file>.ps1`: the hook
#     reads the named script's own text and denies when it both references src/ or tests/ and contains a
#     file-write API (_write-targets.ps1, Test-ScriptHasWriteApi/Test-ScriptReferencesPath), so a throwaway
#     probe script or `gh` stub outside src/ and tests/ runs freely (a relative path resolves against the
#     payload cwd or a prior Set-Location; call-operator and quoted paths count). A script that the same
#     command creates or overwrites is trusted only when every write to it is a literal in the command text: a
#     quoted or here-string -Value/-InputObject of Set-Content/Add-Content/Out-File/Tee-Object/New-Item, a
#     literal piped into them, or `'literal' > file`. Every literal such a command writes must not reference
#     src/ or tests/ or launch another script, and the old text of an existing script is judged like any
#     script text. Every other way of creating it (git or native output, a download, a copy, a file read, a
#     variable or a subexpression, a wildcard target) is denied, naming the creating statement, and so is any
#     other statement the hook cannot name while a script is created (a rename, a copy, a .NET call; #1854).
#     A script path the hook cannot resolve, a script that does not exist and is not created by the same
#     command, and one that exists but cannot be read are denied, since they cannot rule out a write there; a
#     `& $variable` launch counts as unresolvable only when the same command writes a file. The message names
#     the path tried, the raw argument and the base directory (#1181; this only partially closes the gap,
#     since it is a text heuristic, not an execution of the script).
#     Exempt: a script matching Test-ScriptIsSanctioned (_write-targets.ps1), unless the command writes it:
#     the pipeline's own tooling (.claude/hooks/tests/Test-Hooks.ps1, tools/ai/audit/*.ps1, tools/ai/*.ps1,
#     .github/scripts/*.cs), which the orchestrator runs by design and which legitimately mentions src/ or
#     tests/ in template guidance or search regexes while writing only under artifacts/ or its own temp
#     workspace (#1368, #1380).
# The commands of a `pwsh -Command` / `bash -c` wrapper are analysed like the others.
# Not seen: targets that depend on a variable, deletions, and writes by other programs (dotnet run of a script
# that is not itself a bare or `--file` .cs argument, compiled tools, ...).
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call
# (fail open).

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')
    . (Join-Path $PSScriptRoot '_repo-paths.ps1')
    . (Join-Path $PSScriptRoot '_write-targets.ps1')

    . (Join-Path $PSScriptRoot '_read-payload.ps1')
    $payload = Read-HookStdin | ConvertFrom-Json
    # agent_id/agent_type: present (agent_type names the caller) for a subagent's own tool call, absent for the
    # main session's (https://code.claude.com/docs/en/hooks.md, https://code.claude.com/docs/en/sub-agents.md).
    $governed = @('issue-worker', 'mechanical-fixer', 'docs-writer')
    if (-not [string]::IsNullOrWhiteSpace([string]$payload.agent_id) -and $governed -ccontains [string]$payload.agent_type) { exit 0 }
    $who = if ([string]::IsNullOrWhiteSpace([string]$payload.agent_id)) { 'The orchestrator' } else { "A $([string]$payload.agent_type) subagent (not a governed writing agent)" }

    $projectDir = [string]$env:CLAUDE_PROJECT_DIR
    if ([string]::IsNullOrWhiteSpace($projectDir)) { exit 0 }
    $layout = Get-RepoLayout $projectDir
    if ($null -eq $layout) { exit 0 }

    function Test-Guarded([string]$Full, [bool]$CheckoutRoot = $false) {
        if ([string]::IsNullOrEmpty($Full)) { return $false }
        $location = Get-RepoLocation $Full $layout
        if ($null -eq $location) { return $false }
        return $location.Relative -match '^(src|tests)(/|$)' -or ($CheckoutRoot -and $location.Relative -eq '')
    }

    function Write-Block([string]$What) {
        [Console]::Error.WriteLine("Blocked: $who does not edit src/ or tests/; brief an issue-worker (worker-brief skill), which also resolves rebase conflicts there. $What (#1181; .claude/agents/README.md, Delegation).")
        exit 2
    }

    # #2009: the literal string variables of a PowerShell command, for a script path such as `"$wt\probe.ps1"`.
    # A variable counts only when the command assigns it exactly once, with `=`, at its top level (not inside an
    # if, loop, try or script block), to a quoted literal without variables, before any other mention of it, and
    # no rebinding form the hook knows applies (every other command is outside the launcher allowlist below;
    # also foreach or parameter variable, [ref], ++/--, -OutVariable family, multi or member assignment). The
    # guard is an allowlist heuristic, not a proof: it cannot see inside a program the command runs. Returns the table
    # (name -> value) and the argument texts of every command in the call (a script path counts only when it is
    # a command argument itself, not text inside a string a wrapper such as `pwsh -Command '...'` runs later).
    # Any parse problem gives an empty table: the script stays unresolved and is denied as before.
    function Get-LiteralVariableTable([string]$Command) {
        $table = [pscustomobject]@{ Variables = @{}; Arguments = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal) }
        try {
            $parseErrors = $null
            $root = [System.Management.Automation.Language.Parser]::ParseInput($Command, [ref]$null, [ref]$parseErrors)
            if ($null -eq $root -or $parseErrors.Count -gt 0) { return $table }

            function Get-VariableKey($VariableAst) {
                $key = ([string]$VariableAst.VariablePath.UserPath) -replace '^(?i)(script|global|local|private):', ''
                if ($key.Contains(':') -or $key -eq '') { return $null }
                return $key.ToLowerInvariant()
            }

            # An allowlist, not a denylist of rebinding forms (Set-Item variable:, Tee-Object -Variable, a .NET
            # call, ... are endless): every command of the call must be a launcher, a directory change or an
            # output command, and no .NET method call or dot-sourcing may appear. Anything else leaves the
            # table empty and the script path unresolved.
            $commands = @($root.FindAll({ param($a) $a -is [System.Management.Automation.Language.CommandAst] }, $true))
            $allowedNames = 'pwsh', 'powershell', 'pwsh-preview', 'dotnet', 'set-location', 'cd', 'sl', 'chdir', 'push-location', 'pushd', 'pop-location', 'popd', 'write-output', 'write-host', 'echo', 'test-path'
            foreach ($c in $commands) {
                if ($c.InvocationOperator -eq [System.Management.Automation.Language.TokenKind]::Dot) { return $table }
                # A dynamic command name (`& $p`, `& "$wt\x.ps1"`) has no name: it is the launcher.
                $commandName = [string]$c.GetCommandName()
                # A script named literally (`& 'C:\x\a.ps1'`) is not exempt: it runs before the launch and could set
                # `$global:wt`. The launch itself is `& $p` or `& "$wt\x.ps1"` (no static name).
                if ($commandName -ne '' -and $commandName -notin $allowedNames) { return $table }
            }
            if (@($root.FindAll({ param($a) $a -is [System.Management.Automation.Language.InvokeMemberExpressionAst] }, $true)).Count -gt 0) { return $table }

            $occurrences = @{}
            foreach ($v in @($root.FindAll({ param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] }, $true))) {
                $key = Get-VariableKey $v
                if ($null -eq $key) { continue }
                if (-not $occurrences.ContainsKey($key)) { $occurrences[$key] = [System.Collections.Generic.List[object]]::new() }
                $occurrences[$key].Add($v)
            }

            $tainted = [System.Collections.Generic.HashSet[string]]::new()
            function Add-Taint($Ast) {
                if ($null -eq $Ast) { return }
                foreach ($v in @($Ast.FindAll({ param($a) $a -is [System.Management.Automation.Language.VariableExpressionAst] }, $true))) {
                    $k = Get-VariableKey $v
                    if ($null -ne $k) { [void]$tainted.Add($k) }
                }
            }
            $assignments = [System.Collections.Generic.List[object]]::new()
            foreach ($a in @($root.FindAll({ param($x) $x -is [System.Management.Automation.Language.AssignmentStatementAst] }, $true))) {
                $left = $a.Left
                while ($left -is [System.Management.Automation.Language.ConvertExpressionAst]) { $left = $left.Child }
                if ($left -is [System.Management.Automation.Language.VariableExpressionAst]) {
                    $key = Get-VariableKey $left
                    if ($null -eq $key) { continue }
                    $assignments.Add([pscustomobject]@{ Key = $key; Statement = $a; Variable = $left })
                }
                else { Add-Taint $a.Left }
            }
            foreach ($f in @($root.FindAll({ param($x) $x -is [System.Management.Automation.Language.ForEachStatementAst] }, $true))) { Add-Taint $f.Variable }
            foreach ($p in @($root.FindAll({ param($x) $x -is [System.Management.Automation.Language.ParameterAst] }, $true))) { Add-Taint $p.Name }
            foreach ($u in @($root.FindAll({ param($x) $x -is [System.Management.Automation.Language.UnaryExpressionAst] -and $x.TokenKind -in 'PlusPlus', 'MinusMinus', 'PostfixPlusPlus', 'PostfixMinusMinus' }, $true))) { Add-Taint $u.Child }
            foreach ($r in @($root.FindAll({ param($x) $x -is [System.Management.Automation.Language.ConvertExpressionAst] -and $x.StaticType -eq [ref] }, $true))) { Add-Taint $r.Child }
            foreach ($c in $commands) {
                # -OutVariable / -ErrorVariable / ... bind the variable named by the next argument.
                for ($i = 0; $i -lt $c.CommandElements.Count; $i++) {
                    $e = $c.CommandElements[$i]
                    if ($e -is [System.Management.Automation.Language.CommandParameterAst] -and $e.ParameterName -match '^(ov|ev|wv|iv|pv|outv|errorv|warningv|informationv|pipelinev)') {
                        if ($null -ne $e.Argument) { Add-Taint $e.Argument; [void]$tainted.Add(([string]$e.Argument.Extent.Text).Trim('''"').ToLowerInvariant()) }
                        elseif ($i + 1 -lt $c.CommandElements.Count) { [void]$tainted.Add(([string]$c.CommandElements[$i + 1].Extent.Text).Trim('''"').ToLowerInvariant()) }
                    }
                }
            }

            foreach ($group in ($assignments | Group-Object Key)) {
                if ($group.Count -ne 1 -or $tainted.Contains($group.Name)) { continue }
                $entry = $group.Group[0]
                $statement = $entry.Statement
                if ($statement.Operator -ne [System.Management.Automation.Language.TokenKind]::Equals) { continue }
                if ($statement.Parent -isnot [System.Management.Automation.Language.NamedBlockAst] -or $statement.Parent.Parent -ne $root) { continue }
                $expression = if ($statement.Right -is [System.Management.Automation.Language.CommandExpressionAst]) { $statement.Right.Expression } else { $null }
                if ($expression -isnot [System.Management.Automation.Language.StringConstantExpressionAst] -or [string]::IsNullOrEmpty($expression.Value)) { continue }
                $first = $occurrences[$group.Name] | Sort-Object { $_.Extent.StartOffset } | Select-Object -First 1
                if ($first -ne $entry.Variable) { continue }
                $table.Variables[$group.Name] = [string]$expression.Value
            }

            foreach ($c in $commands) {
                foreach ($e in $c.CommandElements) {
                    [void]$table.Arguments.Add([string]$e.Extent.Text)
                    if ($e -is [System.Management.Automation.Language.StringConstantExpressionAst] -or $e -is [System.Management.Automation.Language.ExpandableStringExpressionAst]) { [void]$table.Arguments.Add([string]$e.Value) }
                }
            }
        }
        catch { $table.Variables.Clear() }
        return $table
    }

    # The absolute path of a script launched through variables whose value is a known literal, or $null (#2009).
    function Resolve-LiteralVariablePath($Script, $Table) {
        if ($Table.Variables.Count -eq 0 -or -not $Table.Arguments.Contains([string]$Script.Raw)) { return $null }
        $sb = [System.Text.StringBuilder]::new()
        $last = 0
        $raw = [string]$Script.Raw
        foreach ($m in [regex]::Matches($raw, '\$(?:\{(?<n>\w+)\}|(?<n>\w+))(?![\w:])')) {
            $name = $m.Groups['n'].Value.ToLowerInvariant()
            if (-not $Table.Variables.ContainsKey($name)) { return $null }
            [void]$sb.Append($raw, $last, $m.Index - $last)
            [void]$sb.Append([string]$Table.Variables[$name])
            $last = $m.Index + $m.Length
        }
        if ($last -eq 0) { return $null }
        [void]$sb.Append($raw, $last, $raw.Length - $last)
        $expanded = $sb.ToString()
        $token = New-CommandToken $expanded $false $expanded.Contains('$') $expanded.Contains('$(')
        $full = Resolve-TargetPath $token $Script.Base $false
        if ([string]::IsNullOrEmpty($full)) { return $null }
        return $full
    }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $tool = [string]$payload.tool_name

    if ($tool -in 'Write', 'Edit', 'MultiEdit', 'NotebookEdit') {
        $target = [string]$payload.tool_input.file_path
        if ([string]::IsNullOrWhiteSpace($target)) { $target = [string]$payload.tool_input.notebook_path }
        $full = Get-FullPath $target $cwd
        if (Test-Guarded $full) { Write-Block "Target: $full" }
        exit 0
    }

    if ($tool -notin 'Bash', 'PowerShell') { exit 0 }
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

    $scan = Get-ShellWrites -Command $command -Bash:($tool -eq 'Bash') -Cwd $cwd
    foreach ($w in $scan.Writes) {
        if (Test-Guarded $w.Full $w.Directory) { Write-Block "Target: $($w.Full) ($($w.What))" }
    }

    foreach ($g in $scan.Git) {
        foreach ($p in $g.Paths) {
            if (Test-Guarded $p $true) { Write-Block "'git $($g.Verb)' writes $p" }
        }
        if ($g.Verb -notin 'apply', 'am') { continue }
        $location = if ($g.Dir) { Get-RepoLocation (Get-FullPath $g.Dir $null) $layout } else { $null }
        if ($null -eq $location) { continue }
        if ($g.PatchUnresolved) { Write-Block "'git $($g.Verb)' in $($g.Dir) applies a patch the hook cannot read (stdin or a variable), so it cannot tell whether it writes src/ or tests/; pass the patch file by its literal path" }
        foreach ($patch in $g.Patches) {
            $text = Get-Content -LiteralPath $patch -Raw
            foreach ($m in [regex]::Matches($text, '(?m)^(?:diff --git a/(?<a>\S+) b/(?<b>\S+)|(?:\+\+\+|---) (?:[ab]/)?(?<p>\S+))')) {
                foreach ($group in 'a', 'b', 'p') {
                    $relative = $m.Groups[$group].Value
                    if (-not $relative -or $relative -eq '/dev/null') { continue }
                    $full = Get-FullPath $relative $location.Root
                    if (Test-Guarded $full) { Write-Block "'git $($g.Verb)' applies $patch, which changes $relative" }
                }
            }
        }
    }

    # `dotnet run <file>.cs` / `pwsh -File <file>.ps1`: read the script's own text, since the analysis above
    # only sees the invoking statement, not what the launched script does (#1181).
    $variableTable = $null
    foreach ($s in $scan.Scripts) {
        # #2009: a script path through a variable the command itself assigns once, at its top level, to a literal
        # (`$wt = 'D:\x'; pwsh -File "$wt\probe.ps1"`) resolves; the resolved script goes through every check
        # below. Not when the same command writes a file whose target the hook cannot resolve (it may create the
        # script), and not for Bash. A call through a variable that is not a .ps1 stays an executable, skipped below.
        if ($null -eq $s.Full -and $tool -eq 'PowerShell' -and @($scan.Writes | Where-Object { $null -eq $_.Full -and -not $_.Directory }).Count -eq 0) {
            if ($null -eq $variableTable) { $variableTable = Get-LiteralVariableTable $command }
            $resolved = Resolve-LiteralVariablePath $s $variableTable
            if ($null -ne $resolved) {
                if ($s.Dynamic -and $resolved -notmatch '\.ps1$') { continue }
                $s.Full = $resolved
            }
        }
        # A launch through a variable (`& $x`) is usually an executable or a script block, not a script: it is
        # unresolvable only when the same command also writes a file, which could have created the script (#1854).
        if ($null -eq $s.Full -and $s.Dynamic -and @($scan.Writes | Where-Object { $_.Content }).Count -eq 0) { continue }
        if ($null -eq $s.Full) { Write-Block "runs '$($s.Raw)' ($($s.Kind)), whose script path the hook cannot resolve (tried '$($s.Raw)' against base '$($s.Base)'), so it cannot rule out writes to src/ or tests/; pass its literal path, or assign the variable to a literal earlier in the same command (once, at the top level, e.g. `$wt = 'D:\path'; pwsh -File `"`$wt\script.ps1`")" }
        # What this same command writes to the script (a script created or overwritten before it runs). The
        # hook trusts only text it can read in the command: a quoted or here-string literal written by
        # Set-Content/Add-Content/Out-File/Tee-Object/New-Item -Value, a literal piped into them, or
        # `'literal' > file`. Anything else that creates or changes the script (git or native output, a
        # download, a copy, a file read, a variable, a subexpression) is content the hook cannot see (#1854).
        $guardedTokens = @('src/', 'src\', 'tests/', 'tests\')
        $wildcard = '[*?\[]'
        # A write whose target holds a wildcard may land on the script: it counts as one, and never as a literal.
        # (an invalid wildcard pattern throws in -like: it is treated as a hit, never as a reason to allow)
        function Test-WriteHitsScript($Write, [string]$Full) {
            if (-not $Write.Full) { return $false }
            if ([string]::Equals($Write.Full, $Full, [StringComparison]::OrdinalIgnoreCase)) { return $true }
            if ($Write.Full -notmatch $wildcard) { return $false }
            try { return [bool]($Full -like $Write.Full) } catch { return $true }
        }
        $own = @($scan.Writes | Where-Object { Test-WriteHitsScript $_ $s.Full })
        $newText = $null
        if ($own.Count -gt 0) {
            $opaque = @($own | Where-Object { $null -eq $_.Literal -or $_.Full -match $wildcard })
            if ($opaque.Count -gt 0) {
                Write-Block "runs '$($s.Full)' ($($s.Kind)), which the same command creates or changes with '$($opaque[0].What)' from content that is not a quoted literal in the command (git or native output, a download, a copy, a file read, a variable or a subexpression), so it cannot rule out writes to src/ or tests/; write the script with a quoted or here-string literal (-Value, -InputObject, a literal piped in, or 'literal' > file), or create it in one call and run it in the next"
            }
            $unresolved = @($scan.Writes | Where-Object { $null -eq $_.Full -and -not $_.Directory })
            if ($unresolved.Count -gt 0) {
                Write-Block "runs '$($s.Full)' ($($s.Kind)), which the same command writes, next to '$($unresolved[0].What)' to '$($unresolved[0].Target.Value)', a target the hook cannot resolve and which may be the same script, so it cannot rule out writes to src/ or tests/; pass literal paths"
            }
            # While a script is created by this command, every other statement must be one the hook understands: a
            # program that copies, links, extracts, renames or calls a .NET method could replace the script's
            # content with text the hook never saw. Writers the hook models (Set-Content, ...) are judged above.
            $known = @('script', 'variable', 'set-content', 'add-content', 'ac', 'out-file', 'tee-object', 'tee', 'new-item', 'ni', 'mkdir', 'md',
                'set-location', 'cd', 'sl', 'chdir', 'push-location', 'pushd', 'pop-location', 'popd',
                'write-output', 'write-host', 'echo', 'get-content', 'gc', 'cat', 'type', 'test-path', 'get-childitem', 'gci', 'ls', 'dir', 'get-item', 'gi',
                'select-string', 'sls', 'remove-item', 'rm', 'del', 'ri', 'erase', 'rd', 'rmdir', 'out-null', 'select-object', 'select', 'where-object', 'where',
                'foreach-object', '%', '?', 'sort-object', 'measure-object', 'format-table', 'ft', 'format-list', 'fl', 'out-string', 'convertfrom-json', 'convertto-json',
                'get-location', 'pwd', 'join-path', 'split-path', 'resolve-path', 'start-sleep', 'pwsh', 'powershell', 'dotnet',
                'if', 'else', 'elseif', 'try', 'catch', 'finally', 'foreach', 'while', 'for', 'switch', 'exit', 'return', 'param')
            $unknown = @($scan.Executables | Where-Object { $_ -notin $known })
            if ($unknown.Count -gt 0) {
                Write-Block "runs '$($s.Full)' ($($s.Kind)), which the same command creates, next to a statement running '$($unknown[0])', a program the hook does not understand and which may copy, link, extract or rewrite files, so it cannot rule out writes to src/ or tests/; create the script in one call and run it in the next"
            }
            # Every literal the command writes anywhere is judged: a script written here may launch another one
            # also written here, and neither may name src/ or tests/ or launch a third script the hook cannot read.
            foreach ($literal in @($scan.Writes | Where-Object { $null -ne $_.Literal } | ForEach-Object { $_.Literal })) {
                if (Test-ScriptReferencesPath $literal $guardedTokens) {
                    Write-Block "runs '$($s.Full)' ($($s.Kind)), and the same command writes a literal that references src/ or tests/ (a script or a file), so it cannot rule out writes there; create the script in one call and run it in the next"
                }
                if ($literal -match '(?i)\.(ps1|psm1)\b|(^|[;&|(\s])[&.]\s*[''"`$\w.\\/:~-]|\s-File\s|\bdotnet\s+run\b|\b(Invoke-Expression|iex|Import-Module|Start-Process|Invoke-Command|icm)\b') {
                    Write-Block "runs '$($s.Full)' ($($s.Kind)), whose literal content written by the same command launches or imports another script, which the hook cannot analyse; create the script in one call and run it in the next"
                }
            }
            $newText = (@($own | ForEach-Object { $_.Literal }) -join "`n")
        }

        $text = $null
        $readError = $null
        try { $text = Get-Content -LiteralPath $s.Full -Raw -ErrorAction Stop } catch { $readError = $_.Exception.GetType().Name }
        if ($null -eq $text -and $null -eq $readError) { $text = '' }
        $exists = Test-Path -LiteralPath $s.Full -PathType Leaf
        if ($null -eq $text -and -not $exists) {
            if ($null -eq $newText) {
                Write-Block "runs '$($s.Full)' ($($s.Kind)), which does not exist (resolved from '$($s.Raw)' against base '$($s.Base)'), so it cannot rule out writes to src/ or tests/"
            }
            $text = ''
        }
        if ($null -eq $text) { Write-Block "runs '$($s.Full)' ($($s.Kind)), which the hook could not read ($readError), so it cannot rule out writes to src/ or tests/" }

        # The script's own text and, when the command writes it, the literal the command puts there: both run (the
        # command may run the old text before it rewrites it, or append to it), so each is judged; a script the
        # command writes is never sanctioned, since the sanction covers the repository's own file, not new text.
        $scriptLocation = Get-RepoLocation $s.Full $layout
        $sanctioned = $own.Count -eq 0 -and $null -ne $scriptLocation -and (Test-ScriptIsSanctioned $s.Full $scriptLocation.Root)
        # An append leaves the old text in place, so old and new are judged as one script too (two harmless halves
        # can write src/ together).
        $candidates = [System.Collections.Generic.List[string]]::new()
        if (-not $sanctioned) { $candidates.Add($text) }
        if (@($own | Where-Object { $_.Append }).Count -gt 0 -and $null -ne $newText) { $candidates.Add($text + "`n" + $newText) }
        foreach ($candidate in $candidates) {
            if ((Test-ScriptHasWriteApi $candidate) -and (Test-ScriptReferencesPath $candidate $guardedTokens)) {
                Write-Block "runs '$($s.Full)' ($($s.Kind)), which references src/ or tests/ and writes files$(if ($own.Count -gt 0) { ' (the text the same command writes into it is judged too)' })"
            }
        }
    }
    exit 0
}
catch {
    exit 0
}
