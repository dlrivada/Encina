param(
    [string]$Repo = (git rev-parse --show-toplevel),
    [int]$BatchSize = 10,
    [int[]]$OnlyBatches = @()
)

# Runs the archaeology pass over closed issues: the local model extracts durable knowledge with provenance.
# Inputs : historian/closed-evidence.json, briefs/archaeology-rules.md
# Outputs: historian/closed-batch-NN.md, out/archaeology-NN.md, historian/archaeology-NN.json,
#          historian/archaeology.csv (one row per issue) and historian/knowledge.csv (one row per knowledge item)

$ErrorActionPreference = 'Stop'
$dir = Join-Path $Repo 'artifacts\local-ai\historian'
$outDir = Join-Path $Repo 'artifacts\local-ai\out'
$rules = Join-Path $Repo 'tools\ai\briefs\archaeology-rules.md'
$script = Join-Path $Repo 'tools\ai\local-ai-ask.cs'
$sys = Join-Path $Repo 'tools\ai\briefs\system-json-archaeology.md'
Set-Content -Path $sys -Encoding utf8 -Value 'You are the Historian of the Encina .NET repository doing archaeology on closed issues. You extract only what the evidence says and cite its source ids. You answer with a single JSON array and nothing else: no prose, no Markdown fences. Every input issue appears exactly once, in input order, with the fields number, summary, outcome, knowledge, still_relevant, related.'

$all = @(Get-Content (Join-Path $dir 'closed-evidence.json') -Raw | ConvertFrom-Json | Sort-Object number)
$batches = [math]::Ceiling($all.Count / $BatchSize)
Write-Output "closed issues=$($all.Count) batches=$batches"

function Render($i) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("### Issue #$($i.number) | milestone: $($i.milestone) | created: $($i.created) | closed: $($i.closed) ($($i.state_reason)) | labels: $($i.labels -join ', ')")
    [void]$sb.AppendLine("title: $($i.title)")
    [void]$sb.AppendLine("body: $($i.body)")
    $c = 0; foreach ($x in $i.comments) { $c++; [void]$sb.AppendLine("c${c} ($($x.author), $($x.date)): $($x.text)") }
    $r = 0; foreach ($x in $i.references) { $r++; [void]$sb.AppendLine("r${r}: $x") }
    if ($i.comments.Count -eq 0 -and $i.references.Count -eq 0) { [void]$sb.AppendLine("(no comments, no references)") }
    [void]$sb.AppendLine()
    return $sb.ToString()
}

$validOutcome = @('delivered', 'rejected', 'superseded', 'duplicate', 'moved', 'unknown')
for ($b = 0; $b -lt $batches; $b++) {
    $n = $b + 1
    if ($OnlyBatches.Count -gt 0 -and $OnlyBatches -notcontains $n) { continue }
    $slice = @($all | Select-Object -Skip ($b * $BatchSize) -First $BatchSize)
    $text = ([IO.File]::ReadAllText($rules)) + (($slice | ForEach-Object { Render $_ }) -join '')
    $batchFile = Join-Path $dir ("closed-batch-{0:D2}.md" -f $n)
    Set-Content -Path $batchFile -Encoding utf8 -Value $text
    $outFile = Join-Path $outDir ("archaeology-{0:D2}.md" -f $n)
    $jsonFile = Join-Path $dir ("archaeology-{0:D2}.json" -f $n)
    $ok = $false
    for ($attempt = 1; $attempt -le 2 -and -not $ok; $attempt++) {
        $log = & dotnet run $script -- --task ("archaeology-{0:D2}" -f $n) --brief $batchFile --system $sys --out $outFile --max-tokens 6144 2>&1
        $raw = (Get-Content $outFile -Raw) -replace '^\s*```(json)?\s*', '' -replace '\s*```\s*$', ''
        try {
            $arr = @($raw | ConvertFrom-Json)
            $expected = @($slice | ForEach-Object { [int]$_.number })
            $got = @($arr | ForEach-Object { [int]$_.number })
            $missing = @($expected | Where-Object { $got -notcontains $_ })
            $bad = @($arr | Where-Object { $_.outcome -notin $validOutcome })
            if ($missing.Count -eq 0 -and $bad.Count -eq 0) {
                $arr | ConvertTo-Json -Depth 5 | Set-Content -Path $jsonFile -Encoding utf8
                $ok = $true
                Write-Output ("batch {0:D2}: ok ({1}) {2}" -f $n, $arr.Count, ($log | Select-Object -Last 1))
            }
            else { Write-Output ("batch {0:D2}: attempt {1} incomplete (missing {2}, bad {3})" -f $n, $attempt, $missing.Count, $bad.Count) }
        }
        catch { Write-Output ("batch {0:D2}: attempt {1} invalid JSON" -f $n, $attempt) }
    }
}

$byNumber = @{}; foreach ($i in $all) { $byNumber[[int]$i.number] = $i }
$merged = @{}
Get-ChildItem $dir -Filter 'archaeology-*.json' | ForEach-Object { foreach ($x in (Get-Content $_.FullName -Raw | ConvertFrom-Json)) { $merged[[int]$x.number] = $x } }
$rows = foreach ($k in ($merged.Keys | Sort-Object)) { $x = $merged[$k]; $i = $byNumber[$k]; [pscustomobject]@{ number = $k; outcome = $x.outcome; still_relevant = $x.still_relevant; knowledge_items = @($x.knowledge).Count; milestone = $i.milestone; closed = $i.closed; title = $i.title; summary = $x.summary; related = (@($x.related) -join ' ') } }
$rows | Export-Csv -Path (Join-Path $dir 'archaeology.csv') -NoTypeInformation -Encoding utf8
$items = foreach ($k in ($merged.Keys | Sort-Object)) { $x = $merged[$k]; $i = $byNumber[$k]; foreach ($kn in @($x.knowledge)) { [pscustomobject]@{ number = $k; type = $kn.type; statement = $kn.statement; evidence = $kn.evidence; still_relevant = $x.still_relevant; milestone = $i.milestone; title = $i.title } } }
$items | Export-Csv -Path (Join-Path $dir 'knowledge.csv') -NoTypeInformation -Encoding utf8
Write-Output ("judged={0} of {1}; knowledge items={2}" -f $rows.Count, $all.Count, @($items).Count)
$rows | Group-Object outcome | Sort-Object Name | ForEach-Object { "{0}`t{1}" -f $_.Name, $_.Count }
@($items) | Group-Object type | Sort-Object Name | ForEach-Object { "{0}`t{1}" -f $_.Name, $_.Count }
