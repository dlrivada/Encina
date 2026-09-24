# SPEC-003 evidence extractor (batch 1, #1311): for each closed issue, fetches the body, the human
# comments (no bots), the labels, the milestone, the close reason and date, and the linked pull
# requests. PR links come from `closedByPullRequestsReferences` (GraphQL) and timeline
# cross-references (REST): pilot 1 showed `gh pr list --search` misses them for early issues. For
# each PR it also fetches the title, the merge commit and the files it changed.
#
# One JSON file per issue at <OutDir>/<n>.json. Resumable: an existing file is skipped unless -Force.
#
# Usage:
#   pwsh -File tools/ai/fetch-closed-issue-data.ps1 -Numbers 1,21,1155,1273
#   pwsh -File tools/ai/fetch-closed-issue-data.ps1 -Since 2026-09-01
#   pwsh -File tools/ai/fetch-closed-issue-data.ps1                      # every closed issue

param(
    [string]$Repo = 'dlrivada/Encina',
    [string]$OutDir = (Join-Path (git rev-parse --show-toplevel) 'artifacts\knowledge\raw'),
    [int[]]$Numbers = @(),
    [string]$Since = '',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$bots = @('coderabbitai[bot]', 'github-actions[bot]', 'codecov[bot]', 'dependabot[bot]', 'sonarqubecloud[bot]', 'copilot[bot]')
$repoParts = $Repo.Split('/')
$owner = $repoParts[0]
$repoName = $repoParts[1]

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Force -Path $OutDir | Out-Null }

function Get-ClosedIssues {
    $searchArgs = @()
    if ($Since) { $searchArgs = @('--search', "closed:>$Since") }
    $issues = gh issue list --repo $Repo --state closed --limit 2000 @searchArgs `
        --json number,title,body,labels,milestone,closedAt,stateReason,comments | ConvertFrom-Json
    if ($Numbers.Count -gt 0) { $issues = @($issues | Where-Object { $Numbers -contains [int]$_.number }) }
    return $issues
}

# closedByPullRequestsReferences: the GraphQL field pilot 1 found reliable for early issues, where
# `gh pr list --search` misses the link. Returns fully-detailed PR objects in one call.
$closingPrsQuery = @'
query($owner: String!, $repo: String!, $number: Int!) {
  repository(owner: $owner, name: $repo) {
    issue(number: $number) {
      closedByPullRequestsReferences(first: 50, includeClosedPrs: true) {
        nodes {
          number
          title
          state
          merged
          mergedAt
          closedAt
          mergeCommit { oid }
          files(first: 100) { nodes { path additions deletions } }
        }
      }
    }
  }
}
'@

function ConvertTo-PrRecord($node) {
    [pscustomobject]@{
        number        = $node.number
        title         = $node.title
        state         = $node.state
        merged        = [bool]$node.merged
        mergedAt      = $node.mergedAt
        closedAt      = $node.closedAt
        mergeCommit   = if ($node.mergeCommit) { $node.mergeCommit.oid } else { $null }
        files         = @($node.files.nodes | ForEach-Object { [pscustomobject]@{ path = $_.path; additions = $_.additions; deletions = $_.deletions } })
    }
}

function Get-ClosingPrs([int]$number) {
    try {
        $raw = gh api graphql -f query=$closingPrsQuery -F "owner=$owner" -F "repo=$repoName" -F "number=$number" 2>$null
        if (-not $raw) { return @() }
        $data = $raw | ConvertFrom-Json
        $nodes = $data.data.repository.issue.closedByPullRequestsReferences.nodes
        if (-not $nodes) { return @() }
        return @($nodes | ForEach-Object { ConvertTo-PrRecord $_ })
    }
    catch { return @() }
}

# Timeline cross-references catch PRs that mention the issue without a closing keyword, or that
# GraphQL's closedByPullRequestsReferences does not surface.
function Get-TimelineReferencedPrNumbers([int]$number) {
    try {
        $tl = gh api ("repos/$Repo/issues/$number/timeline?per_page=100") --paginate 2>$null | ConvertFrom-Json
        $result = New-Object System.Collections.Generic.List[int]
        foreach ($e in $tl) {
            if ($e.event -eq 'cross-referenced' -and $e.source.issue -and $e.source.issue.pull_request) {
                $result.Add([int]$e.source.issue.number)
            }
        }
        return @($result | Select-Object -Unique)
    }
    catch { return @() }
}

function Get-PrByNumber([int]$number) {
    try {
        $json = gh pr view $number --repo $Repo --json number,title,state,mergedAt,closedAt,mergeCommit,files 2>$null
        if (-not $json) { return $null }
        $pr = $json | ConvertFrom-Json
        return [pscustomobject]@{
            number        = $pr.number
            title         = $pr.title
            state         = $pr.state
            merged        = ($pr.state -eq 'MERGED')
            mergedAt      = $pr.mergedAt
            closedAt      = $pr.closedAt
            mergeCommit   = if ($pr.mergeCommit) { $pr.mergeCommit.oid } else { $null }
            files         = @($pr.files | ForEach-Object { [pscustomobject]@{ path = $_.path; additions = $_.additions; deletions = $_.deletions } })
        }
    }
    catch { return $null }
}

function Get-LinkedPrs([int]$number) {
    $prs = @{}
    foreach ($pr in (Get-ClosingPrs $number)) { $prs[[int]$pr.number] = $pr }
    foreach ($n in (Get-TimelineReferencedPrNumbers $number)) {
        if (-not $prs.ContainsKey($n)) {
            $pr = Get-PrByNumber $n
            if ($pr) { $prs[$n] = $pr }
        }
    }
    return @($prs.Values | Sort-Object number)
}

$issues = Get-ClosedIssues
Write-Output "closed issues to process: $($issues.Count)"

$done = 0
$skipped = 0
foreach ($i in $issues) {
    $outFile = Join-Path $OutDir "$($i.number).json"
    if ((Test-Path $outFile) -and -not $Force) {
        $skipped++
        continue
    }

    $comments = @($i.comments | Where-Object { $_.author.login -notin $bots } | ForEach-Object {
        [pscustomobject]@{
            author    = $_.author.login
            createdAt = $_.createdAt
            body      = $_.body
        }
    })

    $record = [pscustomobject]@{
        number      = $i.number
        title       = $i.title
        body        = $i.body
        closedAt    = $i.closedAt
        stateReason = $i.stateReason
        labels      = @($i.labels | ForEach-Object { $_.name })
        milestone   = if ($i.milestone) { $i.milestone.title } else { $null }
        comments    = $comments
        prs         = @(Get-LinkedPrs ([int]$i.number))
    }

    $record | ConvertTo-Json -Depth 10 | Set-Content -Path $outFile -Encoding utf8
    $done++
    if (($done + $skipped) % 25 -eq 0) { Write-Output "processed $($done + $skipped) / $($issues.Count) (written: $done, skipped: $skipped)" }
}

Write-Output "done: written $done, skipped $skipped (existing) -> $OutDir"
