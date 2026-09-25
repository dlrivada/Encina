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
    # --limit is generous (well above any realistic closed-issue count) rather than a small cap: `gh
    # issue list` paginates internally up to --limit, so a low cap silently truncates the result set
    # with no error. A `gh` failure here (network, auth, rate limit) must not be mistaken for "zero
    # closed issues" — check $LASTEXITCODE before parsing, matching every other gh call below.
    $raw = gh issue list --repo $Repo --state closed --limit 100000 @searchArgs `
        --json number,title,body,labels,milestone,closedAt,stateReason,comments 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "gh issue list failed (exit $LASTEXITCODE): $raw"
    }
    $issues = $raw | ConvertFrom-Json
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
    $files = @($node.files.nodes | ForEach-Object { [pscustomobject]@{ path = $_.path; additions = $_.additions; deletions = $_.deletions } })
    # files(first: 100) silently truncates a PR touching more than 100 files. There is no error to
    # detect from the API response itself, so flag the exact page-size boundary as suspicious.
    if ($files.Count -eq 100) {
        $script:currentIssueErrors.Add("PR #$($node.number): files list has exactly 100 entries — GraphQL's files(first: 100) page size; it may be truncated for a large PR. Verify with 'gh pr view $($node.number) --repo $Repo --json files' (paginated) if the file count matters.")
    }
    [pscustomobject]@{
        number        = $node.number
        title         = $node.title
        state         = $node.state
        merged        = [bool]$node.merged
        mergedAt      = $node.mergedAt
        closedAt      = $node.closedAt
        mergeCommit   = if ($node.mergeCommit) { $node.mergeCommit.oid } else { $null }
        files         = $files
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
        # A cross-referenced PR can live in another repository (e.g. a fork). Get-PrByNumber below
        # always queries --repo $Repo, so a foreign PR number would either resolve to an unrelated
        # local PR that happens to share the number, or fail outright — both silently wrong. Only
        # local PRs are supported here; a foreign one is recorded as skipped, never guessed at.
        $expectedRepoUrl = "https://api.github.com/repos/$Repo"
        $result = New-Object System.Collections.Generic.List[int]
        foreach ($e in $events) {
            if ($e.event -eq 'cross-referenced' -and $e.source.issue -and $e.source.issue.pull_request) {
                $refRepoUrl = $e.source.issue.repository_url
                if ($refRepoUrl -and $refRepoUrl -ne $expectedRepoUrl) {
                    $script:currentIssueErrors.Add("issue #$number : cross-referenced PR #$($e.source.issue.number) is in a different repository ($refRepoUrl), not $Repo — skipped, not queried against $Repo")
                    continue
                }
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
        $files = @($pr.files | ForEach-Object { [pscustomobject]@{ path = $_.path; additions = $_.additions; deletions = $_.deletions } })
        if ($files.Count -eq 100) {
            $script:currentIssueErrors.Add("PR #$($pr.number): files list has exactly 100 entries — 'gh pr view --json files' page size; it may be truncated for a large PR.")
        }
        return [pscustomobject]@{
            number        = $pr.number
            title         = $pr.title
            state         = $pr.state
            merged        = ($pr.state -eq 'MERGED')
            mergedAt      = $pr.mergedAt
            closedAt      = $pr.closedAt
            mergeCommit   = if ($pr.mergeCommit) { $pr.mergeCommit.oid } else { $null }
            files         = $files
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

    # Reset per issue: a `gh` failure while fetching this issue's PRs must not leak into the next.
    $script:currentIssueErrors = New-Object System.Collections.Generic.List[string]

    # gh issue list --json comments requests comments through GraphQL's default page size (100). An
    # issue with more than 100 comments is silently truncated with no error from `gh` itself — flag
    # the exact page-size boundary as suspicious rather than trust it as the full comment list.
    $rawCommentsCount = @($i.comments).Count
    if ($rawCommentsCount -eq 100) {
        $script:currentIssueErrors.Add("issue #$($i.number): comments field has exactly 100 entries — the GraphQL page size; it may be truncated. Verify with 'gh issue view $($i.number) --repo $Repo --comments' if the comment count matters.")
    }

    $comments = @($i.comments | Where-Object { $_.author.login -notin $bots } | ForEach-Object {
        [pscustomobject]@{
            author    = $_.author.login
            createdAt = $_.createdAt
            body      = $_.body
        }
    })

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
