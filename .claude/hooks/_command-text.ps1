# Shared by the PreToolUse hooks: splits a Bash or PowerShell command line into statements and tokens, so a
# hook inspects the arguments of the `git` / `gh` call itself instead of the whole command text.
#
# Understood: single and double quotes (PowerShell backtick and doubled-quote escapes; backslash escapes for
# Bash), backtick escapes and line continuations outside quotes, PowerShell here-strings (@" "@, @' '@),
# $( ... ) and ( ... ) subexpressions (their content is parsed as statements too), Bash heredocs inside
# $( ... ), and the separators ; | || && newline { }. A token that depends on a variable or a subexpression is
# marked Dynamic: its Value holds the literal text, which may still contain the words the hooks look for.

function New-CommandToken([string]$Value, [bool]$Quoted, [bool]$Dynamic) {
    [pscustomobject]@{ Value = $Value; Quoted = $Quoted; Dynamic = $Dynamic }
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
                $i = if ($close.Success) { $end + 2 } else { $n }
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
        $current.Add((New-CommandToken $sb.ToString() $quoted $dynamic))
    }

    if ($current.Count -gt 0) { $statements.Add($current) }

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
function Resolve-Executable($Tokens) {
    $k = 0
    while ($k -lt $Tokens.Count) {
        $t = $Tokens[$k]
        if (-not $t.Quoted -and $t.Value -in '&', '.') { $k++; continue }
        if (-not $t.Quoted -and $t.Value -match '^\$[\w:]+$' -and $k + 1 -lt $Tokens.Count -and $Tokens[$k + 1].Value -eq '=') { $k += 2; continue }
        if (-not $t.Quoted -and $t.Value -match '^[A-Za-z_][A-Za-z0-9_]*=') { $k++; continue }
        if (-not $t.Quoted -and $t.Value -in 'sudo', 'time', 'env', 'exec', 'command', 'nohup') { $k++; continue }
        return $k
    }
    return -1
}

# `git`, `C:\Program Files\Git\cmd\git.exe`, "gh" -> git / gh.
function Get-ExecutableName([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return '' }
    try { return [IO.Path]::GetFileNameWithoutExtension($Value.Replace('\', '/').Split('/')[-1]).ToLowerInvariant() } catch { return '' }
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
