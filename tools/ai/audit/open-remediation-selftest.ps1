# tools/ai/audit/open-remediation-selftest.ps1
#
# Self-test for open-remediation.ps1 -Consolidate (one issue per delta audit). Builds a throwaway git repository
# with the audit scripts, the technical_debt.md template and remediation drafts for audited issue 2, and runs the
# REAL script with gh replaced by a PowerShell function in the child session (it records every call, copies the
# --body-file and prints a fake URL; nothing is opened). Asserts:
#   1. 2 docs drafts + 1 test draft -> exactly one `gh issue create` with the expected title, labels, no
#      milestone, the local-draft marker first, the template headers verbatim and in order, one checkbox per
#      draft, every draft's text, and opened.csv with one row per draft pointing to the same URL;
#   2. a re-run opens nothing (idempotent);
#   3. a [BUG] draft goes to its own issue and is left out of the consolidated one;
#   4. without -Consolidate every draft is its own issue (full audits unchanged);
#   5. the 65,000-character limit (#1863): under it one unchanged issue; over it the fewest parts, each draft
#      whole, rows pointing at the part that holds the draft, -WhatIf previews per part; a single oversized draft
#      fails naming it with nothing created.
#
# Exit code: 0 if every assertion passes, 1 otherwise.

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$base = Join-Path ([IO.Path]::GetTempPath()) ("open-remediation-selftest-" + [guid]::NewGuid().ToString('N').Substring(0, 8))
$failures = [System.Collections.Generic.List[string]]::new()
$savedGhToken = $env:GITHUB_TOKEN; $savedGhToken2 = $env:GH_TOKEN

function Assert-That([string]$Name, [bool]$Condition, [string]$Detail = '') {
    if ($Condition) { Write-Host "PASS  $Name" }
    else { Write-Host "FAIL  $Name $Detail"; $failures.Add($Name) }
}

function Write-Text([string]$Path, [string]$Text) {
    New-Item -ItemType Directory -Force (Split-Path -Parent $Path) | Out-Null
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}

function New-DocsDraft([string]$Title, [string]$File, [string]$Prio, [string]$Eff, [string]$Related) {
    $p = @{ High = '[ ]'; Medium = '[ ]'; Low = '[ ]' }; $p[$Prio] = '[x]'
    $e = @{ Small = '[ ]'; Medium = '[ ]'; Large = '[ ]' }; $e[$Eff] = '[x]'
    return @"
<!--
title: $Title
labels: technical-debt, area-docs-unknown
milestone:
kind: docs
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

DESCRIPTION of $File.

## Location

- **File(s)**: ``$File``
  - ``:10`` the line
- **Package(s)**: none (documentation only)

## Current Behavior

CURRENT of $File.

## Expected Behavior

EXPECTED of $File.

### Nested heading in $File

nested text

## Root Cause

ROOT of $File.

## Proposed Fix

FIX of $File.

## Priority

- $($p.High) **High** - Blocks functionality or causes failures in production
- $($p.Medium) **Medium** - Should be fixed before 1.0 release
- $($p.Low) **Low** - Nice to have, can be deferred

## Effort Estimate

- $($e.Small) Small (< 1 hour)
- $($e.Medium) Medium (1-4 hours)
- $($e.Large) Large (> 4 hours)

## Related Issues

- #2 (This issue)
$Related
"@
}

$testDraft = @'
<!--
title: [TEST] Raise the unit target for Encina.Foo
labels: area-testing
milestone:
kind: test
-->

## Test Category

- [x] Unit Tests
- [ ] Integration Tests (Docker/Testcontainers)
- [x] Coverage Gap (below 85% target)

## Description

TEST DESCRIPTION.

## Packages / Providers Affected

- **Package(s)**: Encina.Foo
- **Provider(s)**: none

## Current Coverage

| Package | Line Coverage | Target | Gap |
|---------|:------------:|:------:|:---:|
| Encina.Foo | 62.3% | 85% | -22.7% |

## Infrastructure Required

- [ ] Docker / Testcontainers
- [x] None (pure unit tests)

## Test Plan

### Tests to Implement

- [ ] Test 1: TESTPLAN item

## Related Issues

- #999 - TEST related
'@

$bugDraft = @'
<!--
title: [BUG] Something is wrong
labels: bug
milestone:
kind: bug
-->

## Description

BUG DESCRIPTION.
'@

try {
    $main = Join-Path $base 'main'
    $stubs = Join-Path $base 'stubs'
    $log = Join-Path $base 'gh.log'
    $bodies = Join-Path $base 'bodies'
    New-Item -ItemType Directory -Force $main, $stubs, $bodies | Out-Null
    & git init -q -b main $main | Out-Null
    Copy-Item -Recurse (Join-Path $repo 'tools\ai\audit') (Join-Path $main 'tools\ai\audit') -Force
    New-Item -ItemType Directory -Force (Join-Path $main '.github\ISSUE_TEMPLATE') | Out-Null
    Copy-Item (Join-Path $repo '.github\ISSUE_TEMPLATE\technical_debt.md') (Join-Path $main '.github\ISSUE_TEMPLATE\technical_debt.md') -Force

    Write-Text (Join-Path $stubs 'gh-stub.ps1') @'
Add-Content -LiteralPath $env:OR_STUB_LOG -Value ('gh ' + ($args -join ' '))
if ($args[0] -eq 'label') { 'technical-debt'; 'area-testing'; 'bug'; 'p0-mandatory'; 'p1-recommended'; 'p2-post-1.0'; exit 0 }
if ($args[0] -eq 'api') { "v0.14.0 $([char]0x2014) Hardening"; "v0.19.0 $([char]0x2014) Providers & Testing"; "v0.21.0 $([char]0x2014) Documentation"; exit 0 }
if ($args[0] -eq 'project') {
    Add-Content -LiteralPath $env:OR_STUB_LOG -Value ('TOKENS:[' + $env:GITHUB_TOKEN + $env:GH_TOKEN + ']')
    if ($env:OR_STUB_PROJECT_FAIL) { 'project scope missing'; $global:LASTEXITCODE = 1; exit 1 }
    exit 0
}
if ($args[0] -eq 'issue' -and $args[1] -eq 'list') {
    if ($env:OR_STUB_EXISTING) { $env:OR_STUB_EXISTING } else { '[]' }
    exit 0
}
if ($args[0] -eq 'issue' -and $args[1] -eq 'create') {
    $n = @(Get-Content -LiteralPath $env:OR_STUB_LOG | Where-Object { $_ -like 'gh issue create*' }).Count
    $bf = $args[([array]::IndexOf($args, '--body-file') + 1)]
    Copy-Item -LiteralPath $bf -Destination (Join-Path $env:OR_STUB_BODIES "$n.md") -Force
    "https://github.com/dlrivada/Encina/issues/$(1000 + $n)"
    exit 0
}
exit 0
'@
    $env:OR_STUB_LOG = $log
    # The script must clear these for the project call only (#1926); the stub logs what it sees.
    $env:GITHUB_TOKEN = 'tok-must-be-cleared'; $env:GH_TOKEN = 'tok-must-be-cleared'
    $env:OR_STUB_BODIES = $bodies
    $script = Join-Path $main 'tools\ai\audit\open-remediation.ps1'
    $rem = Join-Path $main 'artifacts\knowledge\remediation'
    $csv = Join-Path $rem 'opened.csv'

    function Invoke-Open([string]$ExtraArgs, [int]$IssueNo = 2) {
        if (Test-Path $log) { Remove-Item $log -Force }
        Get-ChildItem $bodies -File -ErrorAction SilentlyContinue | Remove-Item -Force
        $command = "function gh { & '$stubs\gh-stub.ps1' @args }; & '$script' -Issue $IssueNo $ExtraArgs; exit `$LASTEXITCODE"
        $out = & pwsh -NoProfile -Command $command 2>&1 | ForEach-Object { "$_" }
        $creates = if (Test-Path $log) { @(Get-Content $log | Where-Object { $_ -like 'gh issue create*' }) } else { @() }
        return @{ Exit = $LASTEXITCODE; Text = ($out -join "`n"); Creates = @($creates) }
    }

    Write-Text (Join-Path $rem '2-delta-docs-a.md') (New-DocsDraft '[DEBT] Docs page A is stale' 'docs/a.md' 'Low' 'Small' '- #50 - related A')
    Write-Text (Join-Path $rem '2-delta-docs-b.md') (New-DocsDraft '[DEBT] Docs page B is stale' 'docs/b.md' 'High' 'Medium' "- #50 - related A again`n- #51 - related B")
    Write-Text (Join-Path $rem '2-delta-test-c.md') $testDraft
    # A non-delta draft of the same issue must never be folded into the consolidated issue.
    Write-Text (Join-Path $rem '2-extra-z.md') (New-DocsDraft '[DEBT] Extra Z full-audit draft' 'docs/z.md' 'Low' 'Small' '')

    # --- 1. three drafts -> one issue ---------------------------------------------------------------------------
    $r1 = Invoke-Open '-Consolidate'
    Assert-That 'consolidate exits 0' ($r1.Exit -eq 0) $r1.Text
    Assert-That 'exactly one gh issue create' ($r1.Creates.Count -eq 1) ($r1.Creates -join ' | ')
    $create = [string]($r1.Creates | Select-Object -First 1)
    Assert-That 'title' ($create.Contains('--title [DEBT] Delta re-audit (rules-2026-10) of #2: 3 findings (docs and coverage obligations) --body-file')) $create
    Assert-That 'labels are technical-debt and area-testing' ($create.Contains('--label technical-debt') -and $create.Contains('--label area-testing')) $create
    Assert-That 'tests route: v0.19.0 milestone; priority is the highest draft priority (High -> p0-mandatory)' ($create.Contains("--milestone v0.19.0 $([char]0x2014) Providers & Testing") -and $create.Contains('--label p0-mandatory') -and -not $create.Contains('p1-recommended')) $create
    $projCalls = @(Get-Content $log | Where-Object { $_ -like 'gh project item-add 1 --owner dlrivada --url https://github.com/dlrivada/Encina/issues/1001' })
    Assert-That 'the issue is added to project 1 with the keyring token (GITHUB_TOKEN and GH_TOKEN cleared)' ($projCalls.Count -eq 1 -and @(Get-Content $log | Where-Object { $_ -eq 'TOKENS:[]' }).Count -eq 1) ((Get-Content $log) -join ' | ')
    $body = if (Test-Path (Join-Path $bodies '1.md')) { Get-Content -Raw (Join-Path $bodies '1.md') } else { '' }
    Assert-That 'marker is the first line' ($body.TrimStart().StartsWith('<!-- local-draft: none, reason: consolidated from 3 verified remediation drafts of the #2 delta audit -->')) $body
    $headers = @([regex]::Matches($body, '(?m)^## (.+?)\s*$') | ForEach-Object { $_.Groups[1].Value })
    $expectedHeaders = @('Type', 'Description', 'Location', 'Current Behavior', 'Expected Behavior', 'Root Cause', 'Proposed Fix', 'Priority', 'Effort Estimate', 'Related Issues')
    Assert-That 'template headers verbatim and in order' (($headers -join '|') -eq ($expectedHeaders -join '|')) ($headers -join '|')
    Assert-That 'three checkboxes with the severity' ($body.Contains('- [ ] Docs page A is stale (Low)') -and $body.Contains('- [ ] Docs page B is stale (High)') -and $body.Contains('- [ ] Raise the unit target for Encina.Foo' + "`n")) $body
    Assert-That 'type ticks Documentation gap and Missing tests' ($body.Contains('- [x] Documentation gap') -and $body.Contains('- [x] Missing tests') -and $body.Contains('- [ ] Other')) $body
    Assert-That 'priority is the highest (High) and effort the largest (Medium)' ($body -match '(?m)^- \[x\] \*\*High\*\*' -and $body -notmatch '(?m)^- \[x\] \*\*Low\*\*' -and $body -match '(?m)^- \[x\] Medium \(1-4 hours\)') $body
    $kept = $true; $missing = @()
    foreach ($needle in 'DESCRIPTION of docs/a.md', 'CURRENT of docs/b.md', 'EXPECTED of docs/a.md', 'nested text', 'ROOT of docs/b.md', 'FIX of docs/a.md', 'TEST DESCRIPTION.', 'Encina.Foo | 62.3%', 'TESTPLAN item', 'None (pure unit tests)', '`docs/a.md`', '`docs/b.md`', 'Encina.Foo') {
        if (-not $body.Contains($needle)) { $kept = $false; $missing += $needle }
    }
    Assert-That 'every draft text is kept' $kept ($missing -join ', ')
    Assert-That 'per-finding subsections' ($body.Contains('### Docs page A is stale') -and $body.Contains('### Raise the unit target for Encina.Foo') -and $body.Contains('#### Nested heading in docs/a.md')) $body
    Assert-That 'related issues are the union plus the audited issue' ($body.Contains('#50 - related A') -and $body.Contains('#51 - related B') -and $body.Contains('#999 - TEST related') -and $body.Contains('- #2 (audited issue)') -and -not $body.Contains('This issue') -and -not $body.Contains('related A again')) $body
    $rows = @(Get-Content $csv)
    Assert-That 'opened.csv has 3 rows with the same URL' ($rows.Count -eq 3 -and @($rows | ForEach-Object { ($_ -split ',')[1] } | Select-Object -Unique).Count -eq 1 -and $rows[0].StartsWith('2-delta-docs-a.md,https://github.com/')) ($rows -join ' | ')
    Assert-That 'non-delta draft is not folded and has no row' (-not $body.Contains('Extra Z') -and -not ($rows -join "`n").Contains('2-extra-z.md')) ($rows -join ' | ')

    # --- 2. idempotent re-run ----------------------------------------------------------------------------------
    $r2 = Invoke-Open '-Consolidate'
    Assert-That 're-run opens nothing' ($r2.Exit -eq 0 -and $r2.Creates.Count -eq 0 -and @(Get-Content $csv).Count -eq 3) $r2.Text

    # --- 3. drafts added after the consolidated issue exists: refuse, never a second issue -----------------------
    Write-Text (Join-Path $rem '2-delta-docs-e.md') (New-DocsDraft '[DEBT] Docs page E is stale' 'docs/e.md' 'Low' 'Small' '')
    Write-Text (Join-Path $rem '2-delta-docs-f.md') (New-DocsDraft '[DEBT] Docs page F is stale' 'docs/f.md' 'Medium' 'Small' '')
    $r3 = Invoke-Open '-Consolidate'
    $rows3 = @(Get-Content $csv)
    Assert-That -Name 'later drafts are refused: exit 1, nothing created, no rows' -Condition ($r3.Exit -ne 0 -and $r3.Creates.Count -eq 0 -and $rows3.Count -eq 3) -Detail ($r3.Text + ' | ' + ($rows3 -join ' | '))
    Assert-That -Name 'refusal names the existing issue and the new drafts' -Condition ($r3.Text.Contains('https://github.com/dlrivada/Encina/issues/1001') -and $r3.Text.Contains('2-delta-docs-e.md') -and $r3.Text.Contains('2-delta-docs-f.md')) -Detail $r3.Text
    Remove-Item (Join-Path $rem '2-delta-docs-e.md'), (Join-Path $rem '2-delta-docs-f.md') -Force

    # --- 3b. a bug goes to its own issue, the consolidated issue stays alone -----------------------------------
    Write-Text (Join-Path $rem '2-delta-bug-d.md') $bugDraft
    $r3b = Invoke-Open '-Consolidate'
    $bugCreate = @($r3b.Creates | Where-Object { $_.Contains('--title [BUG] Something is wrong') })
    Assert-That -Name 'bug is its own issue with the Hardening milestone' -Condition ($r3b.Exit -eq 0 -and $r3b.Creates.Count -eq 1 -and $bugCreate.Count -eq 1 -and $bugCreate[0].Contains('--milestone v0.14.0') -and $bugCreate[0].Contains('--label bug')) -Detail ($r3b.Creates -join ' | ')
    $rows3b = @(Get-Content $csv)
    $bugUrl = (($rows3b | Where-Object { $_ -like '2-delta-bug-d.md,*' }) -split ',')[1]
    Assert-That -Name 'opened.csv has 4 rows, the bug row carries its own issue URL' -Condition ($rows3b.Count -eq 4 -and $bugUrl -like 'https://github.com/*') -Detail ($rows3b -join ' | ')

    # --- 4. without -Consolidate: one issue per draft -----------------------------------------------------------
    Write-Text (Join-Path $rem '2-docs-g.md') (New-DocsDraft '[DEBT] Docs page G is stale' 'docs/g.md' 'Low' 'Small' '')
    Write-Text (Join-Path $rem '2-docs-h.md') (New-DocsDraft '[DEBT] Docs page H is stale' 'docs/h.md' 'Low' 'Small' '')
    $r4 = Invoke-Open ''
    Assert-That 'without -Consolidate every draft is its own issue' ($r4.Exit -eq 0 -and $r4.Creates.Count -eq 3 -and -not ($r4.Creates -join ' ').Contains('Delta re-audit')) ($r4.Creates -join ' | ')

    # --- 5. an issue with the same title already exists: reuse it ------------------------------------------------
    # Simulate a crashed run: drop the consolidated rows and drafts so no consolidated issue is recorded.
    foreach ($n in 'a', 'b') { Remove-Item (Join-Path $rem "2-delta-docs-$n.md") -Force }
    Remove-Item (Join-Path $rem '2-delta-test-c.md') -Force
    Write-Text $csv ((@(Get-Content $csv | Where-Object { $_ -notlike '2-delta-docs-?.md,*' -and $_ -notlike '2-delta-test-c.md,*' }) -join "`n") + "`n")
    Write-Text (Join-Path $rem '2-delta-docs-i.md') (New-DocsDraft '[DEBT] Docs page I is stale' 'docs/i.md' 'Low' 'Small' '')
    Write-Text (Join-Path $rem '2-delta-docs-j.md') (New-DocsDraft '[DEBT] Docs page J is stale' 'docs/j.md' 'Low' 'Small' '')
    $env:OR_STUB_EXISTING = '[{"number":777,"title":"[DEBT] Delta re-audit (rules-2026-10) of #2: 2 findings (docs and coverage obligations)"}]'
    $r5 = Invoke-Open '-Consolidate'
    $env:OR_STUB_EXISTING = ''
    $rows5 = @(Get-Content $csv)
    Assert-That 'existing same-title issue is reused, nothing created' ($r5.Exit -eq 0 -and $r5.Creates.Count -eq 0 -and $r5.Text.Contains('already exists') -and @($rows5 | Where-Object { $_ -like '2-delta-docs-?.md,https://github.com/dlrivada/Encina/issues/777' }).Count -eq 2) ($r5.Text + ' | ' + ($rows5 -join ' | '))

    # --- 6. -WhatIf: preview only --------------------------------------------------------------------------------
    foreach ($n in 'i', 'j') { Remove-Item (Join-Path $rem "2-delta-docs-$n.md") -Force }
    Write-Text $csv ((@(Get-Content $csv | Where-Object { $_ -notlike '2-delta-docs-?.md,*' }) -join "`n") + "`n")
    Write-Text (Join-Path $rem '2-delta-docs-k.md') (New-DocsDraft '[DEBT] Docs page K is stale' 'docs/k.md' 'Low' 'Small' '')
    $before = @(Get-Content $csv).Count
    $r6 = Invoke-Open '-Consolidate -WhatIf'
    $previewPath = Join-Path $main 'artifacts\issues\delta-2-consolidated.preview.md'
    Assert-That '-WhatIf writes the preview, creates nothing, writes no rows' ($r6.Exit -eq 0 -and $r6.Creates.Count -eq 0 -and (Test-Path $previewPath) -and (Get-Content -Raw $previewPath).Contains('Docs page K is stale') -and $r6.Text.Contains('Delta re-audit') -and @(Get-Content $csv).Count -eq $before) $r6.Text

    # --- 7. -WhatIf without -Consolidate: nothing is opened ------------------------------------------------------
    $r7 = Invoke-Open '-WhatIf'
    Assert-That -Name '-WhatIf without -Consolidate prints the titles, creates nothing, writes no rows' -Condition ($r7.Exit -eq 0 -and $r7.Creates.Count -eq 0 -and $r7.Text.Contains('WhatIf: would open: [DEBT] Docs page K is stale') -and @(Get-Content $csv).Count -eq $before) -Detail $r7.Text

    # --- 8. the 65,000-character limit (#1863) -------------------------------------------------------------------
    function New-BigDraft([string]$Text, [string]$Marker, [int]$Chars) { return $Text.Replace($Marker, $Marker + "`n`n" + ('x' * $Chars)) }
    $urlOf = { param($file, $rowsList) (($rowsList | Where-Object { $_ -like "$file,*" }) -split ',')[1] }

    # 8a. under the limit: one issue, no part suffix, no part note.
    Write-Text (Join-Path $rem '3-delta-docs-a.md') (New-DocsDraft '[DEBT] Small A' 'docs/sa.md' 'Low' 'Small' '')
    Write-Text (Join-Path $rem '3-delta-docs-b.md') (New-DocsDraft '[DEBT] Small B' 'docs/sb.md' 'Low' 'Small' '')
    $r8a = Invoke-Open '-Consolidate' 3
    $rows8a = @(Get-Content $csv | Where-Object { $_ -like '3-delta-*' })
    Assert-That -Name 'under the limit: one issue, no part suffix, both rows on its URL' -Condition ($r8a.Exit -eq 0 -and $r8a.Creates.Count -eq 1 -and $r8a.Creates[0].Contains('of #3: 2 findings (docs and coverage obligations) --body-file') -and -not $r8a.Creates[0].Contains('(part ') -and $rows8a.Count -eq 2 -and (& $urlOf '3-delta-docs-a.md' $rows8a) -eq (& $urlOf '3-delta-docs-b.md' $rows8a)) -Detail ($r8a.Text + ' | ' + ($rows8a -join ' | '))

    # 8b. over the limit: docs A and B (about 25k each) and the test draft (25k) cannot share one body -> two parts.
    Write-Text (Join-Path $rem '4-delta-docs-a.md') (New-BigDraft (New-DocsDraft '[DEBT] Big A' 'docs/ba.md' 'Low' 'Small' '') 'DESCRIPTION of docs/ba.md.' 25000)
    Write-Text (Join-Path $rem '4-delta-docs-b.md') (New-BigDraft (New-DocsDraft '[DEBT] Big B' 'docs/bb.md' 'Low' 'Small' '') 'DESCRIPTION of docs/bb.md.' 25000)
    Write-Text (Join-Path $rem '4-delta-test-c.md') (New-BigDraft $testDraft 'TEST DESCRIPTION.' 25000)
    $r8b = Invoke-Open '-Consolidate' 4
    $rows8b = @(Get-Content $csv | Where-Object { $_ -like '4-delta-*' })
    $b1 = if (Test-Path (Join-Path $bodies '1.md')) { Get-Content -Raw (Join-Path $bodies '1.md') } else { '' }
    $b2 = if (Test-Path (Join-Path $bodies '2.md')) { Get-Content -Raw (Join-Path $bodies '2.md') } else { '' }
    Assert-That -Name 'over the limit: two parts titled (part k/2) with their own counts, each body under 65,000' -Condition ($r8b.Exit -eq 0 -and $r8b.Creates.Count -eq 2 -and $r8b.Creates[0].Contains('of #4: 2 findings (docs and coverage obligations) (part 1/2) --body-file') -and $r8b.Creates[1].Contains('of #4: 1 finding (docs and coverage obligations) (part 2/2) --body-file') -and $b1.Length -gt 0 -and $b1.Length -le 65000 -and $b2.Length -gt 0 -and $b2.Length -le 65000) -Detail ($r8b.Text + ' | ' + ($r8b.Creates -join ' | ') + " | $($b1.Length) $($b2.Length)")
    Assert-That -Name 'over the limit: each draft whole in its part (docs first, tests last) and the part note present' -Condition ($b1.Contains('Big A') -and $b1.Contains('Big B') -and -not $b1.Contains('TEST DESCRIPTION.') -and $b2.Contains('TEST DESCRIPTION.') -and -not $b2.Contains('Big A') -and $b1.Contains('Part 1 of 2 of the delta re-audit') -and $b2.Contains('Part 2 of 2 of the delta re-audit') -and $b1.Contains('x' * 25000) -and $b2.Contains('x' * 25000)) -Detail "$($b1.Length) $($b2.Length)"
    Assert-That -Name 'over the limit: each row points at the part holding the draft' -Condition ($rows8b.Count -eq 3 -and (& $urlOf '4-delta-docs-a.md' $rows8b) -eq 'https://github.com/dlrivada/Encina/issues/1001' -and (& $urlOf '4-delta-docs-b.md' $rows8b) -eq 'https://github.com/dlrivada/Encina/issues/1001' -and (& $urlOf '4-delta-test-c.md' $rows8b) -eq 'https://github.com/dlrivada/Encina/issues/1002') -Detail ($rows8b -join ' | ')
    $items8b = @(Get-Content $log | Where-Object { $_ -like 'gh project item-add 1 --owner dlrivada --url *' })
    Assert-That -Name 'split path: each part is added to project 1 (two item-add calls, one per part URL)' -Condition ($items8b.Count -eq 2 -and $items8b[0].EndsWith('/issues/1001') -and $items8b[1].EndsWith('/issues/1002')) -Detail ($items8b -join ' | ')

    # 8c. a re-run after the split opens nothing; a later draft is refused while any part exists.
    $r8c = Invoke-Open '-Consolidate' 4
    Assert-That -Name 'after a split, a re-run opens nothing' -Condition ($r8c.Exit -eq 0 -and $r8c.Creates.Count -eq 0 -and @(Get-Content $csv | Where-Object { $_ -like '4-delta-*' }).Count -eq 3) -Detail $r8c.Text

    Write-Text (Join-Path $rem '4-delta-docs-late.md') (New-DocsDraft '[DEBT] Late' 'docs/late.md' 'Low' 'Small' '')
    $r8c2 = Invoke-Open '-Consolidate' 4
    Assert-That -Name 'after a split, a later draft is refused: exit 1, nothing created, no rows' -Condition ($r8c2.Exit -ne 0 -and $r8c2.Creates.Count -eq 0 -and $r8c2.Text.Contains('4-delta-docs-late.md') -and @(Get-Content $csv | Where-Object { $_ -like '4-delta-*' }).Count -eq 3) -Detail $r8c2.Text
    Remove-Item (Join-Path $rem '4-delta-docs-late.md') -Force

    # 8d. -WhatIf on a split: one preview per part, nothing created.
    Remove-Item (Join-Path $rem '4-delta-docs-a.md'), (Join-Path $rem '4-delta-docs-b.md'), (Join-Path $rem '4-delta-test-c.md') -Force
    Write-Text $csv ((@(Get-Content $csv | Where-Object { $_ -notlike '4-delta-*' }) -join "`n") + "`n")
    Write-Text (Join-Path $rem '4-delta-docs-a.md') (New-BigDraft (New-DocsDraft '[DEBT] Big A' 'docs/ba.md' 'Low' 'Small' '') 'DESCRIPTION of docs/ba.md.' 25000)
    Write-Text (Join-Path $rem '4-delta-docs-b.md') (New-BigDraft (New-DocsDraft '[DEBT] Big B' 'docs/bb.md' 'Low' 'Small' '') 'DESCRIPTION of docs/bb.md.' 25000)
    Write-Text (Join-Path $rem '4-delta-test-c.md') (New-BigDraft $testDraft 'TEST DESCRIPTION.' 25000)
    $before8d = @(Get-Content $csv).Count
    $r8d = Invoke-Open '-Consolidate -WhatIf' 4
    $pv1 = Join-Path $main 'artifacts\issues\delta-4-consolidated.part1.preview.md'
    $pv2 = Join-Path $main 'artifacts\issues\delta-4-consolidated.part2.preview.md'
    Assert-That -Name '-WhatIf on a split: part titles printed, one preview per part under the limit, nothing created or written' -Condition ($r8d.Exit -eq 0 -and $r8d.Creates.Count -eq 0 -and $r8d.Text.Contains('(part 1/2)') -and $r8d.Text.Contains('(part 2/2)') -and (Test-Path $pv1) -and (Test-Path $pv2) -and (Get-Content -Raw $pv1).Length -le 65000 -and (Get-Content -Raw $pv2).Length -le 65000 -and @(Get-Content $csv).Count -eq $before8d) -Detail $r8d.Text

    # 8e. one draft alone over the limit: fails naming it, nothing created, no rows.
    # The huge draft is not first and a [BUG] draft exists: even the bug issue must not be created.
    Write-Text (Join-Path $rem '5-delta-docs-a.md') (New-DocsDraft '[DEBT] Fine' 'docs/fine.md' 'Low' 'Small' '')
    Write-Text (Join-Path $rem '5-delta-docs-b.md') (New-BigDraft (New-DocsDraft '[DEBT] Huge' 'docs/huge.md' 'Low' 'Small' '') 'DESCRIPTION of docs/huge.md.' 70000)
    Write-Text (Join-Path $rem '5-delta-bug-c.md') $bugDraft
    $before8e = @(Get-Content $csv).Count
    $r8e = Invoke-Open '-Consolidate' 5
    Assert-That -Name 'a single oversized draft (not first) fails naming it, creates nothing (bug included), writes no rows' -Condition ($r8e.Exit -ne 0 -and $r8e.Creates.Count -eq 0 -and $r8e.Text.Contains('5-delta-docs-b.md') -and @(Get-Content $csv).Count -eq $before8e) -Detail $r8e.Text

    # --- 9. milestone and priority routes, project add (#1926) ---------------------------------------------------
    $docsMs = "v0.21.0 $([char]0x2014) Documentation"; $testsMs = "v0.19.0 $([char]0x2014) Providers & Testing"; $hardMs = "v0.14.0 $([char]0x2014) Hardening"
    # 9a. a docs-only consolidated issue -> Documentation + p1.
    Write-Text (Join-Path $rem '6-delta-docs-a.md') (New-DocsDraft '[DEBT] Docs only A' 'docs/oa.md' 'Low' 'Small' '')
    Write-Text (Join-Path $rem '6-delta-docs-b.md') (New-DocsDraft '[DEBT] Docs only B' 'docs/ob.md' 'Low' 'Small' '')
    $r9a = Invoke-Open '-Consolidate' 6
    Assert-That -Name 'docs-only consolidated: Documentation milestone and the drafts own priority (Low -> p2-post-1.0)' -Condition ($r9a.Exit -eq 0 -and $r9a.Creates.Count -eq 1 -and $r9a.Creates[0].Contains("--milestone $docsMs") -and $r9a.Creates[0].Contains('--label p2-post-1.0')) -Detail ($r9a.Text + ' | ' + ($r9a.Creates -join ' | '))

    # 9b. per-draft routes: docs, code debt, tests and a header milestone that wins.
    Write-Text (Join-Path $rem '7-docs.md') (New-DocsDraft '[DEBT] Route docs' 'docs/r1.md' 'Low' 'Small' '')
    Write-Text (Join-Path $rem '7-code.md') ((New-DocsDraft '[DEBT] Route code' 'src/r2.cs' 'Medium' 'Small' '').Replace('kind: docs', 'kind: code'))
    Write-Text (Join-Path $rem '7-test.md') $testDraft
    Write-Text (Join-Path $rem '7-win.md') ((New-DocsDraft '[DEBT] Route header wins' 'src/r3.cs' 'High' 'Small' '').Replace('kind: docs', 'kind: code').Replace('milestone:', "milestone: $testsMs"))
    # A draft without a Priority section takes its route's default priority.
    Write-Text (Join-Path $rem '7-nop.md') ([regex]::Replace((New-DocsDraft '[DEBT] Route no priority' 'src/r4.cs' 'Low' 'Small' '').Replace('kind: docs', 'kind: code'), '(?s)## Priority.*?(?=## Effort)', ''))
    $r9b = Invoke-Open '' 7
    $c = @{}; foreach ($line in $r9b.Creates) { foreach ($t in 'Route docs', 'Route code', 'Raise the unit target', 'Route header wins', 'Route no priority') { if ($line.Contains($t)) { $c[$t] = $line } } }
    Assert-That -Name 'per-draft routes: docs Low -> Documentation/p2, code Medium -> Hardening/p1, test (no priority) -> Providers & Testing/p1, header milestone wins with High -> p0' -Condition ($r9b.Exit -eq 0 -and $r9b.Creates.Count -eq 5 -and $c['Route docs'].Contains("--milestone $docsMs") -and $c['Route docs'].Contains('--label p2-post-1.0') -and $c['Route code'].Contains("--milestone $hardMs") -and $c['Route code'].Contains('--label p1-recommended') -and $c['Raise the unit target'].Contains("--milestone $testsMs") -and $c['Raise the unit target'].Contains('--label p1-recommended') -and $c['Route header wins'].Contains("--milestone $testsMs") -and $c['Route header wins'].Contains('--label p0-mandatory')) -Detail ($r9b.Text + ' | ' + ($r9b.Creates -join ' | '))
    Assert-That -Name 'a code draft without a Priority section gets the route default (Hardening, p0-mandatory)' -Condition ($c['Route no priority'].Contains("--milestone $hardMs") -and $c['Route no priority'].Contains('--label p0-mandatory')) -Detail ($c['Route no priority'])
    Assert-That -Name 'every created issue is added to project 1' -Condition (@(Get-Content $log | Where-Object { $_ -like 'gh project item-add 1 --owner dlrivada --url *' }).Count -eq 5) -Detail ((Get-Content $log) -join ' | ')

    # 9b2. a mixed code + docs consolidation goes to Hardening (never Documentation), with the highest priority.
    Write-Text (Join-Path $rem '9-delta-docs-a.md') (New-DocsDraft '[DEBT] Mixed docs' 'docs/m1.md' 'Low' 'Small' '')
    Write-Text (Join-Path $rem '9-delta-code-b.md') ((New-DocsDraft '[DEBT] Mixed code' 'src/m2.cs' 'Medium' 'Small' '').Replace('kind: docs', 'kind: code'))
    $r9m = Invoke-Open '-Consolidate' 9
    Assert-That -Name 'mixed code + docs consolidation: Hardening milestone and the highest draft priority (Medium -> p1)' -Condition ($r9m.Exit -eq 0 -and $r9m.Creates.Count -eq 1 -and $r9m.Creates[0].Contains("--milestone $hardMs") -and $r9m.Creates[0].Contains('--label p1-recommended') -and -not $r9m.Creates[0].Contains($docsMs)) -Detail ($r9m.Text + ' | ' + ($r9m.Creates -join ' | '))

    # 9c. a project failure is a loud warning (the issue URL and the retry command), never an undone issue or a
    # failed run (#1987); the row is written, so a re-run never duplicates.
    Write-Text (Join-Path $rem '8-docs.md') (New-DocsDraft '[DEBT] Project fails' 'docs/pf.md' 'Low' 'Small' '')
    $env:OR_STUB_PROJECT_FAIL = '1'
    $r9c = Invoke-Open '' 8
    $env:OR_STUB_PROJECT_FAIL = ''
    $retry = 'gh project item-add 1 --owner dlrivada --url https://github.com/dlrivada/Encina/issues/1001'
    Assert-That -Name 'a failed project add warns with the issue URL and the retry command, exits 0 and keeps the issue and its row' -Condition ($r9c.Exit -eq 0 -and $r9c.Creates.Count -eq 1 -and $r9c.Text.Contains('gh project item-add failed') -and $r9c.Text.Contains($retry) -and @(Get-Content $csv | Where-Object { $_ -like '8-docs.md,*' }).Count -eq 1) -Detail $r9c.Text

    # 9d. the same failure on the consolidated path: warning, exit 0, rows written for every draft.
    Write-Text (Join-Path $rem '10-delta-docs-a.md') (New-DocsDraft '[DEBT] Consolidated project fails A' 'docs/cf1.md' 'Low' 'Small' '')
    Write-Text (Join-Path $rem '10-delta-docs-b.md') (New-DocsDraft '[DEBT] Consolidated project fails B' 'docs/cf2.md' 'Low' 'Small' '')
    $env:OR_STUB_PROJECT_FAIL = '1'
    $r9d = Invoke-Open '-Consolidate' 10
    $env:OR_STUB_PROJECT_FAIL = ''
    Assert-That -Name 'a failed project add on the consolidated path warns with the retry command, exits 0 and keeps both rows' -Condition ($r9d.Exit -eq 0 -and $r9d.Creates.Count -eq 1 -and $r9d.Text.Contains($retry) -and @(Get-Content $csv | Where-Object { $_ -like '10-delta-*' }).Count -eq 2) -Detail $r9d.Text
}
finally {
    $env:GITHUB_TOKEN = $savedGhToken; $env:GH_TOKEN = $savedGhToken2
    if (Test-Path $base) {
        Get-ChildItem -LiteralPath $base -Recurse -Force -File -ErrorAction SilentlyContinue | ForEach-Object { $_.IsReadOnly = $false }
        Remove-Item -LiteralPath $base -Recurse -Force -ErrorAction SilentlyContinue
    }
}

if ($failures.Count -gt 0) { Write-Host "`n$($failures.Count) assertion(s) failed."; exit 1 }
Write-Host "`nAll open-remediation assertions passed."
exit 0
