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
# `pwsh -File` case below), Remove-Item / rm (deletions), Rename-Item, and redirections attached to a word
# (x>f).
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

# The pathspecs of `git checkout` / `git restore` (tokens after the verb at $From) that write the working
# tree, as tokens; empty when the command only switches branches or only touches the index.
function Get-GitPathspecs($Tokens, [int]$From, [string]$Verb) {
    $arguments = @($Tokens | Select-Object -Skip $From)
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
    $stack = [System.Collections.Generic.Stack[object]]::new()
    $current = $Cwd

    function Add-Write([bool]$Content, $Target, [string]$Base, [string]$What, [bool]$IsBash, [bool]$Directory = $false) {
        if ($null -eq $Target) { return }
        $writes.Add([pscustomobject]@{ Content = $Content; Target = $Target; Base = $Base; Full = (Resolve-TargetPath $Target $Base $IsBash); What = $What; Directory = $Directory })
    }

    foreach ($tokens in (Split-CommandStatements -Text $Command -Bash:$Bash)) {
        # The shell of this statement (a wrapper's statements run in the wrapper's shell).
        $Bash = [bool]$tokens[0].Bash
        $k = Resolve-Executable $tokens
        $name = if ($k -ge 0 -and -not $tokens[$k].Dynamic) { (Get-ExecutableName $tokens[$k].Value) } else { '' }

        # PowerShell call operator (`& '<file>.ps1'`) / dot-source (`. '<file>.ps1'`): the operator is the
        # statement's first token and Resolve-Executable skips it, so $k lands on the script token (#1345).
        if (-not $Bash -and $k -gt 0 -and $k -lt $tokens.Count -and -not $tokens[0].Quoted -and -not $tokens[$k].Dynamic -and $tokens[$k].Value -match '\.ps1$') {
            $opKind = if ($tokens[0].Value -eq '&') { 'call operator' } elseif ($tokens[0].Value -eq '.') { 'dot-source' } else { $null }
            if ($null -ne $opKind) {
                $scripts.Add([pscustomobject]@{ Full = (Resolve-TargetPath $tokens[$k] $current $Bash); Raw = $tokens[$k].Value; Kind = $opKind; Base = $current })
            }
        }

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
            Add-Write $true (Get-BoundArgument $tokens ($k + 1) @($pathAliases, @('-value')) 0) $current $name $Bash
        }
        elseif (-not $Bash -and $name -in 'out-file', 'tee-object', 'tee') {
            Add-Write $true (Get-BoundArgument $tokens ($k + 1) @(, (@('-filepath') + $pathAliases)) 0) $current $name $Bash
        }
        elseif (-not $Bash -and $name -in 'new-item', 'ni') {
            $target = Get-BoundArgument $tokens ($k + 1) @(, $pathAliases) 0
            $leaf = Get-BoundArgument $tokens ($k + 1) @(, @('-name')) 0
            if ($null -ne $leaf -and (Test-NamedArgument $tokens ($k + 1) @('-name'))) {
                $dir = if ($null -eq $target) { New-CommandToken '.' $false $false } else { $target }
                $target = New-CommandToken ([IO.Path]::Combine($dir.Value, $leaf.Value)) $false ($dir.Dynamic -or $leaf.Dynamic) ($dir.Subexpression -or $leaf.Subexpression)
            }
            Add-Write (Test-NamedArgument $tokens ($k + 1) @('-value')) $target $current $name $Bash
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
            if ($null -ne $fileToken) { $scripts.Add([pscustomobject]@{ Full = (Resolve-TargetPath $fileToken $current $Bash); Raw = $fileToken.Value; Kind = 'pwsh -File'; Base = $current }) }
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
                    foreach ($a in @($tokens | Select-Object -Skip ($j + 1))) {
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
            # > file, >> file, 2> file, *> file (not 2>&1, > $null, > /dev/null, > nul).
            $redirect = [regex]::Match($t.Value, '^(\d|\*)?>>?(?<t>.*)$')
            if ($redirect.Success) {
                $value = $redirect.Groups['t'].Value
                $target = if ($value) { New-CommandToken $value $false $t.Dynamic $t.Subexpression } else {
                    $index = $tokens.IndexOf($t)
                    if ($index + 1 -lt $tokens.Count) { $tokens[$index + 1] } else { $null }
                }
                if ($null -ne $target -and -not $target.Value.StartsWith('&') -and $target.Value -notin '$null', '/dev/null', 'nul') {
                    Add-Write $true $target $current 'redirection' $Bash
                }
            }
        }
    }

    return [pscustomobject]@{ Writes = $writes; Git = $git; Mentions = $mentions; Scripts = $scripts; FinalDirectory = $current }
}
