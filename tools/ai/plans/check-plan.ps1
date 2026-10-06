#requires -Version 7.0
<#
.SYNOPSIS
    Structural gate for implementation plans under docs/plans (issue #1927).

.DESCRIPTION
    Checks that a plan follows the maintainer's prompt
    (docs/engineering/prompts/implementation-plan-prompt.md, section 2 a-f) and fails with
    one line per gap, naming the plan, the check and the location (line number and heading).

    Checks (the id is the bracketed tag in each message):
      sections        the '##' sections of the prompt, in its order; nothing else at '##'
      design-choices  at least 4 decisions in <details>; each with an Options Considered table
                      (Pros and Cons columns), a Chosen Option and a Rationale
      phases          each '### Phase N' has a Tasks <details> and a
                      'Prompt for AI Agents - Phase N' <details> whose fenced block holds
                      CONTEXT, TASK, KEY RULES and REFERENCE FILES
      research        the four tables: standards, existing infrastructure, Event ID allocation,
                      file count
      combined        one <details> with PROJECT CONTEXT, IMPLEMENTATION OVERVIEW, KEY PATTERNS,
                      REFERENCE FILES
      matrix          exactly the 12 functions of AGENTS.md section 6, each with status and note
      decisions       ## Maintainer Decisions is mandatory and last: one dated entry per Design Choice,
                      numbered like the choices; no choice may still be 'pending the maintainer'
                      (skipped by -Draft)
      filename        {feature}-implementation-plan-{issue}.md
      issue           the issue exists and its title starts with [FEATURE]; runs only when gh is
                      installed and authenticated, otherwise it is skipped with a notice

    Exit codes: 0 pass (or exempt), 1 gaps found, 2 usage error.

.PARAMETER Path
    One plan file.
.PARAMETER Changed
    Every plan under docs/plans changed between -BaseRef and HEAD.
.PARAMETER All
    Every docs/plans/*-implementation-plan-*.md.
.PARAMETER SelfTest
    Run the checker against the fixtures in tools/ai/plans/fixtures.
.PARAMETER NoExempt
    Strict mode: ignore the allow-list of plans that predate the current prompt.
.PARAMETER Draft
    Skips only the decisions check (## Maintainer Decisions), so a plan writer can validate the
    structure before the maintainer decides. CI never uses it.
.PARAMETER BaseRef
    Base of -Changed. Default origin/main.
#>
[CmdletBinding()]
param(
    [string]$Path,
    [switch]$Changed,
    [switch]$All,
    [switch]$SelfTest,
    [switch]$NoExempt,
    [switch]$Draft,
    [string]$BaseRef = 'origin/main'
)

$ErrorActionPreference = 'Stop'

# The 20 plan files added before 2026-09-22 (git log --diff-filter=A). Static on purpose: the list
# never grows, so a new plan can never become exempt by date.
$ExemptPlans = @(
    'abac-implementation-plan-401.md'
    'aiact-implementation-plan-415.md'
    'anonymization-implementation-plan-407.md'
    'audit-marten-implementation-plan-800.md'
    'breach-notification-implementation-plan-408.md'
    'cross-border-transfer-implementation-plan-412.md'
    'crypto-shredding-implementation-plan-322.md'
    'data-residency-implementation-plan-405.md'
    'dpia-implementation-plan-409.md'
    'dsr-implementation-plan-404.md'
    'message-encryption-implementation-plan-129.md'
    'nis2-implementation-plan-414.md'
    'persistent-pap-implementation-plan-691.md'
    'privacy-by-design-implementation-plan-411.md'
    'processor-agreements-implementation-plan-410.md'
    'read-auditing-implementation-plan-573.md'
    'retention-implementation-plan-406.md'
    'scheduled-message-processor-implementation-plan-765.md'
    'secrets-caching-migration-implementation-plan-694.md'
    'xacml-xml-serializer-implementation-plan-692.md'
)

$EmDash = [string][char]0x2014

# '##' sections in the order of the prompt. 'Combined AI Agent Prompt' (singular) is a variant that
# four existing plans use.
$SectionDefs = @(
    @{ Name = 'Summary';                          Pattern = '^Summary$';                          Required = $true }
    @{ Name = 'Design Choices';                   Pattern = '^Design Choices$';                   Required = $true }
    @{ Name = 'Implementation Phases';            Pattern = '^Implementation Phases$';            Required = $true }
    @{ Name = 'Research';                         Pattern = '^Research$';                         Required = $true }
    @{ Name = 'Combined AI Agent Prompts';        Pattern = '^Combined AI Agent Prompts?$';       Required = $true }
    @{ Name = 'Cross-Cutting Integration Matrix'; Pattern = '^Cross-Cutting Integration Matrix$'; Required = $true }
    @{ Name = 'Prerequisites & Dependencies';     Pattern = '^Prerequisites (&|and) Dependencies$'; Required = $false }
    @{ Name = 'Next Steps';                       Pattern = '^Next Steps$';                       Required = $true }
    # Mandatory for non-exempt, non-draft plans (checked by Test-Decisions) and only as the last '##'
    # section. 'Decisions of the maintainer' is the variant #751 uses.
    @{ Name = 'Maintainer Decisions';             Pattern = '^(Maintainer decisions|Decisions of the maintainer)(\s*\(.*\))?$'; Required = $false }
)

# AGENTS.md section 6, in order; compared after lower-casing and dropping spaces and hyphens.
$MatrixFunctions = @(
    'Caching', 'OpenTelemetry', 'Structured Logging', 'Health Checks', 'Validation', 'Resilience',
    'Distributed Locks', 'Transactions', 'Idempotency', 'Multi-Tenancy', 'Module Isolation', 'Audit Trail'
)

function Format-Key([string]$Text) {
    return ($Text -replace '[\*`_\s\-]', '').ToLowerInvariant()
}

function New-Gap([string]$Check, [int]$Line, [string]$Message) {
    return [pscustomobject]@{ Check = $Check; Line = $Line; Message = $Message }
}

# Splits the file into lines tagged text / fence (inside a code fence) / delim (the fence lines).
function ConvertTo-PlanLines([string[]]$Raw) {
    $out = [System.Collections.Generic.List[object]]::new()
    $fenceChar = ''
    $fenceLen = 0
    for ($i = 0; $i -lt $Raw.Count; $i++) {
        $t = $Raw[$i]
        $kind = 'text'
        if ($fenceLen -eq 0) {
            if ($t -match '^\s{0,12}(`{3,}|~{3,})') {
                $fenceChar = $Matches[1].Substring(0, 1)
                $fenceLen = $Matches[1].Length
                $kind = 'delim'
            }
        }
        elseif ($t -match '^\s{0,12}(`{3,}|~{3,})\s*$' -and $Matches[1].Substring(0, 1) -eq $fenceChar -and $Matches[1].Length -ge $fenceLen) {
            $fenceLen = 0
            $kind = 'delim'
        }
        else {
            $kind = 'fence'
        }
        $out.Add([pscustomobject]@{ N = $i + 1; Text = $t; Kind = $kind })
    }
    return , $out
}

function Get-BlockSummary($L, [int]$Start, [int]$End) {
    $buf = ''
    $inside = $false
    $last = [Math]::Min($End, $Start + 5)
    for ($i = $Start; $i -le $last; $i++) {
        $t = $L[$i].Text
        if (-not $inside) {
            $m = [regex]::Match($t, '<summary[^>]*>(.*)$', 'IgnoreCase')
            if (-not $m.Success) { continue }
            $t = $m.Groups[1].Value
            $inside = $true
        }
        $e = [regex]::Match($t, '^(.*?)</summary', 'IgnoreCase')
        if ($e.Success) { $buf += ' ' + $e.Groups[1].Value; break }
        $buf += ' ' + $t
    }
    $plain = [regex]::Replace($buf, '<[^>]+>', '')
    return ([regex]::Replace([System.Net.WebUtility]::HtmlDecode($plain), '\s+', ' ')).Trim()
}

# Top-level <details> blocks between two 0-based line indexes (inclusive).
function Get-DetailsBlocks($L, [int]$From, [int]$To) {
    $blocks = [System.Collections.Generic.List[object]]::new()
    $depth = 0
    $cur = $null
    for ($i = $From; $i -le $To; $i++) {
        if ($L[$i].Kind -ne 'text') { continue }
        $t = $L[$i].Text
        # Inline code spans and HTML comments may mention <details> without opening one.
        $t = [regex]::Replace([regex]::Replace($t, '`[^`]*`', ''), '<!--.*?-->', '')
        $o = [regex]::Matches($t, '<details\b', 'IgnoreCase').Count
        $c = [regex]::Matches($t, '</details\s*>', 'IgnoreCase').Count
        if ($o -gt 0 -and $depth -eq 0 -and $null -eq $cur) { $cur = @{ Start = $i } }
        $depth += $o - $c
        if ($null -ne $cur -and $depth -le 0) {
            $blocks.Add([pscustomobject]@{
                    Start = $cur.Start; End = $i; Unclosed = $false
                    Summary = (Get-BlockSummary $L $cur.Start $i)
                })
            $cur = $null
            $depth = 0
        }
    }
    if ($null -ne $cur) {
        $blocks.Add([pscustomobject]@{
                Start = $cur.Start; End = $To; Unclosed = $true
                Summary = (Get-BlockSummary $L $cur.Start $To)
            })
    }
    return , $blocks
}

function Split-TableRow([string]$Line) {
    $inner = $Line.Trim()
    if ($inner.StartsWith('|')) { $inner = $inner.Substring(1) }
    if ($inner.EndsWith('|')) { $inner = $inner.Substring(0, $inner.Length - 1) }
    return @([regex]::Split($inner, '(?<!\\)\|') | ForEach-Object { $_.Trim() })
}

# Markdown tables between two 0-based indexes: header cells, data rows (arrays of cells), line.
function Get-Tables($L, [int]$From, [int]$To) {
    $tables = [System.Collections.Generic.List[object]]::new()
    $i = $From
    while ($i -lt $To) {
        if ($L[$i].Kind -eq 'text' -and $L[$i].Text -match '^\s*\|' -and
            $L[$i + 1].Kind -eq 'text' -and $L[$i + 1].Text -match '^\s*\|[\s:|\-]+\|\s*$' -and $L[$i + 1].Text -match '---') {
            $rows = [System.Collections.Generic.List[object]]::new()
            $j = $i + 2
            while ($j -le $To -and $L[$j].Kind -eq 'text' -and $L[$j].Text -match '^\s*\|') {
                $rows.Add(@{ Cells = (Split-TableRow $L[$j].Text); Line = $L[$j].N })
                $j++
            }
            $tables.Add([pscustomobject]@{ Line = $L[$i].N; Header = (Split-TableRow $L[$i].Text); Rows = $rows })
            $i = $j
        }
        else { $i++ }
    }
    return , $tables
}

function Test-HasLine($L, [int]$From, [int]$To, [string]$Pattern, [string[]]$Kinds = @('text')) {
    for ($i = $From; $i -le $To; $i++) {
        if ($Kinds -contains $L[$i].Kind -and $L[$i].Text -match $Pattern) { return $true }
    }
    return $false
}

function Test-FeatureTitle([string]$Title) {
    return $Title -match '^\[FEATURE\]'
}

function Test-IsExempt([string]$FileName) {
    return $ExemptPlans -contains $FileName
}

# ---------------------------------------------------------------------------------------------
# One check per function; each returns gaps.
# ---------------------------------------------------------------------------------------------

function Test-FileName([string]$FileName) {
    if ($FileName -notmatch '^[a-z0-9]+(-[a-z0-9]+)*-implementation-plan-\d+\.md$') {
        return @(New-Gap 'filename' 0 "file name '$FileName' must match {feature}-implementation-plan-{issue}.md (feature in kebab-case, issue number at the end)")
    }
    return @()
}

function Get-Sections($L) {
    $sections = [System.Collections.Generic.List[object]]::new()
    for ($i = 0; $i -lt $L.Count; $i++) {
        if ($L[$i].Kind -eq 'text' -and $L[$i].Text -match '^##(?!#)\s*(.+?)\s*$') {
            $name = ($Matches[1] -replace '[\*`]', '').Trim().TrimEnd(':')
            $sections.Add([pscustomobject]@{ Name = $name; Index = $i; Line = $L[$i].N; End = $L.Count - 1 })
        }
    }
    for ($k = 0; $k -lt $sections.Count - 1; $k++) { $sections[$k].End = $sections[$k + 1].Index - 1 }
    return , $sections
}

function Test-Sections($Sections) {
    $gaps = [System.Collections.Generic.List[object]]::new()
    $seen = @{}
    $maxPos = -1
    $maxName = ''
    foreach ($s in $Sections) {
        $pos = -1
        for ($d = 0; $d -lt $SectionDefs.Count; $d++) {
            if ($s.Name -match $SectionDefs[$d].Pattern) { $pos = $d; break }
        }
        if ($pos -lt 0) {
            $gaps.Add((New-Gap 'sections' $s.Line "unexpected '##' section '$($s.Name)' (not in the prompt; review logs go to PR or issue comments; the only allowed extra is a last 'Maintainer Decisions' section)"))
            continue
        }
        if ($seen.ContainsKey($pos)) {
            $gaps.Add((New-Gap 'sections' $s.Line "duplicate '##' section '$($s.Name)' (first at line $($seen[$pos]))"))
            continue
        }
        $seen[$pos] = $s.Line
        if ($pos -lt $maxPos) {
            $gaps.Add((New-Gap 'sections' $s.Line "section '$($s.Name)' comes after '$maxName'; the prompt's order is $(($SectionDefs | ForEach-Object { $_.Name }) -join ' > ')"))
        }
        else { $maxPos = $pos; $maxName = $s.Name }
    }
    for ($d = 0; $d -lt $SectionDefs.Count; $d++) {
        if ($SectionDefs[$d].Required -and -not $seen.ContainsKey($d)) {
            $gaps.Add((New-Gap 'sections' 0 "missing '##' section '$($SectionDefs[$d].Name)' (order: $(($SectionDefs | ForEach-Object { $_.Name }) -join ' > '))"))
        }
    }
    return $gaps
}

function Find-Section($Sections, [string]$Name) {
    $def = $SectionDefs | Where-Object { $_.Name -eq $Name }
    return $Sections | Where-Object { $_.Name -match $def.Pattern } | Select-Object -First 1
}

function Test-DesignChoices($L, $Section) {
    $gaps = [System.Collections.Generic.List[object]]::new()
    $blocks = Get-DetailsBlocks $L ($Section.Index + 1) $Section.End
    if ($blocks.Count -lt 4) {
        $gaps.Add((New-Gap 'design-choices' $Section.Line "'Design Choices' has $($blocks.Count) decision(s) inside <details>; the prompt requires at least 4"))
    }
    $k = 0
    foreach ($b in $blocks) {
        $k++
        $where = "decision $k '$($b.Summary)'"
        $line = $L[$b.Start].N
        if ($b.Unclosed) { $gaps.Add((New-Gap 'design-choices' $line "$where has a <details> that is never closed")) }
        $s = $b.Start + 1
        $e = $b.End
        if (-not (Test-HasLine $L $s $e '^\s*(#{2,6}\s*|\*\*)?Options Considered')) {
            $gaps.Add((New-Gap 'design-choices' $line "$where has no 'Options Considered' heading"))
        }
        $tables = Get-Tables $L $s $e
        $proscons = $tables | Where-Object {
            $h = $_.Header | ForEach-Object { Format-Key $_ }
            ($h -contains 'pros') -and ($h -contains 'cons')
        } | Select-Object -First 1
        if ($null -eq $proscons) {
            $gaps.Add((New-Gap 'design-choices' $line "$where has no options table with Pros and Cons columns"))
        }
        elseif ($proscons.Rows.Count -lt 2) {
            $gaps.Add((New-Gap 'design-choices' $proscons.Line "$where lists $($proscons.Rows.Count) option(s) in its Pros/Cons table; compare at least 2"))
        }
        if (-not (Test-HasLine $L $s $e '^\s*(#{2,6}\s*|\*\*)?Chosen Option\*{0,2}\s*:\s*\*{0,2}\s*\S')) {
            $gaps.Add((New-Gap 'design-choices' $line "$where has no 'Chosen Option: <name>' line"))
        }
        $rat = $null
        for ($i = $s; $i -le $e; $i++) {
            if ($L[$i].Kind -eq 'text' -and $L[$i].Text -match '^\s*(#{2,6}\s*|\*\*)Rationale') { $rat = $i; break }
        }
        if ($null -eq $rat) {
            $gaps.Add((New-Gap 'design-choices' $line "$where has no 'Rationale' heading"))
        }
        else {
            $hasText = $false
            for ($i = $rat + 1; $i -lt $e; $i++) {
                if ($L[$i].Kind -eq 'text' -and $L[$i].Text -match '^\s*#{1,6}\s') { break }
                if ($L[$i].Text.Trim() -ne '' -and $L[$i].Text -notmatch '^\s*</?details') { $hasText = $true; break }
            }
            if (-not $hasText) { $gaps.Add((New-Gap 'design-choices' $L[$rat].N "$where has an empty 'Rationale'")) }
        }
    }
    return $gaps
}

function Test-Phases($L, $Section) {
    $gaps = [System.Collections.Generic.List[object]]::new()
    $phases = [System.Collections.Generic.List[object]]::new()
    $headings = [System.Collections.Generic.List[object]]::new()
    for ($i = $Section.Index + 1; $i -le $Section.End; $i++) {
        if ($L[$i].Kind -eq 'text' -and $L[$i].Text -match '^###(?!#)\s') {
            $id = $null
            if ($L[$i].Text -match '^###(?!#)\s+Phase\s+(\d+[a-z]?)\b') { $id = $Matches[1] }
            $headings.Add([pscustomobject]@{ Id = $id; Index = $i; End = $Section.End })
        }
    }
    for ($k = 0; $k -lt $headings.Count; $k++) {
        if ($k -lt $headings.Count - 1) { $headings[$k].End = $headings[$k + 1].Index - 1 }
        if ($null -ne $headings[$k].Id) { $phases.Add($headings[$k]) }
    }
    if ($phases.Count -eq 0) {
        $gaps.Add((New-Gap 'phases' $Section.Line "'Implementation Phases' has no '### Phase N: <title>' headings"))
        return $gaps
    }
    $keywords = 'CONTEXT', 'TASK', 'KEY RULES', 'REFERENCE FILES'
    foreach ($p in $phases) {
        $line = $L[$p.Index].N
        $where = "Phase $($p.Id)"
        $blocks = Get-DetailsBlocks $L ($p.Index + 1) $p.End
        foreach ($b in $blocks | Where-Object { $_.Unclosed }) {
            $gaps.Add((New-Gap 'phases' $L[$b.Start].N "$where has a <details> ('$($b.Summary)') that is never closed"))
        }
        $tasks = @($blocks | Where-Object { $_.Summary -match '^Tasks$' })
        $prompts = @($blocks | Where-Object { $_.Summary -match '^Prompt for AI Agents' })
        if ($tasks.Count -eq 0) {
            $gaps.Add((New-Gap 'phases' $line "$where has no <details><summary>Tasks</summary> block"))
        }
        if ($prompts.Count -eq 0) {
            $gaps.Add((New-Gap 'phases' $line "$where has no <details><summary>Prompt for AI Agents $EmDash Phase $($p.Id)</summary> block"))
            continue
        }
        $pb = $prompts[0]
        if ($pb.Summary -notmatch "^Prompt for AI Agents $EmDash Phase $([regex]::Escape($p.Id))$") {
            $gaps.Add((New-Gap 'phases' $L[$pb.Start].N "$where prompt summary is '$($pb.Summary)'; it must be 'Prompt for AI Agents $EmDash Phase $($p.Id)'"))
        }
        $hasFence = Test-HasLine $L $pb.Start $pb.End '.' @('fence')
        if (-not $hasFence) {
            $gaps.Add((New-Gap 'phases' $L[$pb.Start].N "$where prompt block has no fenced code block"))
            continue
        }
        foreach ($kw in $keywords) {
            if (-not (Test-HasLine $L $pb.Start $pb.End ("^\s*(\*\*)?$kw\b") @('fence'))) {
                $gaps.Add((New-Gap 'phases' $L[$pb.Start].N "$where prompt code block has no '$kw' line"))
            }
        }
    }
    return $gaps
}

# Only the maintainer answers Design Choices. The section is mandatory for a non-draft plan, needs one
# dated entry per Design Choice (numbered like the choices), and no choice may still await the maintainer.
function Test-Decisions($L, $Section, [int]$ChoiceCount) {
    if ($null -eq $Section) {
        return @(New-Gap 'decisions' 0 "Design Choices await the maintainer: present them and record the answers in ## Maintainer Decisions (last section, one dated entry per Design Choice, numbered like the choices)")
    }
    $gaps = [System.Collections.Generic.List[object]]::new()
    # Entries: a line starting with a number (1., D1, **D1**, Design Choice 1, | 1 |) plus its continuation lines.
    $entries = @{}
    $current = $null
    for ($i = $Section.Index + 1; $i -le $Section.End; $i++) {
        if ($L[$i].Kind -ne 'text') { continue }
        $t = $L[$i].Text
        # The number has 1-3 digits, so a wrapped line that starts with a date (2026-...) is not an entry.
        if ($t -match '^\s*(?:#{1,6}\s+|[-*+]\s+|\|\s*)?(?:\*\*)?(?:D|Design Choice\s+|Decision\s+)?(\d{1,3})\b') {
            $current = [int]$Matches[1]
            if (-not $entries.ContainsKey($current)) { $entries[$current] = '' }
        }
        if ($null -ne $current) { $entries[$current] += ' ' + $t }
    }
    $missing = @(1..$ChoiceCount | Where-Object { -not ($entries.ContainsKey($_) -and $entries[$_] -match '\b\d{4}-\d{2}-\d{2}\b') })
    if ($ChoiceCount -gt 0 -and $missing.Count -gt 0) {
        $gaps.Add((New-Gap 'decisions' $Section.Line "'$($Section.Name)' has no dated (yyyy-MM-dd) entry for Design Choice(s) $($missing -join ', ') of $ChoiceCount; the maintainer answers every choice, numbered like the choices. Accepted entry starts (1-3 digit number, date anywhere in the entry): '1. (date) ...', '- 1 ...', '**D1** (date): ...', 'D1 ...', 'Design Choice 1 ...', '### 1.', '### D1', '| 1 | ... |'"))
    }
    for ($i = 0; $i -lt $L.Count; $i++) {
        if ($L[$i].N -ge $Section.Line -and $i -le $Section.End) { continue }
        if ($L[$i].Kind -eq 'text' -and $L[$i].Text -match 'pending the maintainer') {
            $gaps.Add((New-Gap 'decisions' $L[$i].N "choice still marked 'pending the maintainer' although ## Maintainer Decisions exists; update Chosen Option to the maintainer's answer"))
        }
    }
    return $gaps
}

function Test-Research($L, $Section) {
    $gaps = [System.Collections.Generic.List[object]]::new()
    $subs = [System.Collections.Generic.List[object]]::new()
    for ($i = $Section.Index + 1; $i -le $Section.End; $i++) {
        if ($L[$i].Kind -eq 'text' -and $L[$i].Text -match '^###(?!#)\s*(.+?)\s*$') {
            $subs.Add([pscustomobject]@{ Title = $Matches[1]; Index = $i; End = $Section.End; Line = $L[$i].N })
        }
    }
    for ($k = 0; $k -lt $subs.Count - 1; $k++) { $subs[$k].End = $subs[$k + 1].Index - 1 }
    $kinds = @{ infra = $null; event = $null; files = $null; standards = $null }
    foreach ($s in $subs) {
        $hasTable = (Get-Tables $L $s.Index $s.End).Count -gt 0
        $kind = 'standards'
        if ($s.Title -match 'infrastructure') { $kind = 'infra' }
        elseif ($s.Title -match 'event\s*id') { $kind = 'event' }
        elseif ($s.Title -match 'file\s+(count|estimate)|files?\s+count') { $kind = 'files' }
        if ($hasTable -and $null -eq $kinds[$kind]) { $kinds[$kind] = $s }
        elseif (-not $hasTable -and $kind -ne 'standards') {
            $gaps.Add((New-Gap 'research' $s.Line "Research subsection '$($s.Title)' has no table"))
        }
    }
    $expected = [ordered]@{
        standards = "a table of relevant standards/specifications (a '###' subsection of Research with a table, for example '### Relevant Standards & Specifications')"
        infra     = "'### Existing Encina Infrastructure to Leverage' table (component, location, usage)"
        event     = "'### Event ID Allocation' table (package, range, notes)"
        files     = "'### Estimated File Count' table (category, files, notes)"
    }
    foreach ($key in $expected.Keys) {
        if ($null -eq $kinds[$key]) { $gaps.Add((New-Gap 'research' $Section.Line "Research is missing $($expected[$key])")) }
    }
    return $gaps
}

function Test-Combined($L, $Section) {
    $gaps = [System.Collections.Generic.List[object]]::new()
    $blocks = Get-DetailsBlocks $L ($Section.Index + 1) $Section.End
    if ($blocks.Count -ne 1) {
        $gaps.Add((New-Gap 'combined' $Section.Line "'$($Section.Name)' must be a single <details> block; found $($blocks.Count)"))
    }
    if ($blocks.Count -eq 0) { return $gaps }
    $b = $blocks[0]
    if ($b.Unclosed) { $gaps.Add((New-Gap 'combined' $L[$b.Start].N "the <details> block is never closed")) }
    foreach ($kw in 'PROJECT CONTEXT', 'IMPLEMENTATION OVERVIEW', 'KEY PATTERNS', 'REFERENCE FILES') {
        if (-not (Test-HasLine $L $b.Start $b.End ("^\s*(\*\*)?$kw\b") @('text', 'fence'))) {
            $gaps.Add((New-Gap 'combined' $L[$b.Start].N "combined prompt has no '$kw' line"))
        }
    }
    return $gaps
}

function Test-Matrix($L, $Section) {
    $gaps = [System.Collections.Generic.List[object]]::new()
    $tables = Get-Tables $L $Section.Index $Section.End
    $matrix = $tables | Where-Object { (($_.Header | ForEach-Object { Format-Key $_ }) -contains 'function') } | Select-Object -First 1
    if ($null -eq $matrix) {
        $gaps.Add((New-Gap 'matrix' $Section.Line "'$($Section.Name)' has no table with the columns # | Function | Status | Notes"))
        return $gaps
    }
    $header = @($matrix.Header | ForEach-Object { Format-Key $_ })
    if (($header -join '|') -ne '#|function|status|notes') {
        $gaps.Add((New-Gap 'matrix' $matrix.Line "matrix header is '$($matrix.Header -join ' | ')'; it must be '# | Function | Status | Notes'"))
    }
    if ($matrix.Rows.Count -ne 12) {
        $gaps.Add((New-Gap 'matrix' $matrix.Line "matrix has $($matrix.Rows.Count) rows; it must have exactly the 12 functions of AGENTS.md section 6"))
    }
    for ($i = 0; $i -lt $matrix.Rows.Count; $i++) {
        $row = $matrix.Rows[$i]
        $c = $row.Cells
        if ($i -ge 12) {
            $gaps.Add((New-Gap 'matrix' $row.Line "unexpected matrix row $($i + 1) '$($c -join ' | ')'"))
            continue
        }
        $fn = $MatrixFunctions[$i]
        if ($c.Count -lt 4) {
            $gaps.Add((New-Gap 'matrix' $row.Line "matrix row $($i + 1) ($fn) has $($c.Count) cells; it needs #, Function, Status, Notes"))
            continue
        }
        if ((Format-Key $c[0]) -ne [string]($i + 1)) {
            $gaps.Add((New-Gap 'matrix' $row.Line "matrix row $($i + 1) is numbered '$($c[0])'"))
        }
        if ((Format-Key $c[1]) -ne (Format-Key $fn)) {
            $gaps.Add((New-Gap 'matrix' $row.Line "matrix row $($i + 1) is '$($c[1])'; expected '$fn'"))
        }
        if ([regex]::Matches($c[2], '[✅⏭❌]').Count -ne 1) {
            $gaps.Add((New-Gap 'matrix' $row.Line "matrix row $($i + 1) ($fn) needs exactly one status emoji (Include, Defer or N/A from the prompt: check mark, next-track, cross mark); found '$($c[2])'"))
        }
        $note = (($c[3..($c.Count - 1)]) -join '').Trim()
        if ($note -eq '' -or $note -eq 'Justification') {
            $gaps.Add((New-Gap 'matrix' $row.Line "matrix row $($i + 1) ($fn) has an empty or placeholder note"))
        }
    }
    return $gaps
}

# No separate "is gh authenticated" probe (gh auth status can fail on an invalid GITHUB_TOKEN while a
# keyring account works): the real call decides, and a failure other than 404 skips with a notice.
# In CI (the CI environment variable is set) an unreadable issue is a gap, never a silent skip.
$script:GhMissingReported = $false
$script:IssueSkipReason = ''
function Get-IssueTitle([string]$Number) {
    $script:IssueSkipReason = ''
    if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
        $script:IssueSkipReason = 'gh is not installed'
        if (-not $script:GhMissingReported) {
            $script:GhMissingReported = $true
            Write-Host 'NOTICE gh is not installed: the issue check ([FEATURE] title) is skipped'
        }
        return $null
    }
    # REST, not GraphQL: the gate must not depend on the GraphQL rate limit.
    $out = (gh api "repos/dlrivada/Encina/issues/$Number" --jq .title 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -eq 0 -and $out -ne '') { return $out }
    if ($out -match '404|Not Found') { return '' }
    $script:IssueSkipReason = ($out -replace '\s+', ' ')
    Write-Host "NOTICE issue #$Number could not be read ($($script:IssueSkipReason)): the issue check is skipped for it"
    return $null
}

function Test-Issue([string]$FileName) {
    if ($FileName -notmatch '-implementation-plan-(\d+)\.md$') { return @() }
    $n = $Matches[1]
    $title = Get-IssueTitle $n
    if ($null -eq $title) {
        if (-not [string]::IsNullOrEmpty($env:CI)) {
            return @(New-Gap 'issue' 0 "issue #$n could not be read in CI ($($script:IssueSkipReason)); the [FEATURE] check cannot be skipped there")
        }
        return @()
    }
    if ($title -eq '') {
        return @(New-Gap 'issue' 0 "issue #$n does not exist in dlrivada/Encina (the number in the file name must be the feature's issue)")
    }
    if (-not (Test-FeatureTitle $title)) {
        return @(New-Gap 'issue' 0 "issue #$n is '$title'; an implementation plan belongs to a [FEATURE] issue")
    }
    return @()
}

function Test-Plan([string]$File, [switch]$SkipIssue, [switch]$Draft) {
    $name = Split-Path -Leaf $File
    $gaps = [System.Collections.Generic.List[object]]::new()
    foreach ($g in (Test-FileName $name)) { $gaps.Add($g) }
    if (-not $SkipIssue) { foreach ($g in (Test-Issue $name)) { $gaps.Add($g) } }
    $raw = @(Get-Content -LiteralPath $File -Encoding utf8)
    $L = ConvertTo-PlanLines $raw
    $sections = Get-Sections $L
    foreach ($g in (Test-Sections $sections)) { $gaps.Add($g) }
    $checks = @(
        @{ Name = 'Design Choices'; Fn = { param($l, $s) Test-DesignChoices $l $s } }
        @{ Name = 'Implementation Phases'; Fn = { param($l, $s) Test-Phases $l $s } }
        @{ Name = 'Research'; Fn = { param($l, $s) Test-Research $l $s } }
        @{ Name = 'Combined AI Agent Prompts'; Fn = { param($l, $s) Test-Combined $l $s } }
        @{ Name = 'Cross-Cutting Integration Matrix'; Fn = { param($l, $s) Test-Matrix $l $s } }
    )
    foreach ($c in $checks) {
        $sec = Find-Section $sections $c.Name
        if ($null -ne $sec) { foreach ($g in (& $c.Fn $L $sec)) { $gaps.Add($g) } }
    }
    if (-not $Draft) {
        $dsec = Find-Section $sections 'Design Choices'
        $count = if ($null -ne $dsec) { (Get-DetailsBlocks $L ($dsec.Index + 1) $dsec.End).Count } else { 0 }
        foreach ($g in (Test-Decisions $L (Find-Section $sections 'Maintainer Decisions') $count)) { $gaps.Add($g) }
    }
    return $gaps
}

function Write-Gaps([string]$Name, $Gaps) {
    foreach ($g in $Gaps) {
        $loc = if ($g.Line -gt 0) { "${Name}:$($g.Line)" } else { $Name }
        Write-Output "GAP $loc [$($g.Check)] $($g.Message)"
    }
}

function Invoke-PlanFiles([string[]]$Files) {
    $failed = 0
    $exempt = 0
    $passed = 0
    foreach ($f in $Files) {
        $name = Split-Path -Leaf $f
        if (-not $NoExempt -and (Test-IsExempt $name)) {
            Write-Output "EXEMPT $name (predates the current prompt; allow-list in check-plan.ps1)"
            $exempt++
            continue
        }
        $gaps = @(Test-Plan $f -Draft:$Draft)
        if ($gaps.Count -eq 0) { Write-Output "PASS $name"; $passed++ }
        else { Write-Gaps $name $gaps; Write-Output "FAIL $name ($($gaps.Count) gap(s))"; $failed++ }
    }
    Write-Output "SUMMARY $($Files.Count) plan(s): $passed pass, $exempt exempt, $failed fail"
    $script:Failures = $failed
}

function Invoke-SelfTest {
    $dir = Join-Path $PSScriptRoot 'fixtures'
    $cases = @(
        # Draft = $true: the structural fixtures carry no decisions section, so they run in -Draft mode.
        @{ File = 'conforming-implementation-plan-1.md'; Fails = @() }
        @{ File = 'conforming-wrapped-implementation-plan-1.md'; Fails = @() }
        @{ File = 'draft-pending-implementation-plan-1.md'; Fails = @(); Draft = $true }
        @{ File = 'draft-pending-implementation-plan-1.md'; Fails = @('decisions') }
        @{ File = 'fail-decisions-unanswered-implementation-plan-1.md'; Fails = @('decisions') }
        @{ File = 'fail-decisions-pending-implementation-plan-1.md'; Fails = @('decisions') }
        @{ File = 'fail-decisions-order-implementation-plan-1.md'; Fails = @('sections'); Draft = $true }
        @{ File = 'fail-sections-implementation-plan-1.md'; Fails = @('sections'); Draft = $true }
        @{ File = 'fail-design-choices-implementation-plan-1.md'; Fails = @('design-choices'); Draft = $true }
        @{ File = 'fail-phases-implementation-plan-1.md'; Fails = @('phases'); Draft = $true }
        @{ File = 'fail-research-implementation-plan-1.md'; Fails = @('research'); Draft = $true }
        @{ File = 'fail-combined-implementation-plan-1.md'; Fails = @('combined'); Draft = $true }
        @{ File = 'fail-matrix-implementation-plan-1.md'; Fails = @('matrix'); Draft = $true }
        @{ File = 'fail-filename.md'; Fails = @('filename'); Draft = $true }
    )
    $bad = 0
    foreach ($c in $cases) {
        $path = Join-Path $dir $c.File
        if (-not (Test-Path -LiteralPath $path)) { Write-Output "SELFTEST FAIL $($c.File): fixture is missing"; $bad++; continue }
        $isDraft = [bool]$c.Draft
        $got = @(Test-Plan $path -SkipIssue -Draft:$isDraft | ForEach-Object { $_.Check } | Sort-Object -Unique)
        $want = @($c.Fails)
        $label = "$($c.File)$(if ($isDraft) { ' (-Draft)' })"
        if (($got -join ',') -eq ($want -join ',')) { Write-Output "SELFTEST ok   ${label}: $(if ($want.Count) { 'fails only [' + ($want -join ',') + ']' } else { 'passes' })" }
        else { Write-Output "SELFTEST FAIL ${label}: expected [$($want -join ',')] got [$($got -join ',')]"; $bad++ }
    }
    $units = @(
        @{ Name = 'FEATURE title accepted'; Ok = (Test-FeatureTitle '[FEATURE] x') }
        @{ Name = 'BUG title rejected'; Ok = -not (Test-FeatureTitle '[BUG] x') }
        @{ Name = 'untagged title rejected'; Ok = -not (Test-FeatureTitle 'feature x') }
        @{ Name = 'allow-list holds 20 plans'; Ok = ($ExemptPlans.Count -eq 20) }
        @{ Name = 'allow-list matches dsr-404'; Ok = (Test-IsExempt 'dsr-implementation-plan-404.md') }
        @{ Name = 'allow-list excludes 1705'; Ok = -not (Test-IsExempt 'security-context-population-implementation-plan-1705.md') }
    )
    foreach ($u in $units) {
        if ($u.Ok) { Write-Output "SELFTEST ok   $($u.Name)" } else { Write-Output "SELFTEST FAIL $($u.Name)"; $bad++ }
    }
    Write-Output "SELFTEST $(if ($bad -eq 0) { 'PASS' } else { "FAIL ($bad)" })"
    $script:Failures = $bad
}

# ---------------------------------------------------------------------------------------------
# Entry point
# ---------------------------------------------------------------------------------------------

function Stop-Usage([string]$Message) {
    [Console]::Error.WriteLine("check-plan: $Message")
    [Console]::Error.WriteLine('usage: check-plan.ps1 (-Path <file> | -Changed | -All | -SelfTest) [-NoExempt] [-Draft] [-BaseRef <ref>]')
    exit 2
}

$modes = @($PSBoundParameters.ContainsKey('Path'), $Changed.IsPresent, $All.IsPresent, $SelfTest.IsPresent) | Where-Object { $_ }
if ($modes.Count -ne 1) { Stop-Usage 'give exactly one of -Path, -Changed, -All, -SelfTest' }

$repoRoot = (Resolve-Path (Join-Path (Join-Path (Join-Path $PSScriptRoot '..') '..') '..')).Path
$plansDir = Join-Path (Join-Path $repoRoot 'docs') 'plans'

$script:Failures = 0
if ($SelfTest) {
    Invoke-SelfTest
    exit $(if ($script:Failures -eq 0) { 0 } else { 1 })
}

if ($PSBoundParameters.ContainsKey('Path')) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { Stop-Usage "plan file not found: $Path" }
    $files = @((Resolve-Path -LiteralPath $Path).Path)
}
elseif ($All) {
    $files = @(Get-ChildItem -LiteralPath $plansDir -Filter '*-implementation-plan-*.md' -File | Sort-Object Name | ForEach-Object { $_.FullName })
}
else {
    $base = $BaseRef
    git -C $repoRoot rev-parse --verify --quiet $base *> $null
    if ($LASTEXITCODE -ne 0) {
        if ($PSBoundParameters.ContainsKey('BaseRef')) { Stop-Usage "base ref '$BaseRef' not found (fetch it: git fetch origin main)" }
        git -C $repoRoot rev-parse --verify --quiet main *> $null
        if ($LASTEXITCODE -ne 0) { Stop-Usage "base ref '$BaseRef' not found (fetch it: git fetch origin main)" }
        $base = 'main'
    }
    $names = @(git -C $repoRoot diff --name-only --diff-filter=ACMR "$base...HEAD" -- docs/plans)
    $files = @($names | Where-Object { $_ -match '^docs/plans/[^/]*-implementation-plan-[^/]*\.md$' } |
            ForEach-Object { Join-Path $repoRoot $_ } | Where-Object { Test-Path -LiteralPath $_ })
    if ($files.Count -eq 0) {
        Write-Output "No implementation plan changed against $base."
        exit 0
    }
}

Invoke-PlanFiles $files
exit $(if ($script:Failures -eq 0) { 0 } else { 1 })
