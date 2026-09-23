# Shared by the PreToolUse hooks: splits a Bash or PowerShell command line into statements and tokens, so a
# hook inspects the arguments of the `git` / `gh` call itself instead of the whole command text.
#
# Understood: single and double quotes (PowerShell backtick and doubled-quote escapes; backslash escapes for
# Bash), backtick escapes and line continuations outside quotes, PowerShell here-strings (@" "@, @' '@),
# $( ... ) and ( ... ) subexpressions (their content is parsed as statements too), Bash heredocs inside
# $( ... ), comments (`# ...` at the start of a word, PowerShell `<# ... #>`; removed before anything else),
# and the separators ; | || && newline { }. A token that depends on a variable or a subexpression is
# marked Dynamic: its Value holds the literal text, which may still contain the words the hooks look for.
# A token that contains a subexpression is also marked Subexpression: its value cannot be computed at all,
# while a token that only uses variables may still be resolved by a hook that knows them ($env:X, $HOME).
# Bash ANSI-C strings ($'a\nb') are decoded into a quoted, literal token.
#
# Shell wrappers are analysed too (#1181): the command text given to `pwsh` / `powershell` with -Command (-c,
# any unambiguous prefix, -CommandWithArgs / -cwa, or the first positional argument of powershell.exe) and the
# script given to `bash` / `sh` / `zsh` / `dash` with -c (alone or in a cluster such as -lc) is parsed as
# statements of that shell and inserted right after the wrapper statement. Every token carries Bash, the
# shell of its own statement, which may differ from the tool's shell. `pwsh -EncodedCommand` (-e, -ec, -en...)
# cannot be read; Get-WrappedCommand reports it as Encoded so a hook can block it. `pwsh -File` and a Bash
# script file are not read.

function New-CommandToken([string]$Value, [bool]$Quoted, [bool]$Dynamic, [bool]$Subexpression = $false, [bool]$Bash = $false) {
    [pscustomobject]@{ Value = $Value; Quoted = $Quoted; Dynamic = $Dynamic; Subexpression = $Subexpression; Bash = $Bash }
}

# Decodes the Bash ANSI-C string that starts with $' at $Start. Returns Value and End (the index past the
# closing quote).
function Read-AnsiCString([string]$Text, [int]$Start) {
    $sb = [System.Text.StringBuilder]::new()
    $n = $Text.Length
    $j = $Start + 2
    while ($j -lt $n -and $Text[$j] -ne "'") {
        if ($Text[$j] -ne '\' -or $j + 1 -ge $n) { [void]$sb.Append($Text[$j]); $j++; continue }
        $e = $Text[$j + 1]
        $simple = @{ [char]'n' = "`n"; [char]'t' = "`t"; [char]'r' = "`r"; [char]'a' = [string][char]7; [char]'b' = [string][char]8; [char]'e' = [string][char]27; [char]'E' = [string][char]27; [char]'f' = [string][char]12; [char]'v' = [string][char]11; [char]'\' = '\'; [char]"'" = "'"; [char]'"' = '"'; [char]'?' = '?' }
        if ($simple.ContainsKey($e)) { [void]$sb.Append($simple[$e]); $j += 2; continue }
        $hex = [regex]::Match($Text.Substring($j), '^\\(?:x(?<h>[0-9A-Fa-f]{1,2})|u(?<h>[0-9A-Fa-f]{1,4})|U(?<h>[0-9A-Fa-f]{1,8})|(?<o>[0-7]{1,3}))')
        if ($hex.Success) {
            $code = if ($hex.Groups['o'].Success) { [Convert]::ToInt32($hex.Groups['o'].Value, 8) } else { [Convert]::ToInt32($hex.Groups['h'].Value, 16) }
            try { [void]$sb.Append([char]::ConvertFromUtf32($code)) } catch { }
            $j += $hex.Length
            continue
        }
        [void]$sb.Append('\').Append($e)
        $j += 2
    }
    return [pscustomobject]@{ Value = $sb.ToString(); End = [Math]::Min($j + 1, $n) }
}

# Removes comments outside quotes, here-strings and heredocs: `#` at the start of a word up to the end of the
# line (both shells; not Bash `${#x}` or `$#`), and PowerShell `<# ... #>` blocks. A `#` inside a word
# (`a#b`) or inside quotes is kept. The text of a $( ... ) inside double quotes is copied as is.
function Remove-CommandComments {
    param([string]$Text, [switch]$Bash)

    $sb = [System.Text.StringBuilder]::new()
    $n = $Text.Length
    $i = 0
    while ($i -lt $n) {
        $c = $Text[$i]

        if (-not $Bash -and $c -eq '@' -and $i + 2 -lt $n -and $Text[$i + 1] -in '"', "'" -and $Text[$i + 2] -in "`r", "`n") {
            $close = [regex]::Match($Text.Substring($i + 2), "(?m)^$([regex]::Escape("$($Text[$i + 1])@"))")
            $end = if ($close.Success) { $i + 2 + $close.Index + 2 } else { $n }
            [void]$sb.Append($Text, $i, $end - $i)
            $i = $end
            continue
        }

        if ($Bash -and $c -eq '<' -and ($i -eq 0 -or $Text[$i - 1] -ne '<')) {
            $heredoc = [regex]::Match($Text.Substring($i), '^<<-?\s*[''"]?(?<d>[A-Za-z_][A-Za-z0-9_]*)[''"]?')
            if ($heredoc.Success) {
                $delimiter = [regex]::Match($Text.Substring($i + $heredoc.Length), "(?m)^\s*$([regex]::Escape($heredoc.Groups['d'].Value))\s*$")
                $end = if ($delimiter.Success) { $i + $heredoc.Length + $delimiter.Index + $delimiter.Length } else { $n }
                [void]$sb.Append($Text, $i, $end - $i)
                $i = $end
                continue
            }
        }

        if ($Bash -and $c -eq '$' -and $i + 1 -lt $n -and $Text[$i + 1] -eq "'") {
            $end = (Read-AnsiCString $Text $i).End
            [void]$sb.Append($Text, $i, $end - $i)
            $i = $end
            continue
        }

        if ($c -eq "'") {
            $close = $Text.IndexOf("'", $i + 1)
            $end = if ($close -lt 0) { $n } else { $close + 1 }
            [void]$sb.Append($Text, $i, $end - $i)
            $i = $end
            continue
        }

        if ($c -eq '"') {
            $j = $i + 1
            while ($j -lt $n -and $Text[$j] -ne '"') {
                if (($Text[$j] -eq '`' -and -not $Bash) -or ($Text[$j] -eq '\' -and $Bash)) { $j += 2; continue }
                if ($Text[$j] -eq '$' -and $j + 1 -lt $n -and $Text[$j + 1] -eq '(') {
                    $j = Skip-Subexpression $Text ($j + 1) ([System.Text.StringBuilder]::new())
                    continue
                }
                $j++
            }
            $end = [Math]::Min($j + 1, $n)
            [void]$sb.Append($Text, $i, $end - $i)
            $i = $end
            continue
        }

        if ((($c -eq '`' -and -not $Bash) -or ($c -eq '\' -and $Bash)) -and $i + 1 -lt $n) {
            [void]$sb.Append($Text, $i, 2)
            $i += 2
            continue
        }

        if (-not $Bash -and $c -eq '<' -and $i + 1 -lt $n -and $Text[$i + 1] -eq '#') {
            $close = $Text.IndexOf('#>', $i + 2)
            $i = if ($close -lt 0) { $n } else { $close + 2 }
            [void]$sb.Append(' ')
            continue
        }

        if ($c -eq '#') {
            $prev = if ($i -gt 0) { $Text[$i - 1] } else { ' ' }
            $wordStart = $prev -in ' ', "`t", "`r", "`n", ';', '|', '&', '(', ')', '{', '}'
            if ($prev -eq '{' -and $i -ge 2 -and $Text[$i - 2] -eq '$') { $wordStart = $false }
            if ($wordStart) {
                while ($i -lt $n -and $Text[$i] -notin "`r", "`n") { $i++ }
                continue
            }
        }

        [void]$sb.Append($c)
        $i++
    }
    return $sb.ToString()
}

# Returns the index just past the ')' that closes the '(' at $Open, appending the text in between to $Sb.
function Skip-Subexpression([string]$Text, [int]$Open, [System.Text.StringBuilder]$Sb) {
    $n = $Text.Length
    $depth = 1
    $i = $Open + 1
    while ($i -lt $n -and $depth -gt 0) {
        $c = $Text[$i]
        $heredoc = [regex]::Match($Text.Substring($i), '^<<-?\s*[''"]?(?<d>[A-Za-z_][A-Za-z0-9_]*)[''"]?')
        if ($heredoc.Success) {
            $delimiter = [regex]::Match($Text.Substring($i), "(?m)^\s*$([regex]::Escape($heredoc.Groups['d'].Value))\s*$")
            if ($delimiter.Success -and $delimiter.Index -gt 0) {
                [void]$Sb.Append($Text, $i, $delimiter.Index + $delimiter.Length)
                $i += $delimiter.Index + $delimiter.Length
                continue
            }
        }
        if ($c -eq '(') { $depth++ }
        elseif ($c -eq ')') { $depth--; if ($depth -eq 0) { break } }
        elseif ($c -eq "'" -or $c -eq '"') {
            $close = $Text.IndexOf($c, $i + 1)
            if ($close -lt 0) { $close = $n - 1 }
            [void]$Sb.Append($Text, $i, $close - $i + 1)
            $i = $close + 1
            continue
        }
        [void]$Sb.Append($c)
        $i++
    }
    return [Math]::Min($i + 1, $n)
}

function Test-LineContinuation([string]$Text, [int]$At, [bool]$Bash) {
    $escape = if ($Bash) { '\' } else { '`' }
    return $Text[$At] -eq $escape -and $At + 1 -lt $Text.Length -and $Text[$At + 1] -in "`r", "`n"
}

function Split-CommandStatements {
    param([string]$Text, [switch]$Bash, [int]$Depth = 0)

    if ($Depth -eq 0) { $Text = Remove-CommandComments -Text $Text -Bash:$Bash }
    $statements = [System.Collections.Generic.List[object]]::new()
    $current = [System.Collections.Generic.List[object]]::new()
    $inner = [System.Collections.Generic.List[string]]::new()
    $n = $Text.Length
    $i = 0

    while ($i -lt $n) {
        $c = $Text[$i]
        if ($c -eq ' ' -or $c -eq "`t") { $i++; continue }
        if (Test-LineContinuation $Text $i $Bash) {
            $i += 2
            if ($i -lt $n -and $Text[$i - 1] -eq "`r" -and $Text[$i] -eq "`n") { $i++ }
            continue
        }

        $sep = 0
        if (($c -eq '&' -or $c -eq '|') -and $i + 1 -lt $n -and $Text[$i + 1] -eq $c) { $sep = 2 }
        elseif ($c -in ';', "`n", "`r", '|', '{', '}') { $sep = 1 }
        if ($sep -gt 0) {
            if ($current.Count -gt 0) { $statements.Add($current); $current = [System.Collections.Generic.List[object]]::new() }
            $i += $sep
            continue
        }

        $sb = [System.Text.StringBuilder]::new()
        $quoted = $false
        $dynamic = $false
        $subexpression = $false
        while ($i -lt $n) {
            $c = $Text[$i]
            if ($c -in ' ', "`t", ';', "`n", "`r", '|', '{', '}') { break }
            if ($c -eq '&' -and $i + 1 -lt $n -and $Text[$i + 1] -eq '&') { break }
            if (Test-LineContinuation $Text $i $Bash) { break }

            # PowerShell here-string: @" or @' followed by a newline, closed by "@ or '@ at the start of a line.
            if ($c -eq '@' -and $i + 2 -lt $n -and $Text[$i + 1] -in '"', "'" -and $Text[$i + 2] -in "`r", "`n") {
                $q = $Text[$i + 1]
                $bodyStart = $i + 2
                if ($Text[$bodyStart] -eq "`r" -and $bodyStart + 1 -lt $n -and $Text[$bodyStart + 1] -eq "`n") { $bodyStart++ }
                $bodyStart++
                $close = [regex]::Match($Text.Substring($bodyStart), "(?m)^$([regex]::Escape("$q@"))")
                $end = if ($close.Success) { $bodyStart + $close.Index } else { $n }
                [void]$sb.Append($Text, $bodyStart, $end - $bodyStart)
                $quoted = $true
                if ($q -eq '"' -and $sb.ToString() -match '\$[\w{(]') { $dynamic = $true }
                if ($q -eq '"' -and $sb.ToString() -match '\$\(') { $subexpression = $true }
                $i = if ($close.Success) { $end + 2 } else { $n }
                continue
            }

            # Bash ANSI-C string: $'...' with backslash escapes, literal.
            if ($Bash -and $c -eq '$' -and $i + 1 -lt $n -and $Text[$i + 1] -eq "'") {
                $ansi = Read-AnsiCString $Text $i
                [void]$sb.Append($ansi.Value)
                $quoted = $true
                $i = $ansi.End
                continue
            }

            if ($c -eq "'") {
                $j = $i + 1
                while ($j -lt $n) {
                    if ($Text[$j] -eq "'") {
                        if (-not $Bash -and $j + 1 -lt $n -and $Text[$j + 1] -eq "'") { [void]$sb.Append("'"); $j += 2; continue }
                        break
                    }
                    [void]$sb.Append($Text[$j]); $j++
                }
                $quoted = $true
                $i = $j + 1
                continue
            }

            if ($c -eq '"') {
                $j = $i + 1
                while ($j -lt $n) {
                    $d = $Text[$j]
                    if ($d -eq '"') {
                        if (-not $Bash -and $j + 1 -lt $n -and $Text[$j + 1] -eq '"') { [void]$sb.Append('"'); $j += 2; continue }
                        break
                    }
                    if (($d -eq '`' -and -not $Bash) -or ($d -eq '\' -and $Bash)) {
                        if ($j + 1 -lt $n) { [void]$sb.Append($Text[$j + 1]) }
                        $j += 2
                        continue
                    }
                    if ($d -eq '$' -and $j + 1 -lt $n -and $Text[$j + 1] -eq '(') {
                        $dynamic = $true
                        $subexpression = $true
                        $before = $sb.Length
                        $j = Skip-Subexpression $Text ($j + 1) $sb
                        $inner.Add($sb.ToString($before, $sb.Length - $before))
                        continue
                    }
                    if ($d -eq '$' -and $j + 1 -lt $n -and $Text[$j + 1] -match '[\w{]') { $dynamic = $true }
                    [void]$sb.Append($d); $j++
                }
                $quoted = $true
                $i = $j + 1
                continue
            }

            if ($c -eq '(' -or ($c -eq '$' -and $i + 1 -lt $n -and $Text[$i + 1] -eq '(')) {
                $dynamic = $true
                $subexpression = $true
                $open = if ($c -eq '$') { $i + 1 } else { $i }
                $before = $sb.Length
                $i = Skip-Subexpression $Text $open $sb
                $inner.Add($sb.ToString($before, $sb.Length - $before))
                continue
            }

            # Escapes outside quotes: PowerShell backtick, Bash backslash.
            if ((($c -eq '`' -and -not $Bash) -or ($c -eq '\' -and $Bash)) -and $i + 1 -lt $n) {
                [void]$sb.Append($Text[$i + 1]); $i += 2; continue
            }

            if ($c -eq '$') { $dynamic = $true }
            [void]$sb.Append($c)
            $i++
        }
        $current.Add((New-CommandToken $sb.ToString() $quoted $dynamic $subexpression $Bash.IsPresent))
    }

    if ($current.Count -gt 0) { $statements.Add($current) }

    # Commands given to a shell wrapper run too: `pwsh -Command "..."`, `bash -c '...'`. Their statements
    # follow the wrapper statement, so a directory change before the wrapper applies to them.
    if ($Depth -lt 4) {
        $expanded = [System.Collections.Generic.List[object]]::new()
        foreach ($s in $statements) {
            $expanded.Add($s)
            $wrapped = Get-WrappedCommand $s
            if ($null -eq $wrapped -or $wrapped.Encoded -or [string]::IsNullOrWhiteSpace($wrapped.Text)) { continue }
            $innerText = Remove-CommandComments -Text $wrapped.Text -Bash:$wrapped.Bash
            foreach ($x in (Split-CommandStatements -Text $innerText -Bash:$wrapped.Bash -Depth ($Depth + 1))) { $expanded.Add($x) }
        }
        $statements = $expanded
    }

    # Commands inside subexpressions run too: `$r = (gh issue create ...)`, `"$(git commit ...)"`.
    if ($Depth -lt 4) {
        foreach ($text in $inner) {
            foreach ($s in (Split-CommandStatements -Text $text -Bash:$Bash -Depth ($Depth + 1))) { $statements.Add($s) }
        }
    }
    return , $statements
}

# Index of the token that names the program a statement runs, skipping the PowerShell call and dot-source
# operators, `$var =` assignments, Bash `NAME=value` prefixes and wrappers such as `sudo` or `env`.
# Returns -1 for a PowerShell hashtable entry (`@{ head = 1 }` splits into the statement `head = 1`): a bare
# word followed by `=` is a key, not a program.
function Resolve-Executable($Tokens) {
    $k = 0
    while ($k -lt $Tokens.Count) {
        $t = $Tokens[$k]
        if (-not $t.Quoted -and $t.Value -in '&', '.') { $k++; continue }
        if (-not $t.Quoted -and $t.Value -match '^\$[\w:]+$' -and $k + 1 -lt $Tokens.Count -and $Tokens[$k + 1].Value -eq '=') { $k += 2; continue }
        if (-not $t.Quoted -and $t.Value -match '^[A-Za-z_][A-Za-z0-9_]*=') { $k++; continue }
        if (-not $t.Quoted -and $t.Value -in 'sudo', 'time', 'env', 'exec', 'command', 'nohup') { $k++; continue }
        if ($k + 1 -lt $Tokens.Count -and -not $Tokens[$k + 1].Quoted -and $Tokens[$k + 1].Value -eq '=') { return -1 }
        return $k
    }
    return -1
}

# `git`, `C:\Program Files\Git\cmd\git.exe`, "gh" -> git / gh.
function Get-ExecutableName([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return '' }
    try { return [IO.Path]::GetFileNameWithoutExtension($Value.Replace('\', '/').Split('/')[-1]).ToLowerInvariant() } catch { return '' }
}

function Test-ParameterPrefix([string]$Name, [string]$Full, [int]$MinLength) {
    return $Name.Length -ge $MinLength -and $Full.StartsWith($Name)
}

# The command a shell wrapper runs: Text (the command or script text), Bash (its shell) and Encoded (a
# `pwsh -EncodedCommand` whose text cannot be read). $null when the statement is not a wrapper, runs a script
# file (`pwsh -File x.ps1`, `bash x.sh`) or reads the command from stdin (`pwsh -Command -`).
function Get-WrappedCommand($Tokens) {
    $k = Resolve-Executable $Tokens
    if ($k -lt 0 -or $Tokens[$k].Dynamic) { return $null }
    $name = Get-ExecutableName $Tokens[$k].Value

    if ($name -in 'pwsh', 'powershell', 'pwsh-preview') {
        $valueOptions = @('executionpolicy', 'workingdirectory', 'outputformat', 'inputformat', 'windowstyle', 'configurationname', 'configurationfile', 'settingsfile', 'custompipename')
        for ($j = $k + 1; $j -lt $Tokens.Count; $j++) {
            $t = $Tokens[$j]
            $m = [regex]::Match($t.Value, '^(?:--?|/)(?<n>[A-Za-z]+)(?::(?<inline>.*))?$')
            if ($t.Quoted -or -not $m.Success) {
                # powershell.exe runs its first positional argument as a command; pwsh runs it as a script file.
                if ($name -ne 'powershell' -or $t.Value -eq '-') { return $null }
                return [pscustomobject]@{ Text = (@($Tokens | Select-Object -Skip $j | ForEach-Object { $_.Value }) -join ' '); Bash = $false; Encoded = $false }
            }
            $n = $m.Groups['n'].Value.ToLowerInvariant()
            $inline = if ($m.Groups['inline'].Success) { $m.Groups['inline'].Value } else { $null }
            if ($n -in 'e', 'ec' -or (Test-ParameterPrefix $n 'encodedcommand' 2)) { return [pscustomobject]@{ Text = $null; Bash = $false; Encoded = $true } }
            if ($n -eq 'cwa' -or (Test-ParameterPrefix $n 'commandwithargs' 8)) {
                $text = if ($null -ne $inline) { $inline } elseif ($j + 1 -lt $Tokens.Count) { $Tokens[$j + 1].Value } else { $null }
                if ($text -eq '-') { return $null }
                return [pscustomobject]@{ Text = $text; Bash = $false; Encoded = $false }
            }
            if ($n -eq 'c' -or (Test-ParameterPrefix $n 'command' 2)) {
                $rest = @($Tokens | Select-Object -Skip ($j + 1) | ForEach-Object { $_.Value })
                if ($null -ne $inline) { $rest = @($inline) + $rest }
                if ($rest.Count -eq 0 -or $rest[0] -eq '-') { return $null }
                return [pscustomobject]@{ Text = ($rest -join ' '); Bash = $false; Encoded = $false }
            }
            if ($n -eq 'f' -or (Test-ParameterPrefix $n 'file' 2)) { return $null }
            $takesValue = $n -in 'ex', 'ep', 'wd', 'o', 'of', 'if', 'inp', 'w', 'config', 'settings' -or @($valueOptions | Where-Object { Test-ParameterPrefix $n $_ 3 }).Count -gt 0
            if ($takesValue -and $null -eq $inline) { $j++ }
        }
        return $null
    }

    if ($name -in 'bash', 'sh', 'zsh', 'dash') {
        $command = $false
        for ($j = $k + 1; $j -lt $Tokens.Count; $j++) {
            $t = $Tokens[$j]
            if (-not $t.Quoted -and $t.Value -ne '--' -and $t.Value -match '^[-+]') {
                if ($t.Value -cmatch '^-[A-Za-z]*c[A-Za-z]*$') { $command = $true }
                if ($t.Value -in '-o', '+o', '-O', '+O', '--rcfile', '--init-file') { $j++ }
                continue
            }
            if ($t.Value -eq '--') { continue }
            if ($command) { return [pscustomobject]@{ Text = $t.Value; Bash = $true; Encoded = $false } }
            return $null
        }
    }
    return $null
}

# If the statement runs `<program> <verb...>`, returns the index of the token after the verbs; otherwise -1.
function Find-Invocation($Tokens, [string]$Program, [string[]]$Verbs) {
    $k = Resolve-Executable $Tokens
    if ($k -lt 0 -or (Get-ExecutableName $Tokens[$k].Value) -ne $Program) { return -1 }
    for ($v = 0; $v -lt $Verbs.Count; $v++) {
        $t = $k + 1 + $v
        if ($t -ge $Tokens.Count -or ($Verbs[$v] -split '\|') -notcontains $Tokens[$t].Value) { return -1 }
    }
    return $k + 1 + $Verbs.Count
}

# Parses the options after $From: `--name value`, `--name=value`, `-n value`, `-nvalue` (short options that
# take a value), and flags. $ValueOptions lists every option of the command that takes a value, so a value
# that looks like an option (`--body "-F x"`) is consumed as a value, not parsed as an option.
function Get-CommandOptions($Tokens, [int]$From, [string[]]$ValueOptions) {
    $options = [System.Collections.Generic.List[object]]::new()
    for ($k = $From; $k -lt $Tokens.Count; $k++) {
        $v = $Tokens[$k].Value
        if (-not $v.StartsWith('-') -or $v -eq '-' -or $v -eq '--') { continue }
        if ($v.StartsWith('--') -and $v.Contains('=')) {
            $eq = $v.IndexOf('=')
            $options.Add([pscustomobject]@{ Name = $v.Substring(0, $eq); Value = (New-CommandToken $v.Substring($eq + 1) $Tokens[$k].Quoted $Tokens[$k].Dynamic) })
            continue
        }
        if ($ValueOptions -ccontains $v) {
            $value = if ($k + 1 -lt $Tokens.Count) { $Tokens[$k + 1] } else { $null }
            $options.Add([pscustomobject]@{ Name = $v; Value = $value })
            $k++
            continue
        }
        if (-not $v.StartsWith('--') -and $v.Length -gt 2 -and $ValueOptions -ccontains $v.Substring(0, 2)) {
            $options.Add([pscustomobject]@{ Name = $v.Substring(0, 2); Value = (New-CommandToken $v.Substring(2) $Tokens[$k].Quoted $Tokens[$k].Dynamic) })
            continue
        }
        $options.Add([pscustomobject]@{ Name = $v; Value = $null })
    }
    return , $options
}

# Values of the named options, in order of appearance.
function Get-OptionValues($Options, [string[]]$Names) {
    return , @($Options | Where-Object { $Names -ccontains $_.Name -and $null -ne $_.Value } | ForEach-Object { $_.Value })
}

function Test-OptionPresent($Options, [string[]]$Names) {
    return [bool]($Options | Where-Object { $Names -ccontains $_.Name })
}

# Tracks `cd` / `Set-Location` / `Push-Location` statements so relative paths resolve where the command runs.
function Update-WorkingDirectory($Tokens, [string]$Cwd) {
    if ($Tokens.Count -ge 2 -and -not $Tokens[0].Quoted -and $Tokens[0].Value -in 'cd', 'Set-Location', 'sl', 'chdir', 'Push-Location', 'pushd' -and -not $Tokens[1].Dynamic) {
        $target = $Tokens[1].Value
        if ($target -eq '-LiteralPath' -or $target -eq '-Path') { if ($Tokens.Count -ge 3) { $target = $Tokens[2].Value } else { return $Cwd } }
        try { return [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($target)) { $target } else { Join-Path $Cwd $target })) } catch { return $Cwd }
    }
    return $Cwd
}

function Resolve-CommandPath([string]$Path, [string]$Cwd) {
    try {
        $full = if ([IO.Path]::IsPathRooted($Path)) { $Path } else { Join-Path $Cwd $Path }
        if (Test-Path -LiteralPath $full -PathType Leaf) { return $full }
    } catch { }
    return $null
}
