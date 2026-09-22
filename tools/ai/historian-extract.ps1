param(
    [string]$Repo = 'dlrivada/Encina',
    [string]$RepoPath = (git rev-parse --show-toplevel),
    [string]$Csv = (Join-Path (git rev-parse --show-toplevel) 'artifacts\local-ai\issues\classification-final.csv'),
    [string]$Out = (Join-Path (git rev-parse --show-toplevel) 'artifacts\local-ai\historian\evidence.json'),
    [int]$BodyChars = 2500,
    [int]$CommentChars = 600,
    [int]$MaxComments = 12
)

# Historian evidence extractor: for every classified issue (EPICs excluded) collects what the local model
# needs to judge whether the issue is implemented, partial, not implemented or superseded, with provenance:
#   - body (truncated), human comments (author, date, text), cross-referencing PRs/issues with state,
#     referenced commits, and a local code/doc search for the PascalCase / package identifiers in the title.
# No judgement here; this is input for historian-run.ps1.

$ErrorActionPreference = 'Continue'
$bots = @('coderabbitai[bot]', 'github-actions[bot]', 'codecov[bot]', 'dependabot[bot]', 'sonarqubecloud[bot]', 'copilot[bot]')
$rows = Import-Csv $Csv | Where-Object { $_.priority -ne '' }
$open = Get-Content (Join-Path $RepoPath 'artifacts\local-ai\issues\open-issues.json') -Raw | ConvertFrom-Json
$byNumber = @{}; foreach ($i in $open) { $byNumber[[int]$i.number] = $i }

function Clean([string]$t, [int]$max) {
    if ([string]::IsNullOrWhiteSpace($t)) { return '' }
    $t = ($t -replace '```[\s\S]*?```', '[code]') -replace '<!--[\s\S]*?-->', ''
    $t = ($t -replace '\r?\n+', ' ') -replace '\s{2,}', ' '
    $t = $t.Trim()
    if ($t.Length -gt $max) { $t = $t.Substring(0, $max) + '…' }
    return $t
}

function Identifiers([string]$title) {
    $t = $title -replace '^\[[A-Z]+\]\s*', ''
    $ids = [regex]::Matches($t, '\b(Encina(?:\.[A-Za-z0-9]+)+|[A-Z][a-z0-9]+(?:[A-Z][a-z0-9]+)+(?:<[^>]+>)?|[A-Z][A-Za-z0-9]*Async)\b') | ForEach-Object { $_.Value -replace '<.*$', '' } | Select-Object -Unique
    return @($ids | Where-Object { $_ -notin @('Encina', 'GitHub', 'NuGet', 'OpenTelemetry', 'EntityFramework', 'SqlServer', 'PostgreSQL', 'MySQL', 'MongoDB', 'RabbitMQ', 'FluentValidation', 'DataAnnotations', 'AspNetCore') } | Select-Object -First 4)
}

$searchRoots = @('src', 'tests', 'docs', '.github')
$results = New-Object System.Collections.Generic.List[object]
$n = 0
foreach ($r in $rows) {
    $n++
    $num = [int]$r.number
    $i = $byNumber[$num]
    $tl = @()
    try { $tl = gh api ("repos/$Repo/issues/$num/timeline?per_page=100") --paginate 2>$null | ConvertFrom-Json } catch { }
    $comments = @(); $refs = @(); $commits = @()
    foreach ($e in $tl) {
        switch ($e.event) {
            'commented' { if ($e.user.login -notin $bots -and $comments.Count -lt $MaxComments) { $comments += [pscustomobject]@{ author = $e.user.login; date = ([datetime]$e.created_at).ToString('yyyy-MM-dd'); text = Clean $e.body $CommentChars } } }
            'cross-referenced' { $s = $e.source.issue; if ($s) { $kind = if ($s.pull_request) { 'PR' } else { 'issue' }; $merged = if ($s.pull_request -and $s.pull_request.merged_at) { 'merged ' + ([datetime]$s.pull_request.merged_at).ToString('yyyy-MM-dd') } else { $s.state }; $refs += "$kind #$($s.number) [$merged] $($s.title)" } }
            'referenced' { if ($e.commit_id) { $commits += $e.commit_id.Substring(0, 8) } }
        }
    }
    $ids = Identifiers $i.title
    $hits = @()
    foreach ($id in $ids) {
        # git grep is indexed and fast; -F literal, -l file names only, limited to the tracked source/doc trees.
        $found = @(git -C $RepoPath grep -l -F -- $id -- $searchRoots 2>$null | Select-Object -First 6)
        $hits += [pscustomobject]@{ identifier = $id; files = $found }
    }
    $results.Add([pscustomobject]@{
        number = $num
        title = $i.title
        priority = $r.priority
        milestone = $r.milestone
        created = ([datetime]$i.createdAt).ToString('yyyy-MM-dd')
        labels = @($i.labels | ForEach-Object { $_.name })
        body = Clean $i.body $BodyChars
        comments = $comments
        references = @($refs | Select-Object -Unique)
        commits = @($commits | Select-Object -Unique)
        code_search = $hits
    })
    if ($n % 25 -eq 0) { Write-Output "extracted $n / $($rows.Count)" }
}
New-Item -ItemType Directory -Force -Path (Split-Path $Out) | Out-Null
$results | ConvertTo-Json -Depth 6 | Set-Content -Path $Out -Encoding utf8
Write-Output "done: $($results.Count) issues -> $Out"
