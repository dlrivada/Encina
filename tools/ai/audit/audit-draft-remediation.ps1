# tools/ai/audit/audit-draft-remediation.ps1 (#1345, revised by #1375)
#
# The remediation stage: splits the code, tests and docs stage artifacts' '## Findings' sections into
# individual findings (Split-Findings in _audit-lib.ps1, one per numbered "N. **Severity** -- ..." paragraph),
# and for each surviving finding:
#   (a) asks the free local model (tools/ai/local-ai-ask.cs) to classify it -- kind (bug/test/debt/docs) and
#       whether it duplicates one of a short list of open-issue candidates found with `gh issue list --search`;
#   (b) when it is not a duplicate, routes it to the matching issue template
#       (.github/ISSUE_TEMPLATE/{bug_report,test_implementation,technical_debt}.md) and asks the local model to
#       draft the issue file, embedding that template's real headers and checkboxes verbatim, into
#       artifacts/knowledge/remediation/<n>-<stage>-<id>-<slug>.md.
# Writes stages/remediation.md listing, per finding, its draft file, a "duplicate of #m" line, or the reason it
# was skipped, plus a real "## Lessons for the pipeline" section. Requires stages/docs.md -- the docs stage
# must have already run, even when it found nothing to review.
#
# -DryRun performs every step except the two local-model calls: it writes the per-finding input file and the
# per-finding brief (the exact text that would go to the model, template embedded) under
# artifacts/knowledge/remediation/_dryrun-<n>/, and prints the routing it would apply, using a deterministic
# fallback kind (Blocker in the code stage -> bug; tests stage -> test; docs stage -> docs; otherwise -> debt)
# instead of the model's classification. No duplicate is ever assumed in -DryRun (that decision needs the
# model), so every finding gets a routed brief. Pair it with -NoGh to also skip the `gh issue list` duplicate
# search -- what Test-Hooks.ps1 exercises: the real model and `gh` are never called in tests.
#
# -Only "<stage> <n>" (repeatable, e.g. -Only "code 3" -Only "tests 1") (#1492 decision 3): regenerates ONLY the
# named finding(s) -- an audit-verifier FAIL against one or two drafts must not re-roll every other draft's own
# already-correct model choices (severity, kind, template placeholders, Related Issues), which is exactly why a
# FAIL loop failed to converge (audit #17). Every other finding's own draft, input, brief and dry-run preview
# file on disk is left completely untouched (never deleted, never rewritten), and stages/remediation.md keeps
# every other finding's own line verbatim, taken from the file's own previous content -- only the regenerated
# finding(s)' lines and Lessons entries are replaced. Requires stages/remediation.md to already exist (a full
# regeneration must have run at least once) and every OTHER finding currently parsed from the stage artifacts
# to already have a line there; otherwise this errors rather than guessing what an unprocessed finding's line
# should say.

param(
    [switch]$DryRun,
    [switch]$NoGh,
    [string[]]$Only
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')
. (Join-Path $PSScriptRoot '_remediation-checks.ps1')

# #1492 decision 3: parse -Only into a set of "stage|id" keys up front (independent of the findings parsed
# below, so a malformed -Only value is reported before any other work happens).
$onlyKeys = $null
if ($Only -and $Only.Count -gt 0) {
    $onlyKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($spec in $Only) {
        $specParts = @($spec -split '\s+' | Where-Object { $_ -ne '' })
        if ($specParts.Count -ne 2) {
            Write-Error "audit-draft-remediation: -Only value '$spec' must be '<stage> <n>' (e.g. 'code 3')."
            exit 1
        }
        [void]$onlyKeys.Add("$($specParts[0])|$($specParts[1])")
    }
}

$mainRoot = Get-MainRoot $PSScriptRoot
$audit = Get-CurrentAudit $mainRoot
if ($null -eq $audit) { Write-Error 'audit-draft-remediation: no open audit (artifacts/knowledge/current-audit.json not found). Run audit-next.ps1 first.'; exit 1 }

$wt = [string]$audit.worktree
$n = [string]$audit.issue
$stagesDir = Get-StagesDir $wt
$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')

function StageFile([string]$Name) {
    $def = $pipeline.stages | Where-Object { $_.stage -eq $Name }
    if ($null -eq $def) { throw "audit-draft-remediation: pipeline.json has no '$Name' stage." }
    return Join-Path $stagesDir $def.artifact
}

$docsFile = StageFile 'docs'
if (-not (Test-Path -LiteralPath $docsFile)) {
    Write-Error "audit-draft-remediation: stages\$(Split-Path -Leaf $docsFile) is missing; run the docs stage before remediation."
    exit 1
}

# Routing table (#1375 decision 3): finding kind -> template file, title prefix, labels, milestone. The
# milestone title's em dash is built from its code point so this file's own bytes stay ASCII-only while still
# matching the real GitHub milestone title exactly (mirrors open-remediation.ps1's own convention).
$templatesDir = Join-Path $wt '.github\ISSUE_TEMPLATE'
$hardeningMilestone = "v0.14.0 $([char]0x2014) Hardening"
$routing = @{
    bug  = [pscustomobject]@{ Template = 'bug_report.md'; Prefix = '[BUG]'; Labels = @('bug'); Milestone = $hardeningMilestone }
    test = [pscustomobject]@{ Template = 'test_implementation.md'; Prefix = '[TEST]'; Labels = @('area-testing'); Milestone = '' }
    debt = [pscustomobject]@{ Template = 'technical_debt.md'; Prefix = '[DEBT]'; Labels = @('technical-debt'); Milestone = '' }
    docs = [pscustomobject]@{ Template = 'technical_debt.md'; Prefix = '[DEBT]'; Labels = @('technical-debt', 'area-documentation'); Milestone = '' }
}

# -NoGh is meant for -DryRun / offline testing: it skips this check along with the per-finding duplicate
# search below, so a real (non-dry) run should always be started WITHOUT -NoGh, or a docs draft could carry
# a 'area-documentation' label this repository does not actually have.
if (-not $NoGh -and -not $DryRun) {
    $existingLabels = @(& gh label list --repo dlrivada/Encina --limit 400 --json name --jq '.[].name' 2>$null)
    if ($LASTEXITCODE -ne 0) { Write-Error "audit-draft-remediation: 'gh label list' failed (exit $LASTEXITCODE)."; exit 1 }
    if ($existingLabels -notcontains 'area-documentation') { $routing.docs.Labels = @('technical-debt') }
}

function Get-TemplateBody([string]$TemplateFile) {
    $path = Join-Path $templatesDir $TemplateFile
    if (-not (Test-Path -LiteralPath $path)) { throw "audit-draft-remediation: issue template '$TemplateFile' not found at '$path'." }
    $raw = Get-Content -LiteralPath $path -Raw
    return ($raw -replace '(?s)^---.*?---\r?\n', '').Trim()
}

# A '## Findings' header literally present in the file -- distinct from Get-StageSection's return value,
# which is '' both when the file/header is missing AND when the header is present with a genuinely empty body,
# so it cannot tell "no header at all" (a malformed stage artifact) from "header present, body empty" on its
# own (#1375 CodeRabbit review). Only an explicit "- none" body means zero findings; a missing header is
# always an error.
function Test-FindingsHeaderPresent([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $text = Get-Content -LiteralPath $Path -Raw
    return [regex]::IsMatch($text, '(?m)^##\s*Findings\s*$')
}

# A model-named 'duplicate-of #m' is only honored when m is one of the candidates actually offered to the
# model (from the gh search); a fabricated or hallucinated issue number must never suppress a real finding's
# draft. $DuplicateOf and every entry of $CandidateNumbers are plain numeric strings (no '#').
function Test-ValidDuplicate([string]$DuplicateOf, [string[]]$CandidateNumbers) {
    if ([string]::IsNullOrWhiteSpace($DuplicateOf)) { return $false }
    return $CandidateNumbers -contains $DuplicateOf
}

# #1388 decision 2: `gh issue view <n> --json title,body`, cached per issue number for the whole run -- the
# classifier's candidate excerpts and the duplicate-evidence check below both need a candidate's real body
# text, and a candidate the model later names as a duplicate is one this cache already fetched while building
# the classify prompt, so it is never fetched twice.
function Get-CachedIssueTitleBody([string]$Number, [hashtable]$Cache, [string]$Label) {
    if ($Cache.Contains($Number)) { return $Cache[$Number] }
    $viewOut = & gh issue view $Number --repo dlrivada/Encina --json title,body 2>&1
    if ($LASTEXITCODE -ne 0) { Write-Error "audit-draft-remediation: 'gh issue view $Number' failed for $Label (exit $LASTEXITCODE): $viewOut"; exit 1 }
    $parsed = $null
    try { $parsed = $viewOut | ConvertFrom-Json } catch { $parsed = $null }
    $Cache[$Number] = $parsed
    return $parsed
}

function Get-DryRunKind([string]$Stage, [string]$Severity) {
    if ($Stage -eq 'tests') { return 'test' }
    if ($Stage -eq 'docs') { return 'docs' }
    if ($Stage -eq 'code' -and $Severity -eq 'Blocker') { return 'bug' }
    return 'debt'
}

# Up to 4 search terms from a finding's text, for the `gh issue list --search` duplicate query: backticked
# identifiers or file:line citations first (the most specific terms a finding carries), then un-backticked
# path-like tokens with a known extension (a finding may cite 'src/A.cs:12' in plain prose, not backticks).
# Both kinds are reduced to a file basename; the un-backticked kind additionally drops the extension (a search
# for 'A.cs' rarely matches an issue title the way 'A' sometimes does).
function Get-SearchTerms([string]$Text) {
    $terms = [System.Collections.Generic.List[string]]::new()

    foreach ($m in [regex]::Matches($Text, '`([^`]+)`')) {
        $clean = ($m.Groups[1].Value -split '[:\s]')[0]
        if ([string]::IsNullOrWhiteSpace($clean)) { continue }
        $base = Split-Path -Leaf $clean
        if ($base -and ($terms -notcontains $base)) { $terms.Add($base) }
        if ($terms.Count -ge 4) { return $terms }
    }

    foreach ($m in [regex]::Matches($Text, '[\w./\\-]+\.(cs|ps1|md|json|yml|yaml|csproj|txt)(:\d+(-\d+)?)?')) {
        $stripped = ($m.Value -split ':')[0]
        $baseNoExt = [IO.Path]::GetFileNameWithoutExtension((Split-Path -Leaf $stripped))
        if ([string]::IsNullOrWhiteSpace($baseNoExt)) { continue }
        if ($terms -notcontains $baseNoExt) { $terms.Add($baseNoExt) }
        if ($terms.Count -ge 4) { break }
    }

    return $terms
}

function New-Slug([string]$Text) {
    $clean = $Text -replace '[`*_#]', ' '
    $words = @([regex]::Matches($clean, '[A-Za-z0-9]+') | Select-Object -First 8 -ExpandProperty Value)
    $slug = ($words -join '-').ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($slug)) { $slug = 'finding' }
    if ($slug.Length -gt 60) { $slug = $slug.Substring(0, 60) }
    return $slug
}

function Build-DraftBrief([string]$IssueNumber, [pscustomobject]$Finding, [string]$Kind, [pscustomobject]$Route, [string]$CandidateLines) {
    $templateBody = Get-TemplateBody $Route.Template
    $labelsLine = $Route.Labels -join ', '
    $docsNote = if ($Kind -eq 'docs') { "`n- This is a documentation gap: tick only the 'Documentation gap' box in the Type section (leave the other Type boxes unticked)." } else { '' }
    # bug_report.md and test_implementation.md have no Priority/Effort Estimate sections -- only
    # technical_debt.md does; only that template gets this guidance line, so the brief never asks the model to
    # fill a section the chosen template does not have.
    $priorityNote = if ($Route.Template -eq 'technical_debt.md') {
        $priority = switch ($Finding.Severity) { 'Blocker' { 'High' } 'Major' { 'Medium' } default { 'Low' } }
        "`n- Priority: tick $priority (from the finding's severity: Blocker -> High, Major -> Medium, Minor/Unknown -> Low).`n- Effort Estimate: use your own judgement (Small/Medium/Large) from the finding's scope."
    }
    else { '' }
    # #1409: the model has no way to know the real Encina/.NET version or OS for a static-analysis finding, so
    # it otherwise copies bug_report.md's own bracketed placeholders through unchanged. The script overwrites
    # this whole section deterministically after the model replies (Set-BugEnvironment, called from
    # Repair-Draft below) regardless of what the model writes here, but this note still asks the model to match
    # those same facts, so its own prose elsewhere in the draft (e.g. Additional Context) stays consistent with
    # the section the script will actually keep.
    $envNote = if ($Route.Template -eq 'bug_report.md') {
        $version = Get-EncinaVersion $wt
        $inferredPackage = Get-PackageFromFindingText $Finding.Text
        $packageHint = if ($inferredPackage) { $inferredPackage } else { "the package the finding's file path names" }
        "`n- Environment: this section will be overwritten deterministically after you reply, so match it rather than guessing -- Encina Version `"$version`", .NET Version `".NET 10`", OS `"Not applicable (found by static review of the code, not at runtime)`", Package(s) Affected `"$packageHint`"."
    }
    else { '' }
    return @"
Draft ONE remediation issue file for a finding from the SPEC-003 audit of closed GitHub issue #$IssueNumber of
the Encina .NET library ($($Finding.Stage) stage, finding $($Finding.Id), severity $($Finding.Severity)). Use
ONLY the input finding text; never invent facts. Output EXACTLY the header comment block below followed by the
template body below it, keeping every '## ' header of the template body verbatim and in the same order, and
ticking a checkbox only from the options the template body itself lists:

<!--
title: $($Route.Prefix) <specific title drawn from the finding>
labels: $labelsLine
milestone: $($Route.Milestone)
-->

$templateBody

Guidance:
- Put the finding's file:line evidence in the Location (or Steps to Reproduce) section.
- Related Issues: include #$IssueNumber and any of these candidate open issues that are related but are NOT
  the same problem (a same-problem duplicate must never reach this step): $CandidateLines$priorityNote$docsNote$envNote
"@
}

# #1388 decisions 3/4 (#1400 decision 2: placeholders are now derived from the ONE routed template, not every
# template in the directory): strips one outer code fence and reports remaining template placeholders for a
# draft already written to $Path, rewriting the file in place when the fence was stripped. Returns the
# (possibly empty) list of offending placeholder lines still in the draft after the fence strip. $LessonsList
# is the script's own $lessons list, passed explicitly rather than captured, since this function is called once
# per finding across the whole loop below. $RouteTemplateFile is the routed template's own file name
# (e.g. 'technical_debt.md'), read fresh here so Find-TemplatePlaceholders always sees the same template the
# finding was drafted against.
function Repair-Draft([string]$Path, [string]$Label, [string]$RouteTemplateFile, [System.Collections.Generic.List[string]]$LessonsList, [string]$FindingText, [string]$RepoRoot, [string]$DebtType) {
    $raw = Get-Content -LiteralPath $Path -Raw
    $defenced = Remove-OuterFence $raw
    if ($defenced -ne $raw) {
        $LessonsList.Add("$Label`: draft $(Split-Path -Leaf $Path) was wrapped in an outer code fence; stripped it before writing.")
    }
    # #1409: for a bug_report.md-routed draft, the '## Environment' section is overwritten deterministically
    # here, BEFORE Find-TemplatePlaceholders runs below, so the model's own guess at facts it cannot know (the
    # Encina/.NET version, the OS) is never what decides whether the draft is clean.
    # #1492 decision 1: for a technical_debt.md-routed draft, the '## Type' checkbox is overwritten the same
    # way, with the deterministic label the caller already computed (Get-DeterministicDebtType), so the model's
    # own tick is never what decides which box stays checked.
    $repaired = if ($RouteTemplateFile -eq 'bug_report.md') { Set-BugEnvironment $defenced $RepoRoot $FindingText }
    elseif ($RouteTemplateFile -eq 'technical_debt.md' -and $DebtType) { Set-DebtType $defenced $DebtType }
    else { $defenced }
    if ($repaired -ne $raw) {
        Set-Content -LiteralPath $Path -Encoding utf8 -NoNewline -Value $repaired
    }
    $templateText = Get-Content -LiteralPath (Join-Path $templatesDir $RouteTemplateFile) -Raw
    return (Find-TemplatePlaceholders $templateText $repaired)
}

$stageNames = 'code', 'tests', 'docs'
$allFindings = [System.Collections.Generic.List[pscustomobject]]::new()
foreach ($stageName in $stageNames) {
    $stageFile = StageFile $stageName
    if (-not (Test-FindingsHeaderPresent $stageFile)) {
        Write-Error "audit-draft-remediation: stages\$(Split-Path -Leaf $stageFile) has no '## Findings' header; the stage must write one (with '- none' when there are no findings)."
        exit 1
    }
    $section = Get-StageSection $stageFile 'Findings'
    try {
        foreach ($f in (Split-Findings $stageName $section)) { $allFindings.Add($f) }
    }
    catch {
        Write-Error "audit-draft-remediation: $($_.Exception.Message)"
        exit 1
    }
}

$remediationDir = Join-Path $mainRoot 'artifacts\knowledge\remediation'
New-Item -ItemType Directory -Force $remediationDir | Out-Null

# #1492 decision 3: -Only validation and existing-file parse, done before any cleanup so a bad -Only value or a
# missing/incomplete stages/remediation.md is reported before anything on disk is touched.
$existingFindingLines = @{}
$existingLessonLines = [System.Collections.Generic.List[string]]::new()
if ($onlyKeys) {
    $allFindingKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($f in $allFindings) { [void]$allFindingKeys.Add("$($f.Stage)|$($f.Id)") }
    foreach ($key in $onlyKeys) {
        if (-not $allFindingKeys.Contains($key)) {
            $keyParts = $key -split '\|', 2
            Write-Error "audit-draft-remediation: -Only '$($keyParts[0]) $($keyParts[1])' does not match a finding currently parsed from the code, tests or docs stage artifacts."
            exit 1
        }
    }

    $remediationFile = StageFile 'remediation'
    if (-not (Test-Path -LiteralPath $remediationFile)) {
        Write-Error "audit-draft-remediation: -Only requires an existing stages\$(Split-Path -Leaf $remediationFile) to update; run a full regeneration (no -Only) first."
        exit 1
    }
    $inLessonsSection = $false
    foreach ($rawLine in (Get-Content -LiteralPath $remediationFile)) {
        if ($rawLine -eq '## Lessons for the pipeline') { $inLessonsSection = $true; continue }
        if ($inLessonsSection) {
            if ($rawLine -eq '- none') { continue }
            $existingLessonLines.Add(($rawLine -replace '^-\s*', ''))
            continue
        }
        $lineMatch = [regex]::Match($rawLine, '^-\s+(?<stage>\S+)\s+(?<id>\S+)\s+\(')
        if ($lineMatch.Success) {
            $existingFindingLines["$($lineMatch.Groups['stage'].Value)|$($lineMatch.Groups['id'].Value)"] = $rawLine
        }
    }
    foreach ($f in $allFindings) {
        $key = "$($f.Stage)|$($f.Id)"
        if (-not $onlyKeys.Contains($key) -and -not $existingFindingLines.ContainsKey($key)) {
            Write-Error "audit-draft-remediation: -Only cannot find an existing line for '$($f.Stage) $($f.Id)' in stages\$(Split-Path -Leaf $remediationFile); run a full regeneration (no -Only) first."
            exit 1
        }
    }
}

$findingsToProcess = if ($onlyKeys) { @($allFindings | Where-Object { $onlyKeys.Contains("$($_.Stage)|$($_.Id)") }) } else { $allFindings }

if ($onlyKeys) {
    # -Only regenerates ONLY the named finding(s): remove just their own previous outputs, never another
    # finding's draft/input/brief/dry-run-preview file, so every other draft on disk stays byte-identical.
    foreach ($key in $onlyKeys) {
        $keyParts = $key -split '\|', 2
        $keyStage = $keyParts[0]; $keyId = $keyParts[1]
        # #1492 (adversarial review): every pattern below has a literal separator immediately after $keyId, so
        # a numeric-id prefix collision within the same stage (-Only "code 1" vs. an existing "code 10") can
        # never match another finding's file -- "_brief-...-$keyId*.md" (no separator before the wildcard) used
        # to be the one exception, matching "_brief-<n>-code-10.md" too and deleting an untouched finding's
        # brief without ever regenerating it; it is now two exact patterns (the base brief and its "-reask"
        # variant), like the input/classify patterns already were.
        $narrowPatterns = "$n-$keyStage-$keyId-*.md", "_input-$n-$keyStage-$keyId.md", "_classify-brief-$n-$keyStage-$keyId.md", "_classify-$n-$keyStage-$keyId.md", "_brief-$n-$keyStage-$keyId.md", "_brief-$n-$keyStage-$keyId-reask.md"
        foreach ($pattern in $narrowPatterns) {
            foreach ($staleFile in (Get-ChildItem -LiteralPath $remediationDir -Filter $pattern -File -ErrorAction SilentlyContinue)) {
                Remove-Item -LiteralPath $staleFile.FullName -Force
                "audit-draft-remediation: removed previous output $($staleFile.Name)"
            }
        }
    }
    $dryRunDir = Join-Path $remediationDir "_dryrun-$n"
    if ($DryRun) {
        New-Item -ItemType Directory -Force $dryRunDir | Out-Null
        foreach ($key in $onlyKeys) {
            $keyParts = $key -split '\|', 2
            foreach ($staleFile in (Get-ChildItem -LiteralPath $dryRunDir -Filter "$($keyParts[0])-$($keyParts[1])-*.md" -File -ErrorAction SilentlyContinue)) {
                Remove-Item -LiteralPath $staleFile.FullName -Force
            }
        }
    }
    else {
        $dryRunDir = $null
    }
}
else {
    # Remove only THIS audit's previous outputs before drafting -- a re-run (e.g. after a Verdict: FAIL) must not
    # accumulate stale drafts/intermediates from an earlier run of the same audit, and must never touch another
    # audit's files (every pattern below is anchored on "$n-", never a bare wildcard).
    $cleanupPatterns = "$n-*.md", "_input-$n-*.md", "_classify-brief-$n-*.md", "_classify-$n-*.md", "_brief-$n-*.md"
    foreach ($pattern in $cleanupPatterns) {
        foreach ($staleFile in (Get-ChildItem -LiteralPath $remediationDir -Filter $pattern -File -ErrorAction SilentlyContinue)) {
            Remove-Item -LiteralPath $staleFile.FullName -Force
            "audit-draft-remediation: removed previous output $($staleFile.Name)"
        }
    }
    $dryRunDir = Join-Path $remediationDir "_dryrun-$n"
    if (Test-Path -LiteralPath $dryRunDir) {
        Remove-Item -LiteralPath $dryRunDir -Recurse -Force
        "audit-draft-remediation: removed previous output _dryrun-$n\"
    }

    if ($DryRun) {
        New-Item -ItemType Directory -Force $dryRunDir | Out-Null
    }
    else {
        $dryRunDir = $null
    }
}

$lines = [System.Collections.Generic.List[string]]::new()
$lessons = [System.Collections.Generic.List[string]]::new()
$placeholderFailures = [System.Collections.Generic.List[string]]::new()
$ghIssueCache = @{}

# A finding Split-Findings could not parse into the expected numbered layout (Severity 'Unknown', the whole
# section as its Text) is never allowed to pass through silently: the stage's own findings format drifted from
# what this parser recognizes, which is exactly the "one issue eats every finding" failure #1375 was filed to
# fix if it goes unnoticed. Flag it as a pipeline lesson so the orchestrator sees it and can decide whether the
# stage needs re-running with a corrected format, even though it still gets classified and drafted like any
# other finding below (never dropped).
# #1492 decision 3: scoped to $findingsToProcess (not $allFindings), so an -Only run never re-adds this lesson
# for a finding it did not touch this time -- $existingLessonLines below already carries that finding's own
# earlier copy of it forward untouched.
foreach ($unknownFinding in ($findingsToProcess | Where-Object { $_.Severity -eq 'Unknown' })) {
    $lessons.Add("$($unknownFinding.Stage) $($unknownFinding.Id): the stage's '## Findings' section did not match the expected numbered 'N. **Blocker/Major/Minor** -- ...' layout; treated as one Unknown-severity finding covering the whole section instead of being split further.")
}

foreach ($finding in $findingsToProcess) {
    $label = "$($finding.Stage) $($finding.Id) ($($finding.Severity))"
    $inputFile = if ($DryRun) { Join-Path $dryRunDir "$($finding.Stage)-$($finding.Id)-input.md" } else { Join-Path $remediationDir "_input-$n-$($finding.Stage)-$($finding.Id).md" }
    Set-Content -LiteralPath $inputFile -Encoding utf8 -Value $finding.Text

    # One 'gh issue list --search' call per term (never terms joined with spaces -- GitHub's search treats a
    # space-separated query as AND, which misses candidates that match only one term), merged by issue number
    # so the same issue found by two terms is not listed twice, capped at 10 candidates.
    $candidatesByNumber = [ordered]@{}
    if (-not $NoGh) {
        foreach ($term in (Get-SearchTerms $finding.Text)) {
            if ([string]::IsNullOrWhiteSpace($term)) { continue }
            $ghOut = & gh issue list --repo dlrivada/Encina --state open --search $term --json number,title --limit 8 2>&1
            $ghExit = $LASTEXITCODE
            if ($ghExit -ne 0) { Write-Error "audit-draft-remediation: 'gh issue list' failed for $label (term '$term', exit $ghExit): $ghOut"; exit 1 }
            $parsed = @()
            try { $parsed = @($ghOut | ConvertFrom-Json) } catch { $parsed = @() }
            foreach ($c in $parsed) {
                $key = [string]$c.number
                if (-not $candidatesByNumber.Contains($key)) { $candidatesByNumber[$key] = $c }
            }
        }
    }
    $candidates = @($candidatesByNumber.Values | Select-Object -First 10)
    $candidateNumbers = @($candidates | ForEach-Object { [string]$_.number })
    $candidateLines = if ($candidates.Count -gt 0) { (($candidates | ForEach-Object { "#$($_.number): $($_.title)" }) -join '; ') } else { '(none found)' }
    $possiblyRelatedNote = $null

    # #1388 decision 2: the classifier also gets each candidate's own first 400 characters of body, not just
    # its title -- a title alone is often too generic to tell two same-area bugs apart (the failure mode audit
    # #16 hit). Fetched and cached per issue number ($ghIssueCache, declared once above the loop), so the
    # duplicate-evidence check below reuses the same fetch instead of asking `gh` again.
    if (-not $NoGh) {
        foreach ($c in $candidates) {
            [void](Get-CachedIssueTitleBody ([string]$c.number) $ghIssueCache $label)
        }
    }
    $candidateLinesForClassify = if ($candidates.Count -gt 0) {
        (($candidates | ForEach-Object {
            $cached = $ghIssueCache[[string]$_.number]
            $bodyExcerpt = if ($cached -and $cached.body) { ($cached.body.Substring(0, [Math]::Min(400, $cached.body.Length)) -replace '\s+', ' ').Trim() } else { '' }
            "#$($_.number): $($_.title) -- $bodyExcerpt"
        }) -join "`n")
    }
    else { '(none found)' }

    if ($DryRun) {
        $kind = Get-DryRunKind $finding.Stage $finding.Severity
        $route = $routing[$kind]
        $briefFile = Join-Path $dryRunDir "$($finding.Stage)-$($finding.Id)-brief.md"
        Set-Content -LiteralPath $briefFile -Encoding utf8 -Value (Build-DraftBrief $n $finding $kind $route $candidateLines)
        $lines.Add("- $label`: would route to $kind ($($route.Template))")
        "DRYRUN $label -> $kind ($($route.Template))"
        continue
    }

    # Step (a): classify (kind) and deduplicate (duplicate-of) in one short local-model call.
    $classifyBrief = Join-Path $remediationDir "_classify-brief-$n-$($finding.Stage)-$($finding.Id).md"
    Set-Content -LiteralPath $classifyBrief -Encoding utf8 -Value @"
Classify ONE finding from the SPEC-003 audit of closed GitHub issue #$n of the Encina .NET library. Reply with
EXACTLY one line and nothing else:

kind: bug|test|debt|docs; duplicate-of: #m|none; keywords: k1, k2, k3

- kind: "bug" for a code defect, "test" for missing tests or a coverage gap, "debt" for messy, duplicated,
  incomplete or slow code that is not itself a defect, "docs" for documentation drift.
- duplicate-of: the number of one of the candidate open issues below ONLY if it covers the exact same
  problem as this finding; otherwise "none".
- keywords: up to 3 short keywords for the finding.

Candidate open issues (from `gh issue list --search`, title -- first 400 characters of body):
$candidateLinesForClassify
"@
    $classifyOut = Join-Path $remediationDir "_classify-$n-$($finding.Stage)-$($finding.Id).md"
    Push-Location $mainRoot
    try {
        $classifyOutput = & dotnet run (Join-Path $mainRoot 'tools\ai\local-ai-ask.cs') -- --task "remediation-classify-$n-$($finding.Stage)-$($finding.Id)" --brief $classifyBrief --input $inputFile --out $classifyOut 2>&1
        $classifyExit = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    if ($classifyExit -ne 0 -or -not (Test-Path -LiteralPath $classifyOut)) {
        Write-Error "audit-draft-remediation: local model classification failed for $label (exit $classifyExit, output present: $(Test-Path -LiteralPath $classifyOut)): $classifyOutput"
        exit 1
    }
    $classifyText = (Get-Content -LiteralPath $classifyOut -Raw).Trim()
    # Anchored per-line, not '.*?' across the whole reply: a hedging, multi-sentence reply from the model
    # ("I think kind: bug ... but actually duplicate-of: #999 ... though kind: debt is more fitting") would
    # otherwise let a lazy '.*?' pair the FIRST 'kind:' mention with the FIRST 'duplicate-of:' mention instead
    # of the model's real, final answer -- silently mis-routing or silently dropping the finding as a false
    # duplicate. Only a line that starts with the exact "kind: ...; duplicate-of: ..." shape counts; when more
    # than one line qualifies, the LAST one is taken as the model's final answer and the ambiguity is a lesson.
    $classifyLinePattern = '^\s*kind:\s*(?<kind>bug|test|debt|docs)\s*;\s*duplicate-of:\s*(?<dup>#\d+|none)\b'
    $classifyMatches = @(($classifyText -split "`r?`n") | ForEach-Object { [regex]::Match($_, $classifyLinePattern, 'IgnoreCase') } | Where-Object { $_.Success })
    $duplicateOf = $null
    if ($classifyMatches.Count -eq 0) {
        $lessons.Add("$label`: local model classification did not match the expected 'kind: ...; duplicate-of: ...' line format ('$classifyText'); defaulted to debt, no duplicate assumed.")
        $kind = 'debt'
    }
    else {
        if ($classifyMatches.Count -gt 1) {
            $lessons.Add("$label`: local model classification returned $($classifyMatches.Count) lines matching the expected format instead of one; used the last one.")
        }
        $classifyMatch = $classifyMatches[-1]
        $kind = $classifyMatch.Groups['kind'].Value.ToLowerInvariant()
        if ($classifyMatch.Groups['dup'].Value -ne 'none') { $duplicateOf = $classifyMatch.Groups['dup'].Value.TrimStart('#') }
    }

    if ($duplicateOf -and -not (Test-ValidDuplicate $duplicateOf $candidateNumbers)) {
        $lessons.Add("$label`: local model named duplicate-of #$duplicateOf, which is not one of the candidates passed to it; drafting normally.")
        $duplicateOf = $null
    }

    # #1424 decision 2: duplicate-vs-new is decided deterministically against EVERY candidate the duplicate
    # search returned, not only the one the model happened to name -- audit #16 verification pass 4 found the
    # previous, model-named-only check unstable across re-runs for the very same finding and the very same open
    # candidates (16-code-4 vs #1170). When one or more candidates pass Test-DuplicateEvidence, the finding is a
    # duplicate of the lowest-numbered passing one, whatever the model answered; the model's own classification
    # then only decides the drafted template kind below. Skipped under -NoGh, where no candidate body was ever
    # fetched and no duplicate is ever accepted regardless (unchanged from before #1388).
    $evidenceDuplicate = $null
    if (-not $NoGh -and $candidateNumbers.Count -gt 0) {
        $evidenceCandidates = foreach ($num in $candidateNumbers) {
            $cached = Get-CachedIssueTitleBody $num $ghIssueCache $label
            [pscustomobject]@{ Number = $num; TitleAndBody = if ($cached) { "$($cached.title)`n$($cached.body)" } else { '' } }
        }
        $evidenceDuplicate = Find-DuplicateAmongCandidates $finding.Text $evidenceCandidates
    }

    if ($evidenceDuplicate) {
        if ($duplicateOf -and $duplicateOf -ne $evidenceDuplicate) {
            $lessons.Add("$label`: local model named duplicate-of #$duplicateOf, but the deterministic evidence check accepted #$evidenceDuplicate instead (the lowest-numbered candidate with full anchor evidence); used #$evidenceDuplicate.")
        }
        elseif (-not $duplicateOf) {
            $lessons.Add("$label`: local model did not name a duplicate, but the deterministic evidence check found #$evidenceDuplicate as a full-evidence duplicate; used it anyway.")
        }
        $duplicateOf = $evidenceDuplicate
    }
    elseif ($duplicateOf -and -not $NoGh) {
        # #1388 decision 1 (unchanged): the model-named candidate -- like every other candidate offered for
        # this finding -- failed the deterministic evidence check above (a cited file AND a cited symbol
        # actually appearing in the candidate's title/body); draft as new, with a note instead of a silent drop.
        $cachedCandidate = Get-CachedIssueTitleBody $duplicateOf $ghIssueCache $label
        $candidateText = if ($cachedCandidate) { "$($cachedCandidate.title)`n$($cachedCandidate.body)" } else { '' }
        $lessons.Add("$label`: local model named duplicate-of #$duplicateOf, but the evidence check found no matching file anchor and symbol anchor in #$duplicateOf's title/body; drafting as new instead.")
        # #1400 decision 1: a candidate that covers at least one of the finding's own file anchors (just
        # not every one -- Test-DuplicateEvidence's stricter bar) is worded as "partially related" rather than
        # the weaker "possibly related", which is reserved for a candidate with no file-anchor overlap at all.
        $possiblyRelatedNote = if (Test-PartialDuplicateEvidence $finding.Text $candidateText) {
            "- #$duplicateOf - partially related (it covers only part of this finding)"
        }
        else {
            "- #$duplicateOf - possibly related (the local model proposed it as a duplicate; the evidence check rejected it)"
        }
        $duplicateOf = $null
    }

    if ($duplicateOf) {
        $lines.Add("- $label`: duplicate of #$duplicateOf")
        "$label -> duplicate of #$duplicateOf"
        continue
    }

    # Step (b): draft, routed to the matching template.
    $route = $routing[$kind]
    # #1492 decision 1: computed once per finding, before either Repair-Draft call below, so a re-ask (which
    # re-writes the whole draft from a fresh model reply) still gets the same deterministic Type tick applied
    # to it the second time.
    $debtType = if ($route.Template -eq 'technical_debt.md') { Get-DeterministicDebtType $finding.Stage $kind $finding.Text } else { $null }
    $slug = New-Slug $finding.Text
    $outFile = Join-Path $remediationDir "$n-$($finding.Stage)-$($finding.Id)-$slug.md"
    $draftBrief = Join-Path $remediationDir "_brief-$n-$($finding.Stage)-$($finding.Id).md"
    Set-Content -LiteralPath $draftBrief -Encoding utf8 -Value (Build-DraftBrief $n $finding $kind $route $candidateLines)
    Push-Location $mainRoot
    try {
        $draftOutput = & dotnet run (Join-Path $mainRoot 'tools\ai\local-ai-ask.cs') -- --task "remediation-$n-$($finding.Stage)-$($finding.Id)" --brief $draftBrief --input $inputFile --out $outFile 2>&1
        $draftExit = $LASTEXITCODE
    }
    finally {
        Pop-Location
    }
    if ($draftExit -ne 0 -or -not (Test-Path -LiteralPath $outFile)) {
        Write-Error "audit-draft-remediation: local model drafting failed for $label (exit $draftExit, output present: $(Test-Path -LiteralPath $outFile)): $draftOutput"
        exit 1
    }

    # #1388 decisions 3/4: strip an outer code fence and re-ask ONCE, naming the offending lines, when the
    # template's own placeholder text survived into the draft. A draft that still has placeholders after the
    # re-ask is kept (for inspection) rather than deleted, marked in stages/remediation.md, and named in this
    # script's own non-zero exit at the very end -- the orchestrator sees it before audit-verifier does.
    $placeholders = Repair-Draft $outFile $label $route.Template $lessons $finding.Text $wt $debtType
    if ($placeholders.Count -gt 0) {
        $offendingLines = ($placeholders | ForEach-Object { "- $_" }) -join "`n"
        $reaskBrief = Join-Path $remediationDir "_brief-$n-$($finding.Stage)-$($finding.Id)-reask.md"
        Set-Content -LiteralPath $reaskBrief -Encoding utf8 -Value @"
$(Build-DraftBrief $n $finding $kind $route $candidateLines)

Your previous reply still contained the template's own placeholder text, unchanged, on these lines:
$offendingLines

Replace every one of them with real content drawn from the finding; never leave a bracketed example, '#___',
an 'Example.Package' row or a literal 'Test N: Description' row untouched.
"@
        Push-Location $mainRoot
        try {
            $reaskOutput = & dotnet run (Join-Path $mainRoot 'tools\ai\local-ai-ask.cs') -- --task "remediation-$n-$($finding.Stage)-$($finding.Id)-reask" --brief $reaskBrief --input $inputFile --out $outFile 2>&1
            $reaskExit = $LASTEXITCODE
        }
        finally {
            Pop-Location
        }
        if ($reaskExit -ne 0 -or -not (Test-Path -LiteralPath $outFile)) {
            Write-Error "audit-draft-remediation: local model re-ask drafting failed for $label (exit $reaskExit, output present: $(Test-Path -LiteralPath $outFile)): $reaskOutput"
            exit 1
        }
        $placeholders = Repair-Draft $outFile $label $route.Template $lessons $finding.Text $wt $debtType
    }

    # #1388 decision 1: a duplicate the evidence check rejected is drafted as new, but the candidate it
    # rejected is still worth a human glance -- append it to the draft's own Related Issues section.
    # #1400 (adversarial review finding 1): Add-RelatedIssuesLine understands both conventions
    # Limit-RelatedIssues does (the '## Related Issues' H2, and bug_report.md's own bold-bullet convention),
    # so the note lands inside the section it names -- and inside what Limit-RelatedIssues itself scans below
    # -- for every routed template, not only technical_debt.md/test_implementation.md.
    if ($possiblyRelatedNote) {
        $finalText = Get-Content -LiteralPath $outFile -Raw
        $inserted = Add-RelatedIssuesLine $finalText $possiblyRelatedNote
        if (-not $inserted.Found) {
            $lessons.Add("$label`: could not find a Related Issues section in $(Split-Path -Leaf $outFile) to append the rejected duplicate note; appended it at the end of the file instead.")
        }
        $updatedText = if ($inserted.Found) { $inserted.Text } else { $finalText.TrimEnd() + "`n$possiblyRelatedNote`n" }
        Set-Content -LiteralPath $outFile -Encoding utf8 -NoNewline -Value $updatedText
    }

    # #1400 decision 3, narrowed by #1424 decision 1 and widened by #1428 decision 2: sanitize the finished
    # draft's own Related Issues section -- never let the model's free text stand unverified, and never keep a
    # number just because it was offered as a search candidate (being a candidate is not evidence of a real
    # relation). $possiblyRelatedNote (just written above, if present) is itself a legitimate, already
    # anchor-checked reference, so its own line is passed as a script note the sanitizer must keep. For a draft
    # routed to bug_report.md, the sanitizer also scans the whole '## Additional Context' section (that
    # template's only place to put "related issues", per its own text), not only a labelled subsection inside
    # it -- audit #16's real 16-code-5 draft puts a plain 'Related Issues:' line there with no structural
    # marker of its own (#1428's own reproduction).
    $sanitizeScriptNotes = if ($possiblyRelatedNote) { @($possiblyRelatedNote) } else { @() }
    $isBugReportDraft = $route.Template -eq 'bug_report.md'
    $sanitized = Limit-RelatedIssues (Get-Content -LiteralPath $outFile -Raw) $n $finding.Text $sanitizeScriptNotes $isBugReportDraft
    if ($sanitized.Removed.Count -gt 0) {
        Set-Content -LiteralPath $outFile -Encoding utf8 -NoNewline -Value $sanitized.Text
        foreach ($removedNumber in $sanitized.Removed) {
            $lessons.Add("$label`: removed unverified related issue #$removedNumber from $(Split-Path -Leaf $outFile)'s Related Issues section (not in the finding, the candidates offered, or the script's own notes).")
        }
    }

    if ($placeholders.Count -gt 0) {
        $placeholderFailures.Add((Split-Path -Leaf $outFile))
        $lines.Add("- $label`: draft $(Split-Path -Leaf $outFile) (PLACEHOLDERS LEFT after one re-ask)")
        "$label -> $kind draft $(Split-Path -Leaf $outFile) -- PLACEHOLDERS LEFT after one re-ask"
    }
    elseif ($sanitized.Removed.Count -gt 0) {
        $removedList = ($sanitized.Removed | ForEach-Object { "#$_" }) -join ', '
        $lines.Add("- $label`: draft $(Split-Path -Leaf $outFile) (removed unverified related issue $removedList)")
        "$label -> $kind draft $(Split-Path -Leaf $outFile) -- removed unverified related issue $removedList"
    }
    else {
        $lines.Add("- $label`: draft $(Split-Path -Leaf $outFile)")
        "$label -> $kind draft $(Split-Path -Leaf $outFile)"
    }
}

if ($lines.Count -eq 0) {
    $lines.Add("No findings from the code, tests or docs stages for #$n; no remediation drafts were written.")
}

# #1492 decision 3: $lines/$lessons above only ever cover $findingsToProcess (one Add call per finding
# processed this run, in order); under -Only, merge them with every OTHER finding's own line/lesson taken
# verbatim from the previous stages/remediation.md ($existingFindingLines/$existingLessonLines, parsed before
# the cleanup above), so a finding this run never touched keeps its own line and lesson(s) byte-for-byte.
if ($onlyKeys) {
    $computedLines = @{}
    for ($idx = 0; $idx -lt $findingsToProcess.Count; $idx++) {
        $computedLines["$($findingsToProcess[$idx].Stage)|$($findingsToProcess[$idx].Id)"] = $lines[$idx]
    }
    $mergedLines = [System.Collections.Generic.List[string]]::new()
    foreach ($f in $allFindings) {
        $key = "$($f.Stage)|$($f.Id)"
        $mergedLine = if ($computedLines.ContainsKey($key)) { $computedLines[$key] } else { $existingFindingLines[$key] }
        $mergedLines.Add($mergedLine)
    }
    $lines = $mergedLines

    $keptOldLessons = [System.Collections.Generic.List[string]]::new()
    foreach ($oldLesson in $existingLessonLines) {
        $belongsToRegenerated = $false
        foreach ($key in $onlyKeys) {
            $keyParts = $key -split '\|', 2
            if ($oldLesson -match ('^' + [regex]::Escape($keyParts[0]) + '\s+' + [regex]::Escape($keyParts[1]) + '\b')) {
                $belongsToRegenerated = $true
                break
            }
        }
        if (-not $belongsToRegenerated) { $keptOldLessons.Add($oldLesson) }
    }
    $lessons.InsertRange(0, $keptOldLessons)
}

$out = [System.Collections.Generic.List[string]]::new()
$out.Add("Remediation for #$n`:")
$out.AddRange($lines)
$out.Add('')
$out.Add('## Lessons for the pipeline')
if ($lessons.Count -eq 0) { $out.Add('- none') } else { foreach ($lesson in $lessons) { $out.Add("- $lesson") } }

# -DryRun writes stages/remediation.md too (with "would route to" lines instead of real drafts): it is not a
# model call, so it is fully previewable, and a later non-dry run always regenerates it from the same stage
# inputs, so the preview is never mistaken for a finished stage without being overwritten for real.
Set-Content -LiteralPath (StageFile 'remediation') -Encoding utf8 -Value ($out -join "`n")
if ($DryRun) {
    "audit-draft-remediation: -DryRun complete for #$n ($($findingsToProcess.Count) finding(s) routed under artifacts\knowledge\remediation\_dryrun-$n\; stages\remediation.md previewed, no model calls made)"
}
else {
    "audit-draft-remediation: wrote stages\remediation.md for #$n ($($findingsToProcess.Count) finding(s))"
}

# #1388 decision 4: a draft that still has unfilled template placeholders after one re-ask is kept (for
# inspection) and its finding's line in stages/remediation.md is already marked, but the run itself must not
# report success -- the orchestrator needs to see this before audit-verifier does.
if ($placeholderFailures.Count -gt 0) {
    "audit-draft-remediation: $($placeholderFailures.Count) draft(s) still have unfilled template placeholders after one re-ask: $($placeholderFailures -join ', ')"
    exit 1
}
