param(
    [string]$Repo = (git rev-parse --show-toplevel),
    [int]$BatchSize = 12,
    [string[]]$Priorities = @('P0', 'P1', 'P2', 'P3'),
    [int[]]$OnlyBatches = @()
)

# Runs the Historian pass: batches of issues with their extracted evidence go to the local model, which returns
# a verdict per issue (implemented / partial / not-implemented / superseded / unclear) with cited evidence.
# Inputs : artifacts/local-ai/historian/evidence.json, briefs/historian-rules.md
# Outputs: historian/batch-NN.md, out/historian-NN.md, historian/historian-NN.json, historian/historian.csv

$ErrorActionPreference = 'Stop'
$dir = Join-Path $Repo 'artifacts\local-ai\historian'
$outDir = Join-Path $Repo 'artifacts\local-ai\out'
$rules = Join-Path $Repo 'tools\ai\briefs\historian-rules.md'
$script = Join-Path $Repo 'tools\ai\local-ai-ask.cs'
$sys = Join-Path $Repo 'tools\ai\briefs\system-json-historian.md'
Set-Content -Path $sys -Encoding utf8 -Value 'You are the Historian of the Encina .NET repository. You judge only from the evidence given and cite its source ids. You answer with a single JSON array and nothing else: no prose, no Markdown fences. Every input issue appears exactly once, in input order, with the fields number, verdict, evidence, suggested_action, confidence.'

$all = Get-Content (Join-Path $dir 'evidence.json') -Raw | ConvertFrom-Json
$order = @{ P0 = 0; P1 = 1; P2 = 2; P3 = 3 }
$issues = @($all | Where-Object { $Priorities -contains $_.priority } | Sort-Object { $order[$_.priority] }, number)
$batches = [math]::Ceiling($issues.Count / $BatchSize)
Write-Output "issues=$($issues.Count) batches=$batches"

function Render($i) {
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("### Issue #$($i.number) | $($i.priority) | milestone: $($i.milestone) | created: $($i.created) | labels: $($i.labels -join ', ')")
    [void]$sb.AppendLine("title: $($i.title)")
    [void]$sb.AppendLine("body: $($i.body)")
    $c = 0; foreach ($x in $i.comments) { $c++; [void]$sb.AppendLine("c${c} ($($x.author), $($x.date)): $($x.text)") }
    $r = 0; foreach ($x in $i.references) { $r++; [void]$sb.AppendLine("r${r}: $x") }
    $k = 0; foreach ($x in $i.commits) { $k++; [void]$sb.AppendLine("k${k}: commit $x references this issue") }
    $s = 0; foreach ($x in $i.code_search) { $s++; $files = if ($x.files.Count -gt 0) { $x.files -join ', ' } else { 'no files' }; [void]$sb.AppendLine("s${s}: identifier '$($x.identifier)' found in: $files") }
    if ($i.comments.Count -eq 0 -and $i.references.Count -eq 0 -and $i.commits.Count -eq 0) { [void]$sb.AppendLine("(no comments, no references, no commits)") }
    [void]$sb.AppendLine()
    return $sb.ToString()
}

for ($b = 0; $b -lt $batches; $b++) {
    $n = $b + 1
    if ($OnlyBatches.Count -gt 0 -and $OnlyBatches -notcontains $n) { continue }
    $slice = @($issues | Select-Object -Skip ($b * $BatchSize) -First $BatchSize)
    $text = ([IO.File]::ReadAllText($rules)) + (($slice | ForEach-Object { Render $_ }) -join '')
    $batchFile = Join-Path $dir ("batch-{0:D2}.md" -f $n)
    Set-Content -Path $batchFile -Encoding utf8 -Value $text
    $outFile = Join-Path $outDir ("historian-{0:D2}.md" -f $n)
    $jsonFile = Join-Path $dir ("historian-{0:D2}.json" -f $n)
    $ok = $false
    for ($attempt = 1; $attempt -le 2 -and -not $ok; $attempt++) {
        $log = & dotnet run $script -- --task ("historian-{0:D2}" -f $n) --brief $batchFile --system $sys --out $outFile --max-tokens 4096 2>&1
        $raw = (Get-Content $outFile -Raw) -replace '^\s*```(json)?\s*', '' -replace '\s*```\s*$', ''
        try {
            $arr = @($raw | ConvertFrom-Json)
            $expected = @($slice | ForEach-Object { [int]$_.number })
            $got = @($arr | ForEach-Object { [int]$_.number })
            $missing = @($expected | Where-Object { $got -notcontains $_ })
            $bad = @($arr | Where-Object { $_.verdict -notin @('implemented', 'partial', 'not-implemented', 'superseded', 'unclear') })
            if ($missing.Count -eq 0 -and $bad.Count -eq 0) {
                $arr | ConvertTo-Json -Depth 3 | Set-Content -Path $jsonFile -Encoding utf8
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
Get-ChildItem $dir -Filter 'historian-*.json' | ForEach-Object { foreach ($x in (Get-Content $_.FullName -Raw | ConvertFrom-Json)) { $merged[[int]$x.number] = $x } }
$rows = foreach ($k in ($merged.Keys | Sort-Object)) { $x = $merged[$k]; $i = $byNumber[$k]; [pscustomobject]@{ number = $k; priority = $i.priority; verdict = $x.verdict; action = $x.suggested_action; confidence = $x.confidence; milestone = $i.milestone; title = $i.title; evidence = $x.evidence } }
$rows | Export-Csv -Path (Join-Path $dir 'historian.csv') -NoTypeInformation -Encoding utf8
Write-Output ("judged={0} of {1}" -f $rows.Count, $all.Count)
$rows | Group-Object verdict | Sort-Object Name | ForEach-Object { "{0}`t{1}" -f $_.Name, $_.Count }
