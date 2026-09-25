# Shared by the hooks that reason about repository paths (#1181): path normalisation, the layout of the main
# checkout and its worktrees under .claude\worktrees\, and the category of a repository-relative path, which
# decides who owns it (issue-worker, docs-writer, mechanical-fixer) and which specialist must see a change.

$script:RepoSeparator = [IO.Path]::DirectorySeparatorChar
$script:RepoIgnoreCase = [StringComparison]::OrdinalIgnoreCase

# Git Bash spells D:\x as /d/x.
function ConvertTo-NativePath([string]$Path) {
    if ($IsWindows -and $Path -match '^/([A-Za-z])(/|$)') {
        $rest = if ($Path.Length -gt 3) { $Path.Substring(3) } else { '' }
        return "$($Matches[1]):\$rest"
    }
    return $Path
}

function Get-HomeDirectory {
    $candidates = if ($IsWindows) { @($env:USERPROFILE, $env:HOME) } else { @($env:HOME, $env:USERPROFILE) }
    foreach ($candidate in $candidates) { if (-not [string]::IsNullOrWhiteSpace($candidate)) { return $candidate } }
    return [Environment]::GetFolderPath('UserProfile')
}

# Absolute, normalised path; `~` is the user profile; $null when a relative path has no known base or the
# path is not valid.
function Get-FullPath([string]$Path, [string]$Base) {
    if ([string]::IsNullOrWhiteSpace($Path) -or $Path.Length -gt 400 -or $Path.Contains("`n")) { return $null }
    try {
        $p = ConvertTo-NativePath $Path
        if ($p -eq '~' -or $p -match '^~[\\/]') { $p = (Get-HomeDirectory) + $p.Substring(1) }
        if (-not [IO.Path]::IsPathRooted($p)) {
            if ([string]::IsNullOrWhiteSpace($Base)) { return $null }
            $p = [IO.Path]::Combine((ConvertTo-NativePath $Base), $p)
        }
        return [IO.Path]::GetFullPath($p)
    }
    catch { return $null }
}

function Test-Under([string]$Path, [string]$Root) {
    if ([string]::IsNullOrEmpty($Path) -or [string]::IsNullOrEmpty($Root)) { return $false }
    $s = $script:RepoSeparator
    return ($Path.TrimEnd($s) + $s).StartsWith($Root.TrimEnd($s) + $s, $script:RepoIgnoreCase)
}

# MainRoot: the main checkout. Worktrees: its .claude\worktrees\ folder (with a trailing separator). When
# $ProjectDir is itself a worktree, the main checkout is the part before \.claude\worktrees\.
function Get-RepoLayout([string]$ProjectDir) {
    $project = Get-FullPath $ProjectDir $null
    if ($null -eq $project) { return $null }
    $s = $script:RepoSeparator
    $marker = "$s.claude$($s)worktrees$s"
    $at = $project.IndexOf($marker, $script:RepoIgnoreCase)
    $mainRoot = if ($at -ge 0) { $project.Substring(0, $at) } else { $project.TrimEnd($s) }
    return [pscustomobject]@{ MainRoot = $mainRoot; Worktrees = $mainRoot + $marker }
}

function Test-MainCheckout([string]$Path, $Layout) {
    return -not [string]::IsNullOrEmpty($Path) -and (Test-Under $Path $Layout.MainRoot) -and -not (Test-Under $Path $Layout.Worktrees)
}

# The checkout that contains $Full: Root (a worktree root or the main checkout), Relative (forward slashes)
# and InWorktree. $null when $Full is outside the project.
function Get-RepoLocation([string]$Full, $Layout) {
    if (-not (Test-Under $Full $Layout.MainRoot)) { return $null }
    if (Test-Under $Full $Layout.Worktrees) {
        $rest = $Full.Substring([Math]::Min($Layout.Worktrees.Length, $Full.Length))
        $slash = $rest.IndexOfAny([char[]]@('\', '/'))
        if ($slash -lt 0) { return [pscustomobject]@{ Root = $Layout.Worktrees + $rest; Relative = ''; InWorktree = $true } }
        return [pscustomobject]@{ Root = $Layout.Worktrees + $rest.Substring(0, $slash); Relative = $rest.Substring($slash + 1).Replace('\', '/'); InWorktree = $true }
    }
    $relative = $Full.Substring([Math]::Min($Layout.MainRoot.Length, $Full.Length)).TrimStart('\', '/').Replace('\', '/')
    return [pscustomobject]@{ Root = $Layout.MainRoot; Relative = $relative; InWorktree = $false }
}

# Category of a repository-relative path (case-insensitive):
#   plan       docs/plans/**                                  (issue-scoped working documents)
#   knowledge  docs/knowledge/**                              (SPEC-003 per-issue records and audit
#              results: structured data the closing issue-worker writes, not prose; DEC-005, #1311)
#   changelog  changelog.d/**
#   publicapi  **/PublicAPI.*.txt
#   manifest   .github/coverage-manifest/**
#   docs       prose: docs/**/*.md and *.markdown (other than plans and knowledge) with the images
#              they show (.png, .jpg, .jpeg, .gif, .svg, .webp), **/CONTRIBUTING.md, the root
#              README.md, src/**/*.md (package READMEs)
#   code       src/** (other than .md), the rest of docs/** (dashboard and site code and data: *.js, *.html,
#              *.css, *.json, *.yml such as docs/_config.yml, *.cs, ...), .github/scripts/**, .claude/hooks/**
#              (production code and tooling)
#   test       tests/**
#   github     .github/** (other than the above)
#   other      everything else (.claude/agents, .claude/skills, root config files, ...)
function Get-PathCategory([string]$Relative) {
    $r = $Relative.Replace('\', '/').TrimStart('/')
    if ($r -match '^docs/plans/') { return 'plan' }
    if ($r -match '^docs/knowledge/') { return 'knowledge' }
    if ($r -match '^changelog\.d/') { return 'changelog' }
    if ($r -match '(^|/)PublicAPI\.[^/]*\.txt$') { return 'publicapi' }
    if ($r -match '^\.github/coverage-manifest/') { return 'manifest' }
    if ($r -match '^docs/.*\.(md|markdown|png|jpe?g|gif|svg|webp)$' -or $r -match '(^|/)CONTRIBUTING\.md$' -or $r -match '^README\.md$' -or $r -match '^src/.*\.md$') { return 'docs' }
    if ($r -match '^(src|docs)/' -or $r -match '^\.github/scripts/' -or $r -match '^\.claude/hooks/') { return 'code' }
    if ($r -match '^tests/') { return 'test' }
    if ($r -match '^\.github/') { return 'github' }
    return 'other'
}
