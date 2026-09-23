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
# A target that uses only $env:NAME, $HOME, $PWD (PowerShell) or $NAME (Bash, from the environment) is
# resolved; a target that depends on any other variable or on a subexpression stays unresolved (Full = $null).
#
# Not seen: writes by programs that the analysis does not know (dotnet, git apply outside the git verbs
# listed, scripts), Remove-Item / rm (deletions), Rename-Item, and redirections attached to a word (x>f).

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

# Returns Writes (Content, Target token, Base, Full, What), Git (Verb, Dir) and Mentions (Full, Explicit) of
# every path-like token, which the source-edit rule uses to tell whether a command names repository files.
function Get-ShellWrites {
    param([string]$Command, [switch]$Bash, [string]$Cwd)

    $writes = [System.Collections.Generic.List[object]]::new()
    $git = [System.Collections.Generic.List[object]]::new()
    $mentions = [System.Collections.Generic.List[object]]::new()
    $stack = [System.Collections.Generic.Stack[object]]::new()
    $current = $Cwd

    function Add-Write([bool]$Content, $Target, [string]$Base, [string]$What) {
        if ($null -eq $Target) { return }
        $writes.Add([pscustomobject]@{ Content = $Content; Target = $Target; Base = $Base; Full = (Resolve-TargetPath $Target $Base $Bash); What = $What })
    }

    foreach ($tokens in (Split-CommandStatements -Text $Command -Bash:$Bash)) {
        $k = Resolve-Executable $tokens
        $name = if ($k -ge 0 -and -not $tokens[$k].Dynamic) { (Get-ExecutableName $tokens[$k].Value) } else { '' }

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
            Add-Write $true (Get-BoundArgument $tokens ($k + 1) @($pathAliases, @('-value')) 0) $current $name
        }
        elseif (-not $Bash -and $name -in 'out-file', 'tee-object', 'tee') {
            Add-Write $true (Get-BoundArgument $tokens ($k + 1) @(, (@('-filepath') + $pathAliases)) 0) $current $name
        }
        elseif (-not $Bash -and $name -in 'new-item', 'ni') {
            $target = Get-BoundArgument $tokens ($k + 1) @(, $pathAliases) 0
            $leaf = Get-BoundArgument $tokens ($k + 1) @(, @('-name')) 0
            if ($null -ne $leaf -and (Test-NamedArgument $tokens ($k + 1) @('-name'))) {
                $dir = if ($null -eq $target) { New-CommandToken '.' $false $false } else { $target }
                $target = New-CommandToken ([IO.Path]::Combine($dir.Value, $leaf.Value)) $false ($dir.Dynamic -or $leaf.Dynamic) ($dir.Subexpression -or $leaf.Subexpression)
            }
            Add-Write (Test-NamedArgument $tokens ($k + 1) @('-value')) $target $current $name
        }
        elseif (-not $Bash -and $name -in 'copy-item', 'move-item', 'cpi', 'mi', 'copy', 'move', 'cp', 'mv') {
            Add-Write $false (Get-BoundArgument $tokens ($k + 1) @($pathAliases, @('-destination')) 1) $current $name
        }
        elseif ($Bash -and $name -in 'cp', 'mv', 'touch', 'tee') {
            $operands = @($tokens | Select-Object -Skip ($k + 1) | Where-Object { $_.Quoted -or -not $_.Value.StartsWith('-') })
            if ($name -in 'touch', 'tee') { foreach ($o in $operands) { Add-Write ($name -eq 'tee') $o $current $name } }
            elseif ($operands.Count -ge 2) { Add-Write $false $operands[-1] $current $name }
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
            $readOnlyStash = $verb -eq 'stash' -and $j + 1 -lt $tokens.Count -and $tokens[$j + 1].Value -in 'list', 'show'
            if ($script:GitMutatingVerbs -ccontains $verb -and -not $readOnlyStash) { $git.Add([pscustomobject]@{ Verb = $verb; Dir = $dir }) }
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
                if ($index -ge 0 -and $index -lt $arguments.Count) { Add-Write ($method -notin 'Copy', 'Move', 'Replace') (ConvertTo-ArgumentToken $arguments[$index]) $Cwd "[IO.$($io.Groups['type'].Value)]::$method" }
                continue
            }
            # > file, >> file, 2> file, *> file (not 2>&1, > $null, > /dev/null, > nul).
            $redirect = [regex]::Match($t.Value, '^(\d|\*)?>>?(?<t>.*)$')
            if ($redirect.Success) {
                $value = $redirect.Groups['t'].Value
                $target = if ($value) { New-CommandToken $value $false $t.Dynamic $t.Subexpression } else {
                    $index = $tokens.IndexOf($t)
                    if ($index + 1 -lt $tokens.Count) { $tokens[$index + 1] } else { $null }
                }
                if ($null -ne $target -and -not $target.Value.StartsWith('&') -and $target.Value -notin '$null', '/dev/null', 'nul') {
                    Add-Write $true $target $current 'redirection'
                }
            }
        }
    }

    return [pscustomobject]@{ Writes = $writes; Git = $git; Mentions = $mentions; FinalDirectory = $current }
}
