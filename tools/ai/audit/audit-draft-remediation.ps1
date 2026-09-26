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

param(
    [switch]$DryRun,
    [switch]$NoGh
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')
. (Join-Path $PSScriptRoot '_remediation-checks.ps1')

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
  the same problem (a same-problem duplicate must never reach this step): $CandidateLines$priorityNote$docsNote
"@
}

# #1388 decisions 3/4: strips one outer code fence and reports remaining template placeholders for a draft
# already written to $Path, rewriting the file in place when the fence was stripped. Returns the (possibly
# empty) list of offending placeholder lines still in the draft after the fence strip. $LessonsList is the
# script's own $lessons list, passed explicitly rather than captured, since this function is called once per
# finding across the whole loop below.
function Repair-Draft([string]$Path, [string]$Label, [System.Collections.Generic.List[string]]$LessonsList) {
    $raw = Get-Content -LiteralPath $Path -Raw
    $defenced = Remove-OuterFence $raw
    if ($defenced -ne $raw) {
        Set-Content -LiteralPath $Path -Encoding utf8 -NoNewline -Value $defenced
        $LessonsList.Add("$Label`: draft $(Split-Path -Leaf $Path) was wrapped in an outer code fence; stripped it before writing.")
    }
    return (Find-TemplatePlaceholders $templatesDir $defenced)
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
foreach ($unknownFinding in ($allFindings | Where-Object { $_.Severity -eq 'Unknown' })) {
    $lessons.Add("$($unknownFinding.Stage) $($unknownFinding.Id): the stage's '## Findings' section did not match the expected numbered 'N. **Blocker/Major/Minor** -- ...' layout; treated as one Unknown-severity finding covering the whole section instead of being split further.")
}

foreach ($finding in $allFindings) {
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

    # #1388 decision 1: a duplicate-of that named a real candidate is still only honored when the finding's
    # own evidence (a cited file AND a cited symbol) actually appears in that candidate's title/body -- the
    # Qwen classifier picks the closest-SOUNDING candidate, not necessarily the same defect (3 of audit #16's
    # 4 duplicate claims were wrong even though all 4 named a real candidate). Skipped under -NoGh, where no
    # duplicate is ever accepted regardless (unchanged from before #1388).
    if ($duplicateOf -and -not $NoGh) {
        $cachedCandidate = Get-CachedIssueTitleBody $duplicateOf $ghIssueCache $label
        $candidateText = if ($cachedCandidate) { "$($cachedCandidate.title)`n$($cachedCandidate.body)" } else { '' }
        if (-not (Test-DuplicateEvidence $finding.Text $candidateText)) {
            $lessons.Add("$label`: local model named duplicate-of #$duplicateOf, but the evidence check found no matching file anchor and symbol anchor in #$duplicateOf's title/body; drafting as new instead.")
            $possiblyRelatedNote = "- #$duplicateOf - possibly related (the local model proposed it as a duplicate; the evidence check rejected it)"
            $duplicateOf = $null
        }
    }

    if ($duplicateOf) {
        $lines.Add("- $label`: duplicate of #$duplicateOf")
        "$label -> duplicate of #$duplicateOf"
        continue
    }

    # Step (b): draft, routed to the matching template.
    $route = $routing[$kind]
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
    $placeholders = Repair-Draft $outFile $label $lessons
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
        $placeholders = Repair-Draft $outFile $label $lessons
    }

    # #1388 decision 1: a duplicate the evidence check rejected is drafted as new, but the candidate it
    # rejected is still worth a human glance -- append it to the draft's own Related Issues section.
    if ($possiblyRelatedNote) {
        $finalText = Get-Content -LiteralPath $outFile -Raw
        $headerMatch = [regex]::Match($finalText, '(?m)^## Related Issues\s*$')
        if ($headerMatch.Success) {
            $insertAt = $headerMatch.Index + $headerMatch.Length
            $updatedText = $finalText.Substring(0, $insertAt) + "`n" + $possiblyRelatedNote + $finalText.Substring($insertAt)
        }
        else {
            $lessons.Add("$label`: could not find a '## Related Issues' header in $(Split-Path -Leaf $outFile) to append the rejected duplicate note; appended it at the end of the file instead.")
            $updatedText = $finalText.TrimEnd() + "`n$possiblyRelatedNote`n"
        }
        Set-Content -LiteralPath $outFile -Encoding utf8 -NoNewline -Value $updatedText
    }

    if ($placeholders.Count -gt 0) {
        $placeholderFailures.Add((Split-Path -Leaf $outFile))
        $lines.Add("- $label`: draft $(Split-Path -Leaf $outFile) (PLACEHOLDERS LEFT after one re-ask)")
        "$label -> $kind draft $(Split-Path -Leaf $outFile) -- PLACEHOLDERS LEFT after one re-ask"
    }
    else {
        $lines.Add("- $label`: draft $(Split-Path -Leaf $outFile)")
        "$label -> $kind draft $(Split-Path -Leaf $outFile)"
    }
}

if ($lines.Count -eq 0) {
    $lines.Add("No findings from the code, tests or docs stages for #$n; no remediation drafts were written.")
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
    "audit-draft-remediation: -DryRun complete for #$n ($($allFindings.Count) finding(s) routed under artifacts\knowledge\remediation\_dryrun-$n\; stages\remediation.md previewed, no model calls made)"
}
else {
    "audit-draft-remediation: wrote stages\remediation.md for #$n ($($allFindings.Count) finding(s))"
}

# #1388 decision 4: a draft that still has unfilled template placeholders after one re-ask is kept (for
# inspection) and its finding's line in stages/remediation.md is already marked, but the run itself must not
# report success -- the orchestrator needs to see this before audit-verifier does.
if ($placeholderFailures.Count -gt 0) {
    "audit-draft-remediation: $($placeholderFailures.Count) draft(s) still have unfilled template placeholders after one re-ask: $($placeholderFailures -join ', ')"
    exit 1
}
