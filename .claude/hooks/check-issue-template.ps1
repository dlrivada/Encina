# PreToolUse hook (Bash|PowerShell): validates `gh issue create` against .github/ISSUE_TEMPLATE.
#
# CLAUDE.md, "Issue Body Format (MANDATORY)": an issue title starts with its template prefix ([BUG], [DEBT], ...)
# and the body uses that template's level-2 headers verbatim (case included) and in order. The templates are
# read at run time, so the hook never drifts from them.
#
# Only the arguments of the `gh issue create` statement itself are read, never the rest of the command line.
# Allowed without checks: issues on another repository (-R/--repo), --web, --template, calls without a body,
# and titles or bodies that come from a variable or a subexpression without visible headers. Headers inside
# fenced code blocks do not count. Exit code 2 blocks the call and shows stderr to Claude; any failure of the
# hook itself allows the call.

$ErrorActionPreference = 'Stop'

try {
    . (Join-Path $PSScriptRoot '_command-text.ps1')

    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
    $command = [string]$payload.tool_input.command
    if ([string]::IsNullOrWhiteSpace($command) -or $command -notmatch '\bgh\b') { exit 0 }

    $cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
    $statements = Split-CommandStatements -Text $command -Bash:($payload.tool_name -eq 'Bash')

    function Get-Templates([string]$StartDir) {
        $root = $StartDir
        while ($root -and -not (Test-Path -LiteralPath (Join-Path $root '.github/ISSUE_TEMPLATE'))) {
            $parent = Split-Path -Parent $root
            if (-not $parent -or $parent -eq $root) { $root = $null; break }
            $root = $parent
        }
        if (-not $root -and $env:CLAUDE_PROJECT_DIR) { $root = $env:CLAUDE_PROJECT_DIR }
        $dir = if ($root) { Join-Path $root '.github/ISSUE_TEMPLATE' } else { $null }
        $templates = @{}
        if (-not $dir -or -not (Test-Path -LiteralPath $dir)) { return $templates }
        foreach ($file in Get-ChildItem -LiteralPath $dir -Filter '*.md') {
            $lines = Get-Content -LiteralPath $file.FullName
            $titleLine = $lines | Where-Object { $_ -match '^title:\s*"(\[[A-Z]+\])' } | Select-Object -First 1
            if (-not $titleLine) { continue }
            $prefix = [regex]::Match($titleLine, '\[[A-Z]+\]').Value
            $templates[$prefix] = @{ File = $file.Name; Headers = @($lines | Where-Object { $_ -cmatch '^## \S' } | ForEach-Object { $_.Trim() }) }
        }
        return $templates
    }

    function Get-Headers([string]$Body) {
        $headers = [System.Collections.Generic.List[string]]::new()
        $fence = $null
        foreach ($line in ($Body -split "`r?`n")) {
            if ($fence) {
                if ($line -match "^\s{0,3}$([regex]::Escape($fence.Char)){$($fence.Length),}\s*$") { $fence = $null }
                continue
            }
            # CommonMark: a backtick fence's info string cannot contain a backtick (that line is inline code).
            $open = [regex]::Match($line, '^\s{0,3}(?<f>`{3,}|~{3,})(?<info>.*)$')
            if ($open.Success -and $open.Groups['f'].Value[0] -eq '`' -and $open.Groups['info'].Value.Contains('`')) { $open = [System.Text.RegularExpressions.Match]::Empty }
            if ($open.Success) { $fence = @{ Char = [string]$open.Groups['f'].Value[0]; Length = $open.Groups['f'].Value.Length }; continue }
            if ($line -cmatch '^## \S') { $headers.Add($line.Trim()) }
        }
        return , $headers
    }

    # Options of `gh issue create` that take a value, so their values are never parsed as options.
    $issueValueOptions = @('-t', '--title', '-b', '--body', '-F', '--body-file', '-R', '--repo', '-l', '--label', '-m', '--milestone', '-a', '--assignee', '-p', '--project', '-T', '--template', '--recover')

    foreach ($tokens in $statements) {
        $cwd = Update-WorkingDirectory $tokens $cwd
        $at = Find-Invocation $tokens 'gh' @('issue', 'create')
        if ($at -lt 0) { continue }

        $options = Get-CommandOptions $tokens $at $issueValueOptions
        $repo = Get-OptionValues $options @('-R', '--repo')
        if ($repo.Count -gt 0 -and ($repo[0].Dynamic -or $repo[0].Value -ne 'dlrivada/Encina')) { continue }
        if (Test-OptionPresent $options @('-w', '--web', '-T', '--template')) { continue }

        $title = Get-OptionValues $options @('-t', '--title')
        if ($title.Count -eq 0 -or $title[0].Dynamic) { continue }
        $titleText = $title[0].Value

        $body = $null
        $bodyFile = Get-OptionValues $options @('-F', '--body-file')
        $bodyInline = Get-OptionValues $options @('-b', '--body')
        if ($bodyFile.Count -gt 0) {
            if ($bodyFile[0].Dynamic -or $bodyFile[0].Value -eq '-') { continue }
            $path = Resolve-CommandPath $bodyFile[0].Value $cwd
            if (-not $path) { continue }
            $body = [IO.File]::ReadAllText($path)
        }
        elseif ($bodyInline.Count -gt 0) {
            $body = $bodyInline[0].Value
            if ($bodyInline[0].Dynamic -and (Get-Headers $body).Count -eq 0) { continue }
        }
        else { continue }

        $templates = Get-Templates $cwd
        if ($templates.Count -eq 0) { continue }

        $prefix = [regex]::Match($titleText, '^\[[A-Z]+\]')
        if (-not $prefix.Success -or -not $templates.ContainsKey($prefix.Value)) {
            $known = ($templates.Keys | Sort-Object) -join ', '
            [Console]::Error.WriteLine("Blocked: issue title '$titleText' must start with a template prefix ($known). See CLAUDE.md, Issue Templates; normalise [TECH-DEBT] to [DEBT], [TESTING] to [TEST], [ARCHITECTURE]/[DECISION]/[REVIEW] to [SPIKE].")
            exit 2
        }
        $template = $templates[$prefix.Value]
        $bodyHeaders = Get-Headers $body

        $missing = @($template.Headers | Where-Object { $bodyHeaders -cnotcontains $_ })
        if ($missing.Count -gt 0) {
            [Console]::Error.WriteLine("Blocked: a $($prefix.Value) issue uses the headers of .github/ISSUE_TEMPLATE/$($template.File) verbatim and in order (CLAUDE.md, Issue Body Format). Missing: $($missing -join ' | '). Read the template, fill every section (tick the checkboxes that apply) and retry. The open-issue skill describes the procedure.")
            exit 2
        }

        $positions = @($template.Headers | ForEach-Object { $bodyHeaders.IndexOf($_) })
        for ($i = 1; $i -lt $positions.Count; $i++) {
            if ($positions[$i] -lt $positions[$i - 1]) {
                [Console]::Error.WriteLine("Blocked: the headers of .github/ISSUE_TEMPLATE/$($template.File) must appear in template order; '$($template.Headers[$i])' comes before '$($template.Headers[$i - 1])'.")
                exit 2
            }
        }
    }
    exit 0
}
catch {
    exit 0
}
