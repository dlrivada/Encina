# PreToolUse hook (Bash|PowerShell), scoped to the frontmatter of the worker and reviewer agents: blocks the
# executables and Bash constructs that CLAUDE.md (Scripting & Tooling Policy) prohibits, and names the
# PowerShell or Claude Code tool equivalent (#1181).
#
# Blocked in both tools: python, python3, grep, sed, awk, head, tail, wc, xargs, curl (curl.exe too), cut,
# paste, shuf, unzip, basename, xxd, dd, and `bash -c` / `sh -c`.
# Blocked in Bash only: find, cat, ls, sort, tee (in PowerShell they are aliases of Get-ChildItem-style
# cmdlets or, for find, the Windows find.exe, which the policy does not target); statements that start with
# for, while, until, if, case or select (loops and conditionals) or run test, [ or [[ (conditions, as in
# `test -f x && ...`); $( ... ) and backtick command substitution outside single quotes; and <( ... ) / >( ... )
# process substitution outside quotes.
#
# Blocked in both tools: `pwsh` / `powershell -EncodedCommand` (-e, -ec, -en...), whose text no hook can read.
#
# Only the program a statement runs is checked (through the tokenizer in _command-text.ps1), so `git grep`,
# `Select-String`, a word inside a string, a comment, a PowerShell hashtable key (`@{ head = 1 }`) or `--jq`
# filters pass. Bash pipes are not blocked. The command text of a `pwsh -Command "..."` wrapper is checked as
# PowerShell statements and the script of `bash -c '...'` as Bash statements, whatever the tool's shell.
# ANSI-C strings ($'...') are Bash quotes, so their content is not a substitution.
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call
# (fail open).

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }
    $bash = $payload.tool_name -eq 'Bash'

    $equivalents = @{
        python   = 'a C# file-based script (dotnet run script.cs) or PowerShell'
        python3  = 'a C# file-based script (dotnet run script.cs) or PowerShell'
        grep     = 'the Grep tool, or Select-String'
        sed      = 'the Edit tool for files, or the -replace operator on strings'
        awk      = 'PowerShell (-split, ForEach-Object) or a C# script'
        head     = 'the Read tool with limit, or Select-Object -First N'
        tail     = 'the Read tool with offset, or Get-Content -Tail N'
        wc       = '(Get-Content <file>).Count or Measure-Object'
        xargs    = 'ForEach-Object'
        curl     = 'Invoke-RestMethod or Invoke-WebRequest'
        cut      = 'the -split operator or Select-Object'
        paste    = 'the -join operator'
        shuf     = 'Get-Random'
        unzip    = 'Expand-Archive'
        basename = 'Split-Path -Leaf'
        xxd      = 'Format-Hex'
        dd       = 'PowerShell or a C# script'
    }
    $bashOnly = @{
        find = 'the Glob tool, or Get-ChildItem -Recurse -Filter'
        cat  = 'the Read tool, or Get-Content'
        ls   = 'the Glob tool, or Get-ChildItem'
        sort = 'Sort-Object'
        tee  = 'Tee-Object'
    }

    function Write-Block([string]$What, [string]$Instead) {
        [Console]::Error.WriteLine("Blocked: $What is prohibited by CLAUDE.md (Scripting & Tooling Policy). Use $Instead instead (#1181).")
        exit 2
    }

    foreach ($tokens in (Split-CommandStatements -Text $command -Bash:$bash)) {
        # The shell of this statement: a `pwsh -Command` or `bash -c` wrapper runs its text in its own shell.
        $statementBash = [bool]$tokens[0].Bash
        if ($statementBash -and -not $tokens[0].Quoted -and $tokens[0].Value -in 'for', 'while', 'until', 'if', 'case', 'select') {
            Write-Block "the Bash construct '$($tokens[0].Value)'" 'the PowerShell tool (foreach / ForEach-Object, if (...) { }, switch)'
        }

        $wrapped = Get-WrappedCommand $tokens
        if ($null -ne $wrapped -and $wrapped.Encoded) {
            [Console]::Error.WriteLine("Blocked: 'pwsh -EncodedCommand' hides the command from every hook (#1181). Run the command itself with the PowerShell tool, or put it in a script file under artifacts/ and run it with -File.")
            exit 2
        }

        $k = Resolve-Executable $tokens
        if ($k -lt 0 -or $tokens[$k].Dynamic) { continue }
        $name = Get-ExecutableName $tokens[$k].Value
        if ($statementBash -and -not $tokens[$k].Quoted -and $tokens[$k].Value -in 'test', '[', '[[') {
            Write-Block "the Bash condition '$($tokens[$k].Value)'" 'the PowerShell tool (if (Test-Path ...) { })'
        }

        if ($equivalents.ContainsKey($name)) { Write-Block "'$name'" $equivalents[$name] }
        if ($statementBash -and $bashOnly.ContainsKey($name)) { Write-Block "'$name' in Bash" $bashOnly[$name] }
        if ($name -in 'bash', 'sh' -and @($tokens | Select-Object -Skip ($k + 1) | Where-Object { -not $_.Quoted -and $_.Value -ceq '-c' }).Count -gt 0) {
            Write-Block "an inline shell script ('$name -c')" 'the PowerShell tool or a C# file-based script'
        }
    }

    # Bash command substitution ($( ... ) and backticks) outside single quotes, and process substitution
    # (<( ... ), >( ... )) outside any quotes. Comments are removed first.
    if ($bash) {
        $text = Remove-CommandComments -Text $command -Bash
        $single = $false
        $double = $false
        for ($i = 0; $i -lt $text.Length; $i++) {
            $c = $text[$i]
            $next = if ($i + 1 -lt $text.Length) { $text[$i + 1] } else { [char]0 }
            if ($single) { if ($c -eq "'") { $single = $false }; continue }
            if ($c -eq '\') { $i++; continue }
            if (-not $double -and $c -eq '$' -and $next -eq "'") { $i = (Read-AnsiCString $text $i).End - 1; continue }
            if ($c -eq "'" -and -not $double) { $single = $true; continue }
            if ($c -eq '"') { $double = -not $double; continue }
            if ($c -eq '$' -and $next -eq '(') {
                Write-Block 'Bash command substitution $( ... )' 'the PowerShell tool (a variable assignment, or a here-string for multi-line text)'
            }
            if ($c -eq '`') {
                Write-Block 'Bash command substitution with backticks' 'the PowerShell tool (a variable assignment)'
            }
            if (-not $double -and $c -in '<', '>' -and $next -eq '(') {
                Write-Block "Bash process substitution $c( ... )" 'the PowerShell tool (save each output to a variable or a file under artifacts/ first)'
            }
        }
    }
    exit 0
}
catch {
    exit 0
}
