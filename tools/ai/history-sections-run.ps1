param([string]$Repo = (git rev-parse --show-toplevel), [string[]]$Areas = @())
$Areas = @($Areas | ForEach-Object { $_ -split ',' } | Where-Object { $_ })

# Consolidation step 2: one draft section per area from knowledge-areas.csv, written by the local model.
$ErrorActionPreference = 'Stop'
$dir = Join-Path $Repo 'artifacts\local-ai\historian'
$outDir = Join-Path $Repo 'artifacts\local-ai\out\history'
New-Item -ItemType Directory -Force -Path $outDir | Out-Null
$rules = Join-Path $Repo 'tools\ai\briefs\history-section-rules.md'
$script = Join-Path $Repo 'tools\ai\local-ai-ask.cs'
$sys = Join-Path $Repo 'tools\ai\briefs\system-history-writer.md'
Set-Content -Path $sys -Encoding utf8 -Value 'You are a precise technical writer consolidating the engineering history of the Encina .NET repository. You write only from the items given, cite every bullet with its issue numbers, merge duplicates, and never invent facts. You output only the requested Markdown section.'
$titles = @{ core = 'Core pipeline and results'; messaging = 'Messaging patterns and transports'; data = 'Data access and database providers'; caching = 'Caching'; eventsourcing = 'Event sourcing and Marten'; validation = 'Validation'; observability = 'Observability, health checks and logging'; 'security-compliance' = 'Security and regulatory compliance'; 'testing-quality' = 'Testing and the quality system'; 'ci-process' = 'CI, releases and repository process'; 'docs-dx' = 'Documentation and developer experience'; 'web-cloud' = 'Web, APIs and cloud hosting'; 'modules-tenancy' = 'Modules, tenancy and sharding'; resilience = 'Resilience' }
$rows = Import-Csv (Join-Path $dir 'knowledge-areas.csv') | Where-Object area -ne ''
foreach ($g in ($rows | Group-Object area | Sort-Object Name)) {
    $area = $g.Name
    if ($Areas.Count -gt 0 -and $Areas -notcontains $area) { continue }
    # Pre-merge duplicates so the model only sees issue numbers (never internal item ids, which it once cited as issues).
    $byId = @{}; foreach ($x in $g.Group) { $byId[$x.id] = $x }
    $primary = @($g.Group | Where-Object { -not $_.duplicate_of -or -not $byId.ContainsKey($_.duplicate_of) })
    $lines = foreach ($x in $primary) {
        $dups = @($g.Group | Where-Object { $_.duplicate_of -eq $x.id })
        $nums = @($x.number) + @($dups | ForEach-Object { $_.number }) | Select-Object -Unique
        $numText = ($nums | ForEach-Object { "issue #$_" }) -join ', '
        "- $numText ($($x.type); still relevant: $($x.still_relevant)): $($x.statement) | evidence: $($x.evidence)"
    }
    $body = $lines -join "`n"
    # Allowed citations: the items' own issues plus any issue number mentioned inside a statement or evidence text.
    $allowed = @([regex]::Matches($body, '#(\d+)') | ForEach-Object { [int]$_.Groups[1].Value } | Select-Object -Unique)
    $own = @($g.Group | ForEach-Object { [int]$_.number } | Select-Object -Unique)
    $bf = Join-Path $dir "history-batch-$area.md"
    Set-Content -Path $bf -Encoding utf8 -Value (([IO.File]::ReadAllText($rules)) + "Area: $($titles[$area])`n`nThe issue numbers of this area are: " + (($own | Sort-Object | ForEach-Object { "#$_" }) -join ', ') + ". Cite those; a number mentioned inside an item (a consolidation target) may also be cited. Never write any other number after a `#`.`n`n" + $body + "`n")
    $of = Join-Path $outDir "$area.md"
    $best = $null; $bestScore = -1
    for ($attempt = 1; $attempt -le 3; $attempt++) {
        $af = Join-Path $outDir ("$area.attempt$attempt.md")
        $log = & dotnet run $script -- --task "history-$area" --brief $bf --system $sys --out $af --max-tokens 4096 2>&1
        $t = Get-Content $af -Raw
        $cited = @([regex]::Matches($t, '#(\d+)') | ForEach-Object { [int]$_.Groups[1].Value } | Select-Object -Unique)
        $badCites = @($cited | Where-Object { $allowed -notcontains $_ })
        $bullets = [regex]::Matches($t, '(?m)^- ').Count
        $score = if ($badCites.Count -eq 0) { $bullets } else { -1 }
        Write-Output ("{0}: attempt {1} items={2} bullets={3} words={4} badCitations={5} {6}" -f $area, $attempt, $g.Count, $bullets, ($t -split '\s+').Count, ($badCites -join ' '), ($log | Select-Object -Last 1))
        if ($score -gt $bestScore) { $bestScore = $score; $best = $af }
        if ($score -gt 0) { break }
    }
    if ($best) { Copy-Item $best $of -Force }
    Write-Output ("{0}: kept {1} (score {2})" -f $area, (Split-Path $best -Leaf), $bestScore)
}
