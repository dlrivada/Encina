# tools/ai/audit/audit-draft-remediation.ps1 -Prepare | -Finalize (#1345, #1375; rewritten by #1572)
#
# The deterministic half of the SPEC-003 remediation stage. The drafting itself belongs to the
# remediation-drafter agent (.claude/agents/remediation-drafter.md, maintainer decision of 2026-10-02, #1572):
# the local model that drafted here before produced drafts that contradicted their own findings (wrong
# packages, invented figures, meta-text), and audit #18 failed verification on them. No mode of this script
# calls a model. The stage runs in three steps (.claude/skills/issue-audit/SKILL.md):
#
#   1. -Prepare (this script): splits the code, tests and docs stage artifacts' '## Findings' sections into
#      findings (Split-Findings, _audit-lib.ps1), groups same-location findings (#1491), searches open issues
#      for duplicates with deterministic evidence (#1424, Find-DuplicateAmongCandidates), applies -DuplicateOf
#      overrides (#1534), and writes, in the MAIN checkout's artifacts/knowledge/remediation/:
#        _input-<n>-<stage>-<id>.md   one per finding, the finding's own text;
#        _manifest-<n>.json           one per audit: per finding its group, duplicate or merge decision, the
#                                     partially/possibly related candidates, the routed template (or the kinds
#                                     the drafter may choose from), the draft file to write, the Reported-by
#                                     line and the exact stages/remediation.md line.
#      A full -Prepare first removes this audit's previous drafts, inputs and manifest (and its _dryrun-<n>
#      sandbox), so a re-run never mixes outputs of two runs; every gh call happens BEFORE that cleanup, so a
#      failed search leaves the previous outputs untouched (#1548).
#   2. The orchestrator spawns remediation-drafter, which writes each draft and stages/remediation.md from the
#      manifest.
#   3. -Finalize (this script): applies the deterministic sanitizers to every draft the manifest names
#      (Remove-OuterFence, Set-BugEnvironment, Set-DebtType, the missing "partially related" lines,
#      Limit-RelatedIssues, Add-ReportedByLine), then checks each draft's header block, template headers and
#      placeholders, and that stages/remediation.md carries the manifest's line for every finding. It prints
#      every problem and exits 1 when any remains; the orchestrator re-spawns the drafter with that output.
#
# -Only "<stage> <n>" (repeatable, -Prepare only; #1492 decision 3, widened by #1491 decision 4): prepares only
# the named finding's location group. Every other finding keeps its draft, its input and its stages/remediation.md
# line untouched: the manifest carries that line verbatim with "regenerate": false. Requires an existing
# stages/remediation.md with a line for every other finding.
#
# -DuplicateOf "<stage> <n>=<issue>" (repeatable, -Prepare only; #1534): records the named finding's whole
# group as a duplicate of the given OPEN issue by explicit, logged override (" (manual override)" on its line,
# a lesson in the manifest). A malformed entry or an unknown key fails before any file is touched; the issue
# must be OPEN (checked with gh unless -NoGh). An override's group is always prepared, even without -Only.
#
# -DryRun (#1540): both modes work only inside artifacts/knowledge/remediation/_dryrun-<n>/, a self-contained
# sandbox: -Prepare -DryRun writes the inputs, the manifest and the draft paths there (the stage file preview
# is _dryrun-<n>/remediation.md), and -Finalize -DryRun reads that sandbox manifest and refuses any path outside
# the sandbox. A dry run never deletes, writes or overwrites a live draft, input, manifest or
# stages/remediation.md.
#
# -NoGh skips every gh call (the label check, the -DuplicateOf OPEN check and the duplicate search): no
# duplicate is ever found under it. Meant for offline runs and Test-Hooks.ps1.
#
# Every gh call goes through Invoke-GhWithRetry (_remediation-checks.ps1, #1548): transient failures (TLS
# handshake timeout, connection reset, HTTP 5xx, rate limit) are retried 3 times after 5, 15 and 45 seconds;
# a 4xx is not retried.

param(
    [switch]$Prepare,
    [switch]$Finalize,
    [switch]$DryRun,
    [switch]$NoGh,
    [string[]]$Only,
    [string[]]$DuplicateOf
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')
. (Join-Path $PSScriptRoot '_remediation-checks.ps1')

function Stop-Remediation([string]$Message) {
    [Console]::Error.WriteLine("audit-draft-remediation: $Message")
    exit 1
}

if ($Prepare -eq $Finalize) { Stop-Remediation 'pass exactly one of -Prepare or -Finalize.' }
if ($Finalize -and (($Only -and $Only.Count -gt 0) -or ($DuplicateOf -and $DuplicateOf.Count -gt 0))) {
    Stop-Remediation '-Only and -DuplicateOf apply to -Prepare only.'
}

# Fail-fast argument parsing, before any file or gh call (#1492 decision 3, #1534).
$onlyKeys = $null
if ($Only -and $Only.Count -gt 0) {
    $onlyKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($spec in $Only) {
        $specParts = @($spec -split '\s+' | Where-Object { $_ -ne '' })
        if ($specParts.Count -ne 2) { Stop-Remediation "-Only value '$spec' must be '<stage> <n>' (e.g. 'code 3')." }
        [void]$onlyKeys.Add("$($specParts[0])|$($specParts[1])")
    }
}
$duplicateOfEntries = $null
if ($DuplicateOf -and $DuplicateOf.Count -gt 0) {
    $duplicateOfEntries = [ordered]@{}
    foreach ($spec in $DuplicateOf) {
        $dupMatch = [regex]::Match($spec, '^(?<stage>\S+)\s+(?<id>\d+)=(?<issue>\d+)$')
        if (-not $dupMatch.Success) { Stop-Remediation "-DuplicateOf value '$spec' must be '<stage> <n>=<issue>' (e.g. 'docs 12=1177')." }
        $dupKey = "$($dupMatch.Groups['stage'].Value)|$($dupMatch.Groups['id'].Value)"
        $dupIssue = $dupMatch.Groups['issue'].Value
        if ($duplicateOfEntries.Contains($dupKey) -and $duplicateOfEntries[$dupKey] -ne $dupIssue) {
            Stop-Remediation "-DuplicateOf has conflicting entries for '$($dupKey -replace '\|', ' ')' (#$($duplicateOfEntries[$dupKey]) and #$dupIssue)."
        }
        $duplicateOfEntries[$dupKey] = $dupIssue
    }
}

$mainRoot = Get-MainRoot $PSScriptRoot
$audit = Get-CurrentAudit $mainRoot
if ($null -eq $audit) { Stop-Remediation 'no open audit (artifacts/knowledge/current-audit.json not found). Run audit-next.ps1 first.' }

$wt = [string]$audit.worktree
$n = [string]$audit.issue
$stagesDir = Get-StagesDir $wt
$pipeline = Get-Pipeline (Join-Path $wt 'tools\ai\audit')

function Get-StageFile([string]$Name) {
    $def = $pipeline.stages | Where-Object { $_.stage -eq $Name }
    if ($null -eq $def) { Stop-Remediation "pipeline.json has no '$Name' stage." }
    return Join-Path $stagesDir $def.artifact
}

$remediationDir = Join-Path $mainRoot 'artifacts\knowledge\remediation'
$sandboxDir = Join-Path $remediationDir "_dryrun-$n"
$outDir = if ($DryRun) { $sandboxDir } else { $remediationDir }
$stageOut = if ($DryRun) { Join-Path $sandboxDir 'remediation.md' } else { Get-StageFile 'remediation' }
$manifestPath = Join-Path $outDir "_manifest-$n.json"
$lessonsHeading = '## Lessons for the pipeline'

function Get-GhResult([string[]]$Arguments, [string]$What) {
    $r = Invoke-GhWithRetry -Arguments $Arguments
    if (-not $r.Success) { Stop-Remediation "'gh $($Arguments -join ' ')' failed for $What after $($r.Attempts) attempt(s) (exit $($r.ExitCode)): $($r.Output)" }
    return $r.Stdout
}

# ------------------------------------------------------------------------------------------------------------
# -Finalize
# ------------------------------------------------------------------------------------------------------------
if ($Finalize) {
    if (-not (Test-Path -LiteralPath $manifestPath)) { Stop-Remediation "no manifest at $manifestPath; run -Prepare$(if ($DryRun) { ' -DryRun' }) first." }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    $problems = [System.Collections.Generic.List[string]]::new()
    $notes = [System.Collections.Generic.List[string]]::new()

    # #1540: a dry-run Finalize touches nothing outside its own sandbox, whatever the manifest says.
    function Test-InScope([string]$Path) {
        if (-not $DryRun -or [string]::IsNullOrEmpty($Path)) { return $true }
        return [IO.Path]::GetFullPath($Path).StartsWith(([IO.Path]::GetFullPath($sandboxDir).TrimEnd('\') + '\'), [StringComparison]::OrdinalIgnoreCase)
    }

    $findingsByKey = @{}
    foreach ($f in @($manifest.findings)) { $findingsByKey[[string]$f.key] = $f }

    foreach ($f in @($manifest.findings)) {
        $label = [string]$f.label
        if (-not $f.regenerate) {
            if ($f.draftFile -and -not (Test-Path -LiteralPath $f.draftFile)) { $problems.Add("$label`: its existing draft $(Split-Path -Leaf $f.draftFile) is missing (an -Only run keeps it untouched, but it must exist).") }
            continue
        }
        if (-not $f.draftFile) {
            $stale = @(Get-ChildItem -LiteralPath $outDir -Filter "$n-$($f.stage)-$($f.id)-*.md" -File -ErrorAction SilentlyContinue)
            foreach ($s in $stale) { $problems.Add("$label`: $(Split-Path -Leaf $s.FullName) exists, but the manifest records this finding as '$($f.remediationLine)'; no draft may exist for it.") }
            continue
        }
        $draftPath = [string]$f.draftFile
        $draftName = Split-Path -Leaf $draftPath
        if (-not (Test-InScope $draftPath)) { $problems.Add("$label`: $draftPath is outside the dry-run sandbox $sandboxDir; not touched."); continue }
        if (-not (Test-Path -LiteralPath $draftPath)) { $problems.Add("$label`: missing draft $draftName (remediation-drafter must write it)."); continue }

        $memberTexts = [System.Collections.Generic.List[string]]::new()
        $primaryText = ''
        foreach ($memberKey in @($f.groupMembers)) {
            $member = $findingsByKey[[string]$memberKey]
            if ($null -eq $member -or -not $member.inputFile -or -not (Test-Path -LiteralPath $member.inputFile)) { $problems.Add("$label`: the input file of group member '$memberKey' is missing; re-run -Prepare."); continue }
            $memberText = Get-Content -LiteralPath $member.inputFile -Raw
            $memberTexts.Add($memberText)
            # The group's own primary (this entry), never simply the first member: groupMembers is in group order.
            if ([string]$memberKey -eq [string]$f.key) { $primaryText = $memberText }
        }
        $allTexts = $memberTexts -join "`n"

        $raw = Get-Content -LiteralPath $draftPath -Raw
        $text = Remove-OuterFence $raw
        if ($text -ne $raw) { $notes.Add("$label`: stripped an outer code fence from $draftName.") }

        $header = Get-DraftHeader $text
        $kind = $null
        $allowedKinds = @($f.kindOptions | ForEach-Object { [string]$_ })
        if (-not $header.Found) { $problems.Add("$label`: $draftName has no header comment block (<!-- title/labels/milestone/kind -->).") }
        elseif ([string]::IsNullOrWhiteSpace($header.Kind)) { $problems.Add("$label`: $draftName's header has no 'kind:' line (one of: $($allowedKinds -join ', ')).") }
        elseif ($allowedKinds -notcontains $header.Kind.Trim().ToLowerInvariant()) { $problems.Add("$label`: $draftName's header says 'kind: $($header.Kind)', but the manifest allows only: $($allowedKinds -join ', ').") }
        else { $kind = $header.Kind.Trim().ToLowerInvariant() }

        $route = if ($kind) { $manifest.routes.$kind } else { $null }
        if ($route) {
            $expectedLabels = (@($route.labels) -join ', ')
            if ([string]::IsNullOrWhiteSpace($header.Title) -or -not $header.Title.StartsWith("$($route.prefix) ") -or $header.Title.Trim().Length -le $route.prefix.Length) { $problems.Add("$label`: $draftName's title must start with '$($route.prefix) ' followed by a specific title (found: '$($header.Title)').") }
            if ($header.Labels -ne $expectedLabels) { $problems.Add("$label`: $draftName's labels must be '$expectedLabels' (found: '$($header.Labels)').") }
            if ($header.Milestone -ne [string]$route.milestone) { $problems.Add("$label`: $draftName's milestone must be '$($route.milestone)' (found: '$($header.Milestone)').") }

            if ($route.template -like '*bug_report.md') { $text = Set-BugEnvironment $text $wt $primaryText }
            elseif ($route.template -like '*technical_debt.md') { $text = Set-DebtType $text (Get-DeterministicDebtType ([string]$f.stage) $kind $primaryText) }
        }

        $partialLines = @($f.partiallyRelated | Where-Object { $_ } | ForEach-Object { [string]$_ })
        foreach ($partial in $partialLines) {
            $partialNumber = [regex]::Match($partial, '#(\d+)').Groups[1].Value
            if ($text -match "#$partialNumber(?!\d)") { continue }
            $inserted = Add-RelatedIssuesLine $text $partial
            if ($inserted.Found) { $text = $inserted.Text; $notes.Add("$label`: added the manifest's partially related line for #$partialNumber to $draftName.") }
            else { $problems.Add("$label`: $draftName has no Related Issues section to carry the manifest's line '$partial'.") }
        }

        $sanitized = Limit-RelatedIssues $text $n $allTexts $partialLines
        $text = $sanitized.Text
        foreach ($removedNumber in @($sanitized.Removed)) { $notes.Add("$label`: removed unverified issue reference #$removedNumber from $draftName (not in the finding, not #$n, not a manifest partially related candidate).") }

        if ($f.reportedByLine) {
            $withReportedBy = Add-ReportedByLine $text ([string]$f.reportedByLine)
            if ($withReportedBy.Found) { $text = $withReportedBy.Text }
            else { $problems.Add("$label`: $draftName has no '## Description' header for the line '$($f.reportedByLine)'.") }
        }

        if ($text -ne $raw) { Set-Content -LiteralPath $draftPath -Encoding utf8 -NoNewline -Value $text }

        if ($route) {
            $templateText = Get-Content -LiteralPath $route.template -Raw
            foreach ($placeholder in (Find-TemplatePlaceholders $templateText $text)) { $problems.Add("$label`: $draftName still has template placeholder text: $placeholder") }
            foreach ($missingHeader in (Find-MissingTemplateHeaders $templateText $text)) { $problems.Add("$label`: $draftName is missing the template header '$missingHeader' (or has it out of order).") }
        }
    }

    if (-not (Test-InScope $stageOut)) { $problems.Add("$stageOut is outside the dry-run sandbox; not checked.") }
    elseif (-not (Test-Path -LiteralPath $stageOut)) { $problems.Add("$(Split-Path -Leaf $stageOut) is missing; remediation-drafter must write it at $stageOut.") }
    else {
        $stageLines = @(Get-Content -LiteralPath $stageOut)
        $firstLine = @($stageLines | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1)
        if ($firstLine.Count -eq 0 -or $firstLine[0] -ne [string]$manifest.stageHeader) { $problems.Add("$(Split-Path -Leaf $stageOut) must start with the line '$($manifest.stageHeader)'.") }
        foreach ($f in @($manifest.findings)) {
            if ($stageLines -notcontains [string]$f.remediationLine) { $problems.Add("$(Split-Path -Leaf $stageOut) lacks the manifest's line for $($f.label): '$($f.remediationLine)'.") }
        }
        if (@($manifest.findings).Count -eq 0 -and $stageLines -notcontains [string]$manifest.emptyLine) { $problems.Add("$(Split-Path -Leaf $stageOut) lacks the line '$($manifest.emptyLine)'.") }
        if ($stageLines -notcontains $lessonsHeading) { $problems.Add("$(Split-Path -Leaf $stageOut) lacks the '$lessonsHeading' section.") }
        # The manifest's lessons (kept ones from an -Only run, and this run's, e.g. a #1534 manual-override log)
        # must reach the stage file verbatim: audit-verifier and the lessons history read them there.
        foreach ($lesson in @(@($manifest.keptLessons) + @($manifest.lessons) | Where-Object { $_ })) {
            if ($stageLines -notcontains "- $lesson") { $problems.Add("$(Split-Path -Leaf $stageOut) lacks the manifest's lesson '- $lesson' under '$lessonsHeading'.") }
        }
    }

    foreach ($note in $notes) { "note: $note" }
    if ($problems.Count -gt 0) {
        foreach ($problem in $problems) { "problem: $problem" }
        "audit-draft-remediation: -Finalize found $($problems.Count) problem(s) for #$n; re-spawn remediation-drafter with this output, then run -Finalize again."
        exit 1
    }
    "audit-draft-remediation: -Finalize clean for #$n ($(@($manifest.findings | Where-Object { $_.regenerate -and $_.draftFile }).Count) draft(s) checked); commit the stage with audit-commit-stage.ps1 -Stage remediation."
    exit 0
}

# ------------------------------------------------------------------------------------------------------------
# -Prepare
# ------------------------------------------------------------------------------------------------------------
$docsFile = Get-StageFile 'docs'
if (-not (Test-Path -LiteralPath $docsFile)) { Stop-Remediation "stages\$(Split-Path -Leaf $docsFile) is missing; run the docs stage before remediation." }

# Routing table (#1375 decision 3): kind -> template, title prefix, labels, milestone. The milestone's em dash is
# built from its code point so this file stays ASCII-only while matching the real GitHub milestone title.
$templatesDir = Join-Path $wt '.github\ISSUE_TEMPLATE'
$hardeningMilestone = "v0.14.0 $([char]0x2014) Hardening"
$routes = [ordered]@{
    bug  = [ordered]@{ template = (Join-Path $templatesDir 'bug_report.md'); prefix = '[BUG]'; labels = @('bug'); milestone = $hardeningMilestone }
    test = [ordered]@{ template = (Join-Path $templatesDir 'test_implementation.md'); prefix = '[TEST]'; labels = @('area-testing'); milestone = '' }
    debt = [ordered]@{ template = (Join-Path $templatesDir 'technical_debt.md'); prefix = '[DEBT]'; labels = @('technical-debt'); milestone = '' }
    docs = [ordered]@{ template = (Join-Path $templatesDir 'technical_debt.md'); prefix = '[DEBT]'; labels = @('technical-debt', 'area-documentation'); milestone = '' }
}
foreach ($kindName in $routes.Keys) {
    if (-not (Test-Path -LiteralPath $routes[$kindName].template)) { Stop-Remediation "issue template '$($routes[$kindName].template)' not found." }
}
if (-not $NoGh) {
    $existingLabels = @((Get-GhResult @('label', 'list', '--repo', 'dlrivada/Encina', '--limit', '400', '--json', 'name', '--jq', '.[].name') 'the label check') -split "`r?`n")
    if ($existingLabels -notcontains 'area-documentation') { $routes.docs.labels = @('technical-debt') }
}

# A '## Findings' header literally present in the file: only an explicit "- none" body means zero findings; a
# missing header is always an error (#1375 CodeRabbit review).
function Test-FindingsHeaderPresent([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    return [regex]::IsMatch((Get-Content -LiteralPath $Path -Raw), '(?m)^##\s*Findings\s*$')
}

$allFindings = [System.Collections.Generic.List[pscustomobject]]::new()
foreach ($stageName in 'code', 'tests', 'docs') {
    $stageFile = Get-StageFile $stageName
    if (-not (Test-FindingsHeaderPresent $stageFile)) { Stop-Remediation "stages\$(Split-Path -Leaf $stageFile) has no '## Findings' header; the stage must write one (with '- none' when there are no findings)." }
    try { foreach ($f in (Split-Findings $stageName (Get-StageSection $stageFile 'Findings'))) { $allFindings.Add($f) } }
    catch { Stop-Remediation $_.Exception.Message }
}
$allFindingKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($f in $allFindings) { [void]$allFindingKeys.Add("$($f.Stage)|$($f.Id)") }

$requestedKeys = @()
if ($duplicateOfEntries) { $requestedKeys += @($duplicateOfEntries.Keys | ForEach-Object { [pscustomobject]@{ Option = '-DuplicateOf'; Key = $_ } }) }
if ($onlyKeys) { $requestedKeys += @($onlyKeys | ForEach-Object { [pscustomobject]@{ Option = '-Only'; Key = $_ } }) }
foreach ($requested in $requestedKeys) {
    if (-not $allFindingKeys.Contains($requested.Key)) { Stop-Remediation "$($requested.Option) '$($requested.Key -replace '\|', ' ')' does not match a finding currently parsed from the code, tests or docs stage artifacts." }
}

# #1534 decision 3: every -DuplicateOf target must be a real, OPEN issue.
if ($duplicateOfEntries -and -not $NoGh) {
    foreach ($dupIssueNumber in @($duplicateOfEntries.Values | Select-Object -Unique)) {
        $stateJson = Get-GhResult @('issue', 'view', $dupIssueNumber, '--repo', 'dlrivada/Encina', '--json', 'state') "-DuplicateOf #$dupIssueNumber"
        $state = $null
        try { $state = ($stateJson | ConvertFrom-Json).state } catch { $state = $null }
        if ($state -ne 'OPEN') { Stop-Remediation "-DuplicateOf names #$dupIssueNumber, but it is not an OPEN issue (state: $(if ($state) { $state } else { 'unknown' }))." }
    }
}

# #1491: groups by leading location anchor, computed from the FULL finding set so an -Only run picks the same
# group and primary a full run would.
$groups = Group-FindingsByLocation $allFindings
$groupIndexByKey = @{}
for ($gi = 0; $gi -lt $groups.Count; $gi++) {
    $groups[$gi] | Add-Member -NotePropertyName Primary -NotePropertyValue (Get-GroupPrimary $groups[$gi].Members)
    foreach ($m in $groups[$gi].Members) { $groupIndexByKey["$($m.Stage)|$($m.Id)"] = $gi }
}

# #1534 decision 4: an override names a GROUP; two overrides on one group must agree.
$groupDuplicateIssue = @{}
if ($duplicateOfEntries) {
    foreach ($dupKey in $duplicateOfEntries.Keys) {
        $dupGroupIdx = $groupIndexByKey[$dupKey]
        if ($groupDuplicateIssue.ContainsKey($dupGroupIdx) -and $groupDuplicateIssue[$dupGroupIdx] -ne $duplicateOfEntries[$dupKey]) {
            Stop-Remediation "-DuplicateOf '$($dupKey -replace '\|', ' ')' names #$($duplicateOfEntries[$dupKey]), but another finding in the same location group already names #$($groupDuplicateIssue[$dupGroupIdx])."
        }
        $groupDuplicateIssue[$dupGroupIdx] = $duplicateOfEntries[$dupKey]
    }
}

# #1492 decision 3: -Only keeps every other finding's existing line and lessons, read from the stage file this
# run's drafter will rewrite (the sandbox preview under -DryRun).
$existingFindingLines = @{}
$keptLessons = [System.Collections.Generic.List[string]]::new()
$existingLessons = [System.Collections.Generic.List[string]]::new()
if ($onlyKeys) {
    if (-not (Test-Path -LiteralPath $stageOut)) { Stop-Remediation "-Only requires an existing $stageOut to update; run a full -Prepare (no -Only) and the drafter first." }
    $inLessons = $false
    foreach ($rawLine in (Get-Content -LiteralPath $stageOut)) {
        if ($rawLine -eq $lessonsHeading) { $inLessons = $true; continue }
        if ($inLessons) {
            if ($rawLine -match '^-\s+(?<text>.+)$' -and $Matches['text'] -ne 'none') { $existingLessons.Add($Matches['text']) }
            continue
        }
        $lineMatch = [regex]::Match($rawLine, '^-\s+(?<stage>\S+)\s+(?<id>\S+)\s+\(')
        if ($lineMatch.Success) { $existingFindingLines["$($lineMatch.Groups['stage'].Value)|$($lineMatch.Groups['id'].Value)"] = $rawLine }
    }
}

# Touched groups: every group for a full run; for -Only, the groups of the named findings plus every
# -DuplicateOf override's group (an override always takes effect on its own, #1534).
$touchedGroupIndexes = [System.Collections.Generic.List[int]]::new()
if ($onlyKeys) {
    $seenGroups = [System.Collections.Generic.HashSet[int]]::new()
    foreach ($f in $allFindings) {
        $key = "$($f.Stage)|$($f.Id)"
        if ($onlyKeys.Contains($key) -and $seenGroups.Add($groupIndexByKey[$key])) { $touchedGroupIndexes.Add($groupIndexByKey[$key]) }
    }
    foreach ($dupGroupIdx in $groupDuplicateIssue.Keys) { if ($seenGroups.Add($dupGroupIdx)) { $touchedGroupIndexes.Add($dupGroupIdx) } }
}
else { for ($gi = 0; $gi -lt $groups.Count; $gi++) { $touchedGroupIndexes.Add($gi) } }

$touchedMemberKeys = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
foreach ($gi in $touchedGroupIndexes) { foreach ($m in $groups[$gi].Members) { [void]$touchedMemberKeys.Add("$($m.Stage)|$($m.Id)") } }
if ($onlyKeys) {
    foreach ($f in $allFindings) {
        $key = "$($f.Stage)|$($f.Id)"
        if (-not $touchedMemberKeys.Contains($key) -and -not $existingFindingLines.ContainsKey($key)) {
            Stop-Remediation "-Only cannot find an existing line for '$($f.Stage) $($f.Id)' in $(Split-Path -Leaf $stageOut); run a full -Prepare (no -Only) and the drafter first."
        }
    }
    foreach ($oldLesson in $existingLessons) {
        $belongs = $false
        foreach ($touchedKey in $touchedMemberKeys) {
            $keyParts = $touchedKey -split '\|', 2
            if ($oldLesson -match ('^' + [regex]::Escape($keyParts[0]) + '\s+' + [regex]::Escape($keyParts[1]) + '\b')) { $belongs = $true; break }
        }
        if (-not $belongs) { $keptLessons.Add($oldLesson) }
    }
}

function Get-FindingLabel($Finding) { "$($Finding.Stage) $($Finding.Id) ($($Finding.Severity))" }
function Get-InputPath($Finding) { Join-Path $outDir "_input-$n-$($Finding.Stage)-$($Finding.Id).md" }

# Every gh call (the duplicate search) runs here, before anything on disk is touched (#1548): a failure stops
# the run with the previous outputs intact.
$lessons = [System.Collections.Generic.List[string]]::new()
foreach ($f in $allFindings) {
    if ($touchedMemberKeys.Contains("$($f.Stage)|$($f.Id)") -and $f.Severity -eq 'Unknown') {
        $lessons.Add("$($f.Stage) $($f.Id): the stage's '## Findings' section did not match the expected numbered 'N. **Blocker/Major/Minor** -- ...' layout; treated as one Unknown-severity finding covering the whole section instead of being split further.")
    }
}
if ($duplicateOfEntries) {
    foreach ($dupKey in $duplicateOfEntries.Keys) { $lessons.Add("$($dupKey -replace '\|', ' '): recorded as duplicate of #$($duplicateOfEntries[$dupKey]) by manual override") }
}

$entriesByKey = @{}
$ghIssueCache = @{}
foreach ($gi in $touchedGroupIndexes) {
    $group = $groups[$gi]
    $primary = $group.Primary
    $primaryKey = "$($primary.Stage)|$($primary.Id)"
    $primaryLabel = Get-FindingLabel $primary
    $memberKeys = @($group.Members | ForEach-Object { "$($_.Stage) $($_.Id)" })

    $duplicateOfIssue = $null
    $duplicateSource = $null
    $partiallyRelated = [System.Collections.Generic.List[string]]::new()
    $possiblyRelated = [System.Collections.Generic.List[string]]::new()
    if ($groupDuplicateIssue.ContainsKey($gi)) {
        $duplicateOfIssue = $groupDuplicateIssue[$gi]
        $duplicateSource = 'manual override'
    }
    elseif (-not $NoGh) {
        # One search per term (GitHub ANDs a space-separated query), merged by number, capped at 10.
        $candidatesByNumber = [ordered]@{}
        foreach ($term in (Get-SearchTerms $primary.Text)) {
            $listJson = Get-GhResult @('issue', 'list', '--repo', 'dlrivada/Encina', '--state', 'open', '--search', $term, '--json', 'number,title', '--limit', '8') "$primaryLabel (term '$term')"
            $parsed = @()
            try { $parsed = @($listJson | ConvertFrom-Json) } catch { $parsed = @() }
            foreach ($c in $parsed) { if (-not $candidatesByNumber.Contains([string]$c.number)) { $candidatesByNumber[[string]$c.number] = $c } }
        }
        $evidenceCandidates = foreach ($c in @($candidatesByNumber.Values | Select-Object -First 10)) {
            $number = [string]$c.number
            if (-not $ghIssueCache.ContainsKey($number)) {
                $viewJson = Get-GhResult @('issue', 'view', $number, '--repo', 'dlrivada/Encina', '--json', 'title,body') "$primaryLabel (candidate #$number)"
                $ghIssueCache[$number] = try { $viewJson | ConvertFrom-Json } catch { $null }
            }
            $cached = $ghIssueCache[$number]
            [pscustomobject]@{ Number = $number; Title = [string]$c.title; TitleAndBody = if ($cached) { "$($cached.title)`n$($cached.body)" } else { '' } }
        }
        $evidenceCandidates = @($evidenceCandidates)
        $duplicateOfIssue = Find-DuplicateAmongCandidates $primary.Text $evidenceCandidates
        if ($duplicateOfIssue) { $duplicateSource = 'evidence' }
        else {
            foreach ($c in $evidenceCandidates) {
                if ([string]$c.Number -eq $n) { continue }
                if (Test-PartialDuplicateEvidence $primary.Text $c.TitleAndBody) { $partiallyRelated.Add("- #$($c.Number) - partially related (it covers only part of this finding)") }
                else { $possiblyRelated.Add("#$($c.Number): $($c.Title)") }
            }
        }
    }

    $kind = $null
    $kindOptions = @()
    $draftFile = $null
    $primaryLine = $null
    if ($duplicateOfIssue) {
        $primaryLine = if ($duplicateSource -eq 'manual override') { "- $primaryLabel`: duplicate of #$duplicateOfIssue (manual override)" } else { "- $primaryLabel`: duplicate of #$duplicateOfIssue" }
    }
    else {
        # Deterministic where the stage decides it; a code-stage finding is a bug, debt or documentation drift
        # only the drafter can tell apart after reading the code.
        switch ($primary.Stage) {
            'tests' { $kind = 'test'; $kindOptions = @('test') }
            'docs' { $kind = 'docs'; $kindOptions = @('docs') }
            default { $kind = 'drafter-decides'; $kindOptions = @('bug', 'debt', 'docs') }
        }
        $draftFile = Join-Path $outDir "$n-$($primary.Stage)-$($primary.Id)-$(New-Slug $primary.Text).md"
        $primaryLine = "- $primaryLabel`: draft $(Split-Path -Leaf $draftFile)"
    }

    foreach ($member in $group.Members) {
        $memberKey = "$($member.Stage)|$($member.Id)"
        $isPrimary = $memberKey -eq $primaryKey
        $memberLabel = Get-FindingLabel $member
        $line = if ($isPrimary) { $primaryLine }
        elseif ($duplicateSource -eq 'manual override') { "- $memberLabel`: duplicate of #$duplicateOfIssue (manual override)" }
        else { "- $memberLabel`: merged into $($primary.Stage) $($primary.Id) (same location)" }
        $fixedRoute = if ($isPrimary -and $kind -and $kind -ne 'drafter-decides') { $routes[$kind] } else { $null }
        $entriesByKey[$memberKey] = [ordered]@{
            key              = "$($member.Stage) $($member.Id)"
            stage            = $member.Stage
            id               = $member.Id
            severity         = $member.Severity
            label            = $memberLabel
            regenerate       = $true
            inputFile        = (Get-InputPath $member)
            groupPrimary     = "$($primary.Stage) $($primary.Id)"
            groupMembers     = $memberKeys
            mergedInto       = if ($isPrimary -or $duplicateSource -eq 'manual override') { $null } else { "$($primary.Stage) $($primary.Id)" }
            duplicateOf      = $duplicateOfIssue
            duplicateSource  = $duplicateSource
            partiallyRelated = if ($isPrimary) { @($partiallyRelated) } else { @() }
            possiblyRelated  = if ($isPrimary) { @($possiblyRelated) } else { @() }
            kind             = if ($isPrimary) { $kind } else { $null }
            kindOptions      = if ($isPrimary) { $kindOptions } else { @() }
            template         = if ($fixedRoute) { $fixedRoute.template } else { $null }
            prefix           = if ($fixedRoute) { $fixedRoute.prefix } else { $null }
            labels           = if ($fixedRoute) { @($fixedRoute.labels) } else { $null }
            milestone        = if ($fixedRoute) { $fixedRoute.milestone } else { $null }
            draftFile        = if ($isPrimary) { $draftFile } else { $null }
            reportedByLine   = if ($isPrimary -and $draftFile -and $group.Members.Count -gt 1) { 'Reported by: ' + ($memberKeys -join ', ') + '.' } else { $null }
            remediationLine  = $line
        }
        "$memberLabel -> $($line -replace '^-\s+[^:]+:\s*', '')$(if ($isPrimary -and $kind) { " (kind: $kind)" })"
    }
}

# Untouched findings of an -Only run: their existing line, draft and input, verbatim (#1492 decision 3).
$findingEntries = foreach ($f in $allFindings) {
    $key = "$($f.Stage)|$($f.Id)"
    if ($entriesByKey.ContainsKey($key)) { $entriesByKey[$key]; continue }
    $group = $groups[$groupIndexByKey[$key]]
    $existingDraft = @(Get-ChildItem -LiteralPath $outDir -Filter "$n-$($f.Stage)-$($f.Id)-*.md" -File -ErrorAction SilentlyContinue | Select-Object -First 1)
    $existingInput = Get-InputPath $f
    [ordered]@{
        key              = "$($f.Stage) $($f.Id)"
        stage            = $f.Stage
        id               = $f.Id
        severity         = $f.Severity
        label            = Get-FindingLabel $f
        regenerate       = $false
        inputFile        = if (Test-Path -LiteralPath $existingInput) { $existingInput } else { $null }
        groupPrimary     = "$($group.Primary.Stage) $($group.Primary.Id)"
        groupMembers     = @($group.Members | ForEach-Object { "$($_.Stage) $($_.Id)" })
        mergedInto       = $null
        duplicateOf      = $null
        duplicateSource  = $null
        partiallyRelated = @()
        possiblyRelated  = @()
        kind             = $null
        kindOptions      = @()
        template         = $null
        prefix           = $null
        labels           = $null
        milestone        = $null
        draftFile        = if ($existingDraft.Count -gt 0) { $existingDraft[0].FullName } else { $null }
        reportedByLine   = $null
        remediationLine  = $existingFindingLines[$key]
    }
}

$manifest = [ordered]@{
    issue          = [int]$n
    worktree       = $wt
    dryRun         = [bool]$DryRun
    only           = if ($Only) { @($Only) } else { $null }
    outputDir      = $outDir
    stageFile      = $stageOut
    stageHeader    = "Remediation for #$n`:"
    emptyLine      = "No findings from the code, tests or docs stages for #$n; no remediation drafts were written."
    lessonsHeading = $lessonsHeading
    lessons        = @($lessons)
    keptLessons    = @($keptLessons)
    routes         = $routes
    findings       = @($findingEntries)
}

# Cleanup, then write: only now that every gh call has succeeded. A dry run touches nothing but its sandbox.
New-Item -ItemType Directory -Force $outDir | Out-Null
if ($onlyKeys) {
    foreach ($gi in $touchedGroupIndexes) {
        foreach ($member in $groups[$gi].Members) {
            # A literal separator right after the id, so "code 1" never matches "code 10"'s files (#1492).
            foreach ($pattern in "$n-$($member.Stage)-$($member.Id)-*.md", "_input-$n-$($member.Stage)-$($member.Id).md") {
                foreach ($staleFile in (Get-ChildItem -LiteralPath $outDir -Filter $pattern -File -ErrorAction SilentlyContinue)) {
                    Remove-Item -LiteralPath $staleFile.FullName -Force
                    "audit-draft-remediation: removed previous output $($staleFile.Name)"
                }
            }
        }
    }
}
else {
    foreach ($pattern in "$n-*.md", "_input-$n-*.md", "_manifest-$n.json") {
        foreach ($staleFile in (Get-ChildItem -LiteralPath $outDir -Filter $pattern -File -ErrorAction SilentlyContinue)) {
            Remove-Item -LiteralPath $staleFile.FullName -Force
            "audit-draft-remediation: removed previous output $($staleFile.Name)"
        }
    }
    if ($DryRun) {
        if (Test-Path -LiteralPath $stageOut) { Remove-Item -LiteralPath $stageOut -Force; "audit-draft-remediation: removed previous output _dryrun-$n\remediation.md" }
    }
    elseif (Test-Path -LiteralPath $sandboxDir) {
        Remove-Item -LiteralPath $sandboxDir -Recurse -Force
        "audit-draft-remediation: removed previous output _dryrun-$n\"
    }
}

foreach ($f in $allFindings) {
    if ($touchedMemberKeys.Contains("$($f.Stage)|$($f.Id)")) { Set-Content -LiteralPath (Get-InputPath $f) -Encoding utf8 -Value $f.Text }
}
Set-Content -LiteralPath $manifestPath -Encoding utf8 -Value ($manifest | ConvertTo-Json -Depth 8)

$draftCount = @($manifest.findings | Where-Object { $_.regenerate -and $_.draftFile }).Count
"audit-draft-remediation: -Prepare wrote $manifestPath for #$n ($($allFindings.Count) finding(s), $draftCount draft(s) to write$(if ($DryRun) { ', dry run' })). Next: spawn remediation-drafter (naming #$n and wia-$n), then run -Finalize$(if ($DryRun) { ' -DryRun' })."
exit 0
