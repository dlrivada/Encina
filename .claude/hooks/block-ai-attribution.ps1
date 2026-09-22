# PreToolUse hook (Bash|PowerShell): blocks AI attribution in commits and pull requests.
#
# CLAUDE.md, Git Workflow: commits are authored solely by the repository owner, with no AI signatures,
# co-author trailers or "generated with" lines. The Claude Code environment injects such lines by default,
# so this hook enforces the project rule mechanically.
#
# Inspected commands: `git commit` (inline -m text and -F/--file message files) and `gh pr create|edit`
# (inline --body text and -F/--body-file files). Exit code 2 blocks the call; stderr is shown to Claude.

$ErrorActionPreference = 'Stop'

try {
    $payload = [Console]::In.ReadToEnd() | ConvertFrom-Json
} catch {
    exit 0   # not a payload we understand: never block on our own parsing failure
}

$command = [string]$payload.tool_input.command
if ([string]::IsNullOrWhiteSpace($command)) { exit 0 }

$isCommit = $command -match '\bgit\s+(-C\s+\S+\s+)?commit\b'
$isPr = $command -match '\bgh\s+pr\s+(create|edit)\b'
if (-not ($isCommit -or $isPr)) { exit 0 }

$patterns = @(
    'Co-Authored-By:\s*[^\r\n]*(Claude|Anthropic|Copilot|GPT|OpenAI)',
    'noreply@anthropic\.com',
    'Generated with \[?Claude',
    '🤖\s*Generated'
)

function Find-Attribution([string]$text) {
    foreach ($p in $patterns) {
        $m = [regex]::Match($text, $p, 'IgnoreCase')
        if ($m.Success) { return $m.Value }
    }
    return $null
}

$texts = @(@{ Source = 'command'; Text = $command })

# Message / body files referenced by the command.
$fileFlag = if ($isCommit) { '(?:-F|--file)' } else { '(?:-F|--body-file)' }
$cwd = if ($payload.cwd) { [string]$payload.cwd } else { (Get-Location).Path }
foreach ($m in [regex]::Matches($command, "(?<=\s)$fileFlag(?:=|\s+)(?:""(?<p>[^""]+)""|'(?<p>[^']+)'|(?<p>[^\s;|]+))")) {
    $path = $m.Groups['p'].Value
    if ($path -eq '-' -or $path.StartsWith('$')) { continue }
    $full = if ([IO.Path]::IsPathRooted($path)) { $path } else { Join-Path $cwd $path }
    if (Test-Path -LiteralPath $full -PathType Leaf) {
        $texts += @{ Source = $path; Text = [IO.File]::ReadAllText($full) }
    }
}

foreach ($t in $texts) {
    $hit = Find-Attribution $t.Text
    if ($hit) {
        $what = if ($isCommit) { 'commit message' } else { 'pull request body' }
        [Console]::Error.WriteLine("Blocked: the $what contains AI attribution ('$hit' in $($t.Source)). CLAUDE.md (Git Workflow) forbids AI signatures, co-author trailers and 'generated with' lines in commits and PRs. Remove the line and retry.")
        exit 2
    }
}

exit 0
