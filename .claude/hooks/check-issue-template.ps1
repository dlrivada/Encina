# PreToolUse hook (Bash|PowerShell): validates `gh issue create` against .github/ISSUE_TEMPLATE.
#
# CLAUDE.md, "Issue Body Format (MANDATORY)": an issue title starts with its template prefix ([BUG], [DEBT], ...)
# and the body uses that template's level-2 headers verbatim and in order. This hook reads the templates at run
# time (so it never drifts from them), maps the title prefix to its template and checks the body.
#
# The body is taken from --body-file/-F when the file can be resolved, otherwise from the command text itself
# (inline --body or a here-string). When neither the title nor a body can be resolved (for example both come
# from variables) the call is allowed: the hook never blocks on what it cannot see.
# Exit code 2 blocks the call; stderr is shown to Claude.

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
} catch {
    exit 0
}

$command = [string]$payload.tool_input.command
if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }
if ($command -notmatch '\bgh\s+issue\s+create\b') { exit 0 }

$cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }

# Locate the repository root (the templates live there), starting from the call's working directory.
$root = $cwd
while ($root -and -not (Test-Path -LiteralPath (Join-Path $root '.github/ISSUE_TEMPLATE'))) {
    $parent = Split-Path -Parent $root
    if ($parent -eq $root) { $root = $null; break }
    $root = $parent
}
if (-not $root -and $env:CLAUDE_PROJECT_DIR) { $root = $env:CLAUDE_PROJECT_DIR }
$templateDir = if ($root) { Join-Path $root '.github/ISSUE_TEMPLATE' } else { $null }
if (-not $templateDir -or -not (Test-Path -LiteralPath $templateDir)) { exit 0 }

# prefix -> (template file, level-2 headers in order)
$templates = @{}
foreach ($file in Get-ChildItem -LiteralPath $templateDir -Filter '*.md') {
    $lines = Get-Content -LiteralPath $file.FullName
    $titleLine = $lines | Where-Object { $_ -match '^title:\s*"(\[[A-Z]+\])' } | Select-Object -First 1
    if (-not $titleLine) { continue }
    $prefix = [regex]::Match($titleLine, '\[[A-Z]+\]').Value
    $headers = @($lines | Where-Object { $_ -match '^## \S' } | ForEach-Object { $_.Trim() })
    $templates[$prefix] = @{ File = $file.Name; Headers = $headers }
}
if ($templates.Count -eq 0) { exit 0 }

$titleMatch = [regex]::Match($command, '(?:--title|-t)(?:=|\s+)(?:"(?<t>[^"]*)"|''(?<t>[^'']*)'')')
if (-not $titleMatch.Success) { exit 0 }
$title = $titleMatch.Groups['t'].Value
if ($title.StartsWith('$')) { exit 0 }

$prefixMatch = [regex]::Match($title, '^\[[A-Z]+\]')
$known = ($templates.Keys | Sort-Object) -join ', '
if (-not $prefixMatch.Success -or -not $templates.ContainsKey($prefixMatch.Value)) {
    [Console]::Error.WriteLine("Blocked: issue title '$title' must start with a template prefix ($known). See CLAUDE.md, Issue Templates; normalise [TECH-DEBT] to [DEBT], [TESTING] to [TEST], [ARCHITECTURE]/[DECISION]/[REVIEW] to [SPIKE].")
    exit 2
}
$template = $templates[$prefixMatch.Value]

$body = $null
$bodyFile = [regex]::Match($command, '(?<=\s)(?:--body-file|-F)(?:=|\s+)(?:"(?<p>[^"]+)"|''(?<p>[^'']+)''|(?<p>[^\s;|]+))')
if ($bodyFile.Success) {
    $path = $bodyFile.Groups['p'].Value
    if ($path -eq '-' -or $path.StartsWith('$')) { exit 0 }
    $full = if ([IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $cwd $path }
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) { exit 0 }
    $body = [IO.File]::ReadAllText($full)
} elseif (($inline = [regex]::Match($command, '(?<=\s)(?:--body|-b)(?:=|\s+)')).Success) {
    # Everything after the flag, minus the opening quote or here-string marker, so that a header written
    # right after the quote (--body "## Type ...) still starts a line.
    $body = "`n" + $command.Substring($inline.Index + $inline.Length).TrimStart('@', '"', "'")
} else {
    exit 0   # interactive or --template: gh fills the template itself
}

$bodyHeaders = @([regex]::Matches($body, '(?m)^## [^\r\n]+') | ForEach-Object { $_.Value.Trim() })
$missing = @($template.Headers | Where-Object { $bodyHeaders -notcontains $_ })
if ($missing.Count -gt 0) {
    [Console]::Error.WriteLine("Blocked: a $($prefixMatch.Value) issue uses the headers of .github/ISSUE_TEMPLATE/$($template.File) verbatim and in order (CLAUDE.md, Issue Body Format). Missing: $($missing -join ' | '). Read the template, fill every section (tick the checkboxes that apply) and retry. The open-issue skill describes the procedure.")
    exit 2
}

# Order: the template headers must appear in the body in the same relative order.
$positions = @($template.Headers | ForEach-Object { [array]::IndexOf($bodyHeaders, $_) })
for ($i = 1; $i -lt $positions.Count; $i++) {
    if ($positions[$i] -lt $positions[$i - 1]) {
        [Console]::Error.WriteLine("Blocked: the headers of .github/ISSUE_TEMPLATE/$($template.File) must appear in template order; '$($template.Headers[$i])' comes before '$($template.Headers[$i - 1])'.")
        exit 2
    }
}

exit 0
