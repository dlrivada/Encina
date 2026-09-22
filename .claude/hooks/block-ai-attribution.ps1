# PreToolUse hook (Bash|PowerShell): blocks AI attribution in commits and pull requests.
#
# CLAUDE.md, Git Workflow: commits are authored solely by the repository owner, with no AI signatures,
# co-author trailers or "generated with" lines. The Claude Code environment injects such lines by default,
# so this hook enforces the project rule mechanically.
#
# Inspected: the arguments of every `git [global options] commit` statement (-m, --message, --trailer, ...)
# plus its message or template file (-F/--file, -t/--template), and of every `gh pr create|edit|merge`
# statement plus its body file (-F/--body-file). Relative files resolve against `git -C <dir>` or the last
# `cd`/`Set-Location` of the command. A message read from stdin (`-F -`) is not inspected. A commit message that
# quotes the forbidden trailer literally (for example one that describes this hook) is blocked too.
# Exit code 2 blocks the call and shows stderr to Claude; any failure of the hook itself allows the call.

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }
    if ($command -notmatch '\bgit\b|\bgh\b') { exit 0 }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $statements = Split-CommandStatements -Text $command -Bash:($payload.tool_name -eq 'Bash')

    $patterns = @(
        'Co-Authored-By\s*[:=]\s*[^\r\n]*(Claude|Anthropic|Copilot|GPT|OpenAI|Gemini)',
        '(Assisted|Generated|Written)-by\s*[:=]\s*[^\r\n]*(Claude|Anthropic|Copilot|GPT|OpenAI|Gemini)',
        'noreply@anthropic\.com',
        'Generated (with|by) \[?(Claude|Copilot|ChatGPT)',
        '🤖\s*Generated'
    )

    $findings = [System.Collections.Generic.List[string]]::new()
    function Test-Text([string]$Text, [string]$Source, [string]$What) {
        foreach ($p in $patterns) {
            $m = [regex]::Match($Text, $p, 'IgnoreCase')
            if ($m.Success) { $findings.Add("the $What contains '$($m.Value)' ($Source)"); return }
        }
    }

    foreach ($tokens in $statements) {
        $cwd = Update-WorkingDirectory $tokens $cwd

        # git [-C dir] [-c k=v] [--opt[=v]] ... commit
        for ($k = 0; $k -lt $tokens.Count; $k++) {
            if ($tokens[$k].Quoted -or $tokens[$k].Value -notin 'git', 'git.exe') { continue }
            $dir = $cwd
            $j = $k + 1
            while ($j -lt $tokens.Count -and -not $tokens[$j].Quoted -and $tokens[$j].Value.StartsWith('-')) {
                $opt = $tokens[$j].Value
                if ($opt -ceq '-C' -and $j + 1 -lt $tokens.Count) {
                    $target = $tokens[$j + 1].Value
                    try { $dir = [IO.Path]::GetFullPath($(if ([IO.Path]::IsPathRooted($target)) { $target } else { Join-Path $dir $target })) } catch { }
                    $j += 2; continue
                }
                if ($opt -ceq '-c' -or ($opt -in '--git-dir', '--work-tree', '--namespace', '--exec-path' -and -not $opt.Contains('='))) { $j += 2; continue }
                $j++
            }
            if ($j -ge $tokens.Count -or $tokens[$j].Value -ne 'commit') { continue }

            $commitArgs = $tokens | Select-Object -Skip ($j + 1)
            foreach ($t in $commitArgs) { Test-Text $t.Value 'command' 'commit message' }
            foreach ($f in (Get-OptionValues $tokens ($j + 1) @('-F', '--file', '-t', '--template'))) {
                if ($f.Dynamic -or $f.Value -eq '-') { continue }
                $path = Resolve-CommandPath $f.Value $dir
                if ($path) { Test-Text ([IO.File]::ReadAllText($path)) $f.Value 'commit message' }
            }
        }

        # gh pr create|edit|merge
        $at = Find-Invocation $tokens @('gh', 'gh.exe') @('pr', 'create|edit|merge')
        if ($at -ge 0) {
            foreach ($t in ($tokens | Select-Object -Skip $at)) { Test-Text $t.Value 'command' 'pull request text' }
            foreach ($f in (Get-OptionValues $tokens $at @('-F', '--body-file'))) {
                if ($f.Dynamic -or $f.Value -eq '-') { continue }
                $path = Resolve-CommandPath $f.Value $cwd
                if ($path) { Test-Text ([IO.File]::ReadAllText($path)) $f.Value 'pull request text' }
            }
        }
    }

    if ($findings.Count -gt 0) {
        [Console]::Error.WriteLine("Blocked: $($findings[0]). CLAUDE.md (Git Workflow) forbids AI signatures, co-author trailers and 'generated with' lines in commits and PRs. Remove the line and retry.")
        exit 2
    }
    exit 0
}
catch {
    exit 0
}
