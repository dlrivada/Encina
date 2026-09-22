# Shared by the PreToolUse hooks: splits a Bash or PowerShell command line into statements and tokens, so a
# hook inspects the arguments of the `git` / `gh` call itself instead of the whole command text.
#
# Understood: single and double quotes (PowerShell backtick and doubled-quote escapes; backslash escapes for
# Bash), PowerShell here-strings (@" "@, @' '@), $( ... ) and ( ... ) subexpressions, Bash heredocs inside
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

function Split-CommandStatements {
    param([string]$Text, [switch]$Bash)

    $statements = [System.Collections.Generic.List[object]]::new()
    $current = [System.Collections.Generic.List[object]]::new()
    $n = $Text.Length
    $i = 0

    while ($i -lt $n) {
        $c = $Text[$i]
        if ($c -eq ' ' -or $c -eq "`t") { $i++; continue }

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
                        $j = Skip-Subexpression $Text ($j + 1) $sb
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
                $i = Skip-Subexpression $Text $open $sb
                continue
            }

            if ($c -eq '$') { $dynamic = $true }
            if ($Bash -and $c -eq '\' -and $i + 1 -lt $n) { [void]$sb.Append($Text[$i + 1]); $i += 2; continue }
            [void]$sb.Append($c)
            $i++
        }
        $current.Add((New-CommandToken $sb.ToString() $quoted $dynamic))
    }

    if ($current.Count -gt 0) { $statements.Add($current) }
    return , $statements
}

# Finds `<program> <verb...>` inside a statement (the program token unquoted); returns the index of the token
# after the verbs, or -1.
function Find-Invocation($Tokens, [string[]]$Programs, [string[]]$Verbs) {
    for ($k = 0; $k -lt $Tokens.Count; $k++) {
        if ($Tokens[$k].Quoted -or $Programs -notcontains $Tokens[$k].Value) { continue }
        $ok = $true
        for ($v = 0; $v -lt $Verbs.Count; $v++) {
            $t = $k + 1 + $v
            if ($t -ge $Tokens.Count -or ($Verbs[$v] -split '\|') -notcontains $Tokens[$t].Value) { $ok = $false; break }
        }
        if ($ok) { return $k + 1 + $Verbs.Count }
    }
    return -1
}

# Values of an option in `--name value`, `--name=value` or `-n value` form, in order of appearance.
function Get-OptionValues($Tokens, [int]$From, [string[]]$Names) {
    $result = [System.Collections.Generic.List[object]]::new()
    for ($k = $From; $k -lt $Tokens.Count; $k++) {
        $t = $Tokens[$k]
        if ($t.Quoted) { continue }
        foreach ($name in $Names) {
            if ($t.Value -ceq $name -and $k + 1 -lt $Tokens.Count) {
                $result.Add($Tokens[$k + 1]); break
            }
            if ($name.StartsWith('--') -and $t.Value.StartsWith("$name=", [StringComparison]::Ordinal)) {
                $result.Add((New-CommandToken $t.Value.Substring($name.Length + 1) $false $t.Dynamic)); break
            }
        }
    }
    return , $result
}

function Test-OptionPresent($Tokens, [int]$From, [string[]]$Names) {
    for ($k = $From; $k -lt $Tokens.Count; $k++) {
        $t = $Tokens[$k]
        if ($t.Quoted) { continue }
        foreach ($name in $Names) {
            if ($t.Value -ceq $name -or ($name.StartsWith('--') -and $t.Value.StartsWith("$name=", [StringComparison]::Ordinal))) { return $true }
        }
    }
    return $false
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
