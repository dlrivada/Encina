# tools/ai/audit/classify-scope.ps1 -Issue <n> (#1345; moved from the unversioned artifacts/knowledge/)
#
# Classifies an issue's touched files: how many still exist at HEAD. Output:
# "<n> total=<t> alive=<a> class=<code-removed|alive|unknown>".

param([Parameter(Mandatory)][int]$Issue)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$root = Get-MainRoot $PSScriptRoot
$raw = Join-Path $root "artifacts\knowledge\predraft\raw\$Issue.txt"
$files = New-Object System.Collections.Generic.HashSet[string]
if (Test-Path -LiteralPath $raw) {
    foreach ($m in [regex]::Matches((Get-Content -Raw $raw), 'COMMIT ([0-9a-f]{8})')) {
        git -C $root show --name-only --format= $m.Groups[1].Value 2>$null | Where-Object { $_ } | ForEach-Object { [void]$files.Add($_) }
    }
    foreach ($m in [regex]::Matches((Get-Content -Raw $raw), 'LINKED PR #(\d+):.*\(state closed\)')) {
        # REST (#2661): the PR and its files (paginated, so no 100-file truncation).
        $prNumber = $m.Groups[1].Value
        $pr = gh api "repos/dlrivada/Encina/pulls/$prNumber" 2>$null | ConvertFrom-Json
        if ($pr -and $pr.merged_at) {
            $prFiles = gh api "repos/dlrivada/Encina/pulls/$prNumber/files" --paginate --jq '.[].filename' 2>$null
            @($prFiles) | Where-Object { $_ } | ForEach-Object { [void]$files.Add($_) }
        }
    }
}
# Closing comments often list commit hashes ("Completed in commits: - 760e27c: ...").
if (Test-Path -LiteralPath $raw) {
    foreach ($m in [regex]::Matches((Get-Content -Raw $raw), '(?m)^\s*[-*]\s*`?([0-9a-f]{7,40})`?\b')) {
        git -C $root show --name-only --format= $m.Groups[1].Value 2>$null | Where-Object { $_ } | ForEach-Object { [void]$files.Add($_) }
    }
}
# Early issues were often closed by hand, without a timeline link: also count the commits whose message mentions #<n>.
foreach ($h in @(git -C $root log --all --format=%h -E --grep "#$Issue\b" 2>$null)) {
    git -C $root show --name-only --format= $h 2>$null | Where-Object { $_ } | ForEach-Object { [void]$files.Add($_) }
}
$code = @($files | Where-Object { $_ -match '^(src|tests)/' })
$alive = @($code | Where-Object { Test-Path (Join-Path $root $_) })
$class = if ($code.Count -eq 0) { 'unknown' } elseif ($alive.Count -eq 0) { 'code-removed' } else { 'alive' }
"$Issue total=$($code.Count) alive=$($alive.Count) class=$class"
