# tools/ai/priority/collect-issues.ps1 (#1552)
#
# Reads every OPEN issue of dlrivada/Encina through gh's GraphQL API, paged, and writes
# artifacts/priority/issues.json: one entry per issue with number, title, labels, milestone,
# createdAt, body, and 'dependents' — the numbers of OTHER open issues whose body references this
# issue with a dependency keyword ("blocked by", "depends on", "requires", "after", "needs" + #n).
#
# Usage: pwsh -NoProfile -File tools/ai/priority/collect-issues.ps1 [-Out <file>] [-Repo owner/name] [-PageSize 100]

param(
    [string]$Out = (Join-Path (git rev-parse --show-toplevel) 'artifacts/priority/issues.json'),
    [string]$Repo = 'dlrivada/Encina',
    [int]$PageSize = 100
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_priority-lib.ps1')

$parts = $Repo.Split('/')
if ($parts.Count -ne 2) { throw "Repo must be 'owner/name', got '$Repo'" }
$owner, $name = $parts

$query = @'
query($owner: String!, $name: String!, $pageSize: Int!, $cursor: String) {
  repository(owner: $owner, name: $name) {
    issues(states: OPEN, first: $pageSize, after: $cursor, orderBy: {field: CREATED_AT, direction: ASC}) {
      pageInfo { hasNextPage endCursor }
      nodes {
        number
        title
        createdAt
        body
        milestone { title }
        labels(first: 30) { nodes { name } }
      }
    }
  }
}
'@

$nodes = New-Object System.Collections.Generic.List[object]
$cursor = $null
$hasNext = $true
$page = 0
while ($hasNext) {
    $page++
    $ghArgs = @('api', 'graphql', '-f', "query=$query", '-f', "owner=$owner", '-f', "name=$name", '-F', "pageSize=$PageSize")
    if ($cursor) { $ghArgs += @('-f', "cursor=$cursor") } else { $ghArgs += @('-F', 'cursor=null') }
    $raw = & gh @ghArgs 2>&1
    if ($LASTEXITCODE -ne 0) { throw "gh api graphql failed on page $page`: $raw" }
    $resp = $raw | ConvertFrom-Json
    $conn = $resp.data.repository.issues
    foreach ($n in $conn.nodes) { $nodes.Add($n) }
    $hasNext = [bool]$conn.pageInfo.hasNextPage
    $cursor = $conn.pageInfo.endCursor
    Write-Output "page $page`: +$($conn.nodes.Count) issues (total $($nodes.Count))"
}

$openNumbers = New-Object System.Collections.Generic.HashSet[int]
foreach ($n in $nodes) { [void]$openNumbers.Add([int]$n.number) }

# Reverse-dependency map: for every issue A, every keyword+#n reference to an OTHER open issue n
# records A as one of n's dependents.
$dependents = @{}
foreach ($n in $openNumbers) { $dependents[[int]$n] = New-Object System.Collections.Generic.HashSet[int] }
foreach ($a in $nodes) {
    $targets = Get-PriorityDependencyTargets -Body $a.body -SelfNumber ([int]$a.number)
    foreach ($t in $targets) {
        if ($dependents.ContainsKey([int]$t)) { [void]$dependents[[int]$t].Add([int]$a.number) }
    }
}

$issues = foreach ($n in $nodes) {
    $num = [int]$n.number
    [ordered]@{
        number     = $num
        title      = $n.title
        labels     = @($n.labels.nodes | ForEach-Object { $_.name })
        milestone  = if ($n.milestone) { $n.milestone.title } else { $null }
        createdAt  = $n.createdAt
        body       = $n.body
        dependents = @($dependents[$num] | Sort-Object)
    }
}

$outDir = Split-Path -Parent $Out
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Force -Path $outDir | Out-Null }
[ordered]@{
    collectedAtUtc = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
    repo           = $Repo
    count          = $issues.Count
    issues         = $issues
} | ConvertTo-Json -Depth 8 | Set-Content -Path $Out -Encoding utf8

Write-Output "collected $($issues.Count) open issues -> $Out"
