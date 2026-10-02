# tools/ai/audit/_remediation-checks.ps1 (#1388, revised by #1572)
#
# The deterministic helpers of the SPEC-003 remediation stage. audit-draft-remediation.ps1 -Prepare uses the
# finding-side ones (anchors, duplicate evidence, location grouping) to build the remediation-drafter agent's
# manifest; audit-draft-remediation.ps1 -Finalize uses the draft-side ones (outer-fence strip, placeholder and
# template-header checks, Related Issues sanitizer, Type tick, bug Environment fill, Reported-by line) on every
# draft the agent wrote, so a fenced draft, a leftover template placeholder or an unverified issue reference
# never survives the stage. Audit #16 hit all three with the local model that drafted before #1572.
#
# No model call happens anywhere in this file. Every function except Invoke-GhWithRetry takes plain strings
# (already-fetched issue title/body text, already-drafted markdown, a template's text) and returns a plain
# PowerShell value, which lets Test-Hooks.ps1 exercise them directly. Invoke-GhWithRetry (#1548) is the one
# place `gh` runs, so every gh call of the stage shares one retry policy; Test-Hooks.ps1 stubs `gh` with a
# PowerShell function to test it offline.

# The path prefixes and extensions a finding's file citations use (mirrors Get-SearchTerms' own extension list
# in audit-draft-remediation.ps1). A citation always starts with one of these five top-level folders, so this
# never mistakes an arbitrary dotted word (e.g. 'AGENTS.md' referenced by section only) for a repo path.
$script:RemediationPathPrefixes = 'src|tests|docs|tools|\.github'
$script:RemediationPathExtensions = 'cs|ps1|md|json|yml|yaml|csproj|txt'
$script:RemediationFilePattern = "(?:$script:RemediationPathPrefixes)/[\w./\\-]*\.(?:$script:RemediationPathExtensions)(?::\d+(?:-\d+)?)?"

# #1393 decision 2: the sections of a candidate issue that say WHERE its defect is and WHAT the code does
# today -- the only parts of a candidate's body that count as duplicate evidence (plus its title). Derived
# from the repository's own .github/ISSUE_TEMPLATE/*.md headers:
#   technical_debt.md     -> Location, Current Behavior
#   bug_report.md         -> Steps to Reproduce, Actual Behavior, Code Sample, Stack Trace
#   infrastructure.md     -> Component Affected, Current Behavior
#   refactoring.md        -> Current Structure, Affected Files, Packages Affected
#   test_implementation.md -> Packages / Providers Affected, Current Coverage
#   feature_request.md    -> Affected Packages
# Every other section (Description, Motivation, Expected Behavior, Root Cause, Proposed Fix/Solution,
# Additional Context, Related Issues, Alternatives, Cross-Cutting Integration, ...) is where an issue quotes a
# house rule, gives an example of some OTHER defect, lists related issues or explains background -- text that
# MENTIONS a file or symbol without being ABOUT it. Audit #16 verification pass 5 found three false duplicates
# built entirely from such mentions: #1393 names `InstrumentedSagaStore.cs` only in its Description, as an
# example of a false positive; #1343 and #1299 matched only through a generic `src/` token and a generic file
# stem ('sagas', 'README') found in prose or a directory segment.
$script:CandidateLocationSections = @(
    'Location', 'Current Behavior', 'Steps to Reproduce', 'Actual Behavior', 'Code Sample', 'Stack Trace',
    'Component Affected', 'Current Structure', 'Affected Files', 'Packages Affected',
    'Packages / Providers Affected', 'Current Coverage', 'Affected Packages'
)
# The bold-field equivalents (a '- **File(s)**: ...' line), wherever they appear: the templates' own fields
# inside the sections above (technical_debt.md's '**File(s)**' and '**Package(s)**', infrastructure.md's
# '**File(s)**', bug_report.md's '**Package(s) Affected**', test_implementation.md's '**Provider(s)**'), and the
# same names written as a bold pseudo-heading by an issue that does not use the template's '##' headers.
$script:CandidateLocationFields = @(
    'File(s)', 'Files', 'File', 'Package(s)', 'Package(s) Affected', 'Provider(s)', 'Location',
    'Component Affected', 'Affected Files', 'Packages Affected', 'Affected Packages'
)

# #1393 decision 2, second half (#1393's own proposal): a backticked token that AGENTS.md or CLAUDE.md itself
# backticks is house-rule vocabulary (`EncinaError.Message`, `TimeProvider`, `OpenAsync`,
# `TransactionPipelineBehavior`, ...). Findings and issues quote those rules verbatim, so the same token recurs
# across many unrelated defects (#1168, #1173, #1259, #1274, #1319, #1322 are all separate `EncinaError.Message`
# leaks) and identifies the RULE broken, never the specific defect. Read once when this file is dot-sourced;
# the seed keeps the best-known case excluded even where the two files are absent (a copied test fixture).
$script:HouseRuleTokens = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
[void]$script:HouseRuleTokens.Add('EncinaError.Message')
foreach ($ruleFile in 'AGENTS.md', 'CLAUDE.md') {
    $ruleFilePath = Join-Path $PSScriptRoot "..\..\..\$ruleFile"
    if (-not (Test-Path -LiteralPath $ruleFilePath)) { continue }
    foreach ($ruleMatch in [regex]::Matches((Get-Content -LiteralPath $ruleFilePath -Raw), '`([^`\r\n]+)`')) {
        $ruleToken = $ruleMatch.Groups[1].Value.Trim()
        if (-not [string]::IsNullOrWhiteSpace($ruleToken)) { [void]$script:HouseRuleTokens.Add($ruleToken) }
    }
}

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
#                    @{ FullPath; RootlessPath }. RootlessPath is the path without its first segment
#                    ('Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs'), or $null when that would leave a bare file
#                    name; see Test-FileAnchorMatch for how each one is allowed to match (#1393).
#   SymbolAnchors -- every backticked token of the finding's WHOLE text (unchanged by #1400) that is NOT one of
#                    the file anchors above and NOT one of the generic token classes below: a code symbol or a
#                    literal code fragment specific to this defect.
function Get-FindingAnchors {
    param([string]$FindingText)

    $text = if ($null -eq $FindingText) { '' } else { $FindingText }
    $leadingText = Get-LeadingLocationText $text

    $fileAnchors = [System.Collections.Generic.List[pscustomobject]]::new()
    $seenPaths = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($leadingText, $script:RemediationFilePattern)) {
        $full = (($m.Value -split ':')[0]) -replace '\\', '/'
        if (-not $seenPaths.Add($full)) { continue }
        $segments = $full.Split('/')
        $rootless = if ($segments.Count -ge 3) { ($segments[1..($segments.Count - 1)]) -join '/' } else { $null }
        $fileAnchors.Add([pscustomobject]@{ FullPath = $full; RootlessPath = $rootless })
    }

    $exactFilePattern = '^(?:' + $script:RemediationFilePattern + ')$'
    # A backticked token with no path prefix at all -- a bare file name such as `MessagingConfiguration.cs` or
    # `PublicAPI.Unshipped.txt` -- is just as generic as a bare 'UseXxx' toggle: many findings across the
    # codebase cite the same well-known file by its bare name alone (a class's own file, PublicAPI.*.txt, a
    # shared options file), so on its own it identifies "this area of the codebase", not "this defect". It is
    # excluded from symbol evidence for the same reason 'UseXxx' is (adversarial review of #1388: this pattern
    # was found sitting in the symbol pool, unexcluded, for finding-code-1's own `MessagingConfiguration.cs`
    # and `PublicAPI.Unshipped.txt` mentions).
    # #1393 widens it to a bare file name that carries a line suffix (`SagaRunner.cs:165`, `sagas.md:419-432`,
    # `InstrumentedSagaStore.cs:49,59`): the suffix does not make the name any less generic.
    $bareFileNamePattern = '^[\w.-]+\.(?:' + $script:RemediationPathExtensions + ')(?::[\d,\s-]+)?$'
    $symbolAnchors = [System.Collections.Generic.List[string]]::new()
    foreach ($m in [regex]::Matches($text, '`([^`]+)`')) {
        $token = $m.Groups[1].Value.Trim()
        if ([string]::IsNullOrWhiteSpace($token)) { continue }
        if ([regex]::IsMatch($token, $exactFilePattern)) { continue }
        if ([regex]::IsMatch($token, $bareFileNamePattern)) { continue }
        # #1393: three more generic token classes that name an AREA or a RULE, never this defect --
        #   - a bare line reference (`:192-195`, `:355`), which only points back into a file already cited;
        #   - a path that is not one of the file anchors: a folder (`src/`, `src/Encina`, `docs/messaging/`),
        #     a file outside the five cited roots (`artifacts/knowledge/stages/code.md`) or a brace/glob path
        #     pattern. A folder names an area, and a file is location evidence, which only the leading
        #     location clause supplies (#1400). Audit #16's docs findings 1 and 4 were accepted as
        #     duplicates of #1343 and #1299 on "`src/`" alone;
        #   - a house-rule token that AGENTS.md or CLAUDE.md backticks (`EncinaError.Message`, ...), see
        #     $script:HouseRuleTokens: quoting the rule a defect breaks says nothing about which defect it is.
        if ($token -match '^:\d') { continue }
        if ($token.Contains('/') -and $token -match '^[\w.{},*/\\-]+(?::[\d,-]+)?$') { continue }
        if ($script:HouseRuleTokens.Contains($token)) { continue }
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

# #1393 decision 2: reduces a candidate's "title`nbody" text (the shape audit-draft-remediation.ps1 builds) to
# the part that is ABOUT its defect: the title (first line) plus every section named in
# $script:CandidateLocationSections and every bold field named in $script:CandidateLocationFields. A section
# runs from its heading to the next heading of the same or a higher level (so a '###' sub-heading stays
# inside it); headings inside a fenced code block are not headings, and emphasis around a heading's name
# ('## **Location**') is ignored. A bold field counts only where no section decides otherwise: before the
# body's first heading (an issue written without the template's headers) or inside bug_report.md's
# '## Environment' (its '**Package(s) Affected**'); a bold field under any other heading (Root Cause,
# Additional Context, Related Issues, ...) is as much a mention as the rest of that section. It keeps the
# rest of its own line plus the more-indented lines under it (the '- **File(s)**:' + nested '  - `path`' list
# of #1343), or, when it is a bare pseudo-heading with nothing after it, the lines up to the next blank line
# or heading. A candidate with none of these sections contributes its title only -- the strict default: an unrecognised
# body can never supply duplicate evidence, and a real duplicate missed this way is drafted as new, which the
# audit-verifier's own dedup pass still catches (a false accepted duplicate is never caught: it silently
# drops the finding).
function Get-CandidateLocationText {
    param([string]$CandidateTitleAndBody)

    if ([string]::IsNullOrEmpty($CandidateTitleAndBody)) { return '' }
    $lines = $CandidateTitleAndBody -split "`r?`n"
    $kept = [System.Collections.Generic.List[string]]::new()
    $kept.Add($lines[0])

    $sectionSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$script:CandidateLocationSections, [System.StringComparer]::OrdinalIgnoreCase)
    $fieldSet = [System.Collections.Generic.HashSet[string]]::new([string[]]$script:CandidateLocationFields, [System.StringComparer]::OrdinalIgnoreCase)
    $inFence = $false
    $sectionLevel = 0          # > 0 while inside a kept '#' section
    $fieldsAllowed = $true     # bold fields count before the first heading and under '## Environment' only
    $fieldIndent = -1          # >= 0 while collecting the nested lines of a kept bold field
    $fieldUntilBlank = $false  # a bare bold pseudo-heading: collect until the next blank line or heading

    for ($i = 1; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]
        if ($line -match '^\s*(```|~~~)') {
            $inFence = -not $inFence
            if ($sectionLevel -gt 0) { $kept.Add($line) }
            continue
        }
        $heading = if ($inFence) { $null } else { [regex]::Match($line, '^(?<hashes>#{1,6})\s+(?<name>.+?)\s*#*\s*$') }
        if ($heading -and $heading.Success) {
            $fieldIndent = -1
            $fieldUntilBlank = $false
            $level = $heading.Groups['hashes'].Value.Length
            if ($sectionLevel -gt 0 -and $level -gt $sectionLevel) { $kept.Add($line); continue }
            $name = ($heading.Groups['name'].Value -replace '[*_]', '' -replace '\s*\([^)]*\)\s*$', '' -replace ':\s*$', '' -replace '\s+', ' ').Trim()
            $sectionLevel = if ($sectionSet.Contains($name)) { $level } else { 0 }
            $fieldsAllowed = $name -eq 'Environment'
            continue
        }
        if ($sectionLevel -gt 0) { $kept.Add($line); continue }
        if ($inFence -or -not $fieldsAllowed) { continue }

        $indent = $line.Length - $line.TrimStart().Length
        if ($fieldIndent -ge 0) {
            if ($fieldUntilBlank) {
                if ([string]::IsNullOrWhiteSpace($line)) { $fieldIndent = -1; $fieldUntilBlank = $false }
                else { $kept.Add($line); continue }
            }
            elseif (-not [string]::IsNullOrWhiteSpace($line) -and $indent -gt $fieldIndent) { $kept.Add($line); continue }
            else { $fieldIndent = -1 }
        }
        $field = [regex]::Match($line, '^\s*(?:[-*+]\s+)?\*\*(?<name>[^*]+?)\s*:?\s*\*\*\s*:?\s*(?<rest>.*)$')
        if ($field.Success -and $fieldSet.Contains($field.Groups['name'].Value.Trim())) {
            $rest = $field.Groups['rest'].Value
            $kept.Add($rest)
            $fieldIndent = $indent
            $fieldUntilBlank = [string]::IsNullOrWhiteSpace($rest)
        }
    }
    return ($kept -join "`n")
}

# Tests one of the finding's file anchors against a candidate's LOCATION text (see Get-CandidateLocationText).
# #1393 narrows what counts as the same file, because a file name alone is often generic ('sagas', 'README',
# 'index') and the old whole-word stem search matched a directory segment ('src/Encina.Messaging/Sagas/...'
# for docs/messaging/sagas.md, audit #16's docs-1 vs #1343) or a stray word in prose. A bare file name is
# never enough either: 'README.md' or 'sagas.md' says nothing about WHICH such file is meant, and every store
# exists once per provider under the same name ('OutboxStoreADO.cs' in the SqlServer, PostgreSQL and MySQL
# packages), so a bare name would let a finding about one provider "duplicate" a bug in another. A match is:
#   1. the full path, verbatim (case-insensitive);
#   2. the path without its root ('Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs'), standing alone rather than
#      as the end of some other path -- the same file cited with its root left out; or
#   3. the full path as one of the expansions of a brace pattern in the candidate
#      ('src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/{...,Sagas/SagaStoreADO,...}.cs', #1170's writing style,
#      where the literal path never appears). The pattern is expanded completely, so a group that names only
#      another provider does not match.
# Shared by Test-DuplicateEvidence (#1400: requires every anchor) and Test-PartialDuplicateEvidence.
function Test-FileAnchorMatch {
    param([pscustomobject]$FileAnchor, [string]$Candidate)

    if ([string]::IsNullOrEmpty($Candidate)) { return $false }
    if ($Candidate.IndexOf($FileAnchor.FullPath, [System.StringComparison]::OrdinalIgnoreCase) -ge 0) { return $true }
    if ($FileAnchor.RootlessPath -and
        [regex]::IsMatch($Candidate, '(?<![\w./\\-])' + [regex]::Escape($FileAnchor.RootlessPath) + '(?![\w-])', 'IgnoreCase')) { return $true }
    foreach ($pattern in [regex]::Matches($Candidate, '[\w./\\-]*(?:\{[^{}\s`]*\}[\w./\\-]*)+')) {
        foreach ($expanded in (Expand-BracePattern $pattern.Value)) {
            $normalized = $expanded -replace '\\', '/'
            if ([string]::Equals($normalized, $FileAnchor.FullPath, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
            if ($FileAnchor.RootlessPath -and [string]::Equals($normalized, $FileAnchor.RootlessPath, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
        }
    }
    return $false
}

# Expands a shell-style brace pattern ('a/{b,c}/{d,e}.cs' -> a/b/d.cs, a/b/e.cs, a/c/d.cs, a/c/e.cs), one
# non-nested group at a time, left to right. Capped at about 512 expansions so a pathological candidate cannot
# make the duplicate check slow: once the pending and finished expansions together pass the cap, the loop
# stops and a pattern that large may return few or no expansions at all (no match, the strict direction).
function Expand-BracePattern {
    param([string]$Pattern)

    $results = [System.Collections.Generic.List[string]]::new()
    $pending = [System.Collections.Generic.Queue[string]]::new()
    $pending.Enqueue($Pattern)
    while ($pending.Count -gt 0 -and ($results.Count + $pending.Count) -le 512) {
        $current = $pending.Dequeue()
        $group = [regex]::Match($current, '\{([^{}]*)\}')
        if (-not $group.Success) { $results.Add($current); continue }
        $prefix = $current.Substring(0, $group.Index)
        $suffix = $current.Substring($group.Index + $group.Length)
        foreach ($option in $group.Groups[1].Value.Split(',')) { $pending.Enqueue($prefix + $option + $suffix) }
    }
    return , $results
}

# #1393: the candidate's own evidence for one symbol anchor -- exact, case-insensitive equality with one of the
# backtick-delimited tokens of its LOCATION text (see Test-DuplicateEvidence for why exact equality, not a
# substring), or, for a plain identifier ('OpenConnectionAsync', 'IChoreographyStateStore'), a standalone
# occurrence in the candidate's TITLE, where issues name the symbol without backticks. "Standalone" means not
# preceded by a word character, '.' or '<' and not followed by a word character, '<', '(' or a '.member' --
# the same longer-token guard the backtick comparison gives.
function Test-SymbolAnchorMatch {
    param([string]$Symbol, [System.Collections.Generic.HashSet[string]]$CandidateTokens, [string]$CandidateTitle)

    if ($CandidateTokens.Contains($Symbol)) { return $true }
    if ($Symbol -notmatch '^[A-Za-z_][\w]*(?:\.[A-Za-z_]\w*)*$' -or [string]::IsNullOrEmpty($CandidateTitle)) { return $false }
    return [regex]::IsMatch($CandidateTitle, '(?<![\w.<])' + [regex]::Escape($Symbol) + '(?![\w<(]|\.\w)', 'IgnoreCase')
}

# The backtick-delimited tokens of a candidate's location text, as a case-insensitive set.
function Get-CandidateTokenSet {
    param([string]$LocationText)

    $set = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($m in [regex]::Matches($LocationText, '`([^`]+)`')) {
        $tok = $m.Groups[1].Value.Trim()
        if (-not [string]::IsNullOrWhiteSpace($tok)) { [void]$set.Add($tok) }
    }
    return , $set
}

# #1388 decision 1, narrowed by #1400 decision 1: a model-named duplicate is accepted only when EVERY file
# anchor of the finding's own leading location clause (see Get-LeadingLocationText) AND at least one SYMBOL
# anchor both appear in the candidate's LOCATION text. A finding with no file anchor or no symbol anchor at
# all can never be auto-accepted (a Minor citation-only finding, for instance, commonly has no backticked
# symbol). Before #1400, ANY one file anchor was enough, which is exactly how audit #16's code finding 3 was
# wrongly accepted as a duplicate of #1343: #1343 covers `SagaRunner.cs`, but the finding's own leading clause
# also names `SagaOrchestrator.cs`, a second location #1343 never mentions.
#
# #1393: the candidate must be ABOUT the finding's location and symbol, not merely mention them. Both kinds of
# anchor are looked up only in the candidate's title and location sections (Get-CandidateLocationText), never
# in its Description, Root Cause, Proposed Fix, Additional Context, Related Issues or example text; the file
# side uses Test-FileAnchorMatch's narrower rules; and the symbol pool no longer holds folders, line references
# or house-rule tokens (Get-FindingAnchors). Audit #16 verification pass 5 is the reproduction: 16-code-2 was
# accepted as a duplicate of #1393 (which cites `InstrumentedSagaStore.cs` and `EncinaError.Message` only as
# an example in its Description), 16-docs-1 of #1343 and 16-docs-4 of #1299 (a generic `src/` token plus a
# 'sagas'/'README' stem found in a directory segment or prose), and 16-docs-2 of #592 (a 'sagas' stem in prose;
# #592 covers `IChoreographyStateStore`, one of the three missing pieces docs-2 names, so it is only partially
# related -- Test-PartialDuplicateEvidence).
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
# The file side is a substring search under Test-FileAnchorMatch's rules (not exact-token equality) because
# file citations are rarely wrapped in a candidate's OWN backtick token the same way (see the brace-expansion
# case there).
function Test-DuplicateEvidence {
    param([string]$FindingText, [string]$CandidateTitleAndBody)

    $anchors = Get-FindingAnchors $FindingText
    if ($anchors.FileAnchors.Count -eq 0 -or $anchors.SymbolAnchors.Count -eq 0) { return $false }

    $candidate = if ($null -eq $CandidateTitleAndBody) { '' } else { $CandidateTitleAndBody }
    $title = ($candidate -split "`r?`n", 2)[0]
    $location = Get-CandidateLocationText $candidate

    foreach ($fa in $anchors.FileAnchors) {
        if (-not (Test-FileAnchorMatch $fa $location)) { return $false }
    }

    $candidateTokens = Get-CandidateTokenSet $location
    foreach ($sa in $anchors.SymbolAnchors) {
        if (Test-SymbolAnchorMatch $sa $candidateTokens $title) { return $true }
    }
    return $false
}

# #1400 decision 1, widened by #1393 decision 3 and #1572: decides which search candidates
# audit-draft-remediation.ps1 -Prepare lists as "partially related" for a finding that is not a duplicate (the
# candidate's location text covers at least one of the finding's own file anchors OR one of its specific
# symbol anchors, but not enough for a duplicate) versus "possibly related" (a search hit with no anchor
# matched at all, listed for awareness only). A candidate that matches only part of a multi-anchor finding is therefore "partially
# related", never a duplicate: audit #16's docs-2 names `IChoreographyEventBus`, `IChoreographyStateStore` and
# the missing registration surface, and #592 covers only `IChoreographyStateStore`. Uses the same location
# text and generic-token exclusions as Test-DuplicateEvidence, so a mention in a candidate's Description or a
# shared house-rule quote never makes it "partially related" either. Never used to accept a duplicate; a
# finding with zero file anchors is never "partially" related to anything by definition.
function Test-PartialDuplicateEvidence {
    param([string]$FindingText, [string]$CandidateTitleAndBody)

    $anchors = Get-FindingAnchors $FindingText
    if ($anchors.FileAnchors.Count -eq 0) { return $false }

    $candidate = if ($null -eq $CandidateTitleAndBody) { '' } else { $CandidateTitleAndBody }
    $title = ($candidate -split "`r?`n", 2)[0]
    $location = Get-CandidateLocationText $candidate
    foreach ($fa in $anchors.FileAnchors) {
        if (Test-FileAnchorMatch $fa $location) { return $true }
    }
    $candidateTokens = Get-CandidateTokenSet $location
    foreach ($sa in $anchors.SymbolAnchors) {
        if (Test-SymbolAnchorMatch $sa $candidateTokens $title) { return $true }
    }
    return $false
}

# #1424 decision 2: makes duplicate-vs-new deterministic for a given finding and a given set of open candidates
# (since #1572 no model takes part in that decision at all; the history below explains why it is evidence-only). Audit #16 verification pass 4 found finding 16-code-4
# (the real SagaStoreADO OpenConnectionAsync no-op) classified as "duplicate of #1170" in one run and drafted as
# new in the very next run, with #1170 unchanged in between, because the previous logic only ran
# Test-DuplicateEvidence on the ONE candidate the model happened to name that run -- when the model's own reply
# varied (it is not itself deterministic), so did the classification.
#
# This runs Test-DuplicateEvidence against EVERY candidate the duplicate search returned (never only the one the
# model named), using each candidate's own real title+body text. When one or more candidates pass, the finding
# is a duplicate of the LOWEST-numbered passing candidate -- a fixed, order-independent tie-break -- so the same
# finding against the same open issues always classifies the same way, whatever order `gh issue list` happened
# to return them in. Returns that candidate's number as a plain numeric string, or $null when no candidate
# passes at all.
function Find-DuplicateAmongCandidates {
    param([string]$FindingText, [object[]]$Candidates)

    $passing = [System.Collections.Generic.List[int]]::new()
    foreach ($c in $Candidates) {
        if ($null -eq $c -or [string]::IsNullOrWhiteSpace([string]$c.Number)) { continue }
        if (Test-DuplicateEvidence $FindingText $c.TitleAndBody) { [void]$passing.Add([int]$c.Number) }
    }
    if ($passing.Count -eq 0) { return $null }
    return [string]($passing | Sort-Object)[0]
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

# #1492 decision 2: the allowed-set rule now applies to every '#n' reference anywhere in the draft body, not
# only a labelled 'Related Issues' section or (for a bug_report.md-routed draft) '## Additional Context' --
# audit #17 pass 4 found a draft citing #1372 (an unrelated package-count issue) in prose the model wrote
# elsewhere in the draft, which the previous, section-scoped version of this function never looked at (history:
# #1400/#1424/#1428 built up the section-scoped version this replaces -- the H2 header, the bold-bullet
# convention, the plain 'Related Issues:' line, and the bug-only Additional Context scan; all four shapes are
# still sanitized correctly, because they are just prose the global scan below also covers).
#
# Scans every line of the whole draft, skipping a fenced code block (Code Sample, Stack Trace -- a stack trace
# or C# sample is never free prose) and the header HTML comment block (title/labels/milestone/kind), for a
# '#n' reference. A reference survives only when its number is:
#   - the audited issue itself ($IssueNumber) -- every draft cites "#$IssueNumber (This issue)" as a standing
#     convention;
#   - named in the finding's own text ($FindingText); or
#   - named in one of the script's own "partially related" note lines for this finding ($ScriptNoteLines,
#     Test-PartialDuplicateEvidence's "- #m - partially related" lines from the -Prepare manifest -- these are
#     already anchor-checked before they ever reach this function, so they are trusted evidence, unlike a bare
#     search candidate).
# A disallowed reference is removed with Remove-InlineIssueReference, which strips only the '#n' token itself
# (and a bare enclosing "(...)"/"(see ...)" wrapper when the reference is the wrapper's only content), leaving
# the rest of the line -- a Related Issues bullet or a sentence of prose alike -- readable, rather than
# dropping the whole line as the section-scoped version used to for a labelled Related Issues bullet. The
# global scan also covers a bug_report.md draft's own Additional Context section, labelled or not. Returns the
# sanitized draft text and the list of removed numbers, so the caller can report them ("removed unverified
# related issue #n").
function Limit-RelatedIssues {
    param(
        [string]$DraftText,
        [string]$IssueNumber,
        [string]$FindingText,
        [string[]]$ScriptNoteLines
    )

    $text = if ($null -eq $DraftText) { '' } else { $DraftText }
    $lines = @($text -split "`r?`n")

    $allowed = [System.Collections.Generic.HashSet[string]]::new()
    if (-not [string]::IsNullOrWhiteSpace($IssueNumber)) { [void]$allowed.Add($IssueNumber.TrimStart('#')) }
    if ($null -ne $FindingText) {
        foreach ($m in [regex]::Matches($FindingText, '#(\d+)')) { [void]$allowed.Add($m.Groups[1].Value) }
    }
    foreach ($noteLine in $ScriptNoteLines) {
        if ($null -eq $noteLine) { continue }
        foreach ($m in [regex]::Matches($noteLine, '#(\d+)')) { [void]$allowed.Add($m.Groups[1].Value) }
    }

    # #1550 review (PR #1550, MAJOR, and its own second review round): the whole-bullet drop below must fire ONLY
    # inside an actual Related Issues region -- Limit-RelatedIssues scans the WHOLE draft body on purpose (#1492
    # decision 2), and a prose bullet elsewhere (Additional Context, Root Cause, Proposed Fix) that merely starts
    # with a disallowed reference, e.g. "- #1502 already fixed a similar regex escape issue; apply the same
    # pattern here." or "- **#1330** -- similar pattern.", is supporting evidence, not a Related Issues list item;
    # dropping it whole would silently delete real content the model wrote elsewhere in the draft. Recognizes
    # exactly the three conventions Add-RelatedIssuesLine already does, reusing its own three regexes rather than
    # inventing a fourth: the '## Related Issues' H2 (region = every line until the next '##' heading), the
    # '- **Related Issues**:' bold-bullet field, and the plain 'Related Issues:' line.
    #
    # The two field conventions' own region is bounded by INDENTATION, not merely "any following bullet line",
    # because the second review round of #1550 found the first version's looser rule (any contiguous run of
    # bullet/blank lines) still swallowed a real, unrelated prose bullet that happened to follow the field's own
    # sub-bullets with no separating heading: '- **Related Issues**:\n  - #18 (This issue)\n- #1502 already
    # reported this...' lost the whole '#1502' sentence, because nothing distinguished it from a legitimate
    # sibling bullet of the field. The FIRST bullet line under the field header establishes the list's own
    # indentation; the region then extends through every following bullet (or blank) line whose indentation is at
    # least that much, and stops as soon as one is LESS indented (a sibling bullet of some OTHER field, back at
    # the header's own level) -- covering both real shapes seen: an indented sub-bullet list (the real 16-code-5
    # draft's own two-space-indented sub-bullets) and #1400's own adversarial-review case of unindented sibling
    # bullets at the SAME level as the header (indentation 0 throughout, so the "at least" rule still keeps every
    # one of them in-region), while a bullet at a genuinely LOWER indentation than the field's own list is no
    # longer assumed to belong to it.
    $inRelatedRegion = [bool[]]::new($lines.Count)
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^##\s*Related Issues\s*$') {
            for ($j = $i + 1; $j -lt $lines.Count -and $lines[$j] -notmatch '^##\s'; $j++) { $inRelatedRegion[$j] = $true }
            continue
        }
        $isFieldHeader = ($lines[$i] -match '^\s*-?\s*\*\*Related Issues\*\*:?\s*$') -or
            ($lines[$i] -match '(?i)^\s*\*{0,2}Related Issues\*{0,2}:?\s*$')
        if (-not $isFieldHeader) { continue }
        $listIndent = -1
        for ($j = $i + 1; $j -lt $lines.Count; $j++) {
            if ($lines[$j] -match '^#{1,6}\s') { break }
            if ([string]::IsNullOrWhiteSpace($lines[$j])) { $inRelatedRegion[$j] = $true; continue }
            $bulletIndentMatch = [regex]::Match($lines[$j], '^(?<indent>\s*)[-*+]\s')
            if (-not $bulletIndentMatch.Success) { break }
            $bulletIndent = $bulletIndentMatch.Groups['indent'].Value.Length
            if ($listIndent -lt 0) { $listIndent = $bulletIndent }
            elseif ($bulletIndent -lt $listIndent) { break }
            $inRelatedRegion[$j] = $true
        }
    }

    $removed = [System.Collections.Generic.List[string]]::new()
    $newLines = [System.Collections.Generic.List[string]]::new()
    $inFence = $false
    $inHeaderComment = $false
    for ($lineIdx = 0; $lineIdx -lt $lines.Count; $lineIdx++) {
        $line = $lines[$lineIdx]
        if ($line -match '^\s*(```|~~~)') { $inFence = -not $inFence; $newLines.Add($line); continue }
        if (-not $inHeaderComment -and $line.Contains('<!--')) { $inHeaderComment = $true }
        $skip = $inFence -or $inHeaderComment
        if ($inHeaderComment -and $line.Contains('-->')) { $inHeaderComment = $false }
        if ($skip) { $newLines.Add($line); continue }

        $lineNumbers = @([regex]::Matches($line, '#(\d+)') | ForEach-Object { $_.Groups[1].Value })
        if ($lineNumbers.Count -eq 0) { $newLines.Add($line); continue }

        # #1535: inside a Related Issues region, a list item whose OWN leading/sole reference is a disallowed
        # '#n' -- the bullet is ABOUT that issue ('- #910 - [TEST] title', '- **#910**: title', '- #910: title',
        # or a bare '- #910' with no other text) -- is dropped in full, not just token-stripped. Token-only
        # removal (Remove-InlineIssueReference below) leaves a broken list item for these shapes:
        # '- - [TEST] title' (doubled marker, no link), '-: ...' (dangling colon) or a bare '-' (audit #18's
        # 18-tests-1..4, 18-docs-13/14 and 18-docs-5). The bullet marker (with optional bold '**' wrapping the
        # reference) must be immediately followed by the '#n' -- a reference elsewhere in the bullet's own prose
        # ('- Fixed in #910 for the edge case') is not "about" #910 and keeps the token-only removal below.
        $bulletMatch = [regex]::Match($line, '^(?<prefix>\s*[-*+]\s+)\*{0,2}#(?<num>\d+)\*{0,2}(?<rest>.*)$')
        if ($inRelatedRegion[$lineIdx] -and $bulletMatch.Success -and -not $allowed.Contains($bulletMatch.Groups['num'].Value)) {
            foreach ($num in $lineNumbers) {
                if (-not $allowed.Contains($num)) { $removed.Add($num) }
            }
            continue
        }

        $cleaned = $line
        foreach ($num in $lineNumbers) {
            if ($allowed.Contains($num)) { continue }
            $cleaned = Remove-InlineIssueReference $cleaned $num
            $removed.Add($num)
        }
        $newLines.Add($cleaned)
    }

    # #1535: when every bullet of a '## Related Issues' section was dropped above, leave "None." rather than a
    # bare header with no content -- an empty section is as broken as a dangling bullet.
    for ($i = 0; $i -lt $newLines.Count; $i++) {
        if ($newLines[$i] -notmatch '^##\s*Related Issues\s*$') { continue }
        $sectionEnd = $newLines.Count
        for ($j = $i + 1; $j -lt $newLines.Count; $j++) {
            if ($newLines[$j] -match '^##\s') { $sectionEnd = $j; break }
        }
        $hasContent = $false
        for ($j = $i + 1; $j -lt $sectionEnd; $j++) {
            if (-not [string]::IsNullOrWhiteSpace($newLines[$j])) { $hasContent = $true; break }
        }
        if ($hasContent) { break }

        $rebuilt = [System.Collections.Generic.List[string]]::new()
        for ($j = 0; $j -le $i; $j++) { $rebuilt.Add($newLines[$j]) }
        $rebuilt.Add('')
        $rebuilt.Add('None.')
        for ($j = $sectionEnd; $j -lt $newLines.Count; $j++) { $rebuilt.Add($newLines[$j]) }
        $newLines = $rebuilt
        break
    }

    return [pscustomobject]@{ Text = ($newLines -join "`n"); Removed = @($removed) }
}

# #1492: removes one disallowed '#n' reference from a line for Limit-RelatedIssues, leaving the rest of the
# sentence readable (decision 2) instead of dropping the whole line. Collapses a bare "(#n)" or "(see #n)"
# wrapper (case-insensitive 'see') entirely when the reference is the wrapper's only content, drops a bare
# "see #n" with no parentheses, and otherwise removes just the '#n' token; then tidies the leftover
# spacing/punctuation a removal can leave behind (a double space, a space before a comma or period, an empty
# "()" pair, a run of stray commas).
function Remove-InlineIssueReference {
    param([string]$Line, [string]$Number)

    $ref = '#' + [regex]::Escape($Number) + '(?!\d)'
    $result = [regex]::Replace($Line, '\(\s*[Ss]ee\s+' + $ref + '\s*\)', '')
    if ($result -eq $Line) { $result = [regex]::Replace($Line, '\(\s*' + $ref + '\s*\)', '') }
    if ($result -eq $Line) { $result = [regex]::Replace($Line, '(?i)\bsee\s+' + $ref, '') }
    if ($result -eq $Line) { $result = [regex]::Replace($Line, $ref, '') }

    $result = $result -replace '\(\s*\)', ''
    $result = $result -replace '[ \t]{2,}', ' '
    $result = $result -replace '[ \t]+([.,;:!?])', '$1'
    $result = $result -replace '(,\s*){2,}', ', '
    return $result.TrimEnd()
}

# #1492 decision 1: the template's own '## Type' checkbox (technical_debt.md is the only routed template that
# has one -- bug_report.md has none, test_implementation.md has 'Test Category' instead, a different section
# with different values) is decided deterministically from the finding's ORIGINATING STAGE and, for a
# code-stage finding, the kind the draft was routed to (the remediation-drafter's own 'kind:' header line since
# #1572) -- never left to the drafter's tick, which with the local model re-rolled a different box on every
# regeneration (audit #17 passes 3 and 4: a duplicate-test-classes finding ticked "Documentation gap"
# once, and a stale .vscode/tasks.json label ticked "Incorrect implementation" while its siblings ticked
# "Documentation gap"):
#   - a docs-stage finding always ticks "Documentation gap" (it IS a documentation gap by definition of the
#     stage that found it);
#   - a tests-stage finding ticks "Missing tests", unless its own text talks about duplicating, consolidating
#     or refactoring existing tests (the audit #17 pass-3 case), in which case it ticks "Refactoring needed"
#     instead -- a duplicate-test-classes finding is a refactor of existing tests, not a gap in coverage;
#   - a code-stage finding uses the draft's routed kind: "bug" never reaches this function in practice
#     (bug_report.md has no '## Type' section, so a bug-kind finding is never routed to technical_debt.md);
#     "docs" (a code-stage finding the drafter routed as documentation drift, e.g. a stale label or
#     comment) ticks "Documentation gap"; "debt" ticks "Code quality (warnings, analyzers)" (the template's own
#     exact label -- Set-DebtType matches by equality, so a shorter string ticks nothing), unless the finding's
#     own text is about a stale piece of text -- a label, comment or string literal that no longer matches the
#     code -- in which case it ticks "Documentation gap" too (the audit #17 pass-4 case: a stale label in
#     .vscode/tasks.json).
function Get-DeterministicDebtType {
    param([string]$Stage, [string]$Kind, [string]$FindingText)

    $text = if ($null -eq $FindingText) { '' } else { $FindingText }
    if ($Stage -eq 'docs') { return 'Documentation gap' }
    if ($Stage -eq 'tests') {
        if ($text -match '(?i)\b(duplicate\w*|consolidat\w*|refactor\w*)\b') { return 'Refactoring needed' }
        return 'Missing tests'
    }
    # Code stage: the routed kind decides. 'bug' and 'test' never reach here -- their routed templates
    # (bug_report.md, test_implementation.md) have no '## Type' section, so audit-draft-remediation.ps1
    # -Finalize never calls Set-DebtType for them at all.
    if ($Kind -eq 'docs') { return 'Documentation gap' }
    if ($text -match '(?i)\bstale\s+(text|label|comment|string)\b') { return 'Documentation gap' }
    # The real technical_debt.md checkbox text is "Code quality (warnings, analyzers)", not a bare "Code
    # quality" -- Set-DebtType matches a checkbox's label by exact equality (after stripping bold markers), so
    # returning anything shorter here would tick nothing at all and silently clear every box in the section
    # (adversarial review of #1492: the first version of this function returned the bare label and left the
    # whole '## Type' section unticked for the single most common code-stage debt finding).
    return 'Code quality (warnings, analyzers)'
}

# #1492 decision 1: overwrites the whole '## Type' section's checkboxes with exactly one ticked box -- the
# label Get-DeterministicDebtType returned -- clearing whatever the drafter itself ticked first (or nothing, if
# it ticked none). Only technical_debt.md has a '## Type' section among the three routed templates, so this is
# only ever meaningful for a technical_debt.md-routed draft; audit-draft-remediation.ps1 -Finalize only invokes
# it for that template. Matches a checkbox line tolerant of bold markers around the label (a drafter
# sometimes emphasises its own tick); returns $DraftText unchanged when it has no '## Type' header at
# all, or when $Type is blank (defends a malformed draft/call rather than throwing, like Set-BugEnvironment
# does for '## Environment').
function Set-DebtType {
    param([string]$DraftText, [string]$Type)

    $text = if ($null -eq $DraftText) { '' } else { $DraftText }
    if ([string]::IsNullOrWhiteSpace($Type)) { return $text }
    $lines = @($text -split "`r?`n")
    $headerIdx = -1
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq '## Type') { $headerIdx = $i; break }
    }
    if ($headerIdx -lt 0) { return $text }

    $sectionEndLine = $lines.Count
    for ($i = $headerIdx + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^##\s') { $sectionEndLine = $i; break }
    }

    for ($i = $headerIdx + 1; $i -lt $sectionEndLine; $i++) {
        $m = [regex]::Match($lines[$i], '^(?<prefix>\s*-\s*\[)[ xX](?<rest>\]\s*.*)$')
        if (-not $m.Success) { continue }
        $label = (($m.Groups['rest'].Value -replace '^\]\s*', '') -replace '\*', '').Trim()
        $mark = if ([string]::Equals($label, $Type, [System.StringComparison]::OrdinalIgnoreCase)) { 'x' } else { ' ' }
        $lines[$i] = $m.Groups['prefix'].Value + $mark + $m.Groups['rest'].Value
    }

    return ($lines -join "`n")
}

# #1409: bug_report.md's own '## Environment' section asks for facts (Encina version, .NET version, OS) a
# drafter cannot observe for a finding from a static-analysis stage -- the local model copied the template's
# own bracketed placeholders verbatim, so the draft kept 'PLACEHOLDERS LEFT' forever (audit #16's 16-code-5
# draft). These three facts ARE deterministic for a code-review finding (never observed at runtime), so
# audit-draft-remediation.ps1 -Finalize calls Set-BugEnvironment to overwrite the whole '## Environment' section
# body after the fence strip but BEFORE Find-TemplatePlaceholders inspects the draft, for every
# bug_report.md-routed draft -- the drafter's own wording of these facts is never load-bearing. 'Package(s)
# Affected' is the one field the drafter knows (remediation-drafter lists only the packages the finding names),
# so it is kept when it is not itself one of the template's own bracketed placeholder shapes; otherwise it is
# derived from the finding's own first 'src/<Package>/' path, otherwise 'Not determined'.

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

# #1400 (adversarial review finding 1), widened by #1428: inserts one note line (a "partially related" line
# from the -Prepare manifest that the drafter left out; audit-draft-remediation.ps1 -Finalize adds it back
# deterministically) into a draft's own Related Issues section, recognising the SAME three conventions
# Limit-RelatedIssues does: the '## Related Issues' H2, the '- **Related Issues**:' bold-bullet convention
# (bug_report.md has no such header) and #1428's plain 'Related Issues:' line, so the note lands inside the
# section it names and inside what Limit-RelatedIssues scans. Returns the updated text and whether a section
# was found at all; when none of the three conventions is found, the text is returned unchanged so the caller
# can report it.
function Add-RelatedIssuesLine {
    param([string]$DraftText, [string]$Line)

    $text = if ($null -eq $DraftText) { '' } else { $DraftText }
    $lines = @($text -split "`r?`n")

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $insertedLine = $null
        if ($lines[$i] -match '^##\s*Related Issues\s*$') { $insertedLine = $Line }
        elseif ($lines[$i] -match '^\s*-?\s*\*\*Related Issues\*\*:?\s*$') { $insertedLine = "  $Line" }
        # #1428: the plain 'Related Issues:' form's own bullets are unindented top-level bullets (the real
        # 16-code-5 draft's own '- #16: ...' lines under it), not nested sub-bullets like the bold-bullet form,
        # so the note is inserted as-is, with no extra indent.
        elseif ($lines[$i] -match '(?i)^\s*\*{0,2}Related Issues\*{0,2}:?\s*$') { $insertedLine = $Line }
        if ($null -eq $insertedLine) { continue }

        $newLines = [System.Collections.Generic.List[string]]::new()
        for ($j = 0; $j -le $i; $j++) { $newLines.Add($lines[$j]) }
        $newLines.Add($insertedLine)
        for ($j = $i + 1; $j -lt $lines.Count; $j++) { $newLines.Add($lines[$j]) }
        return [pscustomobject]@{ Text = ($newLines -join "`n"); Found = $true }
    }

    return [pscustomobject]@{ Text = $text; Found = $false }
}

# #1491: extracts the FIRST file:line citation from a finding's own leading location clause (see
# Get-LeadingLocationText above) -- the anchor the remediation stage groups findings by, so two stages that
# flag the SAME defect draft ONE issue instead of two (audit #17 verification pass 2: docs finding 7 and code
# finding 4 both cited `src/Encina.DomainModeling/AggregateBase.cs:20`, a stale XML doc comment, and got two
# separate drafted issues for it). Only a citation carrying an explicit line number (or line range) counts: a
# file named with no line number cannot be judged "the same location" as another citation of that file, so it
# is treated the same as having no anchor at all -- decision 1's "a finding with no file anchor is never
# grouped" applies equally to a file anchor with no line. Returns $null when the leading clause has no such
# citation.
function Get-FindingLeadingAnchor {
    param([string]$FindingText)

    $text = if ($null -eq $FindingText) { '' } else { $FindingText }
    $leadingText = Get-LeadingLocationText $text
    $m = [regex]::Match($leadingText, $script:RemediationFilePattern)
    if (-not $m.Success) { return $null }
    $lineMatch = [regex]::Match($m.Value, ':(?<start>\d+)(-(?<end>\d+))?\s*$')
    if (-not $lineMatch.Success) { return $null }
    $full = (($m.Value -split ':')[0]) -replace '\\', '/'
    $start = [int]$lineMatch.Groups['start'].Value
    $end = if ($lineMatch.Groups['end'].Success) { [int]$lineMatch.Groups['end'].Value } else { $start }
    return [pscustomobject]@{ FullPath = $full; StartLine = $start; EndLine = $end }
}

# #1491 decision 1: same file (case-insensitive) AND overlapping or equal line ranges = the same location;
# different lines of the same file, or a different file entirely, are different locations. Used only by
# Group-FindingsByLocation below; a $null anchor (Get-FindingLeadingAnchor found no line-numbered citation)
# never matches anything.
function Test-SameLocationAnchor {
    param([object]$A, [object]$B)

    if ($null -eq $A -or $null -eq $B) { return $false }
    if (-not [string]::Equals($A.FullPath, $B.FullPath, [System.StringComparison]::OrdinalIgnoreCase)) { return $false }
    return ($A.StartLine -le $B.EndLine) -and ($B.StartLine -le $A.EndLine)
}

# #1491 decision 1: groups the audit's own findings (from Split-Findings, already in stage order
# code/tests/docs and within-stage order -- the order audit-draft-remediation.ps1 builds $allFindings in) by
# their leading location anchor. Each finding is compared only against the FIRST finding of each existing
# candidate group (a group's own anchor never grows to cover a later member's range), so this stays a single,
# order-stable pass rather than a transitive union-find. A finding with no leading file:line anchor always
# starts (and stays alone in) its own singleton group -- decision 1's "a finding with no file anchor is never
# grouped". Returns an ordered list of @{ Anchor; Members (ordered list of the input finding objects) }, in
# first-appearance order.
function Group-FindingsByLocation {
    param([object[]]$Findings)

    $groups = [System.Collections.Generic.List[pscustomobject]]::new()
    foreach ($f in $Findings) {
        $anchor = Get-FindingLeadingAnchor $f.Text
        $matched = $null
        if ($null -ne $anchor) {
            foreach ($g in $groups) {
                if (Test-SameLocationAnchor $g.Anchor $anchor) { $matched = $g; break }
            }
        }
        if ($null -ne $matched) {
            $matched.Members.Add($f)
        }
        else {
            $newMembers = [System.Collections.Generic.List[pscustomobject]]::new()
            $newMembers.Add($f)
            $groups.Add([pscustomobject]@{ Anchor = $anchor; Members = $newMembers })
        }
    }
    return $groups
}

# #1491 decision 2: the group's PRIMARY finding -- the one whose own text drafts the group's single
# remediation issue -- is the highest-severity member (Blocker > Major > Minor > Unknown), ties broken by the
# members' own order (stage order code/tests/docs, then within-stage order, since $Members is given in that
# order and only a STRICTLY higher rank ever replaces the current best).
function Get-GroupPrimary {
    param([object[]]$Members)

    $rank = @{ Blocker = 3; Major = 2; Minor = 1; Unknown = 0 }
    $best = $null
    $bestRank = -1
    foreach ($m in $Members) {
        $r = if ($rank.ContainsKey($m.Severity)) { $rank[$m.Severity] } else { 0 }
        if ($r -gt $bestRank) { $bestRank = $r; $best = $m }
    }
    return $best
}

# #1491 decision 2: inserts one "Reported by: <stage> <id>, <stage> <id>, ..." line right after the drafted
# template's own '## Description' header (every routed template -- bug_report.md, technical_debt.md,
# test_implementation.md -- has one), so a merged finding's draft names every stage and finding id in its
# group deterministically -- the same pattern Set-BugEnvironment/Set-DebtType use to overwrite a section after
# the drafter writes, rather than trusting the drafter to copy the manifest's reportedByLine.
# audit-draft-remediation.ps1 -Finalize only calls this when a group has more than one member. Returns the
# updated text and whether the header was found (never missing for a real draft, but defended rather than
# thrown, like the other Set-*/Add-* helpers in this file).
#
# Adversarial review of #1491: the drafter is ALSO told to write the "Reported by: ..." line at the start of
# the Description section, so a draft usually has one there already. Inserting unconditionally would then
# duplicate it. This skips past any blank line(s) right after the header and, only when the first non-blank
# line there already starts with "Reported by:" (case-insensitive), replaces it (and the blank lines before
# it) with the deterministic line instead of trusting the drafter's own wording -- never both. When no such
# line is there: insert after one blank line.
function Add-ReportedByLine {
    param([string]$DraftText, [string]$Line)

    $text = if ($null -eq $DraftText) { '' } else { $DraftText }
    $lines = @($text -split "`r?`n")
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i].Trim() -eq '## Description') {
            $newLines = [System.Collections.Generic.List[string]]::new()
            for ($j = 0; $j -le $i; $j++) { $newLines.Add($lines[$j]) }

            $k = $i + 1
            while ($k -lt $lines.Count -and [string]::IsNullOrWhiteSpace($lines[$k])) { $k++ }
            $skipThrough = if ($k -lt $lines.Count -and $lines[$k].Trim() -match '(?i)^Reported by:') { $k } else { $i }

            $newLines.Add('')
            $newLines.Add($Line)
            for ($j = $skipThrough + 1; $j -lt $lines.Count; $j++) { $newLines.Add($lines[$j]) }
            return [pscustomobject]@{ Text = ($newLines -join "`n"); Found = $true }
        }
    }
    return [pscustomobject]@{ Text = $text; Found = $false }
}

# Up to 4 search terms from a finding's text, for the `gh issue list --search` duplicate query of
# audit-draft-remediation.ps1 -Prepare: backticked identifiers or file:line citations first (the most specific
# terms a finding carries), then un-backticked path-like tokens with a known extension (a finding may cite
# 'src/A.cs:12' in plain prose, not backticks). Both kinds are reduced to a file basename; the un-backticked
# kind additionally drops the extension (a search for 'A.cs' rarely matches an issue title the way 'A'
# sometimes does).
function Get-SearchTerms {
    param([string]$Text)

    $terms = [System.Collections.Generic.List[string]]::new()
    $source = if ($null -eq $Text) { '' } else { $Text }

    foreach ($m in [regex]::Matches($source, '`([^`]+)`')) {
        $clean = ($m.Groups[1].Value -split '[:\s]')[0]
        if ([string]::IsNullOrWhiteSpace($clean)) { continue }
        $base = Split-Path -Leaf $clean
        if ($base -and ($terms -notcontains $base)) { $terms.Add($base) }
        if ($terms.Count -ge 4) { return , $terms }
    }

    foreach ($m in [regex]::Matches($source, '[\w./\\-]+\.(cs|ps1|md|json|yml|yaml|csproj|txt)(:\d+(-\d+)?)?')) {
        $stripped = ($m.Value -split ':')[0]
        $baseNoExt = [IO.Path]::GetFileNameWithoutExtension((Split-Path -Leaf $stripped))
        if ([string]::IsNullOrWhiteSpace($baseNoExt)) { continue }
        if ($terms -notcontains $baseNoExt) { $terms.Add($baseNoExt) }
        if ($terms.Count -ge 4) { break }
    }

    return , $terms
}

# The slug of a draft's file name ('<n>-<stage>-<id>-<slug>.md'): the first 8 alphanumeric words of the
# finding's text, lower-cased, at most 60 characters; 'finding' when the text has none. Deterministic, so
# -Prepare names the same draft file for the same finding on every run.
function New-Slug {
    param([string]$Text)

    $clean = if ($null -eq $Text) { '' } else { $Text -replace '[`*_#]', ' ' }
    $words = @([regex]::Matches($clean, '[A-Za-z0-9]+') | Select-Object -First 8 -ExpandProperty Value)
    $slug = ($words -join '-').ToLowerInvariant()
    if ([string]::IsNullOrWhiteSpace($slug)) { $slug = 'finding' }
    if ($slug.Length -gt 60) { $slug = $slug.Substring(0, 60).TrimEnd('-') }
    return $slug
}

# #1572: the header comment block every remediation draft starts with (open-remediation.ps1 reads title,
# labels and milestone from it; audit-draft-remediation.ps1 -Finalize also reads 'kind', the template the
# remediation-drafter routed the finding to):
#   <!--
#   title: [DEBT] ...
#   labels: technical-debt
#   milestone:
#   kind: debt
#   -->
# Returns @{ Found; Title; Labels; Milestone; Kind } from the FIRST HTML comment of the draft; Found is $false
# (every other field $null) when the draft has no comment block at all. A field absent from the block is $null;
# a field present but empty (an empty milestone) is ''.
function Get-DraftHeader {
    param([string]$DraftText)

    $text = if ($null -eq $DraftText) { '' } else { $DraftText }
    $comment = [regex]::Match($text, '(?s)<!--(?<body>.*?)-->')
    $result = [ordered]@{ Found = $comment.Success; Title = $null; Labels = $null; Milestone = $null; Kind = $null }
    if (-not $comment.Success) { return [pscustomobject]$result }
    $fields = [ordered]@{ title = 'Title'; labels = 'Labels'; milestone = 'Milestone'; kind = 'Kind' }
    foreach ($field in $fields.Keys) {
        $m = [regex]::Match($comment.Groups['body'].Value, "(?im)^[ \t]*$field[ \t]*:[ \t]*(?<value>[^\r\n]*?)[ \t]*\r?$")
        if ($m.Success) { $result[$fields[$field]] = $m.Groups['value'].Value }
    }
    return [pscustomobject]$result
}

# #1572: the '## ' headers of the routed template that are missing from the draft, or present but out of the
# template's order. A draft must keep every template header verbatim and in order (AGENTS.md §11); extra
# headers between them (bug_report.md's optional '## Root Cause') are allowed. Headers inside a fenced code
# block are ignored on both sides. Returns the offending template headers (an empty list means the draft
# keeps them all, in order).
function Find-MissingTemplateHeaders {
    param([string]$TemplateText, [string]$DraftText)

    function Get-H2Lines([string]$Markdown) {
        $result = [System.Collections.Generic.List[string]]::new()
        $inFence = $false
        foreach ($line in (($Markdown -replace '(?s)^---.*?---\r?\n', '') -split "`r?`n")) {
            if ($line -match '^\s*(```|~~~)') { $inFence = -not $inFence; continue }
            if (-not $inFence -and $line -match '^##\s+\S') { $result.Add($line.TrimEnd()) }
        }
        return , $result
    }

    $templateHeaders = Get-H2Lines $(if ($null -eq $TemplateText) { '' } else { $TemplateText })
    $draftHeaders = Get-H2Lines $(if ($null -eq $DraftText) { '' } else { $DraftText })
    $missing = [System.Collections.Generic.List[string]]::new()
    $cursor = 0
    foreach ($header in $templateHeaders) {
        $found = -1
        for ($i = $cursor; $i -lt $draftHeaders.Count; $i++) {
            if ($draftHeaders[$i] -ceq $header) { $found = $i; break }
        }
        if ($found -lt 0) { $missing.Add($header); continue }
        $cursor = $found + 1
    }
    return , $missing
}

# #1548: whether a failed `gh` call's output describes a transient failure worth retrying. A rate limit is
# transient even when GitHub reports it as HTTP 403 (its secondary rate limit does); any other HTTP 4xx
# (401, 403, 404, 422, ...) and "Could not resolve to an Issue" are permanent; a TLS handshake timeout, a
# network timeout, a connection reset or refused, an unexpected EOF and an HTTP 5xx are transient. Anything
# not recognised is treated as permanent (fail fast, never loop on an unknown error).
function Test-GhTransientFailure {
    param([string]$Output)

    $text = if ($null -eq $Output) { '' } else { $Output }
    if ($text -match '(?i)rate limit|HTTP 429\b|abuse detection') { return $true }
    if ($text -match '(?i)HTTP 4\d\d\b|Could not resolve to an? ') { return $false }
    if ($text -match '(?i)HTTP 5\d\d\b|\b50[0-9] (Internal Server Error|Bad Gateway|Service Unavailable|Gateway Timeout)\b') { return $true }
    if ($text -match '(?i)TLS handshake timeout|handshake failure|i/o timeout|timed? ?out|connection reset|connection refused|unexpected EOF|\bEOF\b|no such host|temporary failure') { return $true }
    return $false
}

# #1548: every `gh` call of the remediation stage goes through this helper. It runs `gh @Arguments`; on a
# non-zero exit whose output Test-GhTransientFailure classifies as transient, it waits $DelaysSeconds[k] and
# retries, up to $DelaysSeconds.Count retries (default 3 retries after 5, 15 and 45 seconds); a permanent
# failure is returned at once, never retried. Returns @{ Success; Stdout; Output; ExitCode; Attempts }: Stdout
# holds only the standard-output lines (what a caller parses as JSON), Output the standard output and standard
# error together (what a caller reports). $Sleep is the wait itself, a parameter only so Test-Hooks.ps1 can
# record the delays without waiting for them.
function Invoke-GhWithRetry {
    param(
        [Parameter(Mandatory)][string[]]$Arguments,
        [int[]]$DelaysSeconds = @(5, 15, 45),
        [scriptblock]$Sleep = { param([int]$Seconds) Start-Sleep -Seconds $Seconds }
    )

    $attempt = 0
    while ($true) {
        $attempt++
        $global:LASTEXITCODE = 0
        $raw = @(& gh @Arguments 2>&1)
        $code = $LASTEXITCODE
        $stdout = @($raw | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] } | ForEach-Object { [string]$_ })
        $all = @($raw | ForEach-Object { [string]$_ })
        $result = [pscustomobject]@{ Success = ($code -eq 0); Stdout = ($stdout -join "`n"); Output = ($all -join "`n"); ExitCode = $code; Attempts = $attempt }
        if ($result.Success) { return $result }
        $retryIndex = $attempt - 1
        if ($retryIndex -ge $DelaysSeconds.Count -or -not (Test-GhTransientFailure $result.Output)) { return $result }
        [Console]::Error.WriteLine("gh $($Arguments[0]) $($Arguments[1]): transient failure (attempt $attempt, exit $code); retrying in $($DelaysSeconds[$retryIndex]) s.")
        & $Sleep $DelaysSeconds[$retryIndex]
    }
}
