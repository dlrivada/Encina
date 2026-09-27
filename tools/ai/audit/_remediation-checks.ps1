# tools/ai/audit/_remediation-checks.ps1 (#1388)
#
# Pure, deterministic checks the remediation stage (audit-draft-remediation.ps1) applies to the local model's
# classification and draft output, so a hallucinated duplicate claim, a fenced draft or leftover template
# placeholder text never reaches artifacts/knowledge/remediation/*.md. Audit #16 hit all three: a duplicate
# claim with no shared evidence (3 of its 4 duplicate claims were wrong), a draft wrapped in an outer
# ```markdown fence, and a draft that kept the test_implementation.md placeholder text verbatim.
#
# No `gh` call and no local-model call happens anywhere in this file -- every function takes plain strings
# (already-fetched issue title/body text, already-drafted markdown, the templates directory) and returns a
# plain PowerShell value, which is what lets Test-Hooks.ps1 exercise all four functions without either
# dependency. audit-draft-remediation.ps1 does the I/O (gh issue view, the local model calls, re-asking).

# The path prefixes and extensions a finding's file citations use (mirrors Get-SearchTerms' own extension list
# in audit-draft-remediation.ps1). A citation always starts with one of these five top-level folders, so this
# never mistakes an arbitrary dotted word (e.g. 'AGENTS.md' referenced by section only) for a repo path.
$script:RemediationPathPrefixes = 'src|tests|docs|tools|\.github'
$script:RemediationPathExtensions = 'cs|ps1|md|json|yml|yaml|csproj|txt'
$script:RemediationFilePattern = "(?:$script:RemediationPathPrefixes)/[\w./\\-]*\.(?:$script:RemediationPathExtensions)(?::\d+(?:-\d+)?)?"

# #1400 decision 1: every finding in the code/tests/docs stage artifacts is written as "`loc1`, `loc2`, ...:
# narrative" -- a leading, comma/semicolon-joined list of backtick-delimited file citations (the finding's own
# claimed defect locations), followed by a colon that opens the prose explaining the defect. That colon is
# never inside a backtick span or a parenthetical aside (a citation's own "(`snippet`)" or "(persisted at
# `:355` via `_store.UpdateAsync`)" gloss) -- it is the first ':' at backtick-depth 0 and paren-depth 0. Returns
# the text before that boundary; when no such boundary is found (a malformed or free-form finding), the whole
# text is returned, which makes Get-FindingAnchors treat every citation as a required location -- the safer,
# stricter default when the convention is not followed.
#
# This distinction matters because a finding's prose commonly cites OTHER files too (a masking test fixture, a
# sibling registration file) that are supporting evidence, not the defect's own location -- a duplicate
# candidate that fails to mention those incidental files is still coverage of the finding's actual defect (the
# real code-4 finding of audit #16 cites its 3 SagaStoreADO.cs files up front, then a fixture and a test file
# later in the prose; the true duplicate #1170 only had to cover the first 3). A file mentioned only later,
# alongside a DIFFERENT top-level location added after a semicolon (the real code-3 finding of audit #16 cites
# `SagaRunner.cs` AND `SagaOrchestrator.cs` both before its boundary colon), is exactly the kind of second
# location a partial candidate misses -- issue #1400's own reproduction of the "partial duplicate" defect.
function Get-LeadingLocationText {
    param([string]$Text)

    if ([string]::IsNullOrEmpty($Text)) { return $Text }
    $inBacktick = $false
    $parenDepth = 0
    for ($i = 0; $i -lt $Text.Length; $i++) {
        $ch = $Text[$i]
        if ($ch -eq '`') { $inBacktick = -not $inBacktick; continue }
        if ($inBacktick) { continue }
        if ($ch -eq '(') { $parenDepth++; continue }
        if ($ch -eq ')') { if ($parenDepth -gt 0) { $parenDepth-- }; continue }
        if ($ch -eq ':' -and $parenDepth -eq 0) { return $Text.Substring(0, $i) }
    }
    return $Text
}

# Splits a finding's raw text into its two kinds of anchor (#1388 decision 1a/1b; #1400 decision 1 narrows
# FileAnchors to the finding's leading location clause, see Get-LeadingLocationText above):
#   FileAnchors   -- every repo path the finding cites in its own leading location clause (backticked or in
#                    plain prose), with any trailing ':line' or ':line-line' suffix stripped, as
#                    @{ FullPath; Stem }. Stem is the file name WITHOUT its extension: a citation still matches
#                    a candidate that lists the same file inside a brace-expanded multi-file pattern such as
#                    'src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/{...,Sagas/SagaStoreADO,...}.cs' (a real
#                    issue-writing style, #1170), where the literal substring '...SagaStoreADO.cs' never
#                    appears -- only 'SagaStoreADO' does, because the extension sits outside the brace group.
#   SymbolAnchors -- every backticked token of the finding's WHOLE text (unchanged by #1400) that is NOT one of
#                    the file anchors above: a code symbol, a literal code fragment, or a quoted rule sentence.
function Get-FindingAnchors {
    param([string]$FindingText)

    $text = if ($null -eq $FindingText) { '' } else { $FindingText }
    $leadingText = Get-LeadingLocationText $text

    $fileAnchors = [System.Collections.Generic.List[pscustomobject]]::new()
    $seenPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($leadingText, $script:RemediationFilePattern)) {
        $full = ($m.Value -split ':')[0]
        if (-not $seenPaths.Add($full)) { continue }
        $stem = [IO.Path]::GetFileNameWithoutExtension((Split-Path -Leaf $full))
        $fileAnchors.Add([pscustomobject]@{ FullPath = $full; Stem = $stem })
    }

    $exactFilePattern = '^(?:' + $script:RemediationFilePattern + ')$'
    # A backticked token with no path prefix at all -- a bare file name such as `MessagingConfiguration.cs` or
    # `PublicAPI.Unshipped.txt` -- is just as generic as a bare 'UseXxx' toggle: many findings across the
    # codebase cite the same well-known file by its bare name alone (a class's own file, PublicAPI.*.txt, a
    # shared options file), so on its own it identifies "this area of the codebase", not "this defect". It is
    # excluded from symbol evidence for the same reason 'UseXxx' is (adversarial review of #1388: this pattern
    # was found sitting in the symbol pool, unexcluded, for finding-code-1's own `MessagingConfiguration.cs`
    # and `PublicAPI.Unshipped.txt` mentions).
    $bareFileNamePattern = '^[\w-]+\.(?:' + $script:RemediationPathExtensions + ')$'
    $symbolAnchors = [System.Collections.Generic.List[string]]::new()
    foreach ($m in [regex]::Matches($text, '`([^`]+)`')) {
        $token = $m.Groups[1].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($token)) { continue }
        if ([regex]::IsMatch($token, $exactFilePattern)) { continue }
        if ([regex]::IsMatch($token, $bareFileNamePattern)) { continue }
        # A bare 'UseXxx' token (no dot, parens or generic brackets) is AGENTS.md's own generic naming
        # convention for a messaging pattern's opt-in flag ("Every messaging pattern... is optional... Example:
        # `config.UseOutbox = true;`"), so it recurs, unqualified, across many unrelated issues in this
        # codebase's Sagas/Choreography/Outbox/Inbox area (e.g. a finding about Choreography mentioning
        # `UseSagas` only to contrast it with the missing `UseChoreography`) -- on its own it identifies the
        # FEATURE AREA, not the specific defect, so it is excluded here rather than accepted as evidence.
        if ($token -match '^Use[A-Z][A-Za-z]*$') { continue }
        $symbolAnchors.Add($token)
    }

    return [pscustomobject]@{ FileAnchors = $fileAnchors; SymbolAnchors = $symbolAnchors }
}

# Tests one of the finding's file anchors against the candidate's raw title+body text: either the anchor's
# full path appears verbatim (case-insensitive), or its stem (>= 3 chars, to skip an unparsed glob leftover
# such as '*') appears as a whole word -- the brace-expansion case Get-FindingAnchors' own doc comment
# describes. Shared by Test-DuplicateEvidence (#1400: requires every anchor) and Test-PartialDuplicateEvidence
# (requires only one).
function Test-FileAnchorMatch {
    param([pscustomobject]$FileAnchor, [string]$Candidate)

    if ($Candidate.IndexOf($FileAnchor.FullPath, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { return $true }
    if ([string]::IsNullOrWhiteSpace($FileAnchor.Stem) -or $FileAnchor.Stem.Length -lt 3) { return $false }
    return [regex]::IsMatch($Candidate, '\b' + [regex]::Escape($FileAnchor.Stem) + '\b', 'IgnoreCase')
}

# #1388 decision 1, narrowed by #1400 decision 1: a model-named duplicate is accepted only when EVERY file
# anchor of the finding's own leading location clause (see Get-LeadingLocationText) AND at least one SYMBOL
# anchor both appear in the candidate's own title+body. A finding with no file anchor or no symbol anchor at
# all can never be auto-accepted (a Minor citation-only finding, for instance, commonly has no backticked
# symbol). Before #1400, ANY one file anchor was enough, which is exactly how audit #16's code finding 3 was
# wrongly accepted as a duplicate of #1343: #1343 covers `SagaRunner.cs`, but the finding's own leading clause
# also names `SagaOrchestrator.cs`, a second location #1343 never mentions.
#
# The symbol side is matched by EXACT, case-insensitive equality against one of the candidate's OWN
# backtick-delimited tokens -- not a substring/word-boundary search across the candidate's raw prose. A
# generic identifier such as 'AddMessagingServices' can appear as a strict SUBSTRING of an unrelated, more
# specific mention in a candidate about a different bug (e.g. a candidate that names the generic call
# 'AddMessagingServices<...>'); a plain word-boundary regex match still fires on that substring since '<' is a
# non-word character, which would accept a false duplicate. Comparing whole backtick tokens for exact equality
# only accepts a duplicate when the candidate cites the very same symbol, never a longer/different token that
# happens to start the same way. This also matches the decision's own stated rationale: when in doubt, reject
# the duplicate and draft the finding as new -- a false 'new' draft is caught later by audit-verifier's own
# dedup pass, while a false accepted duplicate silently drops the finding for good.
#
# The file side stays a substring/whole-token search (not exact-token equality) because file citations are
# rarely wrapped in a candidate's OWN backtick token the same way (see the brace-expansion case above), and a
# file path or name is specific enough on its own that a substring match carries little false-positive risk.
function Test-DuplicateEvidence {
    param([string]$FindingText, [string]$CandidateTitleAndBody)

    $anchors = Get-FindingAnchors $FindingText
    if ($anchors.FileAnchors.Count -eq 0 -or $anchors.SymbolAnchors.Count -eq 0) { return $false }

    $candidate = if ($null -eq $CandidateTitleAndBody) { '' } else { $CandidateTitleAndBody }

    foreach ($fa in $anchors.FileAnchors) {
        if (-not (Test-FileAnchorMatch $fa $candidate)) { return $false }
    }

    $candidateSymbols = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($candidate, '`([^`]+)`')) {
        $tok = $m.Groups[1].Value.Trim()
        if (-not [string]::IsNullOrWhiteSpace($tok)) { [void]$candidateSymbols.Add($tok) }
    }
    foreach ($sa in $anchors.SymbolAnchors) {
        if ($candidateSymbols.Contains($sa)) { return $true }
    }
    return $false
}

# #1400 decision 1: used only to word the rejection note audit-draft-remediation.ps1 appends to a drafted
# finding when Test-DuplicateEvidence rejects a model-named duplicate -- "partially related" (at least one of
# the finding's own file anchors matched the candidate, so it is plausibly about the same area) versus the
# existing, weaker "possibly related" (no file anchor matched at all). Never used to accept a duplicate: a
# finding with zero file anchors is never "partially" related to anything by definition.
function Test-PartialDuplicateEvidence {
    param([string]$FindingText, [string]$CandidateTitleAndBody)

    $anchors = Get-FindingAnchors $FindingText
    if ($anchors.FileAnchors.Count -eq 0) { return $false }

    $candidate = if ($null -eq $CandidateTitleAndBody) { '' } else { $CandidateTitleAndBody }
    foreach ($fa in $anchors.FileAnchors) {
        if (Test-FileAnchorMatch $fa $candidate) { return $true }
    }
    return $false
}

# #1388 decision 3: strips exactly ONE outer code fence -- the first non-empty line is ```` ``` ```` or
# ```` ```markdown ````/```` ```md ```` and the last non-empty line is a bare ```` ``` ````, both on their own
# line -- and returns the text between them. Never touches an inner fence (a fenced code sample inside a
# properly-unwrapped draft), and returns the input unchanged when it is not wrapped at all.
function Remove-OuterFence {
    param([string]$Text)

    if ([string]::IsNullOrEmpty($Text)) { return $Text }
    $lines = $Text -split "`r?`n"
    $nonEmptyIndexes = [System.Collections.Generic.List[int]]::new()
    for ($i = 0; $i -lt $lines.Count; $i++) { if ($lines[$i].Trim() -ne '') { $nonEmptyIndexes.Add($i) } }
    if ($nonEmptyIndexes.Count -lt 2) { return $Text }

    $firstIdx = $nonEmptyIndexes[0]
    $lastIdx = $nonEmptyIndexes[$nonEmptyIndexes.Count - 1]
    if ($lastIdx -le $firstIdx) { return $Text }
    if ($lines[$firstIdx] -notmatch '^\s*```(markdown|md)?\s*$') { return $Text }
    if ($lines[$lastIdx] -notmatch '^\s*```\s*$') { return $Text }

    $inner = if ($lastIdx -eq $firstIdx + 1) { @() } else { $lines[($firstIdx + 1)..($lastIdx - 1)] }
    return ($inner -join "`n")
}

# #1388 decision 4, extended by #1400 decision 2: the placeholder markers a drafted issue file must not still
# contain, derived from the ONE routed template's own text ($TemplateText -- the raw file, front matter
# included; the script passes what Get-TemplateBody read for the same template it drafted against):
#   - '[e.g., ...]' / '[How this affects ...]' -- any bracketed example value a template shows in place of a
#     real one (test_implementation.md Package(s)/Provider(s)/Collection/Fixture, bug_report.md Environment
#     fields, technical_debt.md Package(s), and the same idiom other templates in the directory use).
#   - '#___' -- the literal placeholder issue number every template's Related Issues section shows.
#   - the 'Example.Package' row of test_implementation.md's Current Coverage table.
#   - a literal, untouched 'Test <n>: Description' row from test_implementation.md's Test Plan section (a
#     real, filled-in test description never matches this -- only the bare word 'Description' as the whole
#     remainder of the line does).
#   - each template's own placeholder sentence under its '## Description' header ('A clear description of
#     ...' / 'A clear and concise description of ...'), read from the template itself (not hard-coded) so a
#     wording change there is picked up automatically.
#   - #1400: every OTHER non-structural line of the template body -- not blank, not a '#'-level header, not a
#     checkbox line, not a table header or separator row, and at least 20 characters long -- counts as a
#     placeholder when it appears verbatim (trimmed) in the draft. This is what #1388's hand-written marker
#     list missed: a template's plain-prose instruction sentence such as technical_debt.md's Related Issues
#     line "Link any related issues here.", which a model can copy through unchanged just like a bracketed
#     example. A table HEADER row is recognised by lookahead (its very next non-blank line is a separator row
#     of only '|', '-', ':' and spaces); a table DATA row is not excluded here, since a genuinely filled-in
#     data row never matches the template's own placeholder row text verbatim anyway.
# Returns the offending lines (trimmed, one per match); an empty list means the draft is clean.
function Find-TemplatePlaceholders {
    param([string]$TemplateText, [string]$DraftText)

    $found = [System.Collections.Generic.List[string]]::new()
    $text = if ($null -eq $DraftText) { '' } else { $DraftText }
    $template = if ($null -eq $TemplateText) { '' } else { $TemplateText }
    $body = $template -replace '(?s)^---.*?---\r?\n', ''
    $tLines = $body -split "`r?`n"

    $descriptionSentences = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    for ($i = 0; $i -lt $tLines.Count; $i++) {
        if ($tLines[$i].Trim() -eq '## Description' -and ($i + 2) -lt $tLines.Count) {
            $candidateSentence = $tLines[$i + 2].Trim()
            if ($candidateSentence -match '^A clear( and concise)? description of ') { [void]$descriptionSentences.Add($candidateSentence) }
        }
    }

    $tableSeparatorPattern = '^\|[\s:|-]+\|$'
    $derivedMarkers = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    for ($i = 0; $i -lt $tLines.Count; $i++) {
        $tTrim = $tLines[$i].Trim()
        if ($tTrim -eq '') { continue }
        if ($tTrim -match '^#{1,6}\s') { continue }
        if ($tTrim -match '^-\s*\[[ xX]\]') { continue }
        if ($tTrim -match $tableSeparatorPattern) { continue }
        # A blockquote note ('> Fill in if this is a coverage gap issue.', '> Per `AGENTS.md` §9 ...') explains
        # the section and is meant to stay verbatim in every draft, including a fully and correctly filled one
        # (the real 16-tests-1 draft keeps both of test_implementation.md's own '>' notes untouched) -- it is
        # never itself the "fill in a real value" placeholder decision 2 targets, unlike a bracketed example or
        # an unfilled table/description/related-issues line.
        if ($tTrim.StartsWith('>')) { continue }
        if ($tTrim.StartsWith('|')) {
            $nextTrim = if (($i + 1) -lt $tLines.Count) { $tLines[$i + 1].Trim() } else { '' }
            if ($nextTrim -match $tableSeparatorPattern) { continue }  # this is the table's header row
        }
        if ($tTrim.Length -lt 20) { continue }
        [void]$derivedMarkers.Add($tTrim)
    }

    foreach ($line in ($text -split "`r?`n")) {
        $trimmed = $line.Trim()
        if ($trimmed -eq '') { continue }
        if ($trimmed -match '\[e\.g\.,[^\]]*\]' -or $trimmed -match '\[How this affects[^\]]*\]') { $found.Add($trimmed); continue }
        if ($trimmed -match '#___') { $found.Add($trimmed); continue }
        if ($trimmed -match '^\|\s*Example\.Package\s*\|') { $found.Add($trimmed); continue }
        # Allows an optional trailing period ('Test 1: Description.') so a model that copies the placeholder
        # and adds punctuation still gets caught (adversarial review of #1388).
        if ($trimmed -match '^-\s*\[[ xX]\]\s*Test\s+\d+:\s*Description\.?\s*$') { $found.Add($trimmed); continue }
        if ($descriptionSentences.Contains($trimmed)) { $found.Add($trimmed); continue }
        if ($derivedMarkers.Contains($trimmed)) { $found.Add($trimmed); continue }
    }

    return $found
}

# #1400 decision 3: after a draft is written and repaired (fence stripped, placeholders re-asked), its own
# Related Issues section is sanitized -- the model is free to name a candidate as related there
# (Build-DraftBrief's own guidance explicitly invites it), but audit #16's 16-code-5 draft showed it will also
# invent a relation to issues nobody offered it and that have nothing to do with the finding (#699, #696, #181
# -- real open issues about an unrelated caching/health-check feature). Two conventions exist because
# bug_report.md, unlike technical_debt.md and test_implementation.md, has no dedicated 'Related Issues' header
# of its own -- a bug-kind draft (like 16-code-5) puts it as a '- **Related Issues**:' bullet with indented
# sub-bullets inside 'Additional Context' instead:
#   - a '## Related Issues' H2 header: the section runs to the next '## ' header or end of file;
#   - a '- **Related Issues**:' bullet (with or without the leading '- '): the section is the run of
#     immediately-following bullet lines (indented sub-bullets '  - #n' as audit #16's real draft has, or
#     unindented siblings '- #n' at the same level -- both plausible model output), stopping at the first
#     blank line, a new '## ' header, a new sibling bold field ('- **Something Else**:'), or any other line
#     that is not itself a bullet. The header line itself tolerates an optional trailing colon
#     ('**Related Issues**' or '**Related Issues**:') and an optional leading '- '.
# A reference (#n) inside that section survives only when n is:
#   - the audited issue itself ($IssueNumber) -- every draft's brief always injects "#$IssueNumber (This
#     issue)" as a standing convention, never left to the model's own judgement to keep or drop;
#   - named in the finding's own text ($FindingText);
#   - named in the candidate list actually offered to the classifier for this finding ($CandidateText); or
#   - named in one of the script's own duplicate/partially-related/possibly-related note lines for this
#     finding ($ScriptNoteLines, e.g. Test-PartialDuplicateEvidence's "- #m - partially related" line).
# A line with no issue reference at all (prose, a blank line) is always kept untouched; a line naming an
# unverified number is dropped entirely (not just the number). Returns the sanitized draft text and the list of
# removed numbers, so the caller can log them ("removed unverified related issue #n") against this finding's
# own line in stages/remediation.md.
function Limit-RelatedIssues {
    param(
        [string]$DraftText,
        [string]$IssueNumber,
        [string]$FindingText,
        [string]$CandidateText,
        [string[]]$ScriptNoteLines
    )

    $text = if ($null -eq $DraftText) { '' } else { $DraftText }
    $lines = @($text -split "`r?`n")

    $headerIdx = -1
    $isBoldBullet = $false
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^##\s*Related Issues\s*$') { $headerIdx = $i; $isBoldBullet = $false; break }
        if ($lines[$i] -match '^\s*-?\s*\*\*Related Issues\*\*:?\s*$') { $headerIdx = $i; $isBoldBullet = $true; break }
    }
    if ($headerIdx -lt 0) { return [pscustomobject]@{ Text = $text; Removed = @() } }

    $sectionStartLine = $headerIdx + 1
    $sectionEndLine = $lines.Count
    if ($isBoldBullet) {
        for ($i = $sectionStartLine; $i -lt $lines.Count; $i++) {
            $l = $lines[$i]
            if ($l.Trim() -eq '') { $sectionEndLine = $i; break }
            if ($l -match '^##\s') { $sectionEndLine = $i; break }
            # a new sibling bold field at the same list level ('- **Location**:', '- **Priority**:', ...) ends
            # this section; a bullet naming an issue never itself looks like that.
            if ($l -match '^\s*-\s*\*\*[^*]+\*\*:') { $sectionEndLine = $i; break }
            if ($l -match '^[ \t]*-') { continue }  # an indented sub-bullet or an unindented sibling bullet
            $sectionEndLine = $i
            break
        }
    }
    else {
        for ($i = $sectionStartLine; $i -lt $lines.Count; $i++) {
            if ($lines[$i] -match '^##\s') { $sectionEndLine = $i; break }
        }
    }

    $allowed = [System.Collections.Generic.HashSet[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($IssueNumber)) { [void]$allowed.Add($IssueNumber.TrimStart('#')) }
    foreach ($src in @($FindingText, $CandidateText)) {
        if ($null -eq $src) { continue }
        foreach ($m in [regex]::Matches($src, '#(\d+)')) { [void]$allowed.Add($m.Groups[1].Value) }
    }
    foreach ($noteLine in $ScriptNoteLines) {
        if ($null -eq $noteLine) { continue }
        foreach ($m in [regex]::Matches($noteLine, '#(\d+)')) { [void]$allowed.Add($m.Groups[1].Value) }
    }

    $removed = [System.Collections.Generic.List[string]]::new()
    $newLines = [System.Collections.Generic.List[string]]::new()
    for ($i = 0; $i -lt $sectionStartLine; $i++) { $newLines.Add($lines[$i]) }
    for ($i = $sectionStartLine; $i -lt $sectionEndLine; $i++) {
        $line = $lines[$i]
        $lineNumbers = @([regex]::Matches($line, '#(\d+)') | ForEach-Object { $_.Groups[1].Value })
        if ($lineNumbers.Count -eq 0) { $newLines.Add($line); continue }
        $hasAllowedNumber = $false
        foreach ($num in $lineNumbers) { if ($allowed.Contains($num)) { $hasAllowedNumber = $true } }
        if ($hasAllowedNumber) { $newLines.Add($line) }
        else { foreach ($num in $lineNumbers) { $removed.Add($num) } }
    }
    for ($i = $sectionEndLine; $i -lt $lines.Count; $i++) { $newLines.Add($lines[$i]) }

    return [pscustomobject]@{ Text = ($newLines -join "`n"); Removed = @($removed) }
}

# #1409: bug_report.md's own '## Environment' section asks for facts (Encina version, .NET version, OS) the
# local model has no way to know for a finding from a static-analysis stage -- it copies the template's own
# bracketed placeholders verbatim, Find-TemplatePlaceholders flags them, and even the one re-ask above never
# supplies real facts (the model still has no way to know them), so the draft is kept with 'PLACEHOLDERS LEFT'
# forever (audit #16's 16-code-5 draft, reproduced in the issue this fixes). These three facts ARE deterministic
# for a code-review finding (never observed at runtime), so audit-draft-remediation.ps1's Repair-Draft calls
# Set-BugEnvironment to overwrite the whole '## Environment' section body AFTER the model replies (and after the
# fence strip) but BEFORE Find-TemplatePlaceholders ever inspects the draft, for every bug_report.md-routed
# draft -- the model's own guess at these three facts (right or wrong) is never load-bearing. 'Package(s)
# Affected' is the one field the model can sometimes get right (it saw the finding's own file citation), so it
# is kept when it is not itself one of the template's own bracketed placeholder shapes; otherwise it is derived
# from the finding's own first 'src/<Package>/' path, otherwise 'Not determined'.

# Reads '<VersionPrefix>'/'<VersionSuffix>' from Directory.Build.props at $RepoRoot -- the same file AGENTS.md
# §1 and every csproj in this repository derive their NuGet version from. Returns 'Not determined' when the
# file is missing or has no (non-blank) VersionPrefix, rather than a fabricated version string; the suffix is
# appended with a single '-' only when it is present and non-blank (today '0.14.0-dev'; a release build with an
# empty VersionSuffix reads as plain '0.14.0').
function Get-EncinaVersion {
    param([string]$RepoRoot)

    $propsPath = Join-Path $RepoRoot 'Directory.Build.props'
    if (-not (Test-Path -LiteralPath $propsPath)) { return 'Not determined' }
    $raw = Get-Content -LiteralPath $propsPath -Raw
    $prefixMatch = [regex]::Match($raw, '<VersionPrefix>\s*([^<]*?)\s*</VersionPrefix>')
    if (-not $prefixMatch.Success -or [string]::IsNullOrWhiteSpace($prefixMatch.Groups[1].Value)) { return 'Not determined' }
    $prefix = $prefixMatch.Groups[1].Value.Trim()
    $suffixMatch = [regex]::Match($raw, '<VersionSuffix>\s*([^<]*?)\s*</VersionSuffix>')
    $suffix = if ($suffixMatch.Success) { $suffixMatch.Groups[1].Value.Trim() } else { '' }
    if ([string]::IsNullOrWhiteSpace($suffix)) { return $prefix }
    return "$prefix-$suffix"
}

# The finding's own first 'src/<Package>/...' citation (backticked or plain prose), e.g. 'src/Encina.MongoDB/
# Sagas/SagaStoreMongoDB.cs:129-132' -> 'Encina.MongoDB'. Returns $null when the finding cites no src/ path at
# all (a tests/ or docs/ finding routed to bug_report.md would be unusual, but never guessed at).
function Get-PackageFromFindingText {
    param([string]$FindingText)

    if ([string]::IsNullOrWhiteSpace($FindingText)) { return $null }
    $m = [regex]::Match($FindingText, 'src[\\/]([A-Za-z0-9._-]+)[\\/]')
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}

# A model-filled value is trusted only when it is not itself blank and not one of the template's own bracketed
# example shapes ('[e.g., Encina.EntityFrameworkCore, Encina.Dapper.SqlServer]') -- the exact text the model
# copies through unchanged when it does not know the real answer.
function Test-PlaceholderEnvironmentValue {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value)) { return $true }
    return [regex]::IsMatch($Value.Trim(), '^\[.*\]$')
}

# Replaces the whole '## Environment' section body (from the header to the next '## ' header, or end of file)
# with the four deterministic bullets, keeping every other section of $DraftText untouched. Returns $DraftText
# unchanged when it has no '## Environment' header at all (never the case for a real bug_report.md draft, but
# defends against a malformed one rather than throwing).
function Set-BugEnvironment {
    param([string]$DraftText, [string]$RepoRoot, [string]$FindingText)

    $text = if ($null -eq $DraftText) { '' } else { $DraftText }
    $lines = @($text -split "`r?`n")
    $headerIdx = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq '## Environment') { $headerIdx = $i; break }
    }
    if ($headerIdx -lt 0) { return $text }

    $sectionEndLine = $lines.Count
    for ($i = $headerIdx + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^##\s') { $sectionEndLine = $i; break }
    }

    $modelPackage = $null
    for ($i = $headerIdx + 1; $i -lt $sectionEndLine; $i++) {
        $m = [regex]::Match($lines[$i], '^\s*-\s*\*\*Package\(s\)\s*Affected\*\*:\s*(.*)$')
        if ($m.Success) { $modelPackage = $m.Groups[1].Value.Trim(); break }
    }
    $package = if ($modelPackage -and -not (Test-PlaceholderEnvironmentValue $modelPackage)) {
        $modelPackage
    }
    else {
        $derived = Get-PackageFromFindingText $FindingText
        if ($derived) { $derived } else { 'Not determined' }
    }

    $version = Get-EncinaVersion $RepoRoot
    $newSection = @(
        '## Environment'
        ''
        "- **Encina Version**: $version"
        '- **.NET Version**: .NET 10'
        '- **OS**: Not applicable (found by static review of the code, not at runtime)'
        "- **Package(s) Affected**: $package"
        ''
    )

    $newLines = [System.Collections.Generic.List[string]]::new()
    for ($i = 0; $i -lt $headerIdx; $i++) { $newLines.Add($lines[$i]) }
    foreach ($l in $newSection) { $newLines.Add($l) }
    for ($i = $sectionEndLine; $i -lt $lines.Count; $i++) { $newLines.Add($lines[$i]) }
    return ($newLines -join "`n")
}

# #1400 (adversarial review finding 1): inserts one note line (audit-draft-remediation.ps1's
# "partially related"/"possibly related" line for a rejected duplicate-of claim) into a draft's own Related
# Issues section, recognising the SAME two conventions Limit-RelatedIssues does. Before this function existed,
# audit-draft-remediation.ps1 looked only for the '## Related Issues' H2 and, for a bug-kind draft
# (bug_report.md has no such header -- only the '- **Related Issues**:' bold-bullet convention), fell back to
# appending the note at the very end of the file, detached from the section it names and from what
# Limit-RelatedIssues actually scans -- a structurally malformed draft. Returns the updated text and whether a
# section was found at all; when neither convention is found, the text is returned unchanged so the caller can
# fall back and log a lesson, exactly as before.
function Add-RelatedIssuesLine {
    param([string]$DraftText, [string]$Line)

    $text = if ($null -eq $DraftText) { '' } else { $DraftText }
    $lines = @($text -split "`r?`n")

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $insertedLine = $null
        if ($lines[$i] -match '^##\s*Related Issues\s*$') { $insertedLine = $Line }
        elseif ($lines[$i] -match '^\s*-?\s*\*\*Related Issues\*\*:?\s*$') { $insertedLine = "  $Line" }
        if ($null -eq $insertedLine) { continue }

        $newLines = [System.Collections.Generic.List[string]]::new()
        for ($j = 0; $j -le $i; $j++) { $newLines.Add($lines[$j]) }
        $newLines.Add($insertedLine)
        for ($j = $i + 1; $j -lt $lines.Count; $j++) { $newLines.Add($lines[$j]) }
        return [pscustomobject]@{ Text = ($newLines -join "`n"); Found = $true }
    }

    return [pscustomobject]@{ Text = $text; Found = $false }
}
