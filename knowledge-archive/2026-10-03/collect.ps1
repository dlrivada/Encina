param([Parameter(Mandatory)][int]$Issue, [string]$Status = 'done', [string]$Notes = '')
# Collects the outputs of a finished per-issue audit worktree into artifacts/knowledge and removes the worktree.
$root = 'D:\Proyectos\Encina'
$wt = Join-Path $root ".claude\worktrees\wia-$Issue"
$dst = Join-Path $root 'artifacts\knowledge'
foreach ($sub in 'issues','audits','remediation') { New-Item -ItemType Directory -Force (Join-Path $dst $sub) | Out-Null }
$src = Join-Path $wt 'artifacts\knowledge'
if (Test-Path $src) {
    Get-ChildItem (Join-Path $src 'issues') -File -ErrorAction SilentlyContinue | Copy-Item -Destination (Join-Path $dst 'issues') -Force
    Get-ChildItem (Join-Path $src 'audits') -File -ErrorAction SilentlyContinue | Copy-Item -Destination (Join-Path $dst 'audits') -Force
    Get-ChildItem (Join-Path $src 'remediation') -File -ErrorAction SilentlyContinue | Copy-Item -Destination (Join-Path $dst 'remediation') -Force
}
# Workers keep writing drafts to artifacts\issues\ (the issue-worker's default follow-up folder); pick those up too.
Get-ChildItem (Join-Path $wt 'artifacts\issues') -Filter "$Issue-*.md" -File -ErrorAction SilentlyContinue | Copy-Item -Destination (Join-Path $dst 'remediation') -Force
$ledger = Join-Path $wt 'artifacts\agent-usage\ledger.csv'
if (Test-Path $ledger) { Get-Content $ledger | Select-Object -Skip 1 | Add-Content (Join-Path $dst 'agent-ledger.csv') }
$rem = @(Get-ChildItem (Join-Path $dst 'remediation') -Filter "$Issue-*.md" -ErrorAction SilentlyContinue).Count
Add-Content (Join-Path $dst 'progress.csv') "$Issue,$Status,,,,$rem,`"$Notes`""
git -C $root worktree remove $wt --force 2>&1 | Out-Null
"collected #$Issue (remediation drafts: $rem)"
