param(
    [string]$Repo = 'dlrivada/Encina',
    [string]$Out = (Join-Path (git rev-parse --show-toplevel) 'artifacts\local-ai\historian\closed-evidence.json'),
    [int]$BodyChars = 2000,
    [int]$CommentChars = 700,
    [int]$MaxComments = 15
)

# Archaeology extractor: every closed issue with body, human comments, close date and the PRs/issues that reference it.
$ErrorActionPreference = 'Continue'
$bots = @('coderabbitai[bot]', 'github-actions[bot]', 'codecov[bot]', 'dependabot[bot]', 'sonarqubecloud[bot]', 'copilot[bot]')
function Clean([string]$t, [int]$max) {
    if ([string]::IsNullOrWhiteSpace($t)) { return '' }
    $t = ($t -replace '```[\s\S]*?```', '[code]') -replace '<!--[\s\S]*?-->', ''
    $t = ($t -replace '\r?\n+', ' ') -replace '\s{2,}', ' '
    $t = $t.Trim()
    if ($t.Length -gt $max) { $t = $t.Substring(0, $max) + '…' }
    return $t
}
$closed = gh issue list --repo $Repo --state closed --limit 1000 --json number,title,labels,milestone,createdAt,closedAt,body,comments,stateReason | ConvertFrom-Json
Write-Output "closed issues: $($closed.Count)"
$results = New-Object System.Collections.Generic.List[object]
$n = 0
foreach ($i in $closed) {
    $n++
    $refs = @()
    try {
        $tl = gh api ("repos/$Repo/issues/$($i.number)/timeline?per_page=100") --paginate 2>$null | ConvertFrom-Json
        foreach ($e in $tl) { if ($e.event -eq 'cross-referenced' -and $e.source.issue) { $s = $e.source.issue; $kind = if ($s.pull_request) { 'PR' } else { 'issue' }; $st = if ($s.pull_request -and $s.pull_request.merged_at) { 'merged' } else { $s.state }; $refs += "$kind #$($s.number) [$st] $($s.title)" } }
    } catch { }
    $comments = @($i.comments | Where-Object { $_.author.login -notin $bots } | Select-Object -First $MaxComments | ForEach-Object { [pscustomobject]@{ author = $_.author.login; date = ([datetime]$_.createdAt).ToString('yyyy-MM-dd'); text = Clean $_.body $CommentChars } })
    $results.Add([pscustomobject]@{
        number = $i.number; title = $i.title; milestone = if ($i.milestone) { $i.milestone.title } else { '' }
        created = ([datetime]$i.createdAt).ToString('yyyy-MM-dd'); closed = ([datetime]$i.closedAt).ToString('yyyy-MM-dd'); state_reason = $i.stateReason
        labels = @($i.labels | ForEach-Object { $_.name }); body = Clean $i.body $BodyChars; comments = $comments; references = @($refs | Select-Object -Unique)
    })
    if ($n % 50 -eq 0) { Write-Output "extracted $n / $($closed.Count)" }
}
$results | ConvertTo-Json -Depth 6 | Set-Content -Path $Out -Encoding utf8
Write-Output "done: $($results.Count) -> $Out"
