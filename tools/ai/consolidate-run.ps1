param(
    [string]$Repo = (git rev-parse --show-toplevel),
    [int]$ChunkSize = 95
)

# Consolidation step 1: the local model assigns an area and duplicate marks to every knowledge item.
# Input : historian/knowledge.csv ; Output: historian/knowledge-areas.csv (id, area, duplicate_of merged with the item)
$ErrorActionPreference = 'Stop'
$dir = Join-Path $Repo 'artifacts\local-ai\historian'
$outDir = Join-Path $Repo 'artifacts\local-ai\out'
$rules = Join-Path $Repo 'tools\ai\briefs\consolidate-areas-rules.md'
$script = Join-Path $Repo 'tools\ai\local-ai-ask.cs'
$sys = Join-Path $Repo 'tools\ai\briefs\system-json-areas.md'
Set-Content -Path $sys -Encoding utf8 -Value 'You classify knowledge items of the Encina .NET repository into a fixed list of areas and mark duplicates. You answer with a single JSON array and nothing else: no prose, no Markdown fences. Every input item appears exactly once, in input order, with the fields id, area, duplicate_of.'
$areas = @('core','messaging','data','caching','eventsourcing','validation','observability','security-compliance','testing-quality','ci-process','docs-dx','web-cloud','modules-tenancy','resilience')

$items = @(Import-Csv (Join-Path $dir 'knowledge.csv'))
$n = 0
foreach ($it in $items) { $n++; $it | Add-Member -NotePropertyName id -NotePropertyValue ('k{0:D3}' -f $n) -Force }
$chunks = [math]::Ceiling($items.Count / $ChunkSize)
Write-Output "items=$($items.Count) chunks=$chunks"
$assign = @{}
for ($c = 0; $c -lt $chunks; $c++) {
    $slice = @($items | Select-Object -Skip ($c * $ChunkSize) -First $ChunkSize)
    $body = ($slice | ForEach-Object { "- $($_.id) [#$($_.number), $($_.type)] $($_.statement)" }) -join "`n"
    $bf = Join-Path $dir ("areas-batch-{0:D2}.md" -f ($c + 1))
    Set-Content -Path $bf -Encoding utf8 -Value (([IO.File]::ReadAllText($rules)) + $body + "`n")
    $of = Join-Path $outDir ("areas-{0:D2}.md" -f ($c + 1))
    $ok = $false
    for ($attempt = 1; $attempt -le 2 -and -not $ok; $attempt++) {
        $log = & dotnet run $script -- --task ("areas-{0:D2}" -f ($c + 1)) --brief $bf --system $sys --out $of --max-tokens 6144 2>&1
        $raw = (Get-Content $of -Raw) -replace '^\s*```(json)?\s*', '' -replace '\s*```\s*$', ''
        try {
            $arr = @($raw | ConvertFrom-Json)
            $expected = @($slice | ForEach-Object { $_.id }); $got = @($arr | ForEach-Object { $_.id })
            $missing = @($expected | Where-Object { $got -notcontains $_ })
            $bad = @($arr | Where-Object { $areas -notcontains $_.area })
            if ($missing.Count -eq 0 -and $bad.Count -eq 0) { foreach ($a in $arr) { $assign[$a.id] = $a }; $ok = $true; Write-Output ("chunk {0}: ok ({1}) {2}" -f ($c + 1), $arr.Count, ($log | Select-Object -Last 1)) }
            else { Write-Output ("chunk {0}: attempt {1} incomplete (missing {2}, bad area {3})" -f ($c + 1), $attempt, $missing.Count, $bad.Count) }
        } catch { Write-Output ("chunk {0}: attempt {1} invalid JSON" -f ($c + 1), $attempt) }
    }
}
$rows = foreach ($it in $items) { $a = $assign[$it.id]; [pscustomobject]@{ id = $it.id; area = if ($a) { $a.area } else { '' }; duplicate_of = if ($a) { $a.duplicate_of } else { '' }; type = $it.type; number = $it.number; still_relevant = $it.still_relevant; statement = $it.statement; evidence = $it.evidence; title = $it.title } }
$rows | Export-Csv -Path (Join-Path $dir 'knowledge-areas.csv') -NoTypeInformation -Encoding utf8
"assigned=$(@($rows | Where-Object area -ne '').Count) duplicates=$(@($rows | Where-Object duplicate_of -ne '').Count)"
$rows | Where-Object area -ne '' | Group-Object area | Sort-Object Count -Descending | ForEach-Object { "{0}`t{1}" -f $_.Name, $_.Count }
