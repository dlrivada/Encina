# tools/ai/audit/open-remediation.ps1 -Issue <n> [-Consolidate [-Set <set>]] (#1345; moved from the unversioned artifacts/knowledge/)
#
# Opens the remediation drafts collected for one audited issue (artifacts/knowledge/remediation/<n>-*.md).
# Drafts carry an HTML-comment header with title/labels/milestone; unknown labels are dropped; bugs default
# to Hardening.
#
# -Consolidate (delta audits; maintainer decision 2026-10-05: one issue per audit, fixes in batches): every
# non-bug draft becomes a checklist item and a per-finding subsection of ONE technical_debt.md issue; a [BUG]
# draft is still opened as its own issue. Every draft still gets its own row in opened.csv (pointing at the
# consolidated issue URL) so audit-done.ps1's "every draft opened" check keeps working. Full audits do not use it.

param(
    [Parameter(Mandatory)][int]$Issue,
    [switch]$Consolidate,
    [string]$Set = 'rules-2026-10'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$root = Get-MainRoot $PSScriptRoot
$dir = Join-Path $root 'artifacts\knowledge\remediation'
$opened = Join-Path $dir 'opened.csv'
$labels = gh label list --repo dlrivada/Encina --limit 400 --json name --jq '.[].name'
$ms = gh api repos/dlrivada/Encina/milestones --jq '.[].title'

function Get-HardeningMilestone { "v0.14.0 $([char]0x2014) Hardening" }

function Get-Draft([System.IO.FileInfo]$File) {
    $raw = Get-Content -Raw $File.FullName
    $h = ([regex]::Match($raw, '(?s)<!--(.*?)-->')).Groups[1].Value
    # [ \t]*, never \s*: an empty field must not swallow the next header line (#1572 added 'kind:' after 'milestone:').
    $title = ([regex]::Match($h, 'title:[ \t]*(.+)')).Groups[1].Value.Trim()
    if (-not $title) { Write-Warning "no title in $($File.Name)"; return $null }
    $lab = @((([regex]::Match($h, 'labels:[ \t]*(.+)')).Groups[1].Value -split ',\s*') | ForEach-Object { $_.Trim() } | Where-Object { $labels -contains $_ })
    if (-not $lab) { $lab = @(if ($title.StartsWith('[BUG]')) { 'bug' } elseif ($title.StartsWith('[TEST]')) { 'area-testing' } else { 'technical-debt' }) }
    $m = ([regex]::Match($h, 'milestone:[ \t]*(.*)')).Groups[1].Value.Trim()
    # The real GitHub milestone title uses an em dash (U+2014); [char]0x2014 keeps this file's own bytes
    # ASCII-only while still matching that title exactly, so --milestone below resolves it (#1345 review).
    if (-not ($ms -contains $m)) { $m = if ($title.StartsWith('[BUG]')) { Get-HardeningMilestone } else { '' } }
    [pscustomobject]@{
        File   = $File
        Title  = $title
        Labels = $lab
        Milestone = $m
        Kind   = ([regex]::Match($h, 'kind:[ \t]*(.*)')).Groups[1].Value.Trim().ToLowerInvariant()
        Body   = ($raw -replace '(?s)^\s*<!--.*?-->\s*', '').Trim()
    }
}

# Creates one issue; returns its URL or exits 1.
function New-Issue([string]$Title, [string]$Body, [string[]]$Lab, [string]$Milestone, [string]$Name) {
    $bf = Join-Path $env:TEMP "rem-$Name.md"; Set-Content $bf $Body -Encoding utf8
    $a = @('issue', 'create', '--repo', 'dlrivada/Encina', '--title', $Title, '--body-file', $bf)
    if ($Milestone) { $a += @('--milestone', $Milestone) }
    foreach ($l in $Lab) { $a += @('--label', $l) }
    $url = & gh @a
    if ($LASTEXITCODE -ne 0 -or -not $url -or $url -notmatch '^https://github.com/') {
        Write-Error "open-remediation: gh issue create failed for $Name (exit $LASTEXITCODE, url: '$url')"
        exit 1
    }
    return "$url".Trim()
}

function Open-Draft($Draft) {
    $url = New-Issue $Draft.Title $Draft.Body $Draft.Labels $Draft.Milestone $Draft.File.BaseName
    Add-Content $opened "$($Draft.File.Name),$url"
    "$url  $($Draft.Title)"
}

# --- consolidation helpers -------------------------------------------------------------------------------------

# Splits an issue body into ordered { Name; Text } sections on its '## ' headers (fenced code is not scanned).
function Split-Sections([string]$Body) {
    $sections = [System.Collections.Generic.List[object]]::new()
    $name = '(preamble)'; $buf = [System.Collections.Generic.List[string]]::new(); $fence = $false
    foreach ($line in ($Body -split "`r?`n")) {
        if ($line -match '^\s*```') { $fence = -not $fence }
        if (-not $fence -and $line -match '^##\s+(.+?)\s*$' -and $line -notmatch '^###') {
            $sections.Add([pscustomobject]@{ Name = $name; Text = (($buf -join "`n").Trim()) })
            $name = $Matches[1]; $buf = [System.Collections.Generic.List[string]]::new()
        }
        else { $buf.Add($line) }
    }
    $sections.Add([pscustomobject]@{ Name = $name; Text = (($buf -join "`n").Trim()) })
    return @($sections | Where-Object { $_.Text })
}

# A '### ' subsection of the consolidated body holds one draft's text, so the draft's own headings go one level down.
function ConvertTo-Nested([string]$Text) {
    $fence = $false
    $out = foreach ($line in ($Text -split "`r?`n")) {
        if ($line -match '^\s*```') { $fence = -not $fence }
        if (-not $fence -and $line -match '^#{3,}\s') { "#$line" } else { $line }
    }
    return ($out -join "`n")
}

# Keeps only the ticked options of a checkbox list (the unticked ones are template noise) and every other line.
function Select-Ticked([string]$Text) {
    $lines = @($Text -split "`r?`n" | Where-Object { $_ -notmatch '^\s*-\s*\[ \]' })
    return ($lines -join "`n").Trim()
}

# Top-level blocks (a line that starts in column 0 plus its indented continuation lines).
function Get-Blocks([string]$Text) {
    $blocks = [System.Collections.Generic.List[string]]::new(); $cur = $null
    foreach ($line in ($Text -split "`r?`n")) {
        if (-not $line.Trim()) { continue }
        if ($line -match '^\s' -and $null -ne $cur) { $cur += "`n$line" }
        else { if ($null -ne $cur) { $blocks.Add($cur) }; $cur = $line }
    }
    if ($null -ne $cur) { $blocks.Add($cur) }
    return $blocks
}

function Get-TemplateSections([string]$Path) {
    $raw = (Get-Content -Raw $Path) -replace '(?s)^---.*?---\s*', ''
    return Split-Sections $raw
}

function Get-TickedOptions([string]$Text) {
    @($Text -split "`r?`n" | ForEach-Object { if ($_ -match '^\s*-\s*\[[xX]\]\s*(.+?)\s*$') { $Matches[1] -replace '\*', '' } })
}

function Open-Consolidated($Drafts) {
    $k = $Drafts.Count
    $tpl = Get-TemplateSections (Join-Path $root '.github\ISSUE_TEMPLATE\technical_debt.md')
    $parsed = foreach ($d in $Drafts) { [pscustomobject]@{ Draft = $d; Sections = @(Split-Sections $d.Body); Test = $d.Title.StartsWith('[TEST]') } }
    $short = { param($t) ($t -replace '^\[[A-Z-]+\]\s*', '').Trim() }

    # Per-draft subsections for each target header, keeping every piece of the draft's text.
    $target = @{}; foreach ($t in $tpl) { $target[$t.Name] = [System.Collections.Generic.List[string]]::new() }
    $locationBlocks = [System.Collections.Generic.List[string]]::new()
    $typeTicked = [System.Collections.Generic.HashSet[string]]::new()
    $prioRank = @{ High = 3; Medium = 2; Low = 1 }; $effRank = @{ Large = 3; Medium = 2; Small = 1 }
    $prio = 0; $eff = 0; $prioName = @{}; $related = [System.Collections.Generic.List[string]]::new()
    $relatedSeen = [System.Collections.Generic.HashSet[string]]::new()
    $checklist = [System.Collections.Generic.List[string]]::new()
    $anyTest = $false

    foreach ($p in $parsed) {
        $t = & $short $p.Draft.Title
        $perDraft = @{}
        function Add-Text([string]$Header, [string]$Text) {
            if (-not $Text) { return }
            if (-not $perDraft.ContainsKey($Header)) { $perDraft[$Header] = [System.Collections.Generic.List[string]]::new() }
            $perDraft[$Header].Add($Text)
        }
        $dPrio = 'Medium'
        if ($p.Test) { $anyTest = $true; [void]$typeTicked.Add('Missing tests') }
        if ($p.Draft.Kind -eq 'docs') { [void]$typeTicked.Add('Documentation gap') }
        foreach ($s in $p.Sections) {
            $text = ConvertTo-Nested $s.Text
            switch ($s.Name) {
                'Type' { foreach ($o in (Get-TickedOptions $s.Text)) { [void]$typeTicked.Add($o) } }
                'Priority' {
                    foreach ($o in (Get-TickedOptions $s.Text)) {
                        if ($o -match '^(High|Medium|Low)\b') { $dPrio = $Matches[1]; if ($prioRank[$dPrio] -gt $prio) { $prio = $prioRank[$dPrio] } }
                    }
                }
                'Effort Estimate' {
                    foreach ($o in (Get-TickedOptions $s.Text)) {
                        if ($o -match '^(Small|Medium|Large)\b' -and $effRank[$Matches[1]] -gt $eff) { $eff = $effRank[$Matches[1]] }
                    }
                }
                'Location' { foreach ($b in (Get-Blocks $s.Text)) { if (-not $locationBlocks.Contains($b)) { $locationBlocks.Add($b) } } }
                'Packages / Providers Affected' { foreach ($b in (Get-Blocks $s.Text)) { if (-not $locationBlocks.Contains($b)) { $locationBlocks.Add($b) } } }
                'Related Issues' {
                    foreach ($line in ($s.Text -split "`r?`n")) {
                        if ($line -match '^\s*-\s*#_+') { continue }
                        $ref = [regex]::Match($line, '#(\d+)').Groups[1].Value
                        if ($ref -eq "$Issue" -or -not $line.Trim()) { continue }
                        $key = if ($ref) { "#$ref" } else { $line.Trim() }
                        if ($relatedSeen.Add($key)) { $related.Add($line.TrimEnd()) }
                    }
                }
                'Description' { Add-Text 'Description' $text }
                'Current Behavior' { Add-Text 'Current Behavior' $text }
                'Expected Behavior' { Add-Text 'Expected Behavior' $text }
                'Root Cause' { Add-Text 'Root Cause' $text }
                'Proposed Fix' { Add-Text 'Proposed Fix' $text }
                'Test Category' { Add-Text 'Current Behavior' ("#### Test Category`n" + (Select-Ticked $text)) }
                'Current Coverage' { Add-Text 'Current Behavior' ("#### Current Coverage`n" + $text) }
                'Infrastructure Required' { Add-Text 'Proposed Fix' ("#### Infrastructure Required`n" + (Select-Ticked $text)) }
                'Test Plan' { Add-Text 'Proposed Fix' ("#### Test Plan`n" + $text) }
                'Collection Fixture (Integration Tests Only)' { Add-Text 'Proposed Fix' ("#### Collection Fixture`n" + $text) }
                default { Add-Text 'Current Behavior' ("#### $($s.Name)`n" + $text) }
            }
        }
        $checklist.Add("- [ ] $t ($dPrio)")
        foreach ($h in $perDraft.Keys) { $target[$h].Add("### $t`n`n" + ($perDraft[$h] -join "`n`n")) }
    }

    $nothing = 'No finding supplies text for this section; see the per-finding text in the other sections.'
    $bodyParts = [System.Collections.Generic.List[string]]::new()
    $bodyParts.Add("<!-- local-draft: none, reason: consolidated from $k verified remediation drafts of the #$Issue delta audit -->")
    foreach ($t in $tpl) {
        $text = switch ($t.Name) {
            'Type' {
                $opts = foreach ($line in ($t.Text -split "`r?`n")) {
                    if ($line -match '^\s*-\s*\[ \]\s*(.+?)\s*$' -and $typeTicked.Contains($Matches[1])) { "- [x] $($Matches[1])" } else { $line }
                }
                $opts -join "`n"
            }
            'Description' {
                $head = "Delta re-audit (``$Set``) of #$Issue found $k findings. Each is a checklist item below; the details follow per finding. Fix them in one batch.`n`n" + ($checklist -join "`n")
                if ($target['Description'].Count) { $head + "`n`n" + ($target['Description'] -join "`n`n") } else { $head }
            }
            'Location' { if ($locationBlocks.Count) { $locationBlocks -join "`n" } else { $nothing } }
            'Priority' {
                $name = if ($prio -eq 0) { 'Medium' } else { ($prioRank.GetEnumerator() | Where-Object Value -eq $prio).Key }
                (($t.Text -split "`r?`n") | ForEach-Object { if ($_ -match "^\s*-\s*\[ \]\s*\*{0,2}$name\b") { $_ -replace '\[ \]', '[x]' } else { $_ } }) -join "`n"
            }
            'Effort Estimate' {
                $name = if ($eff -eq 0) { 'Medium' } else { ($effRank.GetEnumerator() | Where-Object Value -eq $eff).Key }
                (($t.Text -split "`r?`n") | ForEach-Object { if ($_ -match "^\s*-\s*\[ \]\s*$name\b") { $_ -replace '\[ \]', '[x]' } else { $_ } }) -join "`n"
            }
            'Related Issues' { (@($related) + "- #$Issue (audited issue)") -join "`n" }
            default { if ($target[$t.Name].Count) { $target[$t.Name] -join "`n`n" } else { $nothing } }
        }
        $bodyParts.Add("## $($t.Name)`n`n$text")
    }
    $body = ($bodyParts -join "`n`n") + "`n"

    $title = "[DEBT] Delta re-audit ($Set) of #${Issue}: $k findings (docs and coverage obligations)"
    $lab = @('technical-debt'); if ($anyTest) { $lab += 'area-testing' }
    $lab = @($lab | Where-Object { $labels -contains $_ })
    if (-not $lab) { $lab = @('technical-debt') }
    $url = New-Issue $title $body $lab '' "consolidated-$Issue"
    foreach ($d in $Drafts) { Add-Content $opened "$($d.File.Name),$url" }
    "$url  $title"
}

# --- main ------------------------------------------------------------------------------------------------------

$drafts = foreach ($f in Get-ChildItem $dir -Filter "$Issue-*.md") {
    if ((Test-Path $opened) -and (Select-String -Path $opened -SimpleMatch $f.Name -Quiet)) { continue }
    $d = Get-Draft $f
    if ($d) { $d }
}

if (-not $Consolidate) { foreach ($d in $drafts) { Open-Draft $d }; return }

# A bug is never folded into the batch: it is opened as its own issue, as in a full audit.
foreach ($d in @($drafts | Where-Object { $_.Title.StartsWith('[BUG]') })) { Open-Draft $d }
$rest = @($drafts | Where-Object { -not $_.Title.StartsWith('[BUG]') })
if ($rest.Count) { Open-Consolidated $rest }
