# Shared by block-main-checkout-writes.ps1 and guard-orchestrator-writes.ps1 (#1181): finds the files a Bash
# or PowerShell command writes, and the git commands that change a working tree, so each hook applies its
# own rule to the same analysis. Requires _command-text.ps1 and _repo-paths.ps1.
#
# Write targets, resolved against the directory the statement runs in (followed through literal cd /
# Set-Location / Push-Location / Pop-Location; a variable target makes the directory unknown):
#   Set-Content, Add-Content, Out-File, Tee-Object (-Path/-FilePath or the first positional), New-Item (-Path
#   or the first positional, joined with -Name; a -Value makes it a content write), Copy-Item / Move-Item /
#   cp / mv (the destination, with PowerShell parameter binding: `Copy-Item -Path a b` copies to b), Bash cp /
#   mv / touch / tee, and > / >> redirections.
#   [IO.File]::Write*/Append*/Create*/OpenWrite (first argument), [IO.File]::Copy/Move/Replace (second
#   argument) and [IO.StreamWriter]::new (first argument); a relative path there resolves against the tool
#   call's cwd, where the process runs, not against Set-Location.
#   Invoke-WebRequest / Invoke-RestMethod -OutFile, Start-Process -RedirectStandardOutput /
#   -RedirectStandardError (content writes), and Expand-Archive -DestinationPath (the second positional, or
#   the current directory when omitted; a Directory write, whose files are not known).
# A target that uses only $env:NAME, $HOME, $PWD (PowerShell) or $NAME (Bash, from the environment) is
# resolved; a target that depends on any other variable or on a subexpression stays unresolved (Full = $null).
# The statements of a `pwsh -Command "..."` or `bash -c '...'` wrapper are analysed in their own shell (see
# _command-text.ps1); a directory change inside the wrapper is followed as if it ran in the outer shell.
#
# Git: every git command that changes a working tree or the index (Verb, Dir), with Paths (the full paths of
# the pathspecs of `git checkout [<rev>] -- <paths>` / `git checkout <rev> <paths>` and of `git restore`
# other than --staged alone) and, for `git apply` / `git am`, Patches (the patch files) and PatchUnresolved
# (the patch comes from stdin or a path that cannot be resolved).
#
# Not seen: writes by programs that the analysis does not know (dotnet, scripts other than the `dotnet run` /
# `pwsh -File` case below), Remove-Item / rm (deletions), and redirections attached to a word (x>f).
# Rename-Item / ren / rni is a write to the renamed path (the old folder plus -NewName, content unknown).
#
# Literal and Append (#1854): a write whose content is a quoted or here-string literal in the command
# (-Value / -InputObject, a lone literal piped in, `'literal' > file`) carries it in Literal; every other
# write has Literal = $null. Tokens carry Literal (exactly one quoted segment, no variable, escape or
# unquoted text) and PipeSource (the statement a single `|` pipes in); see _command-text.ps1.
#
# Sanctioned scripts (#1368, #1380): Test-ScriptIsSanctioned/$SanctionedScriptPatterns below give the pipeline's
# own scripts (the hook test suite, tools/ai/audit/*, tools/ai/*, .github/scripts/*.cs) a narrow allowlist
# exemption from the Scripts write-API/reference heuristic, since they are the orchestrator's or a worker's own
# sanctioned tools, not an arbitrary script that happens to mention src/ or tests/.
#
# Scripts: `dotnet run <file>.cs` / `dotnet run --file <file>.cs`, `pwsh`/`powershell -File <file>.ps1`, the
# PowerShell call operator (`& '<file>.ps1'`) and dot-sourcing (`. '<file>.ps1'`) name a script the statement's
# own write-target analysis cannot see into (#1181; ADR/hooks docs note this as a bypass: `Get-WrappedCommand`
# in _command-text.ps1 does not read a `pwsh -File` script either). Each match becomes a Scripts entry (Full,
# Raw, Kind, Base); the caller reads the file's own text and decides, since only it knows which path is guarded
# (src/tests for the orchestrator, the main checkout root for a worker) and how to react when the path cannot
# be resolved or the file cannot be read. Base is the directory the launching statement runs in (followed
# through a prior `cd`/`Set-Location`, same as every other write target): a script launched after
# `Set-Location <worktree>; dotnet run --file <script>` runs with the worktree as its own process directory, so
# the caller tests Base, not the tool call's raw cwd, to decide whether the statement runs from the main
# checkout (#1345; block-main-checkout-writes.ps1 tested the tool call's cwd and so still blocked a script
# launched after a Set-Location into a worktree). A `pwsh -Command "& '<file>.ps1'"` wrapper needs no separate
# handling here: Split-CommandStatements (_command-text.ps1) already re-tokenises the wrapper's -Command text
# as its own statement, so the call-operator detection below sees it there (#1345, a blocked `pwsh -File`
# script launch replayed as `& '<path>.ps1'` and the hooks let it through — #1190).

$script:ValueParameters = @(
    '-value', '-encoding', '-itemtype', '-type', '-name', '-stream', '-filter', '-include', '-exclude', '-width',
    '-inputobject', '-credential', '-delimiter', '-path', '-literalpath', '-lp', '-pspath', '-filepath',
    '-destination', '-target', '-t', '-variable', '-outvariable', '-ov', '-errorvariable', '-ev', '-erroraction',
    '-ea', '-warningaction', '-wa', '-informationaction', '-ia', '-pipelinevariable', '-pv', '-outbuffer', '-ob')

$script:GitMutatingVerbs = @('add', 'am', 'apply', 'checkout', 'cherry-pick', 'clean', 'commit', 'merge', 'mv', 'pull', 'rebase', 'reset', 'restore', 'revert', 'rm', 'stash', 'switch')

# Binds a PowerShell cmdlet's arguments the way the engine does: named parameters first (-Name value,
# -Name:value, unambiguous prefixes of three letters or more), then the remaining positional arguments fill
# the positions that are still free, in order. $Positions lists the alias set of each position. Returns the
# token bound to position $Want, or $null.
function Get-BoundArgument($Tokens, [int]$From, [object[]]$Positions, [int]$Want) {
    $named = @{}
    $positional = [System.Collections.Generic.List[object]]::new()
    for ($i = $From; $i -lt $Tokens.Count; $i++) {
        $t = $Tokens[$i]
        if (-not $t.Quoted -and $t.Value.Length -gt 1 -and $t.Value.StartsWith('-') -and $t.Value -notmatch '^-\d') {
            $name = $t.Value.ToLowerInvariant()
            $inline = $null
            $colon = $name.IndexOf(':')
            if ($colon -gt 0) { $inline = New-CommandToken $t.Value.Substring($colon + 1) $t.Quoted $t.Dynamic $t.Subexpression; $name = $name.Substring(0, $colon) }
            $slot = -1
            for ($s = 0; $s -lt $Positions.Count -and $slot -lt 0; $s++) {
                foreach ($alias in $Positions[$s]) {
                    if ($alias -eq $name -or ($name.Length -ge 4 -and $alias.StartsWith($name))) { $slot = $s; break }
                }
            }
            if ($slot -ge 0) {
                if ($null -ne $inline) { $named[$slot] = $inline }
                elseif ($i + 1 -lt $Tokens.Count) { $named[$slot] = $Tokens[$i + 1]; $i++ }
                continue
            }
            if ($null -eq $inline -and $script:ValueParameters -contains $name) { $i++ }
            continue
        }
        $positional.Add($t)
    }
    $p = 0
    for ($s = 0; $s -lt $Positions.Count; $s++) {
        if ($named.ContainsKey($s)) { if ($s -eq $Want) { return $named[$s] }; continue }
        if ($p -lt $positional.Count) { if ($s -eq $Want) { return $positional[$p] }; $p++ }
    }
    return $null
}

# The value token of a named-only parameter (-Name value, -Name:value, or a prefix of five letters or more).
function Get-NamedArgument($Tokens, [int]$From, [string[]]$Names) {
    for ($i = $From; $i -lt $Tokens.Count; $i++) {
        $t = $Tokens[$i]
        if ($t.Quoted -or -not $t.Value.StartsWith('-')) { continue }
        $name = $t.Value.ToLowerInvariant()
        $inline = $null
        $colon = $name.IndexOf(':')
        if ($colon -gt 0) { $inline = New-CommandToken $t.Value.Substring($colon + 1) $t.Quoted $t.Dynamic $t.Subexpression $t.Bash; $name = $name.Substring(0, $colon) }
        foreach ($n in $Names) {
            if ($name -eq $n -or ($name.Length -ge 5 -and $n.StartsWith($name))) {
                if ($null -ne $inline) { return $inline }
                if ($i + 1 -lt $Tokens.Count) { return $Tokens[$i + 1] }
                return $null
            }
        }
    }
    return $null
}

function Test-NamedArgument($Tokens, [int]$From, [string[]]$Names) {
    for ($i = $From; $i -lt $Tokens.Count; $i++) {
        $t = $Tokens[$i]
        if ($t.Quoted -or -not $t.Value.StartsWith('-')) { continue }
        $name = $t.Value.ToLowerInvariant()
        $colon = $name.IndexOf(':')
        if ($colon -gt 0) { $name = $name.Substring(0, $colon) }
        if ($Names -contains $name) { return $true }
    }
    return $false
}

# Replaces the variables whose value the hook knows; $null when any other variable remains.
function Expand-KnownVariables([string]$Value, [bool]$Bash, [string]$Base) {
    $pattern = if ($Bash) { '\$(?:\{(?<n>\w+)\}|(?<n>\w+))' } else { '\$(?:\{(?<env>env:)?(?<n>\w+)\}|(?<env>env:)?(?<n>\w+))' }
    $sb = [System.Text.StringBuilder]::new()
    $last = 0
    foreach ($m in [regex]::Matches($Value, $pattern)) {
        [void]$sb.Append($Value, $last, $m.Index - $last)
        $name = $m.Groups['n'].Value
        $isEnv = $m.Groups['env'].Success
        $v = $null
        if (-not $isEnv -and $name -ieq 'HOME') { $v = Get-HomeDirectory }
        elseif (-not $isEnv -and $name -ieq 'PWD') { $v = $Base }
        elseif ($Bash -or $isEnv) { $v = [Environment]::GetEnvironmentVariable($name) }
        if ([string]::IsNullOrEmpty($v)) { return $null }
        [void]$sb.Append($v)
        $last = $m.Index + $m.Length
    }
    [void]$sb.Append($Value, $last, $Value.Length - $last)
    $out = $sb.ToString()
    if ($out.Contains('$')) { return $null }
    return $out
}

# Whether a script's own source text writes files: the file-write APIs a `dotnet run <file>.cs` /
# `pwsh -File <file>.ps1` script could use to bypass the shell-level write-target analysis (#1181).
$script:ScriptWriteApiPattern = '\bFile\.(Write\w*|AppendAll\w*|Copy|Move|Replace)\s*\(|\b(New-Object\s+)?(System\.IO\.)?(Stream|File)Writer\b|\bnew\s+(System\.IO\.)?FileStream\s*\([^)]*FileAccess\.(Write|ReadWrite)|(?im)^\s*(Set-Content|Add-Content|Out-File|Copy-Item|Move-Item)\b|\bNew-Item\b[^\r\n]*-Value\b|(?m)^[^#\r\n]*[^><]>>?(?!=|&)\S'

# Whether a script's own source text mentions any of the given guarded-path tokens (case-insensitive literal
# match; a crude but conservative signal, since the script is not tokenised the way a shell statement is).
function Test-ScriptReferencesPath([string]$Text, [string[]]$Tokens) {
    foreach ($t in $Tokens) { if ($Text.IndexOf($t, [StringComparison]::OrdinalIgnoreCase) -ge 0) { return $true } }
    return $false
}

function Test-ScriptHasWriteApi([string]$Text) {
    return $Text -match $script:ScriptWriteApiPattern
}

# #2892: judges where a PowerShell script writes instead of what its text mentions. Parses $Text and finds every
# write site (Set-Content, Add-Content, Out-File, New-Item, Copy-Item, ... with their path parameters, `>`
# redirections, [IO.File]:: and StreamWriter/FileStream calls), resolves each target against the literal
# variables, Join-Path, string concatenation and `foreach` over literal arrays of the script (and the literal
# arguments of the calls of its own functions), and answers:
#   Guarded  a target resolves under a checkout's src/ or tests/ (or is a relative path with a src or tests segment);
#   Safe     every write site was understood, every target resolved outside src/ and tests/, and no write API is
#            left in the text once strings, comments and the understood sites are removed;
#   Unknown  anything else (a command the analysis does not know, a dynamic call, an unresolved target, ...):
#            the caller keeps its text heuristic, which fails closed.
# A text mention of src/ or tests/ inside a string, comment or here-string is never a target by itself.
function Test-ScriptWritesAreSafe {
    param([string]$Text, [string]$ScriptFull, [string]$Base, $Layout)

    $opaque = [System.Collections.Generic.List[string]]::new()
    $guarded = [System.Collections.Generic.List[string]]::new()
    $sites = [System.Collections.Generic.List[object]]::new()
    $ranges = [System.Collections.Generic.List[object]]::new()
    $assign = @{}
    $state = @{ CwdChanged = $false }
    $memo = @{}
    $visiting = @{}

    function SwR([string[]]$V) { return [pscustomobject]@{ V = $V } }
    function SwKind($Ast, [string]$Name) { return $null -ne $Ast -and $Ast.GetType().Name -eq $Name }
    function SwKey($VariableAst) { return (([string]$VariableAst.VariablePath.UserPath) -replace '^(?i)(script|global|local|private):', '').ToLowerInvariant() }
    function SwAdd([string]$Key, $Ast) {
        if (-not $assign.ContainsKey($Key)) { $assign[$Key] = [System.Collections.Generic.List[object]]::new() }
        $assign[$Key].Add($Ast)
    }
    function SwTaint($Ast) {
        if ($null -eq $Ast) { return }
        $found = if (SwKind $Ast 'VariableExpressionAst') { @($Ast) } else { @($Ast.FindAll({ param($a) $a.GetType().Name -eq 'VariableExpressionAst' }, $true)) }
        foreach ($v in $found) { SwAdd (SwKey $v) $null }
    }
    function SwCross($Parts) {
        $acc = @('')
        foreach ($p in $Parts) {
            $next = [System.Collections.Generic.List[string]]::new()
            foreach ($a in $acc) { foreach ($b in $p) { $next.Add($a + $b) } }
            if ($next.Count -gt 64) { return $null }
            $acc = $next.ToArray()
        }
        return , $acc
    }
    function SwUnion($Asts) {
        $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        foreach ($a in $Asts) {
            $r = SwEval $a
            if ($null -eq $r) { return $null }
            foreach ($v in $r.V) { [void]$set.Add($v) }
        }
        if ($set.Count -gt 64) { return $null }
        return (SwR ([string[]]@($set)))
    }
    function SwBaseKnown { return -not $state.CwdChanged -and -not [string]::IsNullOrWhiteSpace($Base) }
    function SwConst($Ast) {
        $r = SwEval $Ast
        if ($null -ne $r -and @($r.V).Count -eq 1) { return [string]$r.V[0] }
        return $null
    }

    function SwVar($Ast) {
        if ($Ast.Splatted) { return $null }
        $raw = [string]$Ast.VariablePath.UserPath
        if ($raw -match '^(?i)env:(?<n>.+)$') {
            $value = [Environment]::GetEnvironmentVariable($Matches['n'])
            if ([string]::IsNullOrEmpty($value)) { return $null }
            return (SwR @($value))
        }
        $key = SwKey $Ast
        switch ($key) {
            'psscriptroot' { if ([string]::IsNullOrEmpty($ScriptFull)) { return $null }; return (SwR @([IO.Path]::GetDirectoryName($ScriptFull))) }
            'pscommandpath' { if ([string]::IsNullOrEmpty($ScriptFull)) { return $null }; return (SwR @($ScriptFull)) }
            'home' { return (SwR @(Get-HomeDirectory)) }
            'pwd' { if (SwBaseKnown) { return (SwR @($Base)) }; return $null }
            'null' { return (SwR @('')) }
            'true' { return (SwR @('True')) }
            'false' { return (SwR @('False')) }
        }
        if ($key.Contains(':') -or -not $assign.ContainsKey($key)) { return $null }
        if ($memo.ContainsKey($key)) { return $memo[$key] }
        if ($visiting.ContainsKey($key)) { return $null }
        $visiting[$key] = $true
        $result = $null
        $entries = @($assign[$key])
        if ($entries.Count -gt 0 -and @($entries | Where-Object { $null -eq $_ }).Count -eq 0) { $result = SwUnion $entries }
        $visiting.Remove($key)
        $memo[$key] = $result
        return $result
    }

    function SwExpand($Ast) {
        $raw = $Ast.Extent.Text
        if ($raw.Contains('`')) { return $null }
        $start = 0
        $end = $raw.Length
        switch ([string]$Ast.StringConstantType) {
            'DoubleQuoted' { $start = 1; $end = $raw.Length - 1; if ($raw.Contains('""')) { return $null } }
            'DoubleQuotedHereString' {
                $start = $raw.IndexOf("`n") + 1
                $end = $raw.LastIndexOf('"@')
                if ($start -le 0 -or $end -lt $start) { return $null }
                while ($end -gt $start -and $raw[$end - 1] -in "`n", "`r") { $end-- }
            }
            'BareWord' { }
            default { return $null }
        }
        $parts = [System.Collections.Generic.List[object]]::new()
        $cursor = $start
        foreach ($n in @($Ast.NestedExpressions | Sort-Object { $_.Extent.StartOffset })) {
            $s = $n.Extent.StartOffset - $Ast.Extent.StartOffset
            $e = $n.Extent.EndOffset - $Ast.Extent.StartOffset
            if ($s -lt $cursor -or $e -gt $end) { return $null }
            $parts.Add([string[]]@($raw.Substring($cursor, $s - $cursor)))
            $r = SwEval $n
            if ($null -eq $r) { return $null }
            $parts.Add([string[]]@($r.V))
            $cursor = $e
        }
        $parts.Add([string[]]@($raw.Substring($cursor, $end - $cursor)))
        $crossed = SwCross $parts
        if ($null -eq $crossed) { return $null }
        return (SwR ([string[]]$crossed))
    }

    function SwTypeName($Ast) {
        if (-not (SwKind $Ast 'TypeExpressionAst')) { return $null }
        $name = ([string]$Ast.TypeName.FullName).ToLowerInvariant()
        return ($name -replace '^system\.', '')
    }

    function SwInvokeValue($Ast) {
        if (-not (SwKind $Ast.Member 'StringConstantExpressionAst')) { return $null }
        $member = ([string]$Ast.Member.Value).ToLowerInvariant()
        $argAsts = @($Ast.Arguments)
        $type = SwTypeName $Ast.Expression
        if ($Ast.Static -and $type -in 'io.path', 'path') {
            $values = @(foreach ($a in $argAsts) { $r = SwEval $a; if ($null -eq $r) { return $null }; , @($r.V) })
            if ($member -in 'combine', 'join') {
                if ($values.Count -eq 0) { return $null }
                $acc = @($values[0])
                foreach ($next in @($values | Select-Object -Skip 1)) {
                    $list = [System.Collections.Generic.List[string]]::new()
                    foreach ($a in $acc) { foreach ($b in $next) { $list.Add([IO.Path]::Combine($a, $b)) } }
                    if ($list.Count -gt 64) { return $null }
                    $acc = $list.ToArray()
                }
                return (SwR ([string[]]$acc))
            }
            if ($member -eq 'gettemppath' -and $values.Count -eq 0) { return (SwR @([IO.Path]::GetTempPath())) }
            if ($member -in 'getdirectoryname', 'getfilename', 'getfullpath' -and $values.Count -eq 1) {
                if ($member -eq 'getfullpath' -and -not (SwBaseKnown)) { return $null }
                $out = foreach ($v in $values[0]) {
                    switch ($member) {
                        'getdirectoryname' { [IO.Path]::GetDirectoryName($v) }
                        'getfilename' { [IO.Path]::GetFileName($v) }
                        default { Get-FullPath $v $Base }
                    }
                }
                if (@($out | Where-Object { $null -eq $_ }).Count -gt 0) { return $null }
                return (SwR ([string[]]@($out)))
            }
            return $null
        }
        if ($Ast.Static -and $type -eq 'environment') {
            if ($member -eq 'getenvironmentvariable' -and $argAsts.Count -eq 1) {
                $name = SwConst $argAsts[0]
                $value = if ($null -ne $name) { [Environment]::GetEnvironmentVariable($name) } else { $null }
                if ([string]::IsNullOrEmpty($value)) { return $null }
                return (SwR @($value))
            }
            return $null
        }
        if (-not $Ast.Static) {
            $receiver = SwEval $Ast.Expression
            if ($null -eq $receiver) { return $null }
            $constants = @(foreach ($a in $argAsts) { $c = SwConst $a; if ($null -eq $c) { return $null }; $c })
            $out = foreach ($v in $receiver.V) {
                switch ($member) {
                    'tostring' { if ($constants.Count -eq 0) { $v } else { return $null } }
                    'trim' { if ($constants.Count -eq 0) { $v.Trim() } else { $v.Trim(([string]$constants[0]).ToCharArray()) } }
                    'trimend' { if ($constants.Count -eq 0) { $v.TrimEnd() } else { $v.TrimEnd(([string]$constants[0]).ToCharArray()) } }
                    'trimstart' { if ($constants.Count -eq 0) { $v.TrimStart() } else { $v.TrimStart(([string]$constants[0]).ToCharArray()) } }
                    'replace' { if ($constants.Count -eq 2) { $v.Replace([string]$constants[0], [string]$constants[1]) } else { return $null } }
                    default { return $null }
                }
            }
            return (SwR ([string[]]@($out)))
        }
        return $null
    }

    $arrayVisiting = @{}
    function SwMayBeArray($Ast) {
        if ($null -eq $Ast) { return $false }
        $nodes2 = @($Ast) + @($Ast.FindAll({ param($a) $true }, $true))
        foreach ($n in $nodes2) {
            $t = $n.GetType().Name
            if ($t -in 'ArrayLiteralAst', 'ArrayExpressionAst') { return $true }
            if ($t -eq 'VariableExpressionAst') {
                $key = SwKey $n
                if ($arrayVisiting.ContainsKey($key) -or -not $assign.ContainsKey($key)) { continue }
                $arrayVisiting[$key] = $true
                $hit = $false
                foreach ($entry in @($assign[$key])) { if ($null -ne $entry -and (SwMayBeArray $entry)) { $hit = $true; break } }
                $arrayVisiting.Remove($key)
                if ($hit) { return $true }
            }
        }
        return $false
    }

    function SwBinary($Ast) {
        $op = [string]$Ast.Operator
        if ($op -eq 'Plus') {
            # Array + value appends in PowerShell; only a string concatenation can be modelled, so an operand that
            # is or may hold an array (or several values) is not evaluated.
            if ((SwMayBeArray $Ast.Left) -or (SwMayBeArray $Ast.Right)) { return $null }
            $l = SwEval $Ast.Left
            $r = SwEval $Ast.Right
            if ($null -eq $l -or $null -eq $r) { return $null }
            if (@($l.V).Count -gt 1 -or @($r.V).Count -gt 1) { return $null }
            $parts = [System.Collections.Generic.List[object]]::new()
            $parts.Add([string[]]@($l.V)); $parts.Add([string[]]@($r.V))
            $crossed = SwCross $parts
            if ($null -eq $crossed) { return $null }
            return (SwR ([string[]]$crossed))
        }
        if ($op -eq 'Format') {
            $format = SwConst $Ast.Left
            if ($null -eq $format) { return $null }
            $argAsts = if (SwKind $Ast.Right 'ArrayLiteralAst') { @($Ast.Right.Elements) } else { @($Ast.Right) }
            $values = @(foreach ($a in $argAsts) { $c = SwConst $a; if ($null -eq $c) { return $null }; $c })
            try { return (SwR @([string]::Format($format, [object[]]$values))) } catch { return $null }
        }
        return $null
    }

    function SwCommandValue($Ast) {
        $name = $Ast.GetCommandName()
        if ($null -eq $name -or $Ast.InvocationOperator -ne 'Unknown') { return $null }
        $lname = $name.ToLowerInvariant()
        if ($lname -eq 'join-path') {
            $named = @{}
            $positional = [System.Collections.Generic.List[object]]::new()
            $elements = @($Ast.CommandElements | Select-Object -Skip 1)
            for ($i = 0; $i -lt $elements.Count; $i++) {
                $e = $elements[$i]
                if (SwKind $e 'CommandParameterAst') {
                    $p = $e.ParameterName.ToLowerInvariant()
                    $slot = if ($p.Length -ge 2 -and 'path'.StartsWith($p)) { 'path' } elseif ($p.Length -ge 2 -and 'childpath'.StartsWith($p)) { 'childpath' } elseif ($p.Length -ge 2 -and 'additionalchildpath'.StartsWith($p)) { 'additional' } else { return $null }
                    if ($null -ne $e.Argument) { $named[$slot] = $e.Argument }
                    elseif ($i + 1 -lt $elements.Count) { $named[$slot] = $elements[$i + 1]; $i++ }
                    else { return $null }
                }
                else { $positional.Add($e) }
            }
            $order = [System.Collections.Generic.List[object]]::new()
            $p = 0
            foreach ($slot in 'path', 'childpath') {
                if ($named.ContainsKey($slot)) { $order.Add($named[$slot]) }
                elseif ($p -lt $positional.Count) { $order.Add($positional[$p]); $p++ }
                else { return $null }
            }
            foreach ($extra in @($positional | Select-Object -Skip $p)) { $order.Add($extra) }
            if ($named.ContainsKey('additional')) { $order.Add($named['additional']) }
            $acc = $null
            foreach ($o in $order) {
                $r = SwEval $o
                if ($null -eq $r) { return $null }
                if ($null -eq $acc) { $acc = @($r.V); continue }
                $list = [System.Collections.Generic.List[string]]::new()
                $sep = [string][IO.Path]::DirectorySeparatorChar
                foreach ($a in $acc) { foreach ($b in $r.V) { $list.Add($(if ($a -eq '') { $b } else { $a.TrimEnd('\', '/') + $sep + $b.TrimStart('\', '/') })) } }
                if ($list.Count -gt 64) { return $null }
                $acc = $list.ToArray()
            }
            return (SwR ([string[]]$acc))
        }
        return $null
    }

    function SwEval($Ast) {
        if ($null -eq $Ast) { return $null }
        switch ($Ast.GetType().Name) {
            'StringConstantExpressionAst' { return (SwR @([string]$Ast.Value)) }
            'ConstantExpressionAst' { return (SwR @([string]$Ast.Value)) }
            'ExpandableStringExpressionAst' { return (SwExpand $Ast) }
            'VariableExpressionAst' { return (SwVar $Ast) }
            'ParenExpressionAst' { return (SwEval $Ast.Pipeline) }
            'SubExpressionAst' { if ($Ast.SubExpression.Statements.Count -eq 1) { return (SwEval $Ast.SubExpression.Statements[0]) }; return $null }
            'PipelineAst' { if ($Ast.PipelineElements.Count -eq 1) { return (SwEval $Ast.PipelineElements[0]) }; return $null }
            'CommandExpressionAst' { if ($Ast.Redirections.Count -gt 0) { return $null }; return (SwEval $Ast.Expression) }
            'ArrayLiteralAst' { return (SwUnion @($Ast.Elements)) }
            'ArrayExpressionAst' { return (SwUnion @($Ast.SubExpression.Statements)) }
            'BinaryExpressionAst' { return (SwBinary $Ast) }
            'ConvertExpressionAst' { if (([string]$Ast.Type.TypeName.FullName) -in 'string', 'System.String') { return (SwEval $Ast.Child) }; return $null }
            'InvokeMemberExpressionAst' {
                if ($Ast.Static -and (SwTypeName $Ast.Expression) -in 'io.directory', 'directory' -and ([string]$Ast.Member.Value) -ieq 'GetCurrentDirectory' -and (SwBaseKnown)) { return (SwR @($Base)) }
                return (SwInvokeValue $Ast)
            }
            'MemberExpressionAst' {
                if (-not (SwKind $Ast.Member 'StringConstantExpressionAst')) { return $null }
                $member = ([string]$Ast.Member.Value).ToLowerInvariant()
                if ($member -eq 'path' -and (SwBaseKnown)) {
                    $inner = $Ast.Expression
                    if (SwKind $inner 'ParenExpressionAst') {
                        $cmd = $inner.Pipeline.PipelineElements | Select-Object -First 1
                        if ($inner.Pipeline.PipelineElements.Count -eq 1 -and (SwKind $cmd 'CommandAst') -and $cmd.GetCommandName() -in 'Get-Location', 'pwd', 'gl') { return (SwR @($Base)) }
                    }
                    if ((SwKind $inner 'VariableExpressionAst') -and (SwKey $inner) -eq 'pwd') { return (SwR @($Base)) }
                }
                if ($member -eq 'currentdirectory' -and $Ast.Static -and (SwTypeName $Ast.Expression) -eq 'environment' -and (SwBaseKnown)) { return (SwR @($Base)) }
                return $null
            }
            'CommandAst' { return (SwCommandValue $Ast) }
            default { return $null }
        }
    }

    # Cmdlet parameters: the ones that take a value and the switches, so a value is never taken for a path.
    $valueParams = 'path', 'literalpath', 'pspath', 'lp', 'filepath', 'destination', 'destinationpath', 'outfile', 'value', 'encoding', 'inputobject', 'itemtype', 'type', 'name', 'stream', 'filter', 'include', 'exclude', 'width', 'newname', 'credential', 'delimiter', 'erroraction', 'ea', 'warningaction', 'wa', 'informationaction', 'ia', 'errorvariable', 'ev', 'outvariable', 'ov', 'pipelinevariable', 'pv', 'outbuffer', 'ob', 'uri', 'method', 'body', 'contenttype', 'headers', 'totalcount', 'tail', 'compressionlevel', 'variable', 'depth', 'property'
    $switchParams = 'force', 'nonewline', 'passthru', 'whatif', 'confirm', 'append', 'noclobber', 'recurse', 'verbose', 'debug', 'usebasicparsing', 'noheader', 'notypeinformation', 'raw', 'update', 'quotefields'
    $allParams = @($valueParams) + @($switchParams)
    $aliases = @{ sc = 'set-content'; ac = 'add-content'; clc = 'clear-content'; tee = 'tee-object'; ni = 'new-item'; mkdir = 'new-item'; md = 'new-item'; cpi = 'copy-item'; copy = 'copy-item'; cp = 'copy-item'; mi = 'move-item'; move = 'move-item'; mv = 'move-item'; ren = 'rename-item'; rni = 'rename-item'; iwr = 'invoke-webrequest'; irm = 'invoke-restmethod' }
    $pathSlot = @('path', 'literalpath', 'pspath', 'lp')
    $specs = @{
        'set-content'        = @{ Slots = @($pathSlot, @('value')); Want = 0 }
        'add-content'        = @{ Slots = @($pathSlot, @('value')); Want = 0 }
        'clear-content'      = @{ Slots = @(, $pathSlot); Want = 0 }
        'out-file'           = @{ Slots = @((@('filepath') + $pathSlot), @('encoding')); Want = 0 }
        'tee-object'         = @{ Slots = @(, @('filepath', 'literalpath', 'lp')); Want = 0; OptionalWhenNamed = 'variable' }
        'export-csv'         = @{ Slots = @(, $pathSlot); Want = 0 }
        'export-clixml'      = @{ Slots = @(, $pathSlot); Want = 0 }
        'new-item'           = @{ Slots = @(@('path'), @('name')); Want = 0; Join = 'name' }
        'copy-item'          = @{ Slots = @($pathSlot, @('destination')); Want = 1 }
        'move-item'          = @{ Slots = @($pathSlot, @('destination')); Want = 1 }
        'rename-item'        = @{ Slots = @($pathSlot, @('newname')); Want = 0; Rename = $true }
        'invoke-webrequest'  = @{ Slots = @(, @('uri')); Named = 'outfile' }
        'invoke-restmethod'  = @{ Slots = @(, @('uri')); Named = 'outfile' }
        'expand-archive'     = @{ Slots = @($pathSlot, @('destinationpath')); Want = 1; DefaultCurrent = $true }
        'compress-archive'   = @{ Slots = @($pathSlot, @('destinationpath')); Want = 1 }
    }
    $cwdCommands = 'set-location', 'cd', 'sl', 'chdir', 'push-location', 'pushd', 'pop-location', 'popd'
    $safeNames = 'foreach-object', '%', 'where-object', '?', 'resolve-path', 'convert-path', 'start-sleep', 'sleep', 'set-strictmode', 'add-member', 'remove-item', 'ri', 'rm', 'del', 'erase', 'rd', 'rmdir', 'read-host', 'clear-host', 'cls', 'clear', 'new-guid', 'new-timespan', 'import-csv', 'import-clixml', 'echo', 'cat', 'type', 'ls', 'dir', 'gci', 'gc', 'gi', 'gl', 'pwd', 'select', 'where', 'sort', 'measure', 'ft', 'fl', 'fw', 'gm', 'group', 'compare-object', 'join-path', 'split-path'
    $safePattern = '^(get|test|write|select|where|format|measure|sort|group|compare|convertto|convertfrom)-|^out-(string|null|host|default)$'
    $safeNew = '^(system\.)?(management\.automation\.)?(psobject|pscustomobject)$|stringbuilder|^(system\.)?collections\.|^hashtable$|^(system\.)?(version|datetime|timespan|uri)$|stringreader|stringwriter|memorystream|utf8encoding|cryptography\.'
    $safeStatic = '^(io\.path|path|math|string|convert|guid|datetime|datetimeoffset|environment|(text\.)?regularexpressions\.regex|regex|text\..+|uri|array|char|int|int32|int64|double|bitconverter|console|timespan|version|globalization\..+|collections\..+|linq\..+|security\.cryptography\..+|io\.streamreader|io\.stringreader|io\.stringwriter|io\.memorystream|pscustomobject|psobject|hashtable|ordered|bool|byte|decimal|stringcomparison)$'
    $safeInstance = 'append', 'appendline', 'appendformat', 'add', 'addrange', 'contains', 'containskey', 'containsvalue', 'startswith', 'endswith', 'trim', 'trimend', 'trimstart', 'split', 'substring', 'replace', 'tostring', 'tolower', 'tolowerinvariant', 'toupper', 'toupperinvariant', 'indexof', 'lastindexof', 'insert', 'remove', 'removeat', 'padleft', 'padright', 'normalize', 'equals', 'gettype', 'getenumerator', 'clone', 'toarray', 'tolist', 'where', 'foreach', 'first', 'match', 'ismatch', 'matches', 'gethashcode', 'compareto', 'sort', 'reverse', 'join', 'write', 'writeline', 'flush', 'close', 'dispose', 'getbytes', 'getstring', 'tochararray', 'elementat', 'select', 'any', 'all', 'readtoend', 'readline', 'read', 'peek', 'getvalue'

    # The bound arguments of a cmdlet call: named parameters (canonical, unique prefixes) and positional ones.
    function SwBind($Command) {
        $b = @{ Ok = $true; Named = @{}; Pos = [System.Collections.Generic.List[object]]::new() }
        $elements = @($Command.CommandElements | Select-Object -Skip 1)
        for ($i = 0; $i -lt $elements.Count; $i++) {
            $e = $elements[$i]
            if (SwKind $e 'VariableExpressionAst') { if ($e.Splatted) { $b.Ok = $false; continue } }
            if (-not (SwKind $e 'CommandParameterAst')) { $b.Pos.Add($e); continue }
            $p = $e.ParameterName.ToLowerInvariant()
            $matched = @($allParams | Where-Object { $_ -eq $p })
            if ($matched.Count -eq 0 -and $p.Length -ge 2) { $matched = @($allParams | Where-Object { $_.StartsWith($p) }) }
            if ($matched.Count -ne 1) { $b.Ok = $false; continue }
            $canonical = [string]$matched[0]
            if ($null -ne $e.Argument) { $b.Named[$canonical] = $e.Argument }
            elseif ($canonical -in $switchParams) { $b.Named[$canonical] = $true }
            elseif ($i + 1 -lt $elements.Count -and -not (SwKind $elements[$i + 1] 'CommandParameterAst')) { $b.Named[$canonical] = $elements[$i + 1]; $i++ }
            else { $b.Ok = $false }
        }
        return $b
    }
    function SwSlot($Bound, $Slots, [int]$Want) {
        $p = 0
        for ($i = 0; $i -lt $Slots.Count; $i++) {
            $named = $null
            foreach ($alias in $Slots[$i]) { if ($Bound.Named.ContainsKey($alias)) { $named = $Bound.Named[$alias]; break } }
            if ($null -ne $named) { if ($i -eq $Want) { return $named }; continue }
            if ($p -lt $Bound.Pos.Count) { if ($i -eq $Want) { return $Bound.Pos[$p] }; $p++ }
        }
        return $null
    }
    function SwAddSite($Extent, $Candidates, [string]$What) {
        $sites.Add([pscustomobject]@{ Candidates = $Candidates; What = $What })
        $ranges.Add($Extent)
    }
    function SwJoinLeaf($Parents, $Leaves) {
        $list = [System.Collections.Generic.List[string]]::new()
        foreach ($a in $Parents) { foreach ($b in $Leaves) { $list.Add($(if ($a -eq '') { $b } else { $a.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar + $b.TrimStart('\', '/') })) } }
        if ($list.Count -gt 64) { return $null }
        return $list.ToArray()
    }

    try {
        $parseErrors = $null
        $parseTokens = $null
        $root = [System.Management.Automation.Language.Parser]::ParseInput($Text, [ref]$parseTokens, [ref]$parseErrors)
        if ($null -eq $root -or $parseErrors.Count -gt 0) { return [pscustomobject]@{ Verdict = 'Unknown'; Detail = 'the script does not parse' } }
        $nodes = @($root.FindAll({ param($a) $true }, $true))
        $commands = @($nodes | Where-Object { $_.GetType().Name -eq 'CommandAst' })
        $functions = @{}
        foreach ($f in @($nodes | Where-Object { $_.GetType().Name -eq 'FunctionDefinitionAst' })) { $functions[$f.Name.ToLowerInvariant()] = $f }

        if (@($nodes | Where-Object { $_.GetType().Name -in 'UsingStatementAst', 'TypeDefinitionAst' }).Count -gt 0) { $opaque.Add('a using statement or a class') }
        if ($Text -match '(?i)\bCurrentDirectory\b') { $state.CwdChanged = $true }
        # $env:NAME resolves from the hook's own environment, so a script that changes the environment is not analysable.
        if ($Text -match '(?i)SetEnvironmentVariable|\$env:\w+\s*[-+*/.]?=|\b(Set-Item|si|New-Item|ni|Set-Variable|sv|New-Variable|nv)\b[^\r\n]*\benv:') { $opaque.Add('a script that changes the environment') }
        # A function that shadows a command the analysis models (Join-Path) makes its value rules wrong.
        if ($functions.ContainsKey('join-path')) { $opaque.Add('a function named Join-Path') }
        if ($functions.Keys | Where-Object { $_ -in $aliases.Keys -or $_ -in $specs.Keys }) { $opaque.Add('a function that shadows a cmdlet') }
        foreach ($c in $commands) {
            $n = $c.GetCommandName()
            if ($null -ne $n -and $n.ToLowerInvariant() -in $cwdCommands) { $state.CwdChanged = $true }
        }

        # Variable table: every assignment, flow-insensitive; a variable the analysis cannot follow is tainted ($null entry).
        foreach ($a in @($nodes | Where-Object { $_.GetType().Name -eq 'AssignmentStatementAst' })) {
            $left = $a.Left
            while ((SwKind $left 'ConvertExpressionAst') -or (SwKind $left 'AttributedExpressionAst')) { $left = $left.Child }
            if (SwKind $left 'VariableExpressionAst') {
                if ([string]$a.Operator -eq 'Equals') {
                    $right = $a.Right
                    SwAdd (SwKey $left) $right
                }
                else { SwAdd (SwKey $left) $null }
            }
            else { SwTaint $left }
        }
        foreach ($f in @($nodes | Where-Object { $_.GetType().Name -eq 'ForEachStatementAst' })) { SwAdd (SwKey $f.Variable) $f.Condition }
        foreach ($u in @($nodes | Where-Object { $_.GetType().Name -eq 'UnaryExpressionAst' -and [string]$_.TokenKind -in 'PlusPlus', 'MinusMinus', 'PostfixPlusPlus', 'PostfixMinusMinus' })) { SwTaint $u.Child }
        foreach ($r in @($nodes | Where-Object { $_.GetType().Name -eq 'ConvertExpressionAst' -and $_.StaticType -eq [ref] })) { SwTaint $r.Child }
        foreach ($c in $commands) {
            $elements = @($c.CommandElements)
            for ($i = 0; $i -lt $elements.Count; $i++) {
                if ((SwKind $elements[$i] 'CommandParameterAst') -and $elements[$i].ParameterName -match '^(?i)(ov|ev|wv|iv|pv|outv\w*|errorv\w*|warningv\w*|informationv\w*|pipelinev\w*|variable)$') {
                    $target = if ($null -ne $elements[$i].Argument) { $elements[$i].Argument } elseif ($i + 1 -lt $elements.Count) { $elements[$i + 1] } else { $null }
                    if ($null -ne $target) { SwAdd (([string]$target.Extent.Text).Trim('''"$ ').ToLowerInvariant()) $null }
                }
            }
        }

        # Parameters: a function's come from the literal arguments of its calls in this script (and their defaults);
        # every other parameter (script or script-block param block, class method) is tainted.
        $functionParams = [System.Collections.Generic.HashSet[object]]::new()
        $tainted = @{}
        foreach ($name in $functions.Keys) {
            $fn = $functions[$name]
            $params = @(if ($null -ne $fn.Parameters) { $fn.Parameters } elseif ($null -ne $fn.Body.ParamBlock) { $fn.Body.ParamBlock.Parameters })
            foreach ($p in $params) { [void]$functionParams.Add($p) }
            $fnText = $fn.Extent.Text
            $pipeline = $fnText -match '(?i)ValueFromPipeline|\$input\b|\$args\b|\bprocess\s*\{|\bdynamicparam\b|PSBoundParameters'
            foreach ($p in $params) {
                if ($pipeline) { SwAdd (SwKey $p.Name) $null }
                elseif ($null -ne $p.DefaultValue) { SwAdd (SwKey $p.Name) $p.DefaultValue }
            }
            foreach ($call in @($commands | Where-Object { $null -ne $_.GetCommandName() -and $_.GetCommandName().ToLowerInvariant() -eq $name -and $_.InvocationOperator -eq 'Unknown' })) {
                $elements = @($call.CommandElements | Select-Object -Skip 1)
                $positionalParams = @($params)
                $bound = [System.Collections.Generic.HashSet[object]]::new()
                $ok = $true
                $position = 0
                for ($i = 0; $i -lt $elements.Count -and $ok; $i++) {
                    $e = $elements[$i]
                    if ((SwKind $e 'VariableExpressionAst') -and $e.Splatted) { $ok = $false; break }
                    if (SwKind $e 'CommandParameterAst') {
                        $found = @($params | Where-Object { (SwKey $_.Name) -eq $e.ParameterName.ToLowerInvariant() })
                        if ($found.Count -eq 0) { $found = @($params | Where-Object { (SwKey $_.Name).StartsWith($e.ParameterName.ToLowerInvariant()) }) }
                        if ($found.Count -ne 1) { $ok = $false; break }
                        $param = $found[0]
                        [void]$bound.Add($param)
                        if ($null -ne $e.Argument) { SwAdd (SwKey $param.Name) $e.Argument }
                        elseif ($param.StaticType -eq [switch]) { SwAdd (SwKey $param.Name) $null }
                        elseif ($i + 1 -lt $elements.Count -and -not (SwKind $elements[$i + 1] 'CommandParameterAst')) { SwAdd (SwKey $param.Name) $elements[$i + 1]; $i++ }
                        else { $ok = $false }
                        continue
                    }
                    while ($position -lt $positionalParams.Count -and $bound.Contains($positionalParams[$position])) { $position++ }
                    if ($position -ge $positionalParams.Count) { $ok = $false; break }
                    [void]$bound.Add($positionalParams[$position])
                    SwAdd (SwKey $positionalParams[$position].Name) $e
                    $position++
                }
                if (-not $ok) { foreach ($p in $params) { SwAdd (SwKey $p.Name) $null } }
            }
        }
        foreach ($p in @($nodes | Where-Object { $_.GetType().Name -eq 'ParameterAst' })) { if (-not $functionParams.Contains($p)) { SwAdd (SwKey $p.Name) $null } }

        # Write sites.
        foreach ($c in $commands) {
            if ($c.InvocationOperator -ne 'Unknown') { $opaque.Add('a call-operator or dot-source launch'); continue }
            $name = $c.GetCommandName()
            if ($null -eq $name) { $opaque.Add('a command named by an expression'); continue }
            $lname = $name.ToLowerInvariant()
            $canonical = if ($aliases.ContainsKey($lname)) { $aliases[$lname] } else { $lname }
            if ($specs.ContainsKey($canonical)) {
                $spec = $specs[$canonical]
                $bound = SwBind $c
                if ($canonical -eq 'new-item' -and $bound.Named.ContainsKey('itemtype')) {
                    $itemType = if ($bound.Named['itemtype'] -is [bool]) { $null } else { SwConst $bound.Named['itemtype'] }
                    # A link's -Value is its target, so later writes through it land elsewhere.
                    if ($null -eq $itemType -or $itemType -notmatch '^(?i)(file|directory)$') { $opaque.Add('New-Item of a link or an unknown item type') }
                }
                $targetAst = $null
                if ($spec.ContainsKey('Named')) { if ($bound.Named.ContainsKey($spec.Named)) { $targetAst = $bound.Named[$spec.Named] } else { continue } }
                else { $targetAst = SwSlot $bound $spec.Slots $spec.Want }
                if ($spec.ContainsKey('OptionalWhenNamed') -and $null -eq $targetAst -and $bound.Named.ContainsKey($spec.OptionalWhenNamed)) { continue }
                $candidates = $null
                if ($bound.Ok) {
                    if ($null -eq $targetAst -and $spec.ContainsKey('DefaultCurrent')) { $candidates = [string[]]@('.') }
                    elseif ($null -eq $targetAst -and $spec.ContainsKey('Join')) { $candidates = [string[]]@('.') }
                    elseif ($null -ne $targetAst -and $targetAst -isnot [bool]) { $r = SwEval $targetAst; if ($null -ne $r) { $candidates = [string[]]@($r.V) } }
                    if ($null -ne $candidates -and $spec.ContainsKey('Join') -and $bound.Named.ContainsKey($spec.Join)) {
                        $leaf = SwEval $bound.Named[$spec.Join]
                        $candidates = if ($null -eq $leaf) { $null } else { SwJoinLeaf $candidates $leaf.V }
                    }
                    if ($null -ne $candidates -and $canonical -eq 'move-item') {
                        # The source is removed by the move, so a source under src/ or tests/ is a write there too.
                        $sourceAst = SwSlot $bound $spec.Slots 0
                        $sourceValue = if ($null -ne $sourceAst -and $sourceAst -isnot [bool]) { SwEval $sourceAst } else { $null }
                        $candidates = if ($null -eq $sourceValue) { $null } else { [string[]]@($candidates) + [string[]]@($sourceValue.V) }
                    }
                    if ($null -ne $candidates -and $spec.ContainsKey('Rename')) {
                        $newName = SwSlot $bound $spec.Slots 1
                        $leaf = if ($null -ne $newName) { SwEval $newName } else { $null }
                        if ($null -eq $leaf) { $candidates = $null }
                        else {
                            $moved = foreach ($old in $candidates) { $dir = [IO.Path]::GetDirectoryName($old); SwJoinLeaf @($(if ($null -eq $dir) { '' } else { $dir })) $leaf.V }
                            $candidates = [string[]]@($candidates) + [string[]]@($moved)
                        }
                    }
                }
                SwAddSite $c.Extent $candidates $canonical
                continue
            }
            if ($lname -eq 'new-object') {
                $bound = SwBind $c
                if ($bound.Named.ContainsKey('comobject') -or -not $bound.Ok) { $opaque.Add('New-Object with an unknown shape'); continue }
                $typeAst = if ($bound.Named.ContainsKey('typename')) { $bound.Named['typename'] } elseif ($bound.Pos.Count -gt 0) { $bound.Pos[0] } else { $null }
                $typeName = if ($null -ne $typeAst) { SwConst $typeAst } else { $null }
                if ($null -eq $typeName) { $opaque.Add('New-Object of an unknown type'); continue }
                $tl = ($typeName.ToLowerInvariant() -replace '^system\.', '')
                if ($tl -match 'streamwriter|filestream|binarywriter') {
                    $argList = if ($bound.Named.ContainsKey('argumentlist')) { $bound.Named['argumentlist'] } elseif ($bound.Pos.Count -gt 1) { $bound.Pos[1] } else { $null }
                    $first = if (SwKind $argList 'ArrayLiteralAst') { $argList.Elements[0] } else { $argList }
                    $r = if ($null -ne $first) { SwEval $first } else { $null }
                    SwAddSite $c.Extent $(if ($null -ne $r) { [string[]]@($r.V) } else { $null }) 'New-Object'
                }
                elseif ($tl -match $safeNew) { }
                else { $opaque.Add("New-Object $typeName") }
                continue
            }
            if ($lname -in $cwdCommands) { continue }
            if ($lname -in 'foreach-object', '%') {
                $elements = @($c.CommandElements | Select-Object -Skip 1)
                $firstValue = @($elements | Where-Object { -not (SwKind $_ 'CommandParameterAst') } | Select-Object -First 1)
                if ($firstValue.Count -gt 0 -and -not (SwKind $firstValue[0] 'ScriptBlockExpressionAst') -and @($elements | Where-Object { (SwKind $_ 'CommandParameterAst') -and $_.ParameterName -match '^(?i)(b|begin|p|process|e|end|r|remaining)' }).Count -eq 0) { $opaque.Add('ForEach-Object with a member name') }
                continue
            }
            if ($lname -in $safeNames -or $lname -match $safePattern -or $functions.ContainsKey($lname)) {
                if ($lname -in 'get-variable', 'gv') { $opaque.Add('Get-Variable') }
                continue
            }
            $opaque.Add("a command the analysis does not know ($name)")
        }

        # Redirections.
        foreach ($r in @($nodes | Where-Object { $_.GetType().Name -in 'FileRedirectionAst', 'MergingRedirectionAst' })) {
            $ranges.Add($r.Extent)
            if ($r.GetType().Name -ne 'FileRedirectionAst') { continue }
            $loc = $r.Location
            if ((SwKind $loc 'VariableExpressionAst') -and (SwKey $loc) -eq 'null') { continue }
            $value = SwConst $loc
            if ($null -ne $value -and $value -in 'nul', '/dev/null', '$null') { continue }
            $candidates = $null
            $resolved = SwEval $loc
            if ($null -ne $resolved) { $candidates = [string[]]@($resolved.V) }
            $sites.Add([pscustomobject]@{ Candidates = $candidates; What = 'redirection' })
        }

        # Static and instance method calls.
        foreach ($m in @($nodes | Where-Object { $_.GetType().Name -eq 'InvokeMemberExpressionAst' })) {
            if (-not (SwKind $m.Member 'StringConstantExpressionAst')) { $opaque.Add('a method named by an expression'); continue }
            $member = ([string]$m.Member.Value).ToLowerInvariant()
            $callArgs = @($m.Arguments)
            $type = SwTypeName $m.Expression
            if (-not $m.Static) {
                if ($member -notin $safeInstance) { $opaque.Add("the method .$($m.Member.Value)()") }
                continue
            }
            if ($null -eq $type) { $opaque.Add("a static call through $($m.Expression.Extent.Text)"); continue }
            $indexes = $null
            if ($type -in 'io.file', 'file') {
                if ($member -match '^(exists|readall\w*|readlines\w*|openread|opentext|getlastwritetime\w*|getcreationtime\w*|getlastaccesstime\w*|getattributes|delete|setattributes|setlastwritetime\w*|setlastaccesstime\w*|setcreationtime\w*|resolvelinktarget|getunixfilemode)$') { }
                elseif ($member -in 'copy', 'move') { $indexes = @(1) }
                elseif ($member -eq 'replace') { $indexes = @(1, 2) }
                elseif ($member -match '^(write\w*|appendall\w*|appendtext|create\w*|open|openwrite|openhandle|encrypt|decrypt|setunixfilemode)$') { $indexes = @(0) }
                else { $opaque.Add("[IO.File]::$($m.Member.Value)") }
            }
            elseif ($type -in 'io.directory', 'directory') {
                if ($member -eq 'setcurrentdirectory') { $state.CwdChanged = $true }
                elseif ($member -in 'createdirectory') { $indexes = @(0) }
                elseif ($member -eq 'move') { $indexes = @(1) }
                elseif ($member -match '^(exists|get\w*|enumerate\w*|delete|createtempsubdirectory)$') { }
                else { $opaque.Add("[IO.Directory]::$($m.Member.Value)") }
            }
            elseif ($type -in 'io.streamwriter', 'streamwriter', 'io.filestream', 'filestream', 'io.binarywriter') {
                if ($member -eq 'new') {
                    $readOnly = $type -match 'filestream' -and $callArgs.Count -gt 1 -and @($callArgs | Select-Object -Skip 1 | Where-Object { $_.Extent.Text -notmatch '^\[?(System\.)?(IO\.)?(FileMode\.Open|FileAccess\.Read|FileShare\.\w+)\]?$' }).Count -eq 0
                    if (-not $readOnly) { $indexes = @(0) }
                }
                else { $opaque.Add("[$type]::$($m.Member.Value)") }
            }
            elseif ($type -notmatch $safeStatic) { $opaque.Add("[$type]::$($m.Member.Value)") }
            if ($null -ne $indexes) {
                $candidates = [System.Collections.Generic.List[string]]::new()
                $resolvedAll = $true
                foreach ($ix in $indexes) {
                    $r = if ($ix -lt $callArgs.Count) { SwEval $callArgs[$ix] } else { $null }
                    if ($null -eq $r) { $resolvedAll = $false; break }
                    foreach ($v in $r.V) { $candidates.Add($v) }
                }
                SwAddSite $m.Extent $(if ($resolvedAll) { $candidates.ToArray() } else { $null }) "[$type]::$($m.Member.Value)"
            }
        }

        # Judge the targets.
        $unresolved = $false
        $layoutGuarded = {
            param([string]$Full)
            $location = Get-RepoLocation $Full $Layout
            if ($null -ne $location -and $location.Relative -match '^(src|tests)(/|$)') { return $true }
            $dir = [IO.Path]::GetDirectoryName($Full)
            while (-not [string]::IsNullOrEmpty($dir)) {
                if (Test-Path -LiteralPath (Join-Path $dir '.git')) {
                    $relative = $Full.Substring([Math]::Min($dir.TrimEnd('\', '/').Length, $Full.Length)).TrimStart('\', '/').Replace('\', '/')
                    if ($relative -match '^(src|tests)(/|$)') { return $true }
                }
                $dir = [IO.Path]::GetDirectoryName($dir)
            }
            return $false
        }
        foreach ($site in $sites) {
            if ($null -eq $site.Candidates) { $unresolved = $true; continue }
            foreach ($value in $site.Candidates) {
                if ([string]::IsNullOrWhiteSpace($value) -or $value -match '::|^\\\\|^[A-Za-z0-9_.]{2,}:|[*?\[]') { $unresolved = $true; continue }
                $native = ConvertTo-NativePath $value
                $rooted = [IO.Path]::IsPathRooted($native) -or $value.StartsWith('~')
                if (-not $rooted -and $value -match '(^|[\\/])(src|tests)([\\/]|$)') { $guarded.Add("$($site.What) -> $value"); continue }
                if (-not $rooted -and -not (SwBaseKnown)) { $unresolved = $true; continue }
                $full = Get-FullPath $value $Base
                if ($null -eq $full) { $unresolved = $true; continue }
                if (& $layoutGuarded $full) { $guarded.Add("$($site.What) -> $full") }
            }
        }
        if ($guarded.Count -gt 0) { return [pscustomobject]@{ Verdict = 'Guarded'; Detail = $guarded[0] } }
        if ($opaque.Count -gt 0) { return [pscustomobject]@{ Verdict = 'Unknown'; Detail = $opaque[0]; Unresolved = $unresolved } }
        if ($unresolved) { return [pscustomobject]@{ Verdict = 'Unknown'; Detail = 'a write target the hook cannot resolve'; Unresolved = $true } }

        # Residual: no write API may be left once the strings, comments and understood sites are removed.
        $chars = $Text.ToCharArray()
        $blank = {
            param($Start, $End)
            for ($i = [Math]::Max(0, $Start); $i -lt [Math]::Min($chars.Length, $End); $i++) { $chars[$i] = ' ' }
        }
        foreach ($r in $ranges) { & $blank $r.StartOffset $r.EndOffset }
        foreach ($s in @($nodes | Where-Object { $_.GetType().Name -in 'StringConstantExpressionAst', 'ExpandableStringExpressionAst' -and [string]$_.StringConstantType -ne 'BareWord' })) { & $blank $s.Extent.StartOffset $s.Extent.EndOffset }
        foreach ($t in @($parseTokens | Where-Object { [string]$_.Kind -eq 'Comment' })) { & $blank $t.Extent.StartOffset $t.Extent.EndOffset }
        if (Test-ScriptHasWriteApi (-join $chars)) { return [pscustomobject]@{ Verdict = 'Unknown'; Detail = 'a write API the analysis did not account for' } }
        return [pscustomobject]@{ Verdict = 'Safe'; Detail = '' }
    }
    catch {
        return [pscustomobject]@{ Verdict = 'Unknown'; Detail = "the analysis failed ($($_.Exception.GetType().Name))" }
    }
}

# Sanctioned scripts (#1368, #1380): the audit pipeline's and the test suite's own tooling legitimately
# mentions src/ and tests/ (in template guidance, search regexes, or test-case fixtures) while writing only
# under artifacts/ or its own temp workspace, so it never trips Test-ScriptHasWriteApi/Test-ScriptReferencesPath
# together on its own text. This is a narrow repository-relative path allowlist, not an analysis of what the
# script actually writes: it deliberately trades precision for being auditable in one place, so a new sanctioned
# script is an explicit addition here, never a broader heuristic.
$script:SanctionedScriptPatterns = @(
    '^\.claude/hooks/tests/Test-Hooks\.ps1$'   # the hook regression suite: the orchestrator's own verification tool, and a worker's per #1368
    '^tools/ai/audit/[^/]+\.ps1$'              # the SPEC-003 audit pipeline stage scripts, run by the orchestrator by design (#1345, #1380)
    '^tools/ai/[^/]+\.ps1$'                    # the rest of the local-AI/tooling scripts the orchestrator runs directly
    '^\.github/scripts/[^/]+\.cs$'             # CI/build scripts (changelog fragments, coverage, ...) the orchestrator runs directly
)

# Whether $Full is one of the pipeline's sanctioned scripts: it must first resolve inside $RepoRoot (the main
# checkout or the worktree the launching statement's own Base belongs to — see Get-RepoLocation), then its
# repository-relative path (forward slashes, case-insensitive) must match one of $SanctionedScriptPatterns. A
# script placed outside the repository under a name that matches one of the patterns (a fixture, a temp copy)
# never matches, since it fails the $RepoRoot containment check first.
function Test-ScriptIsSanctioned([string]$Full, [string]$RepoRoot) {
    if ([string]::IsNullOrEmpty($Full) -or [string]::IsNullOrEmpty($RepoRoot)) { return $false }
    if (-not (Test-Under $Full $RepoRoot)) { return $false }
    $relative = $Full.Substring([Math]::Min($RepoRoot.TrimEnd('\', '/').Length, $Full.Length)).TrimStart('\', '/').Replace('\', '/')
    foreach ($pattern in $script:SanctionedScriptPatterns) { if ($relative -imatch $pattern) { return $true } }
    return $false
}

# Absolute path of a target token, or $null when it cannot be computed.
function Resolve-TargetPath($Token, [string]$Base, [bool]$Bash) {
    if ($null -eq $Token -or $Token.Subexpression) { return $null }
    $value = $Token.Value
    if ($Token.Dynamic) {
        $value = Expand-KnownVariables $value $Bash $Base
        if ($null -eq $value) { return $null }
    }
    return Get-FullPath $value $Base
}

# Splits the argument text of a .NET call at top-level commas.
function Split-CallArguments([string]$Text) {
    $parts = [System.Collections.Generic.List[string]]::new()
    $depth = 0
    $start = 0
    $i = 0
    while ($i -lt $Text.Length) {
        $c = $Text[$i]
        if ($c -eq "'" -or $c -eq '"') {
            $close = $Text.IndexOf($c, $i + 1)
            $i = if ($close -lt 0) { $Text.Length } else { $close + 1 }
            continue
        }
        if ($c -in '(', '[', '{') { $depth++ }
        elseif ($c -in ')', ']', '}') { $depth-- }
        elseif ($c -eq ',' -and $depth -eq 0) { $parts.Add($Text.Substring($start, $i - $start).Trim()); $start = $i + 1 }
        $i++
    }
    $parts.Add($Text.Substring($start).Trim())
    return , $parts
}

# A .NET call argument as a token: a quoted literal without variables is literal; anything else is dynamic.
function ConvertTo-ArgumentToken([string]$Argument) {
    $m = [regex]::Match($Argument, '^(?<q>[''"])(?<p>[^''"]*)\k<q>$')
    if ($m.Success -and -not ($m.Groups['q'].Value -eq '"' -and $m.Groups['p'].Value.Contains('$'))) { return New-CommandToken $m.Groups['p'].Value $true $false }
    if ($m.Success) { return New-CommandToken $m.Groups['p'].Value $true $true }
    return New-CommandToken $Argument $false $true ($Argument -match '[(\[]')
}

# Whether a redirection operand is a stream merge (`2>&1`, `>&2`, `*>&1`, `>&-`): it writes nothing. Only
# `&<digits>` and `&-` merge; any other target after `&` (`>&file`, `&>file` in Bash) is a file (#2009).
function Test-StreamMerge([string]$Target) {
    return $Target -match '^&(\d+|-)$'
}

# The tokens of a git command without its shell redirections (#2009): `2>&1`, `2>$null`, `>&2` and the target
# of a bare `>` / `2>` / `>>` are not pathspecs or patches. Quoted tokens are arguments and stay. The write a
# redirection itself makes is judged by the redirection branch of Get-ShellWrites.
function Remove-RedirectionTokens($Tokens) {
    $kept = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt @($Tokens).Count; $i++) {
        $t = @($Tokens)[$i]
        if (-not $t.Quoted) {
            $m = [regex]::Match($t.Value, '^(\d|\*|&)?>>?(?<t>.*)$')
            if ($m.Success) {
                # A bare operator takes the next token as its target, unless it already carries a stream merge.
                if ($m.Groups['t'].Value -in '', '&' -and $i + 1 -lt @($Tokens).Count) { $i++ }
                continue
            }
        }
        $kept.Add($t)
    }
    return $kept.ToArray()
}

# The output file tokens of `git archive` (tokens after the verb at $From): `-o <file>`, `-o<file>`, `-vo <file>`,
# `--output <file>`, `--output=<file>` and git's unambiguous abbreviations of --output (`--o`, `--out`, ...).
# Scanning stops at `--` (the pathspecs). Each is a file git writes, so it is a write target (#2009).
function Get-GitArchiveOutputs($Tokens, [int]$From) {
    $outputs = [System.Collections.Generic.List[object]]::new()
    $arguments = @($Tokens | Select-Object -Skip $From)
    for ($i = 0; $i -lt $arguments.Count; $i++) {
        $a = $arguments[$i]
        if ($a.Value -eq '--') { break }
        if (-not $a.Value.StartsWith('-') -or $a.Value.Length -lt 2) { continue }
        if ($a.Value.StartsWith('--')) {
            $name = $a.Value.Substring(2)
            $inline = $null
            $eq = $name.IndexOf('=')
            if ($eq -ge 0) { $inline = $name.Substring($eq + 1); $name = $name.Substring(0, $eq) }
            if ($name.Length -ge 1 -and 'output'.StartsWith($name, [StringComparison]::Ordinal)) {
                if ($eq -ge 0) { if ($inline.Length -gt 0) { $outputs.Add((New-CommandToken $inline $a.Quoted $a.Dynamic $a.Subexpression $a.Bash)) } }
                elseif ($i + 1 -lt $arguments.Count) { $outputs.Add($arguments[$i + 1]); $i++ }
            }
            continue
        }
        # Short options: `o` takes the rest of the cluster, or the next token when it is the last letter.
        $cluster = $a.Value.Substring(1)
        $o = $cluster.IndexOf('o')
        if ($o -ge 0) {
            $rest = $cluster.Substring($o + 1)
            if ($rest.Length -gt 0) { $outputs.Add((New-CommandToken $rest $a.Quoted $a.Dynamic $a.Subexpression $a.Bash)) }
            elseif ($i + 1 -lt $arguments.Count) { $outputs.Add($arguments[$i + 1]); $i++ }
        }
    }
    return , @($outputs)
}

# The pathspecs of `git checkout` / `git restore` (tokens after the verb at $From) that write the working
# tree, as tokens; empty when the command only switches branches or only touches the index.
function Get-GitPathspecs($Tokens, [int]$From, [string]$Verb) {
    $arguments = @(Remove-RedirectionTokens @($Tokens | Select-Object -Skip $From))
    $dashDash = -1
    for ($i = 0; $i -lt $arguments.Count; $i++) { if (-not $arguments[$i].Quoted -and $arguments[$i].Value -eq '--') { $dashDash = $i; break } }
    $valueOptions = if ($Verb -eq 'restore') { @('-s', '--source', '--pathspec-from-file') } else { @('-b', '-B', '--orphan', '--conflict', '--pathspec-from-file') }
    $operands = [System.Collections.Generic.List[object]]::new()
    $flags = [System.Collections.Generic.List[string]]::new()
    $end = if ($dashDash -ge 0) { $dashDash } else { $arguments.Count }
    for ($i = 0; $i -lt $end; $i++) {
        $a = $arguments[$i]
        if (-not $a.Quoted -and $a.Value.StartsWith('-') -and $a.Value.Length -gt 1) {
            $flags.Add($a.Value)
            if ($valueOptions -ccontains $a.Value) { $i++ }
            continue
        }
        $operands.Add($a)
    }
    $after = @(if ($dashDash -ge 0) { $arguments | Select-Object -Skip ($dashDash + 1) })

    if ($Verb -eq 'restore') {
        $staged = @($flags | Where-Object { $_ -ceq '--staged' -or $_ -cmatch '^-[A-Za-z]*S' }).Count -gt 0
        $worktree = @($flags | Where-Object { $_ -ceq '--worktree' -or $_ -cmatch '^-[A-Za-z]*W' }).Count -gt 0
        if ($staged -and -not $worktree) { return , @() }
        return , @(@($operands) + @($after))
    }
    # checkout: `-- <paths>` restores paths; without `--`, `<rev> <paths>` does, `<branch>` alone switches.
    if (@($flags | Where-Object { $_ -in '-b', '-B', '--orphan' }).Count -gt 0) { return , @() }
    if ($dashDash -ge 0) { return , @($after) }
    if ($operands.Count -ge 2) { return , @($operands | Select-Object -Skip 1) }
    return , @()
}

# Returns Writes (Content, Target token, Base, Full, What, Directory), Git (Verb, Dir, Paths, Patches,
# PatchUnresolved), Mentions (Full, Explicit) of every path-like token, which the source-edit rule uses to
# tell whether a command names repository files, and Scripts (Full, Raw, Kind) for each `dotnet run <file>.cs`
# / `pwsh -File <file>.ps1` invocation found.
function Get-ShellWrites {
    param([string]$Command, [switch]$Bash, [string]$Cwd)

    $writes = [System.Collections.Generic.List[object]]::new()
    $git = [System.Collections.Generic.List[object]]::new()
    $mentions = [System.Collections.Generic.List[object]]::new()
    $scripts = [System.Collections.Generic.List[object]]::new()
    $executables = [System.Collections.Generic.List[string]]::new()
    $stack = [System.Collections.Generic.Stack[object]]::new()
    $current = $Cwd

    # Literal: the exact text the statement writes when it is a quoted or here-string literal in the command
    # (no variable, subexpression or other command feeding it), else $null. Append: the write keeps the old text.
    function Add-Write([bool]$Content, $Target, [string]$Base, [string]$What, [bool]$IsBash, [bool]$Directory = $false, $Literal = $null, [bool]$Append = $false) {
        if ($null -eq $Target) { return }
        $writes.Add([pscustomobject]@{ Content = $Content; Target = $Target; Base = $Base; Full = (Resolve-TargetPath $Target $Base $IsBash); What = $What; Directory = $Directory; Literal = $Literal; Append = $Append })
    }

    # The literal text of a value token (Literal marks one quoted segment), or $null.
    function Get-LiteralText($Token) {
        if ($null -ne $Token -and $Token.Literal) { return [string]$Token.Value }
        return $null
    }

    # The literal fed through a pipe: the statement is `'literal' | <cmd>`, the source a lone quoted literal that
    # starts its own pipeline. Anything else feeding the pipe (another command, a script block) gives $null.
    function Get-PipedLiteral($Tokens) {
        $source = $Tokens[0].PipeSource
        if ($null -eq $source -or $source.Count -ne 1) { return $null }
        if ($source[0].Literal -and $null -eq $source[0].PipeSource) { return [string]$source[0].Value }
        return $null
    }

    # The text a Set-Content / Add-Content / Out-File / Tee-Object / New-Item statement writes when it is a
    # literal: the explicit value token when there is one (a non-literal one is not a literal), else the piped literal.
    function Get-WrittenLiteral($Tokens, $ValueToken) {
        if ($null -ne $ValueToken) { return (Get-LiteralText $ValueToken) }
        return (Get-PipedLiteral $Tokens)
    }

    foreach ($tokens in (Split-CommandStatements -Text $Command -Bash:$Bash)) {
        # The shell of this statement (a wrapper's statements run in the wrapper's shell).
        $Bash = [bool]$tokens[0].Bash
        $k = Resolve-Executable $tokens
        $name = if ($k -ge 0 -and -not $tokens[$k].Dynamic) { (Get-ExecutableName $tokens[$k].Value) } else { '' }

        # PowerShell call operator (`& '<file>.ps1'`) / dot-source (`. '<file>.ps1'`): the operator is the
        # statement's first token and Resolve-Executable skips it, so $k lands on the script token (#1345).
        if (-not $Bash -and $k -gt 0 -and $k -lt $tokens.Count -and -not $tokens[0].Quoted -and ($tokens[$k].Dynamic -or $tokens[$k].Value -match '\.ps1$')) {
            $opKind = if ($tokens[0].Value -eq '&') { 'call operator' } elseif ($tokens[0].Value -eq '.') { 'dot-source' } else { $null }
            if ($null -ne $opKind) {
                # A script path that depends on a variable is launched too: it resolves when the hook knows every
                # variable in it ($env:X, $HOME), and stays unresolved (Full = $null) otherwise (#1854).
                $scriptFull = Resolve-TargetPath $tokens[$k] $current $Bash
                if ($null -eq $scriptFull -or $scriptFull -match '\.ps1$') {
                    $scripts.Add([pscustomobject]@{ Full = $scriptFull; Raw = $tokens[$k].Value; Kind = $opKind; Base = $current; Dynamic = [bool]$tokens[$k].Dynamic })
                }
            }
        }
        # The program each statement runs, for a caller that allows only statements it knows (#1854): 'script' is
        # a call-operator or dot-source launch of a .ps1, 'variable' a lone variable (its value is only output),
        # 'unknown-program' a program the hook cannot name (dynamic, a subexpression, a method call).
        $executables.Add($(
                if ($k -lt 0) { 'unknown-program' }
                elseif ($tokens[$k].Quoted -and -not ($k -gt 0 -and $tokens[0].Value -in '&', '.')) { 'variable' }
                elseif ($k -gt 0 -and -not $tokens[0].Quoted -and $tokens[0].Value -in '&', '.' -and $tokens[$k].Value -match '\.ps1$') { 'script' }
                elseif ($tokens[$k].Dynamic -or $tokens[$k].Subexpression) { if (-not $tokens[$k].Quoted -and $tokens[$k].Value -match '^\$[\w:?]+$' -and $tokens.Count -eq $k + 1) { 'variable' } else { 'unknown-program' } }
                else { $name }))

        foreach ($t in $tokens) {
            if ((-not $t.Quoted -and $t.Value.StartsWith('-')) -or $t.Value.Length -lt 2) { continue }
            $full = Resolve-TargetPath $t $current $Bash
            if ($null -eq $full) { continue }
            $explicit = [IO.Path]::IsPathRooted((ConvertTo-NativePath $t.Value)) -or $t.Value.Contains('/') -or $t.Value.Contains('\') -or $t.Value.StartsWith('~')
            if (-not $explicit) { try { $explicit = Test-Path -LiteralPath $full } catch { $explicit = $false } }
            $mentions.Add([pscustomobject]@{ Full = $full; Explicit = $explicit })
        }

        # Directory changes. A variable target makes the directory unknown.
        if ($name -in 'cd', 'set-location', 'sl', 'chdir', 'push-location', 'pushd') {
            $target = Get-BoundArgument $tokens ($k + 1) @(, @('-path', '-literalpath', '-lp', '-pspath')) 0
            if ($name -in 'push-location', 'pushd') { $stack.Push($current) }
            $current = if ($null -eq $target) { if ($Bash -and $name -eq 'cd') { Get-HomeDirectory } else { $current } } else { Resolve-TargetPath $target $current $Bash }
            continue
        }
        if ($name -in 'pop-location', 'popd') {
            $current = if ($stack.Count -gt 0) { $stack.Pop() } else { $null }
            continue
        }

        if ($name -in 'get-childitem', 'gci', 'dir', 'ls', 'get-item', 'gi') {
            $pathArgument = Get-BoundArgument $tokens ($k + 1) @(, @('-path', '-literalpath', '-lp', '-pspath')) 0
            if ($null -eq $pathArgument -and $null -ne $current) { $mentions.Add([pscustomobject]@{ Full = $current; Explicit = $true }) }
        }

        $pathAliases = @('-path', '-literalpath', '-lp', '-pspath')
        if (-not $Bash -and $name -in 'set-content', 'add-content', 'ac') {
            $valueToken = Get-BoundArgument $tokens ($k + 1) @($pathAliases, @('-value')) 1
            Add-Write $true (Get-BoundArgument $tokens ($k + 1) @($pathAliases, @('-value')) 0) $current $name $Bash $false (Get-WrittenLiteral $tokens $valueToken) ($name -in 'add-content', 'ac')
        }
        elseif (-not $Bash -and $name -in 'out-file', 'tee-object', 'tee') {
            $valueToken = Get-NamedArgument $tokens ($k + 1) @('-inputobject')
            $append = Test-NamedArgument $tokens ($k + 1) @('-append', '-appen', '-appe', '-app', '-ap')
            Add-Write $true (Get-BoundArgument $tokens ($k + 1) @(, (@('-filepath') + $pathAliases)) 0) $current $name $Bash $false (Get-WrittenLiteral $tokens $valueToken) $append
        }
        elseif (-not $Bash -and $name -in 'new-item', 'ni') {
            $target = Get-BoundArgument $tokens ($k + 1) @(, $pathAliases) 0
            $leaf = Get-BoundArgument $tokens ($k + 1) @(, @('-name')) 0
            if ($null -ne $leaf -and (Test-NamedArgument $tokens ($k + 1) @('-name'))) {
                $dir = if ($null -eq $target) { New-CommandToken '.' $false $false } else { $target }
                $target = New-CommandToken ([IO.Path]::Combine($dir.Value, $leaf.Value)) $false ($dir.Dynamic -or $leaf.Dynamic) ($dir.Subexpression -or $leaf.Subexpression)
            }
            $valueToken = Get-NamedArgument $tokens ($k + 1) @('-value')
            # -Value is the file content only for a file: for a link type it is the link target.
            # (any abbreviation of -ItemType counts, down to two letters, and -Type)
            $itemType = $null
            for ($q = $k + 1; $q -lt $tokens.Count -and $null -eq $itemType; $q++) {
                $named = [regex]::Match($tokens[$q].Value, '^-(?<n>[A-Za-z]+)(?::(?<v>.*))?$')
                if ($tokens[$q].Quoted -or -not $named.Success) { continue }
                $parameter = $named.Groups['n'].Value.ToLowerInvariant()
                if (($parameter.Length -ge 2 -and 'itemtype'.StartsWith($parameter)) -or $parameter -eq 'type') {
                    $itemType = if ($named.Groups['v'].Success) { New-CommandToken $named.Groups['v'].Value $false $false } elseif ($q + 1 -lt $tokens.Count) { $tokens[$q + 1] } else { New-CommandToken '?' $false $true }
                }
            }
            $literal = if ($null -ne $itemType -and -not (-not $itemType.Dynamic -and $itemType.Value -in 'File', 'Directory')) { $null } else { Get-LiteralText $valueToken }
            Add-Write (Test-NamedArgument $tokens ($k + 1) @('-value')) $target $current $name $Bash $false $literal $false
        }
        elseif (-not $Bash -and $name -in 'rename-item', 'ren', 'rni') {
            # The new path is the old path's folder plus -NewName; its content is a file the hook cannot see.
            $old = Get-BoundArgument $tokens ($k + 1) @($pathAliases, @('-newname')) 0
            $new = Get-BoundArgument $tokens ($k + 1) @($pathAliases, @('-newname')) 1
            if ($null -ne $old -and $null -ne $new) {
                $folder = [IO.Path]::GetDirectoryName($old.Value)
                if ($null -eq $folder) { $folder = '' }
                $renamed = New-CommandToken ([IO.Path]::Combine($folder, $new.Value)) $false ($old.Dynamic -or $new.Dynamic) ($old.Subexpression -or $new.Subexpression)
                Add-Write $false $renamed $current $name $Bash
            }
        }
        elseif (-not $Bash -and $name -in 'copy-item', 'move-item', 'cpi', 'mi', 'copy', 'move', 'cp', 'mv') {
            Add-Write $false (Get-BoundArgument $tokens ($k + 1) @($pathAliases, @('-destination')) 1) $current $name $Bash
        }
        elseif (-not $Bash -and $name -in 'invoke-webrequest', 'iwr', 'invoke-restmethod', 'irm') {
            Add-Write $true (Get-NamedArgument $tokens ($k + 1) @('-outfile')) $current $name $Bash
        }
        elseif (-not $Bash -and $name -in 'start-process', 'saps', 'start') {
            foreach ($stream in '-redirectstandardoutput', '-redirectstandarderror') {
                Add-Write $true (Get-NamedArgument $tokens ($k + 1) @($stream)) $current "$name $stream" $Bash
            }
        }
        elseif (-not $Bash -and $name -eq 'expand-archive') {
            $destination = Get-BoundArgument $tokens ($k + 1) @($pathAliases, @('-destinationpath')) 1
            if ($null -eq $destination) { $destination = New-CommandToken '.' $false $false }
            Add-Write $false $destination $current $name $Bash $true
        }
        elseif ($Bash -and $name -in 'cp', 'mv', 'touch', 'tee') {
            $operands = @($tokens | Select-Object -Skip ($k + 1) | Where-Object { $_.Quoted -or -not $_.Value.StartsWith('-') })
            if ($name -in 'touch', 'tee') { foreach ($o in $operands) { Add-Write ($name -eq 'tee') $o $current $name $Bash } }
            elseif ($operands.Count -ge 2) { Add-Write $false $operands[-1] $current $name $Bash }
        }
        elseif ($name -eq 'dotnet') {
            $rest = @($tokens | Select-Object -Skip ($k + 1))
            if ($rest.Count -gt 0 -and -not $rest[0].Quoted -and $rest[0].Value -ieq 'run') {
                $fileToken = $null
                for ($i = 1; $i -lt $rest.Count; $i++) {
                    $t = $rest[$i]
                    if (-not $t.Quoted -and $t.Value -eq '--') { break }
                    if (-not $t.Quoted -and $t.Value -in '--file', '-f') { if ($i + 1 -lt $rest.Count) { $fileToken = $rest[$i + 1] }; break }
                    if (-not $t.Quoted -and $t.Value.StartsWith('-')) { continue }
                    if ($t.Value -match '\.cs$') { $fileToken = $t; break }
                    break
                }
                if ($null -ne $fileToken) { $scripts.Add([pscustomobject]@{ Full = (Resolve-TargetPath $fileToken $current $Bash); Raw = $fileToken.Value; Kind = 'dotnet run'; Base = $current }) }
            }
        }
        elseif ($name -in 'pwsh', 'powershell', 'pwsh-preview') {
            $fileToken = Get-NamedArgument $tokens ($k + 1) @('-file')
            if ($null -ne $fileToken) {
                # #2892: -WorkingDirectory changes where the script's relative targets resolve; an unresolvable one leaves Base unknown.
                $scriptBase = $current
                $wdToken = Get-NamedArgument $tokens ($k + 1) @('-workingdirectory', '-wd')
                if ($null -ne $wdToken) { $scriptBase = Resolve-TargetPath $wdToken $current $Bash }
                $scripts.Add([pscustomobject]@{ Full = (Resolve-TargetPath $fileToken $current $Bash); Raw = $fileToken.Value; Kind = 'pwsh -File'; Base = $scriptBase })
            }
        }
        elseif ($name -eq 'git') {
            $dir = $current
            $j = $k + 1
            while ($j -lt $tokens.Count -and -not $tokens[$j].Quoted -and $tokens[$j].Value.StartsWith('-')) {
                $opt = $tokens[$j].Value
                if ($opt -ceq '-C' -and $j + 1 -lt $tokens.Count) { $dir = Resolve-TargetPath $tokens[$j + 1] $dir $Bash; $j += 2; continue }
                if ($opt -ceq '-c' -or $opt -in '--git-dir', '--work-tree', '--namespace', '--exec-path') { $j += 2; continue }
                $j++
            }
            $verb = if ($j -lt $tokens.Count) { $tokens[$j].Value } else { $null }
            # `git archive --output <file>` writes the file even though archive does not change the working tree.
            if ($verb -ceq 'archive') {
                foreach ($output in (Get-GitArchiveOutputs $tokens ($j + 1))) { Add-Write $true $output $dir 'git archive --output' $Bash }
            }
            $readOnlyStash = $verb -eq 'stash' -and $j + 1 -lt $tokens.Count -and $tokens[$j + 1].Value -in 'list', 'show'
            if ($script:GitMutatingVerbs -ccontains $verb -and -not $readOnlyStash) {
                $paths = [System.Collections.Generic.List[string]]::new()
                $patches = [System.Collections.Generic.List[string]]::new()
                $patchUnresolved = $false
                if ($verb -in 'checkout', 'restore') {
                    foreach ($p in (Get-GitPathspecs $tokens ($j + 1) $verb)) {
                        $full = Resolve-TargetPath $p $dir $Bash
                        # A pathspec that cannot be resolved (a variable, a :(magic) pathspec) counts as the whole checkout.
                        if ($null -eq $full -or $p.Value.StartsWith(':')) { $full = $dir }
                        if ($null -ne $full) { $paths.Add($full) }
                    }
                }
                elseif ($verb -in 'apply', 'am') {
                    foreach ($a in @(Remove-RedirectionTokens @($tokens | Select-Object -Skip ($j + 1)))) {
                        if (-not $a.Quoted -and $a.Value.StartsWith('-')) { continue }
                        $full = Resolve-TargetPath $a $dir $Bash
                        $isFile = $false
                        if ($null -ne $full) { try { $isFile = Test-Path -LiteralPath $full -PathType Leaf } catch { } }
                        if ($isFile) { $patches.Add($full) } else { $patchUnresolved = $true }
                    }
                    if ($patches.Count -eq 0) { $patchUnresolved = $true }
                }
                $git.Add([pscustomobject]@{ Verb = $verb; Dir = $dir; Paths = $paths; Patches = $patches; PatchUnresolved = $patchUnresolved })
            }
        }

        foreach ($t in $tokens) {
            if ($t.Quoted) { continue }
            # [IO.File]::WriteAllText('path', ...): the tokenizer keeps the call's argument text in the token.
            $io = [regex]::Match($t.Value, '^\[(System\.)?IO\.(?<type>File|StreamWriter)\]::(?<method>\w+)\s*(?<args>.*)$', 'IgnoreCase')
            if ($io.Success) {
                $method = $io.Groups['method'].Value
                $arguments = Split-CallArguments $io.Groups['args'].Value
                $index = -1
                if ($io.Groups['type'].Value -ieq 'StreamWriter') { if ($method -ieq 'new') { $index = 0 } }
                elseif ($method -match '^(Write|Append|Create|OpenWrite)') { $index = 0 }
                elseif ($method -in 'Copy', 'Move', 'Replace') { $index = 1 }
                if ($index -ge 0 -and $index -lt $arguments.Count) { Add-Write ($method -notin 'Copy', 'Move', 'Replace') (ConvertTo-ArgumentToken $arguments[$index]) $Cwd "[IO.$($io.Groups['type'].Value)]::$method" $Bash }
                continue
            }
            # > file, >> file, 2> file, *> file, >&file and &>file (Bash); not 2>&1 / >&2 / >&- (stream merges),
            # > $null, > /dev/null, > nul (#2009: only `&<digits>` and `&-` are merges, `>&file` writes `file`).
            $redirect = [regex]::Match($t.Value, '^(\d|\*|&)?>>?(?<t>.*)$')
            if ($redirect.Success) {
                $value = $redirect.Groups['t'].Value
                # `>&file` attached, or `>& file` / `&> file` with the file in the next token.
                if ($value.StartsWith('&') -and -not (Test-StreamMerge $value)) { $value = $value.Substring(1) }
                $target = if ($value -and -not (Test-StreamMerge $value)) { New-CommandToken $value $false $t.Dynamic $t.Subexpression } elseif (Test-StreamMerge $redirect.Groups['t'].Value) { $null } else {
                    $index = $tokens.IndexOf($t)
                    # `2>& 1` (Bash, with a space): the next token is the descriptor, not a file.
                    if ($index + 1 -lt $tokens.Count -and -not ($redirect.Groups['t'].Value -eq '&' -and (Test-StreamMerge ('&' + $tokens[$index + 1].Value)))) { $tokens[$index + 1] } else { $null }
                }
                if ($null -ne $target -and $target.Value -notin '$null', '/dev/null', 'nul') {
                    # A literal only for `'literal' > file`: the lone quoted token starts the statement, the
                    # redirect is the success stream (no 2> or *>) and nothing else is on the statement.
                    $redirectIndex = $tokens.IndexOf($t)
                    $statementLength = if ($value) { 2 } else { 3 }
                    $literal = $null
                    if (-not $Bash -and $redirectIndex -eq 1 -and $tokens.Count -eq $statementLength -and $null -eq $tokens[0].PipeSource -and $redirect.Groups[1].Value -in '', '1') { $literal = Get-LiteralText $tokens[0] }
                    Add-Write $true $target $current 'redirection' $Bash $false $literal ($t.Value -match '^(\d|\*)?>>')
                }
            }
        }
    }

    return [pscustomobject]@{ Writes = $writes; Git = $git; Mentions = $mentions; Scripts = $scripts; Executables = $executables; FinalDirectory = $current }
}
