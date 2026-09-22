param(
    [string]$Repo = (git rev-parse --show-toplevel),
    [int]$BatchSize = 30,
    [int]$BodyChars = 700,
    [int[]]$OnlyBatches = @()
)

# Drives the local AI through the P0-P3 classification of every open issue, one bounded batch per request.
# Inputs : artifacts/local-ai/issues/open-issues.json (gh issue list export), briefs/classify-issues-rules.md
# Outputs: artifacts/local-ai/issues/batch-NN.md (input), out/classify-NN.md (raw reply), issues/classify-NN.json
#          (validated), and issues/classification.csv (merged). Token usage lands in the usual ledger.csv.

$ErrorActionPreference = 'Stop'
$issuesDir = Join-Path $Repo 'artifacts\local-ai\issues'
$outDir = Join-Path $Repo 'artifacts\local-ai\out'
$rules = Join-Path $Repo 'tools\ai\briefs\classify-issues-rules.md'
$script = Join-Path $Repo 'tools\ai\local-ai-ask.cs'
$sys = Join-Path $Repo 'tools\ai\briefs\system-json-classifier.md'

Set-Content -Path $sys -Encoding utf8 -Value 'You are a precise backlog classifier for the Encina .NET repository. You answer with a single JSON array and nothing else: no prose, no Markdown fences. Every input issue appears exactly once, in input order, with the fields number, priority, reason, confidence. You follow every numbered rule of the brief.'

$issues = Get-Content (Join-Path $issuesDir 'open-issues.json') -Raw | ConvertFrom-Json | Sort-Object number
$batches = [math]::Ceiling($issues.Count / $BatchSize)
Write-Output "issues=$($issues.Count) batches=$batches"

function Excerpt([string]$body, [int]$max) {
    if ([string]::IsNullOrWhiteSpace($body)) { return '(no body)' }
    $t = ($body -replace '```[\s\S]*?```', '[code]') -replace '<!--[\s\S]*?-->', ''
    $t = ($t -replace '\r?\n+', ' ') -replace '\s{2,}', ' '
    $t = $t.Trim()
    if ($t.Length -gt $max) { $t = $t.Substring(0, $max) + '…' }
    return $t
}

$results = @{}
for ($b = 0; $b -lt $batches; $b++) {
    $n = $b + 1
    if ($OnlyBatches.Count -gt 0 -and $OnlyBatches -notcontains $n) { continue }
    $slice = $issues | Select-Object -Skip ($b * $BatchSize) -First $BatchSize
    $sb = New-Object System.Text.StringBuilder
    foreach ($i in $slice) {
        $ms = if ($i.milestone) { $i.milestone.title } else { '(none)' }
        $labels = ($i.labels | ForEach-Object { $_.name }) -join ', '
        [void]$sb.AppendLine("- #$($i.number) | milestone: $ms | labels: $labels | created: $(([datetime]$i.createdAt).ToString('yyyy-MM-dd')) | comments: $($i.comments.Count)")
        [void]$sb.AppendLine("  title: $($i.title)")
        [void]$sb.AppendLine("  body: $(Excerpt $i.body $BodyChars)")
    }
    $batchFile = Join-Path $issuesDir ("batch-{0:D2}.md" -f $n)
    Set-Content -Path $batchFile -Encoding utf8 -Value (([IO.File]::ReadAllText($rules)) + $sb.ToString())
    $outFile = Join-Path $outDir ("classify-{0:D2}.md" -f $n)
    $jsonFile = Join-Path $issuesDir ("classify-{0:D2}.json" -f $n)

    $ok = $false
    for ($attempt = 1; $attempt -le 2 -and -not $ok; $attempt++) {
        $log = & dotnet run $script -- --task ("classify-{0:D2}" -f $n) --brief $batchFile --system $sys --out $outFile --max-tokens 6144 2>&1
        $raw = Get-Content $outFile -Raw
        $raw = $raw -replace '^\s*```(json)?\s*', '' -replace '\s*```\s*$', ''
        try {
            $arr = $raw | ConvertFrom-Json
            $expected = @($slice | ForEach-Object { $_.number })
            $got = @($arr | ForEach-Object { [int]$_.number })
            $missing = @($expected | Where-Object { $got -notcontains $_ })
            $badPri = @($arr | Where-Object { $_.priority -notin @('P0','P1','P2','P3') })
            if ($missing.Count -eq 0 -and $badPri.Count -eq 0) {
                $arr | ConvertTo-Json -Depth 3 | Set-Content -Path $jsonFile -Encoding utf8
                foreach ($x in $arr) { $results[[int]$x.number] = $x }
                $ok = $true
                Write-Output ("batch {0:D2}: ok ({1} issues) {2}" -f $n, $arr.Count, ($log | Select-Object -Last 1))
            }
            else {
                Write-Output ("batch {0:D2}: attempt {1} incomplete (missing {2}, bad priority {3})" -f $n, $attempt, $missing.Count, $badPri.Count)
            }
        }
        catch {
            Write-Output ("batch {0:D2}: attempt {1} invalid JSON: {2}" -f $n, $attempt, $_.Exception.Message.Substring(0, [Math]::Min(120, $_.Exception.Message.Length)))
        }
    }
}

# Merge everything classified so far (including earlier runs) into one CSV.
$all = @{}
Get-ChildItem $issuesDir -Filter 'classify-*.json' | ForEach-Object {
    foreach ($x in (Get-Content $_.FullName -Raw | ConvertFrom-Json)) { $all[[int]$x.number] = $x }
}
$byNumber = @{}
foreach ($i in $issues) { $byNumber[[int]$i.number] = $i }
$rows = foreach ($k in ($all.Keys | Sort-Object)) {
    $x = $all[$k]; $i = $byNumber[$k]
    [pscustomobject]@{
        number = $k
        priority = $x.priority
        confidence = $x.confidence
        milestone = if ($i.milestone) { $i.milestone.title } else { '' }
        title = $i.title
        reason = $x.reason
    }
}
$rows | Export-Csv -Path (Join-Path $issuesDir 'classification.csv') -NoTypeInformation -Encoding utf8
Write-Output ("classified={0} of {1}" -f $rows.Count, $issues.Count)
$rows | Group-Object priority | Sort-Object Name | ForEach-Object { "{0}`t{1}" -f $_.Name, $_.Count }
