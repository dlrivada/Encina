param(
    [string]$Repo = 'dlrivada/Encina',
    [string]$Csv = (Join-Path (git rev-parse --show-toplevel) 'artifacts\local-ai\issues\classification-final.csv'),
    [switch]$Execute
)

# Repairs the first renumbering run (2026-09-22), whose gh arguments were passed without $() and produced literal
# PowerShell object text as milestone titles, and then performs the issue moves with correct quoting.

$ErrorActionPreference = 'Stop'
$blocks = [ordered]@{
    'v0.14.0' = @{ title = 'v0.14.0 — Hardening'; desc = 'Encina 1.0 block 1 (SPEC-000 DEC-005): defects, security debt and cross-cutting gaps of the existing packages. Absorbs the P0/P1 issues of the former v0.13.0, v0.13.4, v0.13.5 and v0.13.6 milestones plus new findings.'; from = @('v0.13.0', 'v0.13.4', 'v0.13.5', 'v0.13.6', '') }
    'v0.15.0' = @{ title = 'v0.15.0 — EU Compliance: NIS2 & Digital Omnibus'; desc = 'Encina 1.0 block 2 (SPEC-000 DEC-002/DEC-005): EPIC #880 lifecycle and Digital Omnibus adaptations. The five new regulation packages (DORA, eIDAS2, Data Act, ENS, EHDS) stay post-1.0.'; from = @('v0.16.1') }
    'v0.16.0' = @{ title = 'v0.16.0 — AI Act'; desc = 'Encina 1.0 block 3 (SPEC-000 DEC-002/DEC-005): EPIC #881 obligations (Arts. 9, 10, 11, 12, 13/50, 14, 43, 51-56) and the Marten migration of the AI Act module.'; from = @('v0.16.2') }
    'v0.17.0' = @{ title = 'v0.17.0 — Providers & Testing'; desc = 'Encina 1.0 block 4 (SPEC-000 DEC-003/DEC-005): complete the caching and lock provider sets (Memcached, PostgreSQL and MySQL locks), remaining health checks, and the testing work recommended for 1.0.'; from = @('v0.21.0') }
    'v0.18.0' = @{ title = 'v0.18.0 — Documentation'; desc = 'Encina 1.0 block 5 (SPEC-000 DEC-005): documentation needed to use the public API (introduction, quickstart, fundamentals, providers, examples) and the removal of unbacked claims (#1090).'; from = @('v0.22.0') }
    'v0.19.0' = @{ title = 'v0.19.0 — Release Engineering'; desc = 'Encina 1.0 block 6 (SPEC-000 DEC-004/DEC-005, REQ-018..023): packaging, signing, provenance, NuGet publish path, evidence report and pre-release checklist.'; from = @('v0.23.0') }
    'v1.0.0-rc.1' = @{ title = 'v1.0.0-rc.1'; desc = 'Release candidate gate: opened when the six 1.0 blocks are closed (SPEC-000 §9).'; from = @() }
}
$featureP1Overrides = @{ 1050 = 'v0.14.0' }
$epicMoves = @{ 870 = 'v0.14.0'; 871 = 'v0.14.0'; 872 = 'v0.14.0'; 873 = 'v0.14.0'; 880 = 'v0.15.0'; 881 = 'v0.16.0'; 891 = 'v0.17.0'; 892 = 'v0.18.0'; 893 = 'v0.19.0' }

$ms = gh api "repos/$Repo/milestones?state=open&per_page=100" --paginate | ConvertFrom-Json
$fixes = @(); $newIds = @{}; $oldByNumber = @{}
foreach ($m in $ms) {
    if ($m.title -match '^@\{number=(\d+); old=([^;]+); new=(.+?)(; open=\d+)?\}\.new$') {
        $fixes += [pscustomobject]@{ number = $m.number; title = $Matches[3]; desc = $null }
        $oldByNumber[$m.number] = $Matches[2]
    }
    elseif ($m.title -match '^System\.Collections\.Specialized\.OrderedDictionary\[([^\]]+)\]\.title$') {
        $key = $Matches[1]
        $fixes += [pscustomobject]@{ number = $m.number; title = $blocks[$key].title; desc = $blocks[$key].desc }
        $newIds[$key] = $m.number
    }
    elseif ($m.title -match '^v0\.1[4-9]\.0 — |^v1\.0\.0-rc\.1$') {
        $key = ($m.title -split ' ')[0]; $newIds[$key] = $m.number
    }
}
Write-Output "=== title repairs ($($fixes.Count)) ==="
$fixes | ForEach-Object { "#{0,-3} -> {1}" -f $_.number, $_.title }
Write-Output "=== block ids ==="
$newIds.Keys | Sort-Object | ForEach-Object { "$_ = #$($newIds[$_])" }

# Old milestone title -> old milestone number (from the parsed titles)
$oldNumberByTitle = @{}; foreach ($k in $oldByNumber.Keys) { $oldNumberByTitle[$oldByNumber[$k]] = $k }
$blockOfOld = @{}; foreach ($k in $blocks.Keys) { foreach ($f in $blocks[$k].from) { $blockOfOld[$f] = $k } }

$rows = Import-Csv $Csv
$moves = @()
foreach ($r in $rows) {
    $n = [int]$r.number
    if ($epicMoves.ContainsKey($n)) { $moves += [pscustomobject]@{ number = $n; to = $epicMoves[$n] }; continue }
    if ($r.priority -notin @('P0', 'P1')) { continue }
    $target = if ($featureP1Overrides.ContainsKey($n)) { $featureP1Overrides[$n] } elseif ($blockOfOld.ContainsKey($r.milestone)) { $blockOfOld[$r.milestone] } else { 'v0.17.0' }
    $moves += [pscustomobject]@{ number = $n; to = $target }
}
Write-Output "=== moves ($($moves.Count)) ==="
$moves | Group-Object to | Sort-Object Name | ForEach-Object { "{0,-12} {1,3}" -f $_.Name, $_.Count }
if (-not $Execute) { Write-Output 'dry run: nothing changed'; return }

foreach ($f in $fixes) {
    $t = $f.title
    if ($f.desc) { $d = $f.desc; gh api --method PATCH "repos/$Repo/milestones/$($f.number)" -f "title=$t" -f "description=$d" | Out-Null }
    else { gh api --method PATCH "repos/$Repo/milestones/$($f.number)" -f "title=$t" | Out-Null }
    Write-Output "repaired #$($f.number): $t"
}
$done = 0; $failed = @()
foreach ($mv in $moves) {
    $id = $newIds[$mv.to]
    if (-not $id) { $failed += "#$($mv.number): no milestone id for $($mv.to)"; continue }
    $r = gh api --method PATCH "repos/$Repo/issues/$($mv.number)" -F "milestone=$id" 2>&1
    if ($LASTEXITCODE -ne 0) { $failed += "#$($mv.number): $r" } else { $done++ }
    if ($done % 25 -eq 0 -and $done -gt 0) { Write-Output "moved $done" }
}
Write-Output "moved=$done failed=$($failed.Count)"
$failed | ForEach-Object { Write-Output $_ }
$after = gh api "repos/$Repo/milestones?state=open&per_page=100" --paginate | ConvertFrom-Json
$after | Sort-Object title | ForEach-Object { "{0,-70} {1,3} open" -f $_.title, $_.open_issues }
