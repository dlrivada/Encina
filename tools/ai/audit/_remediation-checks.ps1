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

# Splits a finding's raw text into its two kinds of anchor (#1388 decision 1a/1b):
#   FileAnchors   -- every repo path the finding cites (backticked or in plain prose), with any trailing
#                    ':line' or ':line-line' suffix stripped, as @{ FullPath; Stem }. Stem is the file name
#                    WITHOUT its extension: a citation still matches a candidate that lists the same file
#                    inside a brace-expanded multi-file pattern such as
#                    'src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/{...,Sagas/SagaStoreADO,...}.cs' (a real
#                    issue-writing style, #1170), where the literal substring '...SagaStoreADO.cs' never
#                    appears -- only 'SagaStoreADO' does, because the extension sits outside the brace group.
#   SymbolAnchors -- every backticked token of the finding that is NOT one of the file anchors above: a code
#                    symbol, a literal code fragment, or a quoted rule sentence.
function Get-FindingAnchors {
    param([string]$FindingText)

    $text = if ($null -eq $FindingText) { '' } else { $FindingText }

    $fileAnchors = [System.Collections.Generic.List[pscustomobject]]::new()
    $seenPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($text, $script:RemediationFilePattern)) {
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

# #1388 decision 1: a model-named duplicate is accepted only when at least one FILE anchor AND at least one
# SYMBOL anchor of the finding both appear in the candidate's own title+body. A finding with no file anchor or
# no symbol anchor at all can never be auto-accepted (a Minor citation-only finding, for instance, commonly
# has no backticked symbol).
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

    $fileMatch = $false
    foreach ($fa in $anchors.FileAnchors) {
        if ($candidate.IndexOf($fa.FullPath, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { $fileMatch = $true; break }
        # A stem shorter than 3 characters (an unparsed glob leftover such as '*') is too generic to trust as
        # evidence on its own; skip it rather than risk a spurious match.
        if ([string]::IsNullOrWhiteSpace($fa.Stem) -or $fa.Stem.Length -lt 3) { continue }
        if ([regex]::IsMatch($candidate, '\b' + [regex]::Escape($fa.Stem) + '\b', 'IgnoreCase')) { $fileMatch = $true; break }
    }
    if (-not $fileMatch) { return $false }

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

# #1388 decision 4: the placeholder markers a drafted issue file must not still contain, collected from the
# templates under .github/ISSUE_TEMPLATE/ ($TemplatesDir):
#   - '[e.g., ...]' / '[How this affects ...]' -- any bracketed example value a template shows in place of a
#     real one (test_implementation.md Package(s)/Provider(s)/Collection/Fixture, bug_report.md Environment
#     fields, technical_debt.md Package(s), and the same idiom other templates in the directory use).
#   - '#___' -- the literal placeholder issue number every template's Related Issues section shows.
#   - the 'Example.Package' row of test_implementation.md's Current Coverage table.
#   - a literal, untouched 'Test <n>: Description' row from test_implementation.md's Test Plan section (a
#     real, filled-in test description never matches this -- only the bare word 'Description' as the whole
#     remainder of the line does).
#   - each template's own placeholder sentence under its '## Description' header ('A clear description of
#     ...' / 'A clear and concise description of ...'), read from the templates themselves (not hard-coded)
#     so a wording change there is picked up automatically.
# Returns the offending lines (trimmed, one per match); an empty list means the draft is clean.
function Find-TemplatePlaceholders {
    param([string]$TemplatesDir, [string]$DraftText)

    $found = [System.Collections.Generic.List[string]]::new()
    $text = if ($null -eq $DraftText) { '' } else { $DraftText }

    $descriptionSentences = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    if (Test-Path -LiteralPath $TemplatesDir) {
        foreach ($templateFile in Get-ChildItem -LiteralPath $TemplatesDir -Filter '*.md' -File) {
            $raw = (Get-Content -LiteralPath $templateFile.FullName -Raw) -replace '(?s)^---.*?---\r?\n', ''
            $tLines = $raw -split "`r?`n"
            for ($i = 0; $i -lt $tLines.Count; $i++) {
                if ($tLines[$i].Trim() -eq '## Description' -and ($i + 2) -lt $tLines.Count) {
                    $candidateSentence = $tLines[$i + 2].Trim()
                    if ($candidateSentence -match '^A clear( and concise)? description of ') { [void]$descriptionSentences.Add($candidateSentence) }
                }
            }
        }
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
    }

    return $found
}
