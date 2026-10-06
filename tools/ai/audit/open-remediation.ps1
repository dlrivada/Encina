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
# When one body would exceed GitHub's 65,000-character limit (#1863), the drafts are split, whole and docs before
# tests, into the fewest parts that each fit; each part is titled "... (part k/n)" with its own finding count and
# opens with a line naming its place (no URLs: the later parts do not exist yet), and each draft's row carries the
# URL of the part that holds it. A single draft that alone exceeds the limit fails the run naming it. -WhatIf prints
# each part's title and writes one preview file per part (delta-<n>-consolidated.part<k>.preview.md).

param(
    [Parameter(Mandatory)][int]$Issue,
    [switch]$Consolidate,
    [switch]$WhatIf,
    [string]$Set = 'rules-2026-10'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$root = Get-MainRoot $PSScriptRoot
$dir = Join-Path $root 'artifacts\knowledge\remediation'
$opened = Join-Path $dir 'opened.csv'
$labels = gh label list --repo dlrivada/Encina --limit 400 --json name --jq '.[].name'
if ($LASTEXITCODE -ne 0) { Write-Error "open-remediation: gh label list failed (exit $LASTEXITCODE); refusing to open issues without the label list"; exit 1 }
$ms = gh api repos/dlrivada/Encina/milestones --jq '.[].title'
if ($LASTEXITCODE -ne 0) { Write-Error "open-remediation: gh api milestones failed (exit $LASTEXITCODE); refusing to open issues without the milestone list"; exit 1 }

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

# GitHub rejects an issue body over 65,000 characters. $PartReserve leaves room for the one-line "part k of n" note
# that is added to each part after the split is decided.
$MaxBody = 65000
$PartReserve = 400

# Builds the consolidated body of the given parsed drafts. $PartNote (empty for a single issue) is one line placed
# right after the local-draft marker. Returns @{ Body; AnyTest; K }.
function Build-ConsolidatedBody($parsed, [string]$PartNote) {
    $k = @($parsed).Count
    $tpl = Get-TemplateSections (Join-Path $root '.github\ISSUE_TEMPLATE\technical_debt.md')
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
        $dPrio = ''   # a draft without a Priority section (test_implementation.md has none) shows no invented severity
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
                        $refs = @([regex]::Matches($line, '#(\d+)') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
                        if (-not $line.Trim() -or $line -match '^\s*-\s*none\s*$') { continue }
                        # Only a line that refers to nothing but the audited issue is replaced by the canonical line below.
                        if ($refs.Count -eq 1 -and $refs[0] -eq "$Issue") { continue }
                        $key = if ($refs.Count -eq 1) { "#$($refs[0])" } else { $line.Trim() }
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
        $checklist.Add($(if ($dPrio) { "- [ ] $t ($dPrio)" } else { "- [ ] $t" }))
        foreach ($h in $perDraft.Keys) { $target[$h].Add("### $t`n`n" + ($perDraft[$h] -join "`n`n")) }
    }

    $nothing = 'No finding supplies text for this section; see the per-finding text in the other sections.'
    $bodyParts = [System.Collections.Generic.List[string]]::new()
    $bodyParts.Add("<!-- local-draft: none, reason: consolidated from $k verified remediation drafts of the #$Issue delta audit -->")
    if ($PartNote) { $bodyParts.Add($PartNote) }
    foreach ($t in $tpl) {
        $text = switch ($t.Name) {
            'Type' {
                $opts = foreach ($line in ($t.Text -split "`r?`n")) {
                    if ($line -match '^\s*-\s*\[ \]\s*(.+?)\s*$' -and $typeTicked.Contains($Matches[1])) { "- [x] $($Matches[1])" } else { $line }
                }
                $opts -join "`n"
            }
            'Description' {
                $head = "Delta re-audit (``$Set``) of #${Issue}: $k findings are folded into this issue (bugs are opened separately). Each is a checklist item below; the details follow per finding. Fix them in one batch.`n`n" + ($checklist -join "`n")
                if ($target['Description'].Count) { $head + "`n`n" + ($target['Description'] -join "`n`n") } else { $head }
            }
            'Location' { if ($locationBlocks.Count) { $locationBlocks -join "`n" } else { $nothing } }
            'Priority' {
                if ($prio -eq 0) { $t.Text + "`n`nNot stated by the drafts" } else {
                $name = ($prioRank.GetEnumerator() | Where-Object Value -eq $prio).Key
                (($t.Text -split "`r?`n") | ForEach-Object { if ($_ -match "^\s*-\s*\[ \]\s*\*{0,2}$name\b") { $_ -replace '\[ \]', '[x]' } else { $_ } }) -join "`n" }
            }
            'Effort Estimate' {
                if ($eff -eq 0) { $t.Text + "`n`nNot stated by the drafts" } else {
                $name = ($effRank.GetEnumerator() | Where-Object Value -eq $eff).Key
                (($t.Text -split "`r?`n") | ForEach-Object { if ($_ -match "^\s*-\s*\[ \]\s*$name\b") { $_ -replace '\[ \]', '[x]' } else { $_ } }) -join "`n" }
            }
            'Related Issues' { (@($related) + "- #$Issue (audited issue)") -join "`n" }
            default { if ($target[$t.Name].Count) { $target[$t.Name] -join "`n`n" } else { $nothing } }
        }
        $bodyParts.Add("## $($t.Name)`n`n$text")
    }
    return @{ Body = (($bodyParts -join "`n`n") + "`n"); AnyTest = $anyTest; K = $k }
}

# Splits the ordered parsed drafts into the fewest consecutive groups whose bodies each fit $Limit characters
# (greedy: a draft is never split, and a group is closed only when the next draft would overflow it).
# Fails (exit 1) when one draft alone does not fit.
function Split-ForLimit($parsed, [int]$Limit) {
    $groups = [System.Collections.Generic.List[object]]::new()
    $cur = [System.Collections.Generic.List[object]]::new()
    # Every draft must fit alone, wherever it sits in the order.
    foreach ($p in $parsed) {
        $len = (Build-ConsolidatedBody @($p) '').Body.Length
        if ($len -gt $Limit) {
            Write-Error "open-remediation: the draft $($p.Draft.File.Name) alone makes a $len-character body, over the $Limit-character budget of a GitHub issue (65,000 limit); shorten that draft; no issue was created"
            exit 1
        }
    }
    foreach ($p in $parsed) {
        $try = @($cur) + $p
        if ((Build-ConsolidatedBody $try '').Body.Length -le $Limit) { $cur.Add($p); continue }
        $groups.Add(@($cur)); $cur = [System.Collections.Generic.List[object]]::new(); $cur.Add($p)
    }
    if ($cur.Count) { $groups.Add(@($cur)) }
    return , $groups
}

# Decides the parts (and validates every size) without creating anything, so a failure leaves nothing behind.
function Get-ConsolidatedPlan($Drafts) {
    # Docs drafts before tests drafts, the existing per-draft order kept inside each group.
    $parsed = @(foreach ($d in $Drafts) { [pscustomobject]@{ Draft = $d; Sections = @(Split-Sections $d.Body); Test = $d.Title.StartsWith('[TEST]') } })
    $parsed = @(@($parsed | Where-Object { -not $_.Test }) + @($parsed | Where-Object { $_.Test }))

    # Keep one issue when it fits; otherwise the fewest parts that each fit.
    if ((Build-ConsolidatedBody $parsed '').Body.Length -le $MaxBody) {
        $groups = [System.Collections.Generic.List[object]]::new(); $groups.Add(@($parsed))
    }
    else { $groups = Split-ForLimit $parsed ($MaxBody - $PartReserve) }
    $n = $groups.Count
    $topic = switch ($Set) { 'rules-2026-10' { ' (docs and coverage obligations)' } default { '' } }

    $plan = for ($i = 0; $i -lt $n; $i++) {
        $g = @($groups[$i]); $j = $i + 1
        # Each part states its place; no URLs, because the later parts do not exist yet when it is created.
        $note = if ($n -gt 1) { "Part $j of $n of the delta re-audit (``$Set``) of #${Issue}: the other parts are the issues titled with the same text and (part k/$n); each finding is in exactly one part." } else { '' }
        $built = Build-ConsolidatedBody $g $note
        if ($built.Body.Length -gt $MaxBody) {
            Write-Error "open-remediation: part $j of $n is $($built.Body.Length) characters, over the 65,000 limit of a GitHub issue; no issue was created"
            exit 1
        }
        $noun = if ($built.K -eq 1) { 'finding' } else { 'findings' }
        $suffix = if ($n -gt 1) { " (part $j/$n)" } else { '' }
        $lab = @('technical-debt'); if ($built.AnyTest) { $lab += 'area-testing' }
        $lab = @($lab | Where-Object { $labels -contains $_ })
        if (-not $lab) { $lab = @('technical-debt') }
        [pscustomobject]@{ Part = $j; Title = "[DEBT] Delta re-audit ($Set) of #${Issue}: $($built.K) $noun$topic$suffix"; Body = $built.Body; Labels = $lab; Drafts = @($g | ForEach-Object { $_.Draft }) }
    }
    return , @($plan)
}

function Open-Consolidated($plan) {
    $n = $plan.Count
    if ($WhatIf) {
        $pdir = Join-Path $root 'artifacts\issues'
        New-Item -ItemType Directory -Force $pdir | Out-Null
        foreach ($p in $plan) {
            $name = if ($n -gt 1) { "delta-$Issue-consolidated.part$($p.Part).preview.md" } else { "delta-$Issue-consolidated.preview.md" }
            $preview = Join-Path $pdir $name
            Set-Content -LiteralPath $preview $p.Body -Encoding utf8
            "WhatIf: $($p.Title)"
            "WhatIf: preview written to $preview ($($p.Body.Length) characters); nothing created, no rows written"
        }
        return
    }

    # Create every part first, then write the rows: a crashed run is recovered by the same-title reuse below.
    $rows = [System.Collections.Generic.List[string]]::new()
    foreach ($p in $plan) {
        # Reuse an issue with the exact same title instead of creating a duplicate.
        $json = & gh issue list --repo dlrivada/Encina --state all --search "$($p.Title) in:title" --json number,title --limit 100
        if ($LASTEXITCODE -ne 0) { Write-Error "open-remediation: gh issue list failed (exit $LASTEXITCODE)"; exit 1 }
        $found = @(("$($json -join "`n")" | ConvertFrom-Json) | Where-Object { $_.title -ceq $p.Title }) | Select-Object -First 1
        if ($found) {
            $url = "https://github.com/dlrivada/Encina/issues/$($found.number)"
            "$url  $($p.Title) (already exists; reused, nothing created)"
        }
        else {
            $url = New-Issue $p.Title $p.Body $p.Labels '' "consolidated-$Issue-part$($p.Part)"
            "$url  $($p.Title)"
        }
        foreach ($d in $p.Drafts) { $rows.Add("$($d.File.Name),$url") }
    }
    Add-Content $opened @($rows)
}

# --- main ------------------------------------------------------------------------------------------------------

$pattern = if ($Consolidate) { "$Issue-delta-*.md" } else { "$Issue-*.md" }
$done = if (Test-Path $opened) { @(Get-Content $opened | ForEach-Object { ($_ -split ',')[0].Trim() }) } else { @() }
$drafts = foreach ($f in Get-ChildItem $dir -Filter $pattern) {
    if ($done -contains $f.Name) { continue }
    $d = Get-Draft $f
    if ($d) { $d }
}

if (-not $Consolidate) {
    foreach ($d in $drafts) { if ($WhatIf) { "WhatIf: would open: $($d.Title)" } else { Open-Draft $d } }
    return
}

$rest = @($drafts | Where-Object { -not $_.Title.StartsWith('[BUG]') })
$plan = $null
if ($rest.Count) {
    # One consolidated issue per delta audit: when a non-bug delta draft of this issue already has a row, the
    # consolidated issue exists, so new drafts are never turned into a second issue.
    $existing = if (Test-Path $opened) {
        @(Get-Content $opened | Where-Object { $_.Trim() } | ForEach-Object {
            $parts = $_ -split ',', 2
            $name = $parts[0].Trim()
            $file = Join-Path $dir $name
            if ($name -like "$Issue-delta-*.md" -and (Test-Path $file)) {
                $d = Get-Draft (Get-Item $file)
                if ($d -and -not $d.Title.StartsWith('[BUG]')) { [pscustomobject]@{ Name = $name; Url = $parts[1].Trim() } }
            }
        })
    } else { @() }
    if ($existing.Count) {
        $names = ($rest | ForEach-Object { $_.File.Name }) -join ', '
        Write-Error "open-remediation: the consolidated issue of the #$Issue delta audit already exists ($($existing[0].Url)); refusing to open a second one for the new unopened drafts: $names. Add them to that issue by hand (or open a documented follow-up); no rows were written"
        exit 1
    }
    $plan = Get-ConsolidatedPlan $rest
}

# A bug is never folded into the batch: it is opened as its own issue, as in a full audit. It is opened only after
# the consolidated plan is validated, so a refusal or a size failure leaves nothing created.
foreach ($d in @($drafts | Where-Object { $_.Title.StartsWith('[BUG]') })) {
    if ($WhatIf) { "WhatIf: would open its own issue: $($d.Title)" } else { Open-Draft $d }
}
if ($plan) { Open-Consolidated $plan }
