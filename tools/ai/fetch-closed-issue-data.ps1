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
    # [string[]] rather than [int[]]: invoked through `pwsh -File`, an unquoted comma list
    # ("-Numbers 1,21,1155,1273") arrives as ONE argv string, not four ints — PowerShell only splits
    # commas into an array when it parses the expression itself (console, -Command), not for -File's
    # raw argv. Each element is split on commas/whitespace below so both call styles work.
    [string[]]$Numbers = @(),
    [string]$Since = '',
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$bots = @('coderabbitai[bot]', 'github-actions[bot]', 'codecov[bot]', 'dependabot[bot]', 'sonarqubecloud[bot]', 'copilot[bot]')
$repoParts = $Repo.Split('/')
$owner = $repoParts[0]
$repoName = $repoParts[1]
$parsedNumbers = @($Numbers | ForEach-Object { $_ -split '[,\s]+' } | Where-Object { $_ -ne '' } | ForEach-Object { [int]$_ })

if (-not (Test-Path $OutDir)) { New-Item -ItemType Directory -Force -Path $OutDir | Out-Null }

function Get-ClosedIssues {
    $searchArgs = @()
    if ($Since) { $searchArgs = @('--search', "closed:>$Since") }
    $issues = gh issue list --repo $Repo --state closed --limit 2000 @searchArgs `
        --json number,title,body,labels,milestone,closedAt,stateReason,comments | ConvertFrom-Json
    if ($parsedNumbers.Count -gt 0) { $issues = @($issues | Where-Object { $parsedNumbers -contains [int]$_.number }) }
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

# $script:currentIssueErrors is reset per issue in the main loop below. A `gh` failure (network,
# auth, rate limit) must never be recorded as "this issue has no linked PRs" — that would silently
# write wrong knowledge into the record. Every helper below distinguishes "gh succeeded with an
# empty result" (a real, empty answer) from "gh failed" (an error, captured and surfaced).
function Get-ClosingPrs([int]$number) {
    try {
        $raw = gh api graphql -f query=$closingPrsQuery -F "owner=$owner" -F "repo=$repoName" -F "number=$number" 2>&1
        if ($LASTEXITCODE -ne 0) {
            $script:currentIssueErrors.Add("gh api graphql (closedByPullRequestsReferences) failed for #$number : $raw")
            return @()
        }
        $data = $raw | ConvertFrom-Json
        $nodes = $data.data.repository.issue.closedByPullRequestsReferences.nodes
        if (-not $nodes) { return @() }
        return @($nodes | ForEach-Object { ConvertTo-PrRecord $_ })
    }
    catch {
        $script:currentIssueErrors.Add("gh api graphql (closedByPullRequestsReferences) threw for #$number : $($_.Exception.Message)")
        return @()
    }
}

# Timeline cross-references catch PRs that mention the issue without a closing keyword, or that
# GraphQL's closedByPullRequestsReferences does not surface.
function Get-TimelineReferencedPrNumbers([int]$number) {
    try {
        $tl = gh api ("repos/$Repo/issues/$number/timeline?per_page=100") --paginate 2>&1
        if ($LASTEXITCODE -ne 0) {
            $script:currentIssueErrors.Add("gh api timeline failed for #$number : $tl")
            return @()
        }
        $events = $tl | ConvertFrom-Json
        $result = New-Object System.Collections.Generic.List[int]
        foreach ($e in $events) {
            if ($e.event -eq 'cross-referenced' -and $e.source.issue -and $e.source.issue.pull_request) {
                $result.Add([int]$e.source.issue.number)
            }
        }
        return @($result | Select-Object -Unique)
    }
    catch {
        $script:currentIssueErrors.Add("gh api timeline threw for #$number : $($_.Exception.Message)")
        return @()
    }
}

function Get-PrByNumber([int]$number) {
    try {
        $json = gh pr view $number --repo $Repo --json number,title,state,mergedAt,closedAt,mergeCommit,files 2>&1
        if ($LASTEXITCODE -ne 0) {
            $script:currentIssueErrors.Add("gh pr view $number failed : $json")
            return $null
        }
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
    catch {
        $script:currentIssueErrors.Add("gh pr view $number threw : $($_.Exception.Message)")
        return $null
    }
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
$failed = 0
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

    # Reset per issue: a `gh` failure while fetching this issue's PRs must not leak into the next.
    $script:currentIssueErrors = New-Object System.Collections.Generic.List[string]
    $prs = @(Get-LinkedPrs ([int]$i.number))

    $record = [pscustomobject]@{
        number      = $i.number
        title       = $i.title
        body        = $i.body
        closedAt    = $i.closedAt
        stateReason = $i.stateReason
        labels      = @($i.labels | ForEach-Object { $_.name })
        milestone   = if ($i.milestone) { $i.milestone.title } else { $null }
        comments    = $comments
        prs         = $prs
        fetchErrors = @($script:currentIssueErrors)
    }

    $record | ConvertTo-Json -Depth 10 | Set-Content -Path $outFile -Encoding utf8
    $done++
    if ($script:currentIssueErrors.Count -gt 0) {
        $failed++
        Write-Warning "issue #$($i.number): $($script:currentIssueErrors.Count) fetch error(s) recorded in fetchErrors — its PR list is incomplete; re-run with -Force -Numbers $($i.number) once the cause is fixed"
        foreach ($e in $script:currentIssueErrors) { Write-Warning "  $e" }
    }
    if (($done + $skipped) % 25 -eq 0) { Write-Output "processed $($done + $skipped) / $($issues.Count) (written: $done, skipped: $skipped, with errors: $failed)" }
}

Write-Output "done: written $done, skipped $skipped (existing), $failed with fetch errors -> $OutDir"
if ($failed -gt 0) {
    Write-Error "$failed issue(s) have incomplete evidence (see fetchErrors in their JSON and the warnings above); never treat their empty PR lists as ground truth."
    exit 1
}
