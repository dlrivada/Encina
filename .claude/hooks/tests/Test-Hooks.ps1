# Regression suite for the hooks in .claude/hooks. Feeds each case to the hook the way Claude Code does (the
# hook input JSON on stdin, the frontmatter arguments on the command line) and compares the exit code
# (0 = allowed, 2 = blocked) and, where a case says so, the JSON the hook prints on stdout (warnings, Stop
# decisions).
#
# Usage: pwsh -NoProfile -File <repo>/.claude/hooks/tests/Test-Hooks.ps1        (exit code 1 when any case fails)

$ErrorActionPreference = 'Stop'
$hooks = Split-Path -Parent $PSScriptRoot
$repo = Split-Path -Parent (Split-Path -Parent $hooks)
$attribution = Join-Path $hooks 'block-ai-attribution.ps1'
$issue = Join-Path $hooks 'check-issue-template.ps1'
$publish = Join-Path $hooks 'block-worker-publish.ps1'
$spawn = Join-Path $hooks 'block-worker-spawn.ps1'
$mainCheckout = Join-Path $hooks 'block-main-checkout-writes.ps1'
$prohibited = Join-Path $hooks 'block-prohibited-commands.ps1'
$ownership = Join-Path $hooks 'enforce-path-ownership.ps1'
$gate = Join-Path $hooks 'require-specialists.ps1'
$orchestrator = Join-Path $hooks 'guard-orchestrator-writes.ps1'
$auditGuard = Join-Path $hooks 'audit-stage-guard.ps1'
$noBg = Join-Path $hooks 'no-background-specialists.ps1'

$work = Join-Path ([IO.Path]::GetTempPath()) "encina-hook-tests-$PID"
$sub = Join-Path $work 'sub dir'
New-Item -ItemType Directory -Force $sub | Out-Null
$env:CLAUDE_PROJECT_DIR = $repo

# #1410: two isolated fake repository roots for check-issue-template's local-draft evidence checks, so those
# tests never read or write the real worktree's own artifacts/ folder. Both copy the real .github/ISSUE_TEMPLATE
# so the header/order checks still run against the real templates.
$issueRoot = Join-Path $work 'IssueRepo'
New-Item -ItemType Directory -Force (Join-Path $issueRoot '.github') | Out-Null
Copy-Item -Recurse -Force (Join-Path $repo '.github/ISSUE_TEMPLATE') (Join-Path $issueRoot '.github/ISSUE_TEMPLATE')
New-Item -ItemType Directory -Force (Join-Path $issueRoot 'artifacts/local-ai/out') | Out-Null
New-Item -ItemType Directory -Force (Join-Path $issueRoot 'artifacts/knowledge/remediation') | Out-Null

$optOutRoot = Join-Path $work 'OptOutRepo'
New-Item -ItemType Directory -Force (Join-Path $optOutRoot '.github') | Out-Null
Copy-Item -Recurse -Force (Join-Path $repo '.github/ISSUE_TEMPLATE') (Join-Path $optOutRoot '.github/ISSUE_TEMPLATE')

# Assembled so this file never contains the literal patterns the attribution hook blocks.
$trailer = 'Co-Authored-By: ' + 'Claude Opus <noreply@' + 'anthropic.com>'
$generated = 'Generated with ' + '[Claude Code](https://claude.com/claude-code)'

Set-Content (Join-Path $work 'msg-bad.txt') "fix: thing`n`n$trailer"
Set-Content (Join-Path $work 'msg-ok.txt') "fix: thing`n`nPlain body."
Set-Content (Join-Path $sub 'msg-bad.txt') "fix: thing`n`n$trailer"

$debtCore = @'
## Type
- [x] Code smell
## Description
d
## Location
l
## Current Behavior
c
## Expected Behavior
e
## Root Cause
r
## Proposed Fix
p
## Priority
- [x] Medium
## Effort Estimate
- [x] Small
## Related Issues
- #1
'@
# #1410: every pre-existing $debt-derived fixture now needs local-draft evidence too, since it is passed with
# --body-file; a logged opt-out is the simplest fixture-wide fix and does not depend on any ledger/candidate
# root. $debtCore (no opt-out line) is kept separately for the #1410 remediation-draft fixture below, whose
# content must match a real draft file byte-for-byte once headers are stripped.
$debt = "<!-- local-draft: none, reason: hook test fixture, not a drafting task -->`n$debtCore"
Set-Content (Join-Path $work 'debt-ok.md') $debt
Set-Content (Join-Path $work 'debt-missing.md') ($debt -replace '## Root Cause\r?\nr\r?\n', '')
Set-Content (Join-Path $work 'debt-order.md') ($debt -replace '## Type', '## TMP' -replace '## Related Issues', '## Type' -replace '## TMP', '## Related Issues')
Set-Content (Join-Path $work 'debt-case.md') ($debt -replace '## Expected Behavior', '## Expected behavior')
Set-Content (Join-Path $work 'debt-quoted.md') ($debt -replace '(## Description\r?\n)d', "`$1d`n``````md`n## Related Issues`n``````")
Set-Content (Join-Path $work 'free.md') "## Summary`nx`n## Proposed fix`ny"
# A line starting with ``` whose info string contains a backtick is inline code, not a fence (CommonMark).
Set-Content (Join-Path $work 'debt-infostring.md') ($debt -replace '(## Description\r?\n)d', "`$1`````` inline ``code`` ``````")
Set-Content (Join-Path $work 'free-fenced.md') "Free form.`n``````md`n$($debt)`n``````"

# #1410 local-draft evidence fixtures (all under $issueRoot / $work; $env:CLAUDE_PROJECT_DIR is swapped to
# $issueRoot only while $localDraftCases runs below, so Get-RepoRoot's fallback resolves there).
Set-Content (Join-Path $issueRoot 'artifacts/local-ai/out/case-a.md') 'drafted by the local model (case a, pointer target)'
Set-Content (Join-Path $issueRoot 'artifacts/local-ai/out/case-g.md') 'drafted by the local model (case g, no ledger line)'
Set-Content (Join-Path $issueRoot 'artifacts/local-ai/out/case-h.md') 'drafted by the local model (case h, stale ledger line)'
Set-Content (Join-Path $issueRoot 'artifacts/local-ai/out/case-b.md') $debtCore

$ledgerNow = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
$ledgerStale = (Get-Date).ToUniversalTime().AddHours(-48).ToString('yyyy-MM-ddTHH:mm:ssZ')
Set-Content (Join-Path $issueRoot 'artifacts/local-ai/ledger.csv') @"
timestampUtc,task,promptTokens,completionTokens,seconds,tokensPerSecond,outFile
$ledgerNow,case-a,10,20,1.0,20.0,artifacts/local-ai/out/case-a.md
$ledgerNow,case-b,10,20,1.0,20.0,artifacts/local-ai/out/case-b.md
$ledgerStale,case-h,10,20,1.0,20.0,artifacts/local-ai/out/case-h.md
"@

Set-Content (Join-Path $work 'draft-pointer-a.md') "<!-- local-draft: artifacts/local-ai/out/case-a.md -->`n$debtCore"
Set-Content (Join-Path $work 'draft-pointer-g.md') "<!-- local-draft: artifacts/local-ai/out/case-g.md -->`n$debtCore"
Set-Content (Join-Path $work 'draft-pointer-h.md') "<!-- local-draft: artifacts/local-ai/out/case-h.md -->`n$debtCore"
# A distinct body (not byte-for-byte $debtCore) so it can never accidentally content-match the #1410
# remediation-draft fixture below and pass evidence it does not actually have.
Set-Content (Join-Path $work 'debt-no-evidence.md') ($debtCore -replace '(?m)^d\r?$', 'no local-draft evidence at all')
Set-Content (Join-Path $work 'debt-optout-empty.md') "<!-- local-draft: none, reason:  -->`n$debtCore"
Set-Content (Join-Path $work 'debt-optout.md') "<!-- local-draft: none, reason: hook test opt-out -->`n$debtCore"

# Mirrors tools/ai/audit/open-remediation.ps1: a draft under artifacts/knowledge/remediation/ carries a
# '<!-- title: ...; labels: ...; milestone: ... -->' header the script strips before writing a $env:TEMP copy
# and calling `gh issue create --body-file` on THAT copy -- never the ledger outFile itself, so this fixture's
# temp body is built the same way (the exact regex check-issue-template.ps1 also uses), not hand-duplicated.
$remediationTitle = '[DEBT] Remediation finding x'
$remediationDraft = @"
<!--
title: $remediationTitle
labels: technical-debt
milestone:
-->

$debtCore
"@
Set-Content -LiteralPath (Join-Path $issueRoot 'artifacts/knowledge/remediation/9001-code-1-foo.md') -Value $remediationDraft
$remediationBody = [regex]::Replace($remediationDraft, '(?s)^\s*<!--.*?-->\s*', '').Trim()
$remediationTempBody = Join-Path $work 'rem-9001-code-1-foo.md'
Set-Content -LiteralPath $remediationTempBody -Value $remediationBody

$cases = @(
    # hook, tool, command, expected, label
    @($attribution, 'PowerShell', "git commit -m `"fix: x`" -m `"$trailer`"", 2, 'inline trailer'),
    @($attribution, 'PowerShell', 'git commit -F msg-bad.txt', 2, 'trailer in -F file'),
    @($attribution, 'PowerShell', 'git commit -F msg-ok.txt', 0, 'clean -F file'),
    @($attribution, 'PowerShell', 'git commit -m "fix: plain"', 0, 'clean inline commit'),
    @($attribution, 'PowerShell', "gh pr create --title t --body `"x`n`n$generated`"", 2, 'generated-with line in PR body'),
    @($attribution, 'PowerShell', 'gh pr create --title t --body-file msg-bad.txt', 2, 'trailer in PR body file'),
    @($attribution, 'PowerShell', 'gh pr edit 5 --body-file msg-ok.txt', 0, 'clean PR body file'),
    @($attribution, 'PowerShell', "git log --grep `"$trailer`"", 0, 'git log is not a commit'),
    @($attribution, 'PowerShell', 'dotnet build', 0, 'unrelated command'),
    @($attribution, 'PowerShell', "git --no-pager commit -m `"$trailer`"", 2, 'git --no-pager commit'),
    @($attribution, 'PowerShell', "git -c a.b=1 -c c.d=2 commit -m `"$trailer`"", 2, 'git -c k=v twice'),
    @($attribution, 'PowerShell', "git -C `"sub dir`" commit -m `"$trailer`"", 2, 'git -C quoted dir'),
    @($attribution, 'PowerShell', "git.exe commit -m `"$trailer`"", 2, 'git.exe'),
    @($attribution, 'PowerShell', "gh pr merge 5 --squash --body `"$trailer`"", 2, 'gh pr merge --body'),
    @($attribution, 'PowerShell', "git commit -m `"feat: x`" --trailer `"Co-authored-by=Claude`"", 2, '--trailer with ='),
    @($attribution, 'PowerShell', "gh pr create -t t --body `"Generated by Claude Code`"", 2, 'generated by'),
    @($attribution, 'PowerShell', "git commit -m `"Assisted-by: Claude`"", 2, 'assisted-by trailer'),
    @($attribution, 'PowerShell', 'git -C "sub dir" commit -F msg-bad.txt', 2, 'relative -F under git -C'),
    @($attribution, 'PowerShell', 'Set-Location "sub dir"; git commit -F msg-bad.txt', 2, 'relative -F after Set-Location'),
    @($attribution, 'PowerShell', 'git commit --template=msg-bad.txt', 2, '--template file'),
    @($attribution, 'PowerShell', "Select-String -Path CLAUDE.md -Pattern `"$trailer`"; git commit -m `"docs: tidy`"", 0, 'pattern in another statement'),
    @($attribution, 'Bash', "git commit -m `"`$(cat <<'EOF'`nfix: x`n`n$trailer`nEOF`n)`"", 2, 'bash heredoc message'),
    @($attribution, 'Bash', "git commit -m `"`$(cat <<'EOF'`nfix: \`"quoted\`" (paren`nEOF`n)`"", 0, 'bash heredoc clean with quotes and paren'),
    @($attribution, 'PowerShell', "git commit -m @`"`nfix: x`n`n$trailer`n`"@", 2, 'PowerShell here-string message'),
    @($attribution, 'PowerShell', 'not json', 0, 'malformed payload'),
    @($attribution, 'PowerShell', "git comm``it -m `"$trailer`"", 2, 'backtick-escaped verb'),
    @($attribution, 'PowerShell', "git commit ``-m `"$trailer`"", 2, 'backtick-escaped option'),
    @($attribution, 'PowerShell', "git commit ```n  -m `"$trailer`"", 2, 'line continuation'),
    @($attribution, 'PowerShell', "& `"git`" commit -m `"$trailer`"", 2, 'call operator and quoted executable'),
    @($attribution, 'PowerShell', "`"gh`" pr create -t t -b `"$trailer`"", 2, 'quoted gh executable'),
    @($attribution, 'PowerShell', "& 'C:\Program Files\Git\cmd\git.exe' commit -m `"$trailer`"", 2, 'full path to git.exe'),
    @($attribution, 'PowerShell', "Write-Output gh pr create -b `"$trailer`"", 0, 'gh only as an argument'),
    @($attribution, 'PowerShell', "Write-Output git commit -m `"$trailer`"", 0, 'git only as an argument'),
    @($attribution, 'PowerShell', "`$out = git commit -m `"$trailer`"", 2, 'assignment'),
    @($attribution, 'PowerShell', "Write-Output (git commit -m `"$trailer`")", 2, 'inside a subexpression'),
    @($attribution, 'Bash', "GIT_AUTHOR_NAME=x git commit -m `"$trailer`"", 2, 'Bash environment prefix'),
    @($attribution, 'PowerShell', 'git commit -Fmsg-bad.txt', 2, 'attached short -F value'),
    @($attribution, 'PowerShell', 'git commit --file=msg-bad.txt', 2, '--file= form'),
    @($attribution, 'PowerShell', 'git commit "-F" msg-bad.txt', 2, 'quoted option name'),
    @($attribution, 'PowerShell', "git commit -m `"fix: x`" # $trailer", 0, 'trailer only in a PowerShell comment'),
    @($attribution, 'PowerShell', "git commit -m `"fix: x #1`" -m `"$trailer`"", 2, '# inside a quoted message is not a comment'),
    @($attribution, 'Bash', "git commit -m `"`$(cat <<'EOF'`nfix: x`n# heading`n`n$trailer`nEOF`n)`"", 2, '# line inside a heredoc is not a comment'),
    # M2: shell wrappers; m8: Bash ANSI-C strings.
    @($attribution, 'PowerShell', "pwsh -NoProfile -Command `"git commit -m 'fix: x' -m '$trailer'`"", 2, 'commit inside pwsh -Command'),
    @($attribution, 'Bash', "bash -c `"git commit -m 'fix: x' -m '$trailer'`"", 2, 'commit inside bash -c'),
    @($attribution, 'Bash', "git commit -m `$'fix: x\n\n$trailer'", 2, 'trailer in a Bash ANSI-C message'),
    @($attribution, 'Bash', "git commit -m `$'fix: it\'s done\n\nPlain body.'", 0, 'clean Bash ANSI-C message with an escaped quote'),

    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body-file debt-ok.md', 0, 'DEBT complete'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body-file debt-missing.md', 2, 'DEBT missing Root Cause'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body-file debt-order.md', 2, 'DEBT out of order'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body-file free.md', 2, 'free-form body'),
    @($issue, 'PowerShell', 'gh issue create --title "[TECH-DEBT] x" --body-file debt-ok.md', 2, 'non-normalised prefix'),
    @($issue, 'PowerShell', 'gh issue create --title "No prefix" --body-file debt-ok.md', 2, 'no prefix'),
    @($issue, 'PowerShell', "gh issue create --title `"[DEBT] x`" --body `"$($debt -replace '"', '')`"", 0, 'inline body complete'),
    @($issue, 'PowerShell', "gh issue create --title '[DEBT] x' --body-file debt-ok.md", 0, 'single-quoted title'),
    @($issue, 'PowerShell', 'gh issue create --title $title --body-file $f', 0, 'variables'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body $body', 0, 'inline body from variable'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body (Get-Content b.md -Raw)', 0, 'inline body from subexpression'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body "$(cat b.md)"', 0, 'inline body from $()'),
    @($issue, 'PowerShell', 'gh issue create --repo stryker-mutator/stryker-net --title "Crash on xUnit v3" --body-file free.md', 0, 'other repository'),
    @($issue, 'PowerShell', 'gh issue create -R dlrivada/Encina --title "No prefix" --body-file debt-ok.md', 2, 'explicit Encina repository'),
    @($issue, 'PowerShell', "Select-String -Path x.md -Pattern 'gh issue create --title `"x`"'", 0, 'mentioned inside a string'),
    @($issue, 'PowerShell', 'git checkout -b fix/x; gh issue create --title "[DEBT] x" --template "Technical Debt"', 0, '--template after another statement'),
    @($issue, 'PowerShell', 'git commit -F msg-ok.txt && gh issue create --title "[DEBT] x" --body-file debt-ok.md', 0, '-F of an earlier git statement'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body "Free form. Repro: git commit -F msg.txt fails"', 2, '-F inside the body text'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body-file free-fenced.md', 2, 'headers only inside a fence'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body-file debt-quoted.md', 0, 'fenced header quoted in a valid body'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body-file debt-case.md', 2, 'header case differs'),
    @($issue, 'PowerShell', 'gh issue list --label bug', 0, 'not issue create'),
    @($issue, 'PowerShell', 'gh issue create "--title" "No prefix" --body-file debt-ok.md', 2, 'quoted --title'),
    @($issue, 'PowerShell', 'gh issue create --title="No prefix" --body-file debt-ok.md', 2, '--title= with quoted value'),
    @($issue, 'PowerShell', 'gh issue create -t"No prefix" -Fdebt-ok.md', 2, 'attached short options'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" -Ffree.md', 2, 'attached -F with a free-form body'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body "-F debt-ok.md"', 2, '-F as the body value'),
    @($issue, 'PowerShell', '& "gh" issue create --title "No prefix" --body-file debt-ok.md', 2, 'quoted gh with call operator'),
    @($issue, 'PowerShell', 'Write-Output gh issue create --title "No prefix" --body-file debt-ok.md', 0, 'gh only as an argument'),
    @($issue, 'PowerShell', '$r = (gh issue create --title "No prefix" --body-file debt-ok.md)', 2, 'assignment of a subexpression'),
    @($issue, 'PowerShell', "gh iss``ue create --title `"No prefix`" --body-file debt-ok.md", 2, 'backtick-escaped verb'),
    @($issue, 'PowerShell', 'gh issue create --title "[DEBT] x" --body-file debt-infostring.md', 0, 'backtick in fence info string is not a fence'),
    @($issue, 'PowerShell', 'not json', 0, 'malformed payload'),
    @($issue, 'PowerShell', "pwsh -c `"gh issue create --title 'No prefix' --body-file debt-ok.md`"", 2, 'issue create inside pwsh -c'),

    @($publish, 'PowerShell', 'git push', 2, 'git push'),
    @($publish, 'PowerShell', 'git -C dir push origin x', 2, 'git -C dir push'),
    @($publish, 'PowerShell', 'gh pr create --title t --body-file msg-ok.txt', 2, 'gh pr create'),
    @($publish, 'PowerShell', 'gh issue comment 5 -b x', 2, 'gh issue comment'),
    @($publish, 'PowerShell', 'gh api -X POST repos/o/r/issues', 2, 'gh api -X POST'),
    @($publish, 'PowerShell', 'git commit -m x', 0, 'git commit is allowed'),
    @($publish, 'PowerShell', 'gh pr view 5', 0, 'gh pr view is allowed'),
    @($publish, 'PowerShell', 'gh issue view 5', 0, 'gh issue view is allowed'),
    @($publish, 'PowerShell', 'gh api repos/o/r/pulls/5', 0, 'gh api GET is allowed'),
    @($publish, 'PowerShell', 'git log', 0, 'git log is allowed'),
    @($publish, 'PowerShell', 'git -c alias.publish=push publish', 2, 'git -c alias.publish=push publish'),
    @($publish, 'PowerShell', 'git config alias.p push', 2, 'git config alias.p push'),
    @($publish, 'PowerShell', 'git p', 2, 'git p (undefined alias, not on the allowlist)'),
    @($publish, 'PowerShell', 'git send-pack origin', 2, 'git send-pack'),
    @($publish, 'PowerShell', 'gh api repos/o/r/issues/5/comments -f body=x', 2, 'gh api -f defaults to POST'),
    @($publish, 'PowerShell', 'gh api repos/o/r/issues --input body.json', 2, 'gh api --input defaults to POST'),
    @($publish, 'PowerShell', 'git status', 0, 'git status is allowed'),
    @($publish, 'PowerShell', 'git worktree list', 0, 'git worktree list is allowed'),
    @($publish, 'PowerShell', 'git -C "dir x" log', 0, 'git -C quoted dir log is allowed'),
    @($publish, 'PowerShell', 'gh api -X GET repos/o/r/pulls -f state=open', 0, 'gh api explicit GET with fields is allowed'),
    @($publish, 'PowerShell', 'gh api repos/o/r/pulls/5', 0, 'gh api with no method or fields is allowed'),
    @($publish, 'PowerShell', 'git clone https://github.com/dlrivada/Encina.git C:\temp\x', 0, 'git clone is allowed'),
    @($publish, 'PowerShell', 'git -C D:\x clone ../repo target', 0, 'git -C dir clone is allowed'),
    @($publish, 'PowerShell', 'pwsh -NoProfile -Command "git push origin x"', 2, 'git push inside pwsh -Command'),
    @($publish, 'PowerShell', 'powershell -c "git -C x push"', 2, 'git push inside powershell -c'),
    @($publish, 'Bash', "bash -lc 'git push'", 2, 'git push inside bash -lc'),
    @($publish, 'PowerShell', 'pwsh -Command "git status"', 0, 'git status inside pwsh -Command is allowed')
)

# -Agent argument, payload agent_type, subagent_type, expected, label
$spawnCases = @(
    @('issue-worker', $null, 'issue-worker', 2, 'issue-worker: issue-worker is blocked'),
    @('issue-worker', $null, 'general-purpose', 2, 'issue-worker: general-purpose is blocked'),
    @('issue-worker', $null, $null, 2, 'issue-worker: missing subagent_type is blocked'),
    @('issue-worker', $null, 'claude', 2, 'issue-worker: unknown subagent_type is blocked'),
    @('issue-worker', $null, 'ci-diagnoser', 0, 'issue-worker: ci-diagnoser is allowed'),
    @('issue-worker', $null, 'mechanical-fixer', 0, 'issue-worker: mechanical-fixer is allowed'),
    @('issue-worker', $null, 'Explore', 0, 'issue-worker: Explore is allowed'),
    @('issue-worker', $null, 'adversarial-reviewer', 0, 'issue-worker: adversarial-reviewer is allowed (self-review)'),
    @('issue-worker', $null, 'docs-writer', 0, 'issue-worker: docs-writer is allowed (documentation)'),
    @('issue-worker', $null, 'docs-reviewer', 0, 'issue-worker: docs-reviewer is allowed (#1345: the audit pipeline docs stage)'),
    @('issue-worker', $null, 'Adversarial-Reviewer', 2, 'issue-worker: subagent_type is case-sensitive'),
    @('docs-writer', $null, 'mechanical-fixer', 0, 'docs-writer: mechanical-fixer is allowed'),
    @('docs-writer', $null, 'docs-reviewer', 0, 'docs-writer: docs-reviewer is allowed (self-review)'),
    @('docs-writer', $null, 'Explore', 0, 'docs-writer: Explore is allowed'),
    @('docs-writer', $null, 'adversarial-reviewer', 2, 'docs-writer: adversarial-reviewer is blocked'),
    @('docs-writer', $null, 'docs-writer', 2, 'docs-writer: docs-writer is blocked (recursion)'),
    @('docs-writer', $null, $null, 2, 'docs-writer: missing subagent_type is blocked'),
    @('mechanical-fixer', $null, 'ci-diagnoser', 0, 'mechanical-fixer: ci-diagnoser is allowed'),
    @('mechanical-fixer', $null, 'Explore', 0, 'mechanical-fixer: Explore is allowed'),
    @('mechanical-fixer', $null, 'mechanical-fixer', 2, 'mechanical-fixer: mechanical-fixer is blocked (recursion)'),
    @('mechanical-fixer', $null, 'issue-worker', 2, 'mechanical-fixer: issue-worker is blocked'),
    # #1382: site-steward may spawn only ci-diagnoser.
    @('site-steward', $null, 'ci-diagnoser', 0, 'site-steward: ci-diagnoser is allowed'),
    @('site-steward', $null, 'mechanical-fixer', 2, 'site-steward: mechanical-fixer is blocked'),
    @('site-steward', $null, 'general-purpose', 2, 'site-steward: general-purpose is blocked'),
    @('site-steward', $null, $null, 2, 'site-steward: missing subagent_type is blocked'),
    # #1447: pr-reviewer may spawn only Explore.
    @('pr-reviewer', $null, 'Explore', 0, 'pr-reviewer: Explore is allowed'),
    @('pr-reviewer', $null, 'mechanical-fixer', 2, 'pr-reviewer: mechanical-fixer is blocked'),
    @('pr-reviewer', $null, 'general-purpose', 2, 'pr-reviewer: general-purpose is blocked'),
    @('pr-reviewer', $null, $null, 2, 'pr-reviewer: missing subagent_type is blocked'),
    @($null, 'issue-worker', 'general-purpose', 2, 'no -Agent: agent_type from the hook input'),
    @('issue-worker', 'docs-writer', 'docs-reviewer', 0, 'agent_type of the input wins over -Agent (inherited hook)'),
    @($null, $null, 'general-purpose', 0, 'no agent known: not restricted (fail open)'),
    @('claude-code-guide', $null, 'general-purpose', 0, 'agent without an allowlist: not restricted'),
    @('pr-watcher', $null, 'general-purpose', 2, 'pr-watcher: read-only specialist never delegates (#1345)'),
    @('issue-archivist', $null, 'mechanical-fixer', 2, 'issue-archivist: no delegation (stage agent, #1345)'),
    @('issue-auditor', $null, 'mechanical-fixer', 2, 'issue-auditor: no delegation (stage agent, #1345)'),
    @('test-auditor', $null, 'mechanical-fixer', 2, 'test-auditor: no delegation (stage agent, #1345)'),
    @('audit-verifier', $null, 'mechanical-fixer', 2, 'audit-verifier: no delegation (stage agent, #1345)'),
    @('docs-reviewer', $null, 'mechanical-fixer', 2, 'docs-reviewer: read-only specialist never delegates (#1345)'),
    @('adversarial-reviewer', $null, 'mechanical-fixer', 2, 'adversarial-reviewer: read-only specialist never delegates (#1345)'),
    @('ci-diagnoser', $null, 'mechanical-fixer', 2, 'ci-diagnoser: read-only specialist never delegates (#1345)'),
    # M1: the orchestrator's allowlist (settings.json, -Agent orchestrator).
    @('orchestrator', $null, 'issue-worker', 0, 'orchestrator: issue-worker is allowed'),
    @('orchestrator', $null, 'docs-reviewer', 0, 'orchestrator: docs-reviewer is allowed'),
    @('orchestrator', $null, 'pr-watcher', 0, 'orchestrator: pr-watcher is allowed'),
    @('orchestrator', $null, 'site-steward', 0, 'orchestrator: site-steward is allowed (#1382)'),
    @('orchestrator', $null, 'pr-reviewer', 0, 'orchestrator: pr-reviewer is allowed (#1447)'),
    @('orchestrator', $null, 'Plan', 0, 'orchestrator: Plan is allowed'),
    @('orchestrator', $null, 'claude-code-guide', 0, 'orchestrator: claude-code-guide is allowed'),
    @('orchestrator', $null, 'general-purpose', 0, 'orchestrator: general-purpose is allowed (covered by guard-orchestrator-writes)'),
    @('orchestrator', $null, $null, 0, 'orchestrator: missing subagent_type is general-purpose, allowed'),
    @('orchestrator', $null, 'claude', 2, 'orchestrator: claude is blocked'),
    @('orchestrator', $null, 'statusline-setup', 2, 'orchestrator: statusline-setup is blocked'),
    @('orchestrator', $null, 'searchfit-seo:seo-auditor:AGENT', 2, 'orchestrator: plugin agent is blocked'),
    @('orchestrator', 'general-purpose', 'claude', 2, 'ungoverned subagent gets the orchestrator allowlist'),
    @('orchestrator', 'general-purpose', 'Explore', 0, 'ungoverned subagent: Explore is allowed'),
    @('orchestrator', 'issue-worker', 'general-purpose', 2, 'project hook in an issue-worker applies its own allowlist')
)

# block-main-checkout-writes.ps1 runs against a fake project: $main is the main checkout, $wt a worktree.
$main = Join-Path $work 'Encina'
$wt = Join-Path $main '.claude\worktrees\w1'
$outside = Join-Path $work 'outside'
function ConvertTo-Msys([string]$Path) { '/' + $Path.Substring(0, 1).ToLowerInvariant() + $Path.Substring(2).Replace('\', '/') }
$msysMain = ConvertTo-Msys $main
$msysWt = ConvertTo-Msys $wt

# Scripts for the `dotnet run <file>.cs` / `pwsh -File <file>.ps1` bypass cases (#1181): the hook reads these
# files' own text, so their content (not the invoking command) is what the tests exercise.
$scriptWritesSrc = Join-Path $work 'script-writes-src.cs'
$scriptWritesDocs = Join-Path $work 'script-writes-docs.cs'
$scriptWritesTestsPs1 = Join-Path $work 'script-writes-tests.ps1'
$scriptWritesDocsPs1 = Join-Path $work 'script-writes-docs.ps1'
$scriptMissing = Join-Path $work 'script-missing.cs'
Set-Content $scriptWritesSrc 'File.WriteAllText("src/x.cs", "y");'
Set-Content $scriptWritesDocs 'File.WriteAllText("docs/x.md", "y");'
Set-Content $scriptWritesTestsPs1 "Set-Content 'tests/x.cs' 'y'"
Set-Content $scriptWritesDocsPs1 "Set-Content 'docs/x.md' 'y'"

# tool, tool_input, cwd, expected, label[, CLAUDE_PROJECT_DIR (default $main)[, environment overrides[, stdout regex]]]
$warned = 'additionalContext'
$writeCases = @(
    @('Write', @{ file_path = "$main\src\x.cs" }, $wt, 2, 'Write into the main checkout'),
    @('Write', @{ file_path = "$wt\src\x.cs" }, $main, 0, 'Write into a worktree'),
    @('Edit', @{ file_path = "$($main.Replace('\', '/'))/src/x.cs" }, $wt, 2, 'Edit with forward slashes into the main checkout'),
    @('Edit', @{ file_path = "$($main.ToUpperInvariant())\SRC\X.CS" }, $wt, 2, 'Edit into the main checkout with different case'),
    @('Edit', @{ file_path = "$($main.Replace('\', '/'))\src/x.cs" }, $wt, 2, 'Edit with mixed separators into the main checkout'),
    @('Write', @{ file_path = "$($wt.ToUpperInvariant())\x.md" }, $main, 0, 'Write into a worktree with different case'),
    @('Write', @{ file_path = "$($wt.Replace('\', '/'))/docs/x.md" }, $main, 0, 'Write into a worktree with forward slashes'),
    @('Write', @{ file_path = "$outside\x.md" }, $main, 0, 'Write outside the project (temp)'),
    @('NotebookEdit', @{ notebook_path = "$main\n.ipynb" }, $wt, 2, 'NotebookEdit into the main checkout'),
    @('Write', @{ file_path = "$wt\..\..\..\src\x.cs" }, $wt, 2, 'Write escaping the worktree with ..'),
    @('Write', @{ file_path = "$main\.claude\settings.json" }, $wt, 2, 'Write into the main .claude folder'),
    @('Write', @{ file_path = "$($main)2\x.cs" }, $wt, 0, 'sibling folder sharing the prefix'),
    @('Write', @{ file_path = 'src/x.cs' }, $main, 2, 'relative Write path from the main checkout'),
    @('Write', @{ file_path = "$main\src\x.cs" }, $wt, 2, 'project dir is a worktree, target in main', $wt),
    @('Write', @{ file_path = "$wt\src\x.cs" }, $wt, 0, 'project dir is a worktree, target in it', $wt),
    @('Read', @{ file_path = "$main\src\x.cs" }, $wt, 0, 'Read is not a write'),

    @('PowerShell', @{ command = "[IO.File]::WriteAllText('docs/x.txt', 'a')" }, $main, 2, 'relative [IO.File] write from the main checkout'),
    @('PowerShell', @{ command = "[System.IO.File]::AppendAllText(`"docs\x.txt`", 'a')" }, $main, 2, 'relative [System.IO.File] append from the main checkout'),
    @('PowerShell', @{ command = "[IO.File]::WriteAllText('docs/x.log', 'a')" }, $wt, 0, 'relative [IO.File] write from a worktree'),
    @('PowerShell', @{ command = "Set-Location '$wt'; [IO.File]::WriteAllText('x.log', 'a')" }, $main, 2, '[IO.File] ignores Set-Location'),
    @('PowerShell', @{ command = '[IO.File]::WriteAllText($path, $text)' }, $main, 0, '[IO.File] with a variable target', $null, $null, $warned),
    @('PowerShell', @{ command = 'Set-Content -Path notes.log -Value x' }, $main, 2, 'Set-Content relative in the main checkout'),
    @('PowerShell', @{ command = 'Set-Content -Path notes.log -Value x' }, $wt, 0, 'Set-Content relative in a worktree'),
    @('PowerShell', @{ command = "Set-Content '$main\notes.log' x" }, $wt, 2, 'Set-Content absolute into the main checkout'),
    @('PowerShell', @{ command = "Set-Location '$wt'; Set-Content notes.log x" }, $main, 0, 'Set-Location into a worktree first'),
    @('PowerShell', @{ command = 'Set-Location $wt; Set-Content notes.log x' }, $main, 0, 'Set-Location to a variable (unknown directory)', $null, $null, $warned),
    @('PowerShell', @{ command = "'x' | Out-File out.txt" }, $main, 2, 'Out-File relative in the main checkout'),
    @('PowerShell', @{ command = 'Get-Content a.txt > b.txt' }, $main, 2, '> redirection in the main checkout'),
    @('PowerShell', @{ command = 'Get-Content a.txt >>b.txt' }, $main, 2, '>> attached redirection in the main checkout'),
    @('PowerShell', @{ command = "Get-Content a.txt > '$outside\b.txt'" }, $main, 0, 'redirection outside the project'),
    @('PowerShell', @{ command = 'git status 2>&1; Get-ChildItem > $null' }, $main, 0, '2>&1 and > $null are not writes'),
    @('PowerShell', @{ command = "Copy-Item '$outside\a.txt' docs\a.txt" }, $main, 2, 'Copy-Item into the main checkout'),
    @('PowerShell', @{ command = "Copy-Item docs\a.txt '$outside\a.txt'" }, $main, 0, 'Copy-Item out of the main checkout'),
    @('PowerShell', @{ command = "Move-Item -Path '$wt\a.txt' -Destination '$main\a.txt'" }, $wt, 2, 'Move-Item -Destination into the main checkout'),
    @('PowerShell', @{ command = 'New-Item -ItemType Directory scratch' }, $main, 2, 'New-Item relative in the main checkout'),
    @('PowerShell', @{ command = 'New-Item -ItemType Directory -Force $dir' }, $main, 0, 'New-Item with a variable target', $null, $null, $warned),
    @('PowerShell', @{ command = 'git checkout -- src/x.cs' }, $main, 2, 'git checkout -- in the main checkout'),
    @('PowerShell', @{ command = 'git restore src/x.cs' }, $main, 2, 'git restore in the main checkout'),
    @('PowerShell', @{ command = 'git commit -m x' }, $main, 2, 'git commit in the main checkout'),
    @('PowerShell', @{ command = "git -C '$wt' restore src/x.cs" }, $main, 0, 'git -C worktree restore'),
    @('PowerShell', @{ command = "git -C '$wt' commit -m x" }, $main, 0, 'git -C worktree commit'),
    @('PowerShell', @{ command = "git -C '$main' commit -m x" }, $wt, 2, 'git -C main checkout commit'),
    @('PowerShell', @{ command = 'git status; git log --oneline -3; git diff; git stash list' }, $main, 0, 'read-only git in the main checkout'),
    @('PowerShell', @{ command = 'Get-Content src\x.cs; Get-ChildItem -Recurse src' }, $main, 0, 'read-only commands in the main checkout'),
    @('Bash', @{ command = 'echo x > notes.txt' }, $main, 2, 'Bash > in the main checkout'),
    @('Bash', @{ command = 'cp /c/tmp/a.txt notes.txt' }, $main, 2, 'Bash cp into the main checkout'),
    @('Bash', @{ command = "cd $msysWt && touch a.txt" }, $main, 0, 'Bash cd into a worktree (msys path) then touch'),
    @('Bash', @{ command = "touch $msysMain/a.txt" }, $wt, 2, 'Bash touch into the main checkout (msys path)'),
    @('PowerShell', @{ command = 'not json' }, $main, 0, 'malformed payload'),

    @('PowerShell', @{ command = "(Get-Content '$wt\src\x.cs') -replace 'a','b' | Set-Content '$wt\src\x.cs'" }, $wt, 2, '-replace piped to Set-Content on a .cs'),
    @('PowerShell', @{ command = "Set-Content -Path '$wt\docs\x.md' -Value y" }, $wt, 2, 'Set-Content on a .md in a worktree'),
    @('PowerShell', @{ command = "[IO.File]::WriteAllText('$wt\src\x.cs', `$c)" }, $wt, 2, '[IO.File] write to a .cs in a worktree'),
    @('PowerShell', @{ command = "'x' | Out-File '$wt\Directory.Build.props'" }, $wt, 2, 'Out-File on a .props'),
    @('PowerShell', @{ command = "Get-ChildItem '$wt\src' -Filter *.cs | ForEach-Object { (Get-Content `$_.FullName -Raw) -replace 'a','b' | Set-Content `$_.FullName }" }, $wt, 2, '-replace loop over *.cs with a variable target'),

    # M4: the `dotnet run <file>.cs` / `pwsh -File <file>.ps1` bypass (#1181).
    @('PowerShell', @{ command = "dotnet run '$scriptWritesSrc'" }, $main, 2, 'dotnet run of a script that writes src/ from the main checkout'),
    @('PowerShell', @{ command = "dotnet run '$scriptWritesSrc'" }, $wt, 0, 'dotnet run of the same script from a worktree'),
    @('PowerShell', @{ command = "dotnet run '$scriptWritesDocs'" }, $main, 0, 'dotnet run of a script that writes docs/ only'),
    @('PowerShell', @{ command = "dotnet run --file '$scriptWritesSrc'" }, $main, 2, 'dotnet run --file of a script that writes src/'),
    @('PowerShell', @{ command = "pwsh -File '$scriptWritesTestsPs1'" }, $main, 2, 'pwsh -File of a script that writes tests/'),
    @('PowerShell', @{ command = "dotnet run '$scriptMissing'" }, $main, 0, 'dotnet run of a script the hook cannot read: allowed for a worker'),
    # #1345: the script's own Base (after a prior Set-Location), not the tool call's raw cwd, decides whether
    # the launching statement runs from the main checkout.
    @('PowerShell', @{ command = "Set-Location '$wt'; dotnet run --file '$scriptWritesSrc'" }, $main, 0, 'Set-Location into the worktree before dotnet run --file: allowed'),
    @('PowerShell', @{ command = "Set-Location '$main'; dotnet run --file '$scriptWritesSrc'" }, $wt, 2, 'Set-Location into the main checkout before dotnet run --file: blocked'),
    @('PowerShell', @{ command = "Set-Location '$wt'; pwsh -File '$scriptWritesTestsPs1'" }, $main, 0, 'Set-Location into the worktree before pwsh -File: allowed'),
    @('PowerShell', @{ command = "Set-Location '$main'; pwsh -File '$scriptWritesTestsPs1'" }, $wt, 2, 'Set-Location into the main checkout before pwsh -File: blocked'),
    # #1345: a blocked `pwsh -File` replayed with the call operator or dot-sourcing must be seen too (#1190).
    @('PowerShell', @{ command = "& '$scriptWritesTestsPs1'" }, $main, 2, 'call operator of a script that writes tests/, from the main checkout'),
    @('PowerShell', @{ command = ". '$scriptWritesTestsPs1'" }, $main, 2, 'dot-source of a script that writes tests/, from the main checkout'),
    @('PowerShell', @{ command = "& '$scriptWritesDocsPs1'" }, $main, 0, 'call operator of a script that writes only docs/, from the main checkout'),
    @('PowerShell', @{ command = "`$f = '$wt\src\x.json'; `$t = (Get-Content `$f -Raw).Replace('a', 'b'); [IO.File]::WriteAllText(`$f, `$t)" }, $wt, 2, '.Replace( with [IO.File] to a variable, repo .json named'),
    @('PowerShell', @{ command = "Set-Content '$wt\artifacts\issues\x.md' y" }, $wt, 0, 'artifacts are not repo files'),
    @('PowerShell', @{ command = "Set-Content '$outside\body.md' y" }, $wt, 0, 'source extension outside the project'),
    @('PowerShell', @{ command = "Set-Content '$wt\notes.log' y" }, $wt, 0, 'non-source extension in a worktree'),
    @('PowerShell', @{ command = "`$c = 'x' -replace 'a','b'; Set-Content `$f `$c" }, $wt, 0, '-replace to a variable target, no source extension named'),
    @('PowerShell', @{ command = "dotnet format '$wt\Encina.slnx' --verify-no-changes" }, $wt, 0, 'dotnet format is not a text write'),
    @('Bash', @{ command = "echo x >> $msysWt/src/x.cs" }, $wt, 2, 'Bash >> to a .cs'),

    # A2: the source-file rule covers PublicAPI .txt files, .sql, .ps1, .editorconfig, .xml, .razor, .cshtml, .sh.
    @('PowerShell', @{ command = "Add-Content '$wt\src\Encina\PublicAPI.Unshipped.txt' 'X.Y() -> void'" }, $wt, 2, 'Add-Content to a PublicAPI .txt'),
    @('PowerShell', @{ command = "Set-Content '$wt\src\x.sql' y" }, $wt, 2, 'Set-Content to a .sql'),
    @('PowerShell', @{ command = "Set-Content '$wt\tools\x.ps1' y" }, $wt, 2, 'Set-Content to a .ps1'),
    @('PowerShell', @{ command = "'root = true' | Out-File '$wt\.editorconfig'" }, $wt, 2, 'Out-File to .editorconfig'),
    @('PowerShell', @{ command = "Set-Content '$wt\src\x.xml' y" }, $wt, 2, 'Set-Content to a .xml'),
    @('PowerShell', @{ command = "Set-Content '$wt\src\x.razor' y" }, $wt, 2, 'Set-Content to a .razor'),
    @('PowerShell', @{ command = "Set-Content '$wt\src\x.cshtml' y" }, $wt, 2, 'Set-Content to a .cshtml'),
    @('Bash', @{ command = "echo x > $msysWt/scripts/x.sh" }, $wt, 2, 'Bash > to a .sh'),
    @('PowerShell', @{ command = "New-Item '$wt\src\x.cs' -Value 'class X {}'" }, $wt, 2, 'New-Item -Value writes content to a .cs'),
    @('PowerShell', @{ command = "New-Item -ItemType File '$wt\src\x.cs'" }, $wt, 0, 'New-Item without -Value creates an empty file'),

    # A3: PowerShell parameter binding for Copy-Item / Move-Item.
    @('PowerShell', @{ command = "Copy-Item -Path '$outside\a.txt' docs\a.txt" }, $main, 2, 'Copy-Item -Path a b copies to b (main checkout)'),
    @('PowerShell', @{ command = "Copy-Item -Path docs\a.txt '$outside\a.txt'" }, $main, 0, 'Copy-Item -Path main b copies out of the main checkout'),
    @('PowerShell', @{ command = "Copy-Item -Destination '$outside\x' docs\a.txt" }, $main, 0, 'Copy-Item -Destination first, source positional'),
    @('PowerShell', @{ command = "Move-Item -Dest '$main\a.txt' '$wt\a.txt'" }, $wt, 2, 'Move-Item with an abbreviated -Dest'),

    # A4: -replace on strings whose write target is outside the project.
    @('PowerShell', @{ command = "`$b = (Get-Content '$outside\a.md' -Raw) -replace 'a','b'; Set-Content `"`$env:TEMP\issue-body.md`" `$b" }, $wt, 0, '-replace, target resolves to %TEMP% through $env:TEMP'),
    @('PowerShell', @{ command = "`$out = '$outside\body.md'; `$b = `$b -replace 'x','y'; Set-Content `$out `$b" }, $wt, 0, '-replace, variable target, only paths outside the project named'),
    @('PowerShell', @{ command = "Get-ChildItem -Recurse -Filter *.cs | ForEach-Object { (Get-Content `$_.FullName -Raw) -replace 'a','b' | Set-Content `$_.FullName }" }, $wt, 2, '-replace loop over *.cs in the current worktree'),
    @('PowerShell', @{ command = "`$f = '$wt\src\x.cs'; [IO.File]::WriteAllText(`$f, [regex]::Replace((Get-Content `$f -Raw), 'a', 'b'))" }, $wt, 2, '[regex]::Replace to a variable target, repo .cs named'),
    @('PowerShell', @{ command = "`$b = `$t -replace 'a','b'; Set-Content `"`$env:TEMP\x.md`" `$b; Get-Content '$wt\src\x.cs'" }, $wt, 0, '-replace, target in %TEMP%, repo file only read'),

    # A5: ~ is the user profile.
    @('PowerShell', @{ command = 'Set-Content -Path ~\notes.log -Value x' }, $wt, 2, '~ resolves to a profile inside the main checkout', $null, @{ USERPROFILE = $main; HOME = $main }),
    @('PowerShell', @{ command = 'Set-Content -Path ~\notes.log -Value x' }, $main, 0, '~ resolves to a profile outside the project', $null, @{ USERPROFILE = $outside; HOME = $outside }),
    @('Bash', @{ command = 'echo x > ~/notes.log' }, $wt, 2, 'Bash ~ inside the main checkout', $null, @{ USERPROFILE = $main; HOME = $main }),
    @('PowerShell', @{ command = 'Set-Content "$HOME\notes.log" x' }, $wt, 2, '$HOME inside the main checkout', $null, @{ USERPROFILE = $main; HOME = $main }),

    # A6: comments are not commands.
    @('PowerShell', @{ command = 'Get-Content a.txt # > b.txt' }, $main, 0, 'redirection inside a comment'),
    @('PowerShell', @{ command = "<# Set-Content notes.log x #>`ngit status" }, $main, 0, 'PowerShell block comment'),
    @('Bash', @{ command = 'git status # echo x > notes.log' }, $main, 0, 'Bash comment'),
    @('Bash', @{ command = "echo '#x' > notes.log" }, $main, 2, '# inside quotes is not a comment'),
    @('PowerShell', @{ command = "`$c = `$c -replace 'a','b'; Set-Content `$f `$c # '$wt\src\x.cs'" }, $wt, 0, 'repo path only in a comment'),

    # A8: Pop-Location, Tee-Object, more [IO.File] calls, warnings.
    @('PowerShell', @{ command = "Push-Location '$wt'; Pop-Location; Set-Content notes.log x" }, $main, 2, 'Pop-Location returns to the main checkout'),
    @('PowerShell', @{ command = "Push-Location '$main'; Pop-Location; Set-Content notes.log x" }, $wt, 0, 'Pop-Location returns to the worktree'),
    @('PowerShell', @{ command = "'x' | Tee-Object -FilePath notes.log" }, $main, 2, 'Tee-Object -FilePath in the main checkout'),
    @('PowerShell', @{ command = "'x' | Tee-Object notes.log" }, $main, 2, 'Tee-Object positional in the main checkout'),
    @('PowerShell', @{ command = "'x' | Tee-Object -Variable v" }, $main, 0, 'Tee-Object -Variable is not a file'),
    @('PowerShell', @{ command = "[IO.File]::OpenWrite('notes.log')" }, $main, 2, '[IO.File]::OpenWrite relative'),
    @('PowerShell', @{ command = "[IO.File]::Copy('$outside\a.txt', 'notes.log')" }, $main, 2, '[IO.File]::Copy destination in the main checkout'),
    @('PowerShell', @{ command = "[IO.File]::Move('notes.log', '$outside\a.txt')" }, $main, 0, '[IO.File]::Move out of the main checkout'),
    @('PowerShell', @{ command = "`$w = [IO.StreamWriter]::new('notes.log')" }, $main, 2, '[IO.StreamWriter]::new relative'),
    @('PowerShell', @{ command = 'Set-Content $p x' }, $main, 0, 'variable target from the main checkout warns', $null, $null, $warned),
    @('PowerShell', @{ command = 'Set-Content $p x' }, $wt, 0, 'variable target from a worktree does not warn', $null, $null, '^$'),

    # A7: hashtable keys are not commands (see also $commandCases).
    @('PowerShell', @{ command = "@{ head = 'x'; tail = 'y' } | ConvertTo-Json" }, $main, 0, 'hashtable keys'),

    # M2: the command of a pwsh / powershell / bash wrapper is analysed in its own shell.
    @('PowerShell', @{ command = "pwsh -Command `"(Get-Content $wt\src\a.cs) -replace 'a','b' | Set-Content $wt\src\a.cs`"" }, $wt, 2, '#1159 vector inside pwsh -Command'),
    @('Bash', @{ command = "pwsh -Command `"(Get-Content $msysWt/src/a.cs) -replace 'a','b' | Set-Content $msysWt/src/a.cs`"" }, $wt, 2, '#1159 vector inside pwsh -Command from Bash'),
    @('PowerShell', @{ command = "pwsh -NoProfile -ExecutionPolicy Bypass -c `"Set-Content '$wt\docs\x.md' y`"" }, $wt, 2, 'pwsh -c after options that take values'),
    @('PowerShell', @{ command = 'powershell -NoProfile -Command "Set-Content notes.log x"' }, $main, 2, 'powershell -Command writes to the main checkout'),
    @('PowerShell', @{ command = 'powershell "Set-Content notes.log x"' }, $main, 2, 'powershell positional command writes to the main checkout'),
    @('PowerShell', @{ command = 'pwsh -co "git commit -m x"' }, $main, 2, 'git commit inside pwsh -co (prefix of -Command)'),
    @('PowerShell', @{ command = "Set-Location '$wt'; pwsh -c `"Set-Content notes.log x`"" }, $main, 0, 'directory change before the wrapper applies to it'),
    @('PowerShell', @{ command = 'pwsh -NoProfile -File tools\x.ps1' }, $main, 0, 'pwsh -File is a script, not a command'),
    @('Bash', @{ command = "bash -lc 'echo x > notes.log'" }, $main, 2, 'bash -lc redirection into the main checkout'),
    @('PowerShell', @{ command = "sh -c 'cp /c/tmp/a.txt notes.txt'" }, $main, 2, 'sh -c cp from the PowerShell tool'),
    # m8: Bash ANSI-C strings.
    @('Bash', @{ command = "echo x > `$'$msysWt/src/x.cs'" }, $wt, 2, 'Bash > to an ANSI-C quoted .cs'),
    @('Bash', @{ command = "printf `$'it\'s' > notes.log" }, $main, 2, 'ANSI-C escaped quote does not hide the redirection'),
    # m2: more write kinds.
    @('PowerShell', @{ command = "Invoke-WebRequest https://example.com/x -OutFile '$wt\src\x.cs'" }, $wt, 2, 'Invoke-WebRequest -OutFile to a .cs'),
    @('PowerShell', @{ command = 'iwr https://example.com/x -OutFile notes.log' }, $main, 2, 'iwr -OutFile into the main checkout'),
    @('PowerShell', @{ command = "Expand-Archive '$outside\a.zip' -DestinationPath '$main\x'" }, $wt, 2, 'Expand-Archive into the main checkout'),
    @('PowerShell', @{ command = "Start-Process dotnet -RedirectStandardOutput notes.log" }, $main, 2, 'Start-Process -RedirectStandardOutput into the main checkout')
)

# tool, command, expected, label (block-prohibited-commands.ps1)
$commandCases = @(
    @('PowerShell', 'grep -r foo src', 2, 'grep'),
    @('PowerShell', 'python script.py', 2, 'python'),
    @('PowerShell', 'python3 -c "print(1)"', 2, 'python3'),
    @('PowerShell', "& 'C:\Python312\python.exe' x.py", 2, 'full path to python.exe'),
    @('PowerShell', 'curl.exe -s https://example.com', 2, 'curl.exe'),
    @('PowerShell', 'Get-Content a.txt | head -n 5', 2, 'head after a pipe'),
    @('PowerShell', 'Get-Content a.txt | tail -n 5', 2, 'tail after a pipe'),
    @('PowerShell', "Write-Output (sed 's/a/b/' x)", 2, 'sed inside a subexpression'),
    @('PowerShell', '$n = wc -l x', 2, 'wc in an assignment'),
    @('PowerShell', 'Get-ChildItem | xargs echo', 2, 'xargs'),
    @('PowerShell', "awk '{print `$1}' x", 2, 'awk'),
    @('PowerShell', 'unzip a.zip', 2, 'unzip'),
    @('PowerShell', 'bash -c "echo hi"', 2, 'bash -c'),
    @('PowerShell', 'Get-Content a.txt | Select-Object -First 5', 0, 'Select-Object -First'),
    @('PowerShell', 'git grep foo', 0, 'git grep'),
    @('PowerShell', "Select-String -Path x -Pattern 'grep'", 0, 'grep as a pattern'),
    @('PowerShell', "gh pr view 5 --json title --jq '.title'", 0, 'gh --jq'),
    @('PowerShell', 'Get-ChildItem | sort Name', 0, 'sort is Sort-Object in PowerShell'),
    @('PowerShell', 'cat a.txt; ls; tee -FilePath x', 0, 'cat, ls and tee are cmdlet aliases in PowerShell'),
    @('PowerShell', 'find "x" a.txt', 0, 'find is find.exe in PowerShell'),
    @('PowerShell', 'git commit -m "use grep and sed"', 0, 'words inside a commit message'),
    @('PowerShell', 'for ($i = 0; $i -lt 3; $i++) { $i }', 0, 'PowerShell for loop'),
    @('PowerShell', 'if (Test-Path x) { "y" }', 0, 'PowerShell if'),
    @('PowerShell', 'dotnet run x.cs -- --head 3', 0, 'head as an argument'),
    @('PowerShell', 'Write-Output "$(Get-Date)"', 0, 'PowerShell $( ) subexpression'),
    @('Bash', 'grep foo x', 2, 'Bash grep'),
    @('Bash', "find . -name '*.cs'", 2, 'Bash find'),
    @('Bash', 'cat a.txt', 2, 'Bash cat'),
    @('Bash', 'ls -la', 2, 'Bash ls'),
    @('Bash', 'git log --oneline | sort', 2, 'Bash sort'),
    @('Bash', 'echo x | tee out.txt', 2, 'Bash tee'),
    @('Bash', 'for f in *.cs; do echo $f; done', 2, 'Bash for loop'),
    @('Bash', 'if [ -f x ]; then echo y; fi', 2, 'Bash if'),
    @('Bash', 'while true; do sleep 1; done', 2, 'Bash while loop'),
    @('Bash', 'x=$(git rev-parse HEAD)', 2, 'Bash $( ) substitution'),
    @('Bash', "git commit -m `"`$(git log -1 --format=%s)`"", 2, 'Bash $( ) inside double quotes'),
    @('Bash', 'env python x.py', 2, 'python through env'),
    @('Bash', "sh -c 'echo hi'", 2, 'sh -c'),
    @('Bash', '"curl" https://example.com', 2, 'quoted curl'),
    @('Bash', "echo '`$(not a subshell)'", 0, '$( inside single quotes'),
    @('Bash', 'git status', 0, 'Bash git status'),
    @('Bash', 'dotnet build Encina.slnx', 0, 'Bash dotnet build'),
    @('Bash', 'echo "grep is text"', 0, 'grep inside a string'),
    @('Bash', "git log --format='%H' -1", 0, 'Bash git log'),
    @('Bash', 'not json', 0, 'malformed payload'),

    # A6: comments.
    @('PowerShell', 'Get-Content x # | grep foo', 0, 'grep inside a PowerShell comment'),
    @('PowerShell', "<# head -n 3 #> Get-Content x -TotalCount 3", 0, 'head inside a PowerShell block comment'),
    @('Bash', 'git status # cat x', 0, 'cat inside a Bash comment'),
    @('Bash', 'git status # $(x) and `y`', 0, 'substitutions inside a Bash comment'),
    @('Bash', 'echo "a #b" | grep a', 2, '# inside quotes does not hide the next statement'),
    @('Bash', 'echo ${#x}; cat y', 2, '${#x} is not a comment'),
    # A7: PowerShell hashtable keys.
    @('PowerShell', "@{ head = 'x'; tail = 'y'; sort = 1 } | ConvertTo-Json", 0, 'hashtable keys named like prohibited commands'),
    @('PowerShell', '$o = @{ head=1; wc=2 }', 0, 'compact hashtable keys'),
    @('PowerShell', "@{ a = 1 }; grep x y", 2, 'a real command after a hashtable'),
    # A9: more Bash constructs.
    @('Bash', 'echo `git rev-parse HEAD`', 2, 'Bash backtick substitution'),
    @('Bash', 'echo "`date`"', 2, 'Bash backticks inside double quotes'),
    @('Bash', "echo '``not run``'", 0, 'backticks inside single quotes'),
    @('Bash', 'case $x in a) echo a;; esac', 2, 'Bash case'),
    @('Bash', '[[ -f x ]] && echo y', 2, 'Bash [[ ]]'),
    @('Bash', 'test -f x && echo y', 2, 'Bash test ... &&'),
    @('Bash', '[ -f x ] || echo y', 2, 'Bash [ ]'),
    @('Bash', 'diff <(git show a:x) x', 2, 'Bash process substitution <( )'),
    @('Bash', 'git log > >(cat)', 2, 'Bash process substitution >( )'),
    @('Bash', "git log --format='<(x)'", 0, '<( inside single quotes'),
    @('Bash', 'git commit -m "see <(x)"', 0, '<( inside double quotes'),
    @('PowerShell', 'test-path x', 0, 'test is not special in PowerShell'),
    # M2: wrappers are analysed in their own shell; encoded commands are blocked.
    @('PowerShell', 'pwsh -Command "Get-Content x | grep foo"', 2, 'grep inside pwsh -Command'),
    @('Bash', 'pwsh -c "Get-Content x | Select-Object -First 3"', 0, 'PowerShell inside pwsh -c from Bash'),
    @('Bash', 'pwsh -c "cat x; ls"', 0, 'cat and ls inside pwsh -c are PowerShell aliases'),
    @('PowerShell', "powershell -NoProfile -Command `"Get-ChildItem | sort Name`"", 0, 'sort inside powershell -Command is Sort-Object'),
    @('PowerShell', 'pwsh -EncodedCommand ZQBjAGgAbwAgAHgA', 2, 'pwsh -EncodedCommand'),
    @('PowerShell', 'powershell -NoProfile -enc ZQBjAGgAbwA=', 2, 'powershell -enc'),
    @('Bash', 'pwsh -ec ZQBjAGgAbwA=', 2, 'pwsh -ec from Bash'),
    @('PowerShell', 'pwsh -e ZQBjAGgAbwA=', 2, 'pwsh -e'),
    @('PowerShell', 'pwsh -ExecutionPolicy Bypass -File x.ps1', 0, 'pwsh -ExecutionPolicy is not -EncodedCommand'),
    @('PowerShell', "pwsh -c `"Write-Output 'for x in y'`"", 0, 'for inside a PowerShell string of a wrapper'),
    # m8: Bash ANSI-C strings.
    @('Bash', "echo `$'it\'s'; grep x y", 2, 'ANSI-C escaped quote does not hide the next statement'),
    @('Bash', "echo `$'a\'`$(b)'", 0, '$( inside an ANSI-C string is not a substitution'),
    @('Bash', "echo `$'x' | cat", 2, 'cat after an ANSI-C string')
)

# A2 documentation: the extension list of block-main-checkout-writes.ps1 must match issue-worker.md.
$workerDefinition = Get-Content (Join-Path $repo '.claude\agents\issue-worker.md') -Raw
$hookSource = Get-Content $mainCheckout -Raw
$hookExtensions = ([regex]::Match($hookSource, "\`$SourceExtensions = '(?<e>[^']+)'").Groups['e'].Value -replace '\?', '') -split '\|'

# #1345: a tools/ai/audit/pipeline.json fixture under $wt so the stage-ownership (fabrication-gap) check has
# a pipeline to resolve stage -> agent from, matching the one tools/ai/audit/pipeline.json actually ships.
$ownershipPipelineJson = '{"stages":[{"stage":"archivist","agent":"issue-archivist","model":"sonnet","artifact":"archivist.md"},{"stage":"code","agent":"issue-auditor","model":"sonnet","artifact":"code.md"},{"stage":"tests","agent":"test-auditor","model":"sonnet","artifact":"tests.md"},{"stage":"docs","agent":"docs-reviewer","model":"sonnet","artifact":"docs.md"},{"stage":"remediation","agent":"local-model (script tools/ai/audit/audit-draft-remediation.ps1)","model":"qwen","artifact":"remediation.md"},{"stage":"verification","agent":"audit-verifier","model":"sonnet","artifact":"verification.md"}],"minModel":"sonnet","forbiddenModels":["haiku"],"verdictLine":"Verdict: PASS","lessonsHeading":"## Lessons for the pipeline"}'
$ownershipAuthorsPath = Join-Path $wt 'artifacts\knowledge\stages\.authors.json'
New-Item -ItemType Directory -Force (Join-Path $wt 'tools\ai\audit') | Out-Null
Set-Content (Join-Path $wt 'tools\ai\audit\pipeline.json') $ownershipPipelineJson

# agent (-Agent), tool, path, cwd, expected, label[, payload agent_type]
$ownershipCases = @(
    @('issue-worker', 'Write', "$wt\docs\en\guides\x.md", $wt, 2, 'issue-worker: docs page'),
    @('issue-worker', 'Edit', "$wt\docs\plans\x-implementation-plan-1.md", $wt, 0, 'issue-worker: docs/plans is its own'),
    @('issue-worker', 'Edit', "$wt\src\Encina.Caching\README.md", $wt, 2, 'issue-worker: package README'),
    @('issue-worker', 'Edit', "$wt\README.md", $wt, 2, 'issue-worker: root README'),
    @('issue-worker', 'Edit', "$wt\.github\CONTRIBUTING.md", $wt, 2, 'issue-worker: CONTRIBUTING'),
    @('issue-worker', 'Write', "$wt\changelog.d\1181-x.fixed.md", $wt, 2, 'issue-worker: changelog fragment'),
    @('issue-worker', 'Edit', "$wt\src\Encina\PublicAPI.Unshipped.txt", $wt, 2, 'issue-worker: PublicAPI file'),
    @('issue-worker', 'Edit', "$wt\.github\coverage-manifest\Encina.json", $wt, 2, 'issue-worker: coverage manifest'),
    @('issue-worker', 'Edit', "$wt\src\Encina\X.cs", $wt, 0, 'issue-worker: source file'),
    @('issue-worker', 'Edit', "$wt\tests\Encina.UnitTests\X\Justification.md", $wt, 0, 'issue-worker: test justification .md'),
    @('issue-worker', 'Edit', "$wt\.claude\agents\README.md", $wt, 0, 'issue-worker: agent definitions'),
    @('issue-worker', 'Write', "$outside\x.md", $wt, 0, 'issue-worker: outside the project'),
    @('issue-worker', 'Write', 'docs/x.md', $wt, 2, 'issue-worker: relative docs path'),
    @('issue-worker', 'Edit', "$($wt.ToUpperInvariant())\DOCS\X.MD", $wt, 2, 'issue-worker: docs path in upper case'),
    @('issue-worker', 'Edit', "$wt\docs\x.md", $wt, 0, 'issue-worker hook inherited by mechanical-fixer: allowed', 'mechanical-fixer'),
    @('issue-worker', 'Read', "$wt\docs\x.md", $wt, 0, 'issue-worker: Read is not a write'),
    @('docs-writer', 'Write', "$wt\docs\en\guides\x.md", $wt, 0, 'docs-writer: docs page'),
    @('docs-writer', 'Edit', "$wt\src\Encina.Caching\README.md", $wt, 0, 'docs-writer: package README'),
    @('docs-writer', 'Edit', "$wt\.github\CONTRIBUTING.md", $wt, 0, 'docs-writer: CONTRIBUTING under .github'),
    @('docs-writer', 'Edit', "$wt\changelog.d\1181-x.changed.md", $wt, 0, 'docs-writer: changelog fragment'),
    @('docs-writer', 'Edit', "$wt\src\Encina\X.cs", $wt, 2, 'docs-writer: source file'),
    @('docs-writer', 'Edit', "$wt\src\Encina\PublicAPI.Unshipped.txt", $wt, 2, 'docs-writer: PublicAPI file'),
    @('docs-writer', 'Write', "$wt\tests\Encina.UnitTests\X.cs", $wt, 2, 'docs-writer: test file'),
    @('docs-writer', 'Edit', "$wt\.github\workflows\ci.yml", $wt, 2, 'docs-writer: workflow'),
    @('mechanical-fixer', 'Edit', "$wt\changelog.d\1181-x.fixed.md", $wt, 0, 'mechanical-fixer: not restricted'),
    @($null, 'Edit', "$wt\docs\x.md", $wt, 0, 'no agent known: not restricted'),
    # M3: docs/** code and data are code, prose and images are documentation.
    @('issue-worker', 'Edit', "$wt\docs\coverage\app.js", $wt, 0, 'issue-worker: dashboard .js under docs is code'),
    @('issue-worker', 'Edit', "$wt\docs\_config.yml", $wt, 0, 'issue-worker: docs/_config.yml is code'),
    @('issue-worker', 'Edit', "$wt\docs\mutations\index.html", $wt, 0, 'issue-worker: dashboard .html is code'),
    @('issue-worker', 'Edit', "$wt\docs\coverage\data\latest.json", $wt, 0, 'issue-worker: dashboard data .json is code'),
    @('issue-worker', 'Write', "$wt\docs\images\flow.png", $wt, 2, 'issue-worker: an image a docs page shows'),
    @('issue-worker', 'Edit', "$wt\docs\en\guide.markdown", $wt, 2, 'issue-worker: .markdown page'),
    # m4: docs-writer allowlist.
    @('docs-writer', 'Edit', "$wt\docs\_config.yml", $wt, 2, 'docs-writer: docs/_config.yml'),
    @('docs-writer', 'Edit', "$wt\docs\coverage\app.js", $wt, 2, 'docs-writer: dashboard .js'),
    @('docs-writer', 'Write', "$wt\docs\images\flow.png", $wt, 0, 'docs-writer: an image for a page'),
    @('docs-writer', 'Edit', "$wt\.github\workflows\README.md", $wt, 0, 'docs-writer: a README under .github'),
    @('docs-writer', 'Edit', "$wt\tests\Encina.UnitTests\README.md", $wt, 0, 'docs-writer: a README under tests'),
    @('docs-writer', 'Edit', "$wt\.claude\agents\README.md", $wt, 2, 'docs-writer: .claude README'),
    @('docs-writer', 'Edit', "$wt\.claude\skills\encina-docs\SKILL.md", $wt, 2, 'docs-writer: a skill'),
    @('docs-writer', 'Edit', "$wt\Directory.Build.props", $wt, 2, 'docs-writer: build file'),
    @('docs-writer', 'Edit', "$wt\docs\plans\x-implementation-plan-1.md", $wt, 2, 'docs-writer: a plan belongs to the issue-worker'),
    @('docs-writer', 'Write', "$wt\artifacts\issues\gap.md", $wt, 0, 'docs-writer: its issue files under artifacts'),
    @('docs-writer', 'Edit', "$wt\CLAUDE.md", $wt, 2, 'docs-writer: CLAUDE.md'),
    # SPEC-003 DEC-005 (#1311): docs/knowledge/** moves to the issue-worker allowlist; docs-writer keeps access.
    @('issue-worker', 'Write', "$wt\docs\knowledge\issues\1311.md", $wt, 0, 'issue-worker: knowledge record'),
    @('issue-worker', 'Write', "$wt\docs\knowledge\audits\Encina.Messaging.md", $wt, 0, 'issue-worker: audit result'),
    @('docs-writer', 'Edit', "$wt\docs\knowledge\issues\1311.md", $wt, 0, 'docs-writer: knowledge record still allowed'),
    # #1345: the four SPEC-003 audit-stage agents write only under their audit worktree's artifacts/ folder.
    @('issue-archivist', 'Write', "$wt\artifacts\knowledge\issues\1345.md", $wt, 0, 'issue-archivist: its own knowledge record'),
    @('issue-archivist', 'Write', "$wt\artifacts\knowledge\stages\archivist.md", $wt, 0, 'issue-archivist: its own stage artifact'),
    @('issue-archivist', 'Edit', "$wt\src\Encina\X.cs", $wt, 2, 'issue-archivist: source file is denied'),
    @('issue-auditor', 'Write', "$wt\artifacts\knowledge\stages\code.md", $wt, 0, 'issue-auditor: its own stage artifact'),
    @('issue-auditor', 'Edit', "$wt\docs\en\guide.md", $wt, 2, 'issue-auditor: documentation is denied'),
    @('test-auditor', 'Write', "$wt\artifacts\knowledge\stages\tests.md", $wt, 0, 'test-auditor: its own stage artifact'),
    @('test-auditor', 'Edit', "$wt\tests\Encina.UnitTests\X.cs", $wt, 2, 'test-auditor: test file is denied'),
    @('audit-verifier', 'Write', "$wt\artifacts\knowledge\stages\verification.md", $wt, 0, 'audit-verifier: its own stage artifact'),
    @('audit-verifier', 'Edit', "$wt\CLAUDE.md", $wt, 2, 'audit-verifier: CLAUDE.md is denied'),
    # #1523: test-auditor and audit-verifier share one allowance beyond their own stage artifact — coverage
    # scratch under artifacts/audit/coverage/** (test-auditor.md Method step 1; audit-verifier.md Method step 5).
    # No other single-owner audit-stage agent gains anything from this prefix, and it stays out of the four
    # already-covered denials above (a test/source/doc/CLAUDE.md file).
    @('test-auditor', 'Write', "$wt\artifacts\audit\coverage\unit\x.xml", $wt, 0, 'test-auditor: coverage scratch (#1523)'),
    @('audit-verifier', 'Write', "$wt\artifacts\audit\coverage\verify-unit\x.xml", $wt, 0, 'audit-verifier: verify- coverage scratch (#1523)'),
    @('test-auditor', 'Write', "$wt\artifacts\audit\other.txt", $wt, 2, 'test-auditor: artifacts/audit outside coverage/ stays denied (#1523)'),
    @('issue-auditor', 'Write', "$wt\artifacts\audit\coverage\x.xml", $wt, 2, 'issue-auditor: coverage scratch is not its allowance (#1523)'),
    # #1345 fabrication gap: a stage artifact is written ONLY by the agent pipeline.json assigns to it —
    # never the orchestrator (no -Agent/agent_type at all) and never a different stage's agent.
    @($null, 'Write', "$wt\artifacts\knowledge\stages\code.md", $wt, 2, 'fabrication gap: the orchestrator writing code.md is denied'),
    @('test-auditor', 'Write', "$wt\artifacts\knowledge\stages\code.md", $wt, 2, 'fabrication gap: test-auditor writing code.md is denied'),
    @('issue-auditor', 'Write', "$wt\artifacts\knowledge\stages\code.md", $wt, 0, 'fabrication gap: issue-auditor writing code.md is allowed'),
    # The gap this closes is exactly a caller OTHER than the assigned agent, no matter its own identity: an
    # ungoverned agent (mechanical-fixer has no path-ownership hook of its own) fabricating the code stage via
    # the globally-wired instance of this hook (no -Agent, its agent_type from the payload; #1345 review).
    @($null, 'Write', "$wt\artifacts\knowledge\stages\code.md", $wt, 2, 'fabrication gap: mechanical-fixer (no dedicated ownership hook) writing code.md is denied by the global wiring', 'mechanical-fixer'),
    @('docs-reviewer', 'Write', "$wt\artifacts\knowledge\stages\docs.md", $wt, 0, 'docs-reviewer: its own stage artifact (audit mode)'),
    @('issue-archivist', 'Write', "$wt\artifacts\knowledge\stages\docs.md", $wt, 2, 'fabrication gap: issue-archivist writing the docs stage is denied'),
    @($null, 'Write', "$wt\artifacts\knowledge\stages\remediation.md", $wt, 2, 'the remediation stage artifact is written only by its own script, never via the Write/Edit tool, even by the orchestrator'),
    @('issue-auditor', 'Write', "$wt\artifacts\knowledge\stages\remediation.md", $wt, 2, 'fabrication gap: an agent may not fabricate the script-owned remediation stage via Write'),
    @($null, 'Write', "$wt\artifacts\knowledge\stages\lessons.md", $wt, 0, 'lessons.md is not a pipeline stage: the orchestrator writes it'),
    @('issue-auditor', 'Write', "$wt\artifacts\knowledge\stages\lessons.md", $wt, 2, 'lessons.md is denied to a stage agent'),
    # #1345 review blocker: the stage-ownership match must not evade case-insensitively (the filesystem this
    # project runs on is case-insensitive, so 'Artifacts\Knowledge\Stages\code.md' is the very same on-disk
    # file as 'artifacts\knowledge\stages\code.md').
    @($null, 'Write', "$wt\Artifacts\Knowledge\Stages\code.md", $wt, 2, 'fabrication gap: mixed-case path still matches the stage-artifact rule'),
    # #1345 review blocker: the authorship sidecar has exactly one legitimate writer (this hook's own
    # Set-Content, invoked internally when it allows a stage-artifact write) — never a tool call, not even the
    # orchestrator's, which previously fell through to the default allow.
    @($null, 'Write', "$wt\artifacts\knowledge\stages\.authors.json", $wt, 2, 'fabrication gap: the orchestrator writing .authors.json directly is denied'),
    @('issue-auditor', 'Write', "$wt\artifacts\knowledge\stages\.authors.json", $wt, 2, 'fabrication gap: a stage agent writing .authors.json directly is denied'),
    # #1466: the .rerun-archivist marker gets the same unconditional treatment as .authors.json above — its
    # only legitimate writers are tools/ai/audit/audit-done.ps1 (create) and audit-commit-stage.ps1 (remove),
    # both from inside their own script text, never through a direct Write/Edit/shell-write tool call.
    @($null, 'Write', "$wt\artifacts\knowledge\stages\.rerun-archivist", $wt, 2, 'fabrication gap (#1466): the orchestrator writing .rerun-archivist directly is denied'),
    @('issue-archivist', 'Write', "$wt\artifacts\knowledge\stages\.rerun-archivist", $wt, 2, 'fabrication gap (#1466): a stage agent writing .rerun-archivist directly is denied'),
    # #1382: site-steward writes only under artifacts/site-health/**; it is read-only on the rest of the
    # repository, including documentation (docs-writer's) and every other artifacts/ subfolder.
    @('site-steward', 'Write', "$wt\artifacts\site-health\report.md", $wt, 0, 'site-steward: its own report under artifacts/site-health'),
    @('site-steward', 'Write', "$wt\artifacts\site-health\issues\gap.md", $wt, 0, 'site-steward: an issue draft under artifacts/site-health'),
    @('site-steward', 'Edit', "$wt\src\Encina\X.cs", $wt, 2, 'site-steward: a repo source file is denied'),
    @('site-steward', 'Edit', "$wt\docs\en\guide.md", $wt, 2, 'site-steward: documentation is denied'),
    @('site-steward', 'Write', "$wt\artifacts\board\db-summary.json", $wt, 2, 'site-steward: another artifacts/ subfolder is denied'),
    # #1447: pr-reviewer writes only under artifacts/pr-review/**; it is read-only on the rest of the
    # repository, including documentation (docs-writer's) and every other artifacts/ subfolder.
    @('pr-reviewer', 'Write', "$wt\artifacts\pr-review\1447.md", $wt, 0, 'pr-reviewer: its own review under artifacts/pr-review'),
    @('pr-reviewer', 'Edit', "$wt\src\Encina\X.cs", $wt, 2, 'pr-reviewer: a repo source file is denied'),
    @('pr-reviewer', 'Edit', "$wt\docs\en\guide.md", $wt, 2, 'pr-reviewer: documentation is denied'),
    @('pr-reviewer', 'Write', "$wt\artifacts\site-health\report.md", $wt, 2, 'pr-reviewer: another artifacts/ subfolder is denied')
)

# agent_type (payload), agent_id, subagent_type, run_in_background, expected, label[, -Agent (hook CLI arg)]
$noBgCases = @(
    @('issue-worker', 'a1', 'adversarial-reviewer', $true, 2, 'no-background-specialists: worker background spawn is blocked'),
    @('issue-worker', 'a1', 'adversarial-reviewer', $false, 0, 'no-background-specialists: worker foreground spawn is allowed'),
    @('pr-reviewer', 'a4', 'Explore', $true, 2, 'no-background-specialists: pr-reviewer background spawn is blocked (#1447)'),
    @('pr-reviewer', 'a4', 'Explore', $false, 0, 'no-background-specialists: pr-reviewer foreground spawn is allowed (#1447)'),
    @($null, $null, 'issue-worker', $true, 0, 'no-background-specialists: main session background spawn is allowed'),
    @('docs-writer', 'a2', 'docs-reviewer', $true, 2, 'no-background-specialists: docs-writer background spawn is blocked'),
    @('issue-archivist', 'a3', 'issue-auditor', $true, 2, 'no-background-specialists: audit-stage agent background spawn is blocked'),
    # m1: a blank/missing agent_type falls back to -Agent (frontmatter wiring), not a single point of trust.
    @($null, $null, 'adversarial-reviewer', $true, 2, 'no-background-specialists: blank agent_type falls back to -Agent', 'issue-worker'),
    @($null, $null, 'adversarial-reviewer', $false, 0, 'no-background-specialists: -Agent fallback, foreground is allowed', 'issue-worker'),
    @('docs-writer', 'a2', 'docs-reviewer', $true, 0, 'no-background-specialists: -Agent for a different agent than agent_type is not this agent stopping', 'issue-worker')
)

# #1345: the same hook also denies run_in_background: true on a subagent's OWN Bash/PowerShell tool call (not
# just on the Agent/Task calls it makes), because a worker that backgrounds a shell command and ends its turn
# waiting on it stalls exactly like a backgrounded specialist spawn.
# agent_type (payload), agent_id, tool (Bash/PowerShell), run_in_background, expected, label[, -Agent]
$noBgShellCases = @(
    @('issue-worker', 'a1', 'PowerShell', $true, 2, 'no-background-specialists: worker PowerShell background command is blocked'),
    @('issue-worker', 'a1', 'PowerShell', $false, 0, 'no-background-specialists: worker PowerShell foreground command is allowed'),
    @($null, $null, 'PowerShell', $true, 0, 'no-background-specialists: main session background command is allowed'),
    @($null, $null, 'PowerShell', $true, 2, 'no-background-specialists: blank agent_type falls back to -Agent for a shell command', 'issue-worker'),
    @('issue-archivist', 'a3', 'Bash', $true, 2, 'no-background-specialists: audit-stage agent Bash background command is blocked')
)

$srcPatch = Join-Path $work 'src.patch'
$docsPatch = Join-Path $work 'docs.patch'
Set-Content $srcPatch "diff --git a/src/x.cs b/src/x.cs`n--- a/src/x.cs`n+++ b/src/x.cs`n@@ -1 +1 @@`n-a`n+b"
Set-Content $docsPatch "diff --git a/docs/x.md b/docs/x.md`n--- a/docs/x.md`n+++ b/docs/x.md`n@@ -1 +1 @@`n-a`n+b"

# tool, tool_input, cwd, agent_id, expected, label[, agent_type (default issue-worker; '-' omits it)]
$orchestratorCases = @(
    @('Write', @{ file_path = "$main\src\Encina\X.cs" }, $main, $null, 2, 'main session: src in the main checkout'),
    @('Edit', @{ file_path = "$wt\tests\Encina.UnitTests\X.cs" }, $main, $null, 2, 'main session: tests in a worktree'),
    @('Edit', @{ file_path = "$wt\src\Encina\README.md" }, $main, $null, 2, 'main session: a README under src'),
    @('Edit', @{ file_path = 'src/Encina/X.cs' }, $main, $null, 2, 'main session: relative src path'),
    @('Write', @{ file_path = "$wt\docs\specifications\SPEC-9.md" }, $main, $null, 0, 'main session: specification'),
    @('Write', @{ file_path = "$main\.claude\agents\x.md" }, $main, $null, 0, 'main session: .claude'),
    @('Write', @{ file_path = "$outside\scratch.cs" }, $main, $null, 0, 'main session: scratch file outside the project'),
    @('Write', @{ file_path = "$main\srcx\X.cs" }, $main, $null, 0, 'main session: a folder that only starts with src'),
    @('Write', @{ file_path = "$main\src\Encina\X.cs" }, $wt, 'a1b2', 0, 'subagent: governed by its own hooks'),
    @('PowerShell', @{ command = "Set-Content '$wt\src\x.cs' y" }, $main, $null, 2, 'main session: Set-Content into src'),
    @('PowerShell', @{ command = "Copy-Item '$outside\a.cs' '$main\tests\a.cs'" }, $main, $null, 2, 'main session: Copy-Item into tests'),
    @('Bash', @{ command = "echo x > $msysWt/src/x.cs" }, $main, $null, 2, 'main session: Bash redirection into src'),
    @('PowerShell', @{ command = "Set-Content '$outside\x.md' y" }, $main, $null, 0, 'main session: write outside the project'),
    @('PowerShell', @{ command = "git -C '$wt' commit -m x" }, $main, $null, 0, 'main session: git is allowed'),
    @('PowerShell', @{ command = 'Set-Content $p x' }, $main, $null, 0, 'main session: variable target is not resolved'),
    @('PowerShell', @{ command = "Set-Content '$wt\src\x.cs' y" }, $main, 'a1b2', 0, 'subagent: shell write allowed'),
    @('PowerShell', @{ command = 'not json' }, $main, $null, 0, 'malformed payload'),
    # M1: only the governed writing agents are exempt.
    @('Write', @{ file_path = "$wt\src\Encina\X.cs" }, $main, 'a1b2', 2, 'general-purpose subagent: src', 'general-purpose'),
    @('Edit', @{ file_path = "$wt\tests\X.cs" }, $main, 'a1b2', 2, 'Plan subagent: tests', 'Plan'),
    @('Edit', @{ file_path = "$wt\src\X.cs" }, $main, 'a1b2', 2, 'claude subagent: src', 'claude'),
    @('Edit', @{ file_path = "$wt\src\X.cs" }, $main, 'a1b2', 2, 'subagent without agent_type: src', '-'),
    @('Edit', @{ file_path = "$wt\src\X.cs" }, $main, 'a1b2', 0, 'mechanical-fixer: exempt', 'mechanical-fixer'),
    @('Edit', @{ file_path = "$wt\src\Encina\README.md" }, $main, 'a1b2', 0, 'docs-writer: exempt', 'docs-writer'),
    @('PowerShell', @{ command = "Set-Content '$wt\src\x.cs' y" }, $main, 'a1b2', 2, 'general-purpose subagent: shell write into src', 'general-purpose'),
    @('Write', @{ file_path = "$wt\docs\x.md" }, $main, 'a1b2', 0, 'general-purpose subagent: docs are not guarded', 'general-purpose'),
    # M2: wrappers.
    @('PowerShell', @{ command = "pwsh -Command `"(Get-Content $wt\src\a.cs) -replace 'a','b' | Set-Content $wt\src\a.cs`"" }, $main, $null, 2, 'main session: #1159 vector inside pwsh -Command'),
    @('Bash', @{ command = "pwsh -c `"Set-Content $msysWt/src/x.cs y`"" }, $main, $null, 2, 'main session: pwsh -c from Bash'),
    @('PowerShell', @{ command = "bash -c 'echo x > $msysWt/tests/x.cs'" }, $main, $null, 2, 'main session: bash -c redirection into tests'),
    # m2: git writes into src/ and tests/.
    @('PowerShell', @{ command = "git -C '$wt' checkout origin/main -- src/x.cs" }, $main, $null, 2, 'main session: git checkout <rev> -- src path'),
    @('PowerShell', @{ command = "git -C '$wt' checkout origin/main tests/x.cs" }, $main, $null, 2, 'main session: git checkout <rev> tests path'),
    @('PowerShell', @{ command = "git -C '$wt' checkout HEAD -- ." }, $main, $null, 2, 'main session: git checkout -- . at the checkout root'),
    @('PowerShell', @{ command = "git -C '$wt' checkout HEAD -- docs/x.md" }, $main, $null, 0, 'main session: git checkout -- docs path'),
    @('PowerShell', @{ command = "git -C '$wt' checkout -b feature origin/main" }, $main, $null, 0, 'main session: git checkout -b'),
    @('PowerShell', @{ command = "git -C '$wt' checkout main" }, $main, $null, 0, 'main session: git checkout <branch>'),
    @('PowerShell', @{ command = "git -C '$wt' restore --source=HEAD~1 src/x.cs" }, $main, $null, 2, 'main session: git restore --source src path'),
    @('PowerShell', @{ command = "git -C '$wt' restore -s HEAD~1 -- tests" }, $main, $null, 2, 'main session: git restore -s -- tests'),
    @('PowerShell', @{ command = "git -C '$wt' restore --staged src/x.cs" }, $main, $null, 0, 'main session: git restore --staged only touches the index'),
    @('PowerShell', @{ command = "git -C '$wt' restore -SW src/x.cs" }, $main, $null, 2, 'main session: git restore -SW also writes the working tree'),
    @('PowerShell', @{ command = "git -C '$wt' apply '$srcPatch'" }, $main, $null, 2, 'main session: git apply of a patch that changes src'),
    @('PowerShell', @{ command = "git -C '$wt' apply '$docsPatch'" }, $main, $null, 0, 'main session: git apply of a docs patch'),
    @('PowerShell', @{ command = "Get-Content x.patch | git -C '$wt' apply" }, $main, $null, 2, 'main session: git apply from stdin'),
    @('PowerShell', @{ command = "git -C '$wt' am '$srcPatch'" }, $main, $null, 2, 'main session: git am of a patch that changes src'),
    @('PowerShell', @{ command = "git -C '$wt' rebase origin/main" }, $main, $null, 0, 'main session: git rebase is allowed'),
    @('PowerShell', @{ command = "Invoke-WebRequest https://example.com/x -OutFile '$wt\src\x.cs'" }, $main, $null, 2, 'main session: Invoke-WebRequest -OutFile into src'),
    @('PowerShell', @{ command = "Invoke-RestMethod https://example.com/x -OutFile '$outside\x.json'" }, $main, $null, 0, 'main session: Invoke-RestMethod -OutFile outside'),
    @('PowerShell', @{ command = "Expand-Archive '$outside\a.zip' -DestinationPath '$wt'" }, $main, $null, 2, 'main session: Expand-Archive into a checkout root'),
    @('PowerShell', @{ command = "Expand-Archive '$outside\a.zip' '$wt\tests\data'" }, $main, $null, 2, 'main session: Expand-Archive positional into tests'),
    @('PowerShell', @{ command = "Expand-Archive '$outside\a.zip' -DestinationPath '$wt\docs\data'" }, $main, $null, 0, 'main session: Expand-Archive into docs'),
    @('PowerShell', @{ command = "Start-Process dotnet -ArgumentList build -RedirectStandardOutput '$wt\src\out.txt'" }, $main, $null, 2, 'main session: Start-Process redirect into src'),
    @('PowerShell', @{ command = "Start-Process dotnet -RedirectStandardError '$wt\artifacts\err.txt'" }, $main, $null, 0, 'main session: Start-Process redirect into artifacts'),
    # M4: the `dotnet run <file>.cs` / `pwsh -File <file>.ps1` bypass (#1181).
    @('PowerShell', @{ command = "dotnet run '$scriptWritesSrc'" }, $main, $null, 2, 'main session: dotnet run of a script that writes src/'),
    @('PowerShell', @{ command = "dotnet run '$scriptWritesDocs'" }, $main, $null, 0, 'main session: dotnet run of a script that writes docs/ only'),
    @('PowerShell', @{ command = "pwsh -File '$scriptWritesTestsPs1'" }, $main, $null, 2, 'main session: pwsh -File of a script that writes tests/'),
    @('PowerShell', @{ command = "dotnet run '$scriptMissing'" }, $main, $null, 2, 'main session: dotnet run of a script the hook cannot read: denied'),
    @('PowerShell', @{ command = "dotnet run '$scriptWritesSrc'" }, $main, 'a1b2', 2, 'general-purpose subagent: dotnet run of a script that writes src/', 'general-purpose'),
    @('PowerShell', @{ command = "dotnet run '$scriptWritesSrc'" }, $main, 'a1b2', 0, 'mechanical-fixer: exempt from the script check', 'mechanical-fixer'),
    # #1345: the same call-operator / dot-source detection applies with no -Agent (guard-orchestrator-writes).
    @('PowerShell', @{ command = "& '$scriptWritesTestsPs1'" }, $main, $null, 2, 'main session: call operator of a script that writes tests/'),
    @('PowerShell', @{ command = ". '$scriptWritesTestsPs1'" }, $main, $null, 2, 'main session: dot-source of a script that writes tests/')
)

# require-specialists.ps1 (Stop gate) runs against a fake project with a real git worktree.
$gateMain = Join-Path $work 'GateMain'
$gateWt = Join-Path $gateMain '.claude\worktrees\g1'
$sessionDir = Join-Path $work 'transcripts'
$sessionFile = Join-Path $sessionDir 'session.jsonl'
$agentTranscript = Join-Path $sessionDir 'session\subagents\agent-g1agent.jsonl'
$briefNamingWorktree = "Issue #1. Worktree $gateWt, branch feature, base origin/main."
$gateEmpty = Join-Path $gateMain '.claude\worktrees\g0'
$briefNamingGone = "Issue #1. Worktree $(Join-Path $gateMain '.claude\worktrees\gone'), branch feature."
$briefNamingEmpty = "Issue #1. Worktree $gateEmpty, branch feature."

# agent (-Agent), committed files, uncommitted files, spawned subagent types, payload overrides, outcome
# (block | allow | warn), label[, regex the block reason must match]. overrides.agent_type = '-' omits
# agent_id/agent_type entirely, simulating a plain Stop input (hooks.md; see require-specialists.ps1).
$gateCases = @(
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{}, 'block', 'production code without adversarial-reviewer', 'adversarial-reviewer'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @('adversarial-reviewer'), @{}, 'allow', 'production code with adversarial-reviewer'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ stop_hook_active = $true }, 'allow', 'stop_hook_active: no loop'),
    @('issue-worker', @('.claude/hooks/x.ps1'), @(), @(), @{}, 'block', 'hook change is production code', 'adversarial-reviewer'),
    @('issue-worker', @('docs/en/guide.md'), @(), @(), @{}, 'block', 'documentation without docs-writer', 'docs-writer'),
    @('issue-worker', @('docs/en/guide.md'), @(), @('docs-writer'), @{}, 'allow', 'documentation with docs-writer'),
    @('issue-worker', @('docs/plans/x-implementation-plan-1.md'), @(), @(), @{}, 'allow', 'docs/plans needs no specialist'),
    @('issue-worker', @(), @('changelog.d/1-x.fixed.md'), @(), @{}, 'block', 'untracked changelog fragment without mechanical-fixer', 'mechanical-fixer'),
    @('issue-worker', @('src/Encina/X.cs', 'src/Encina/PublicAPI.Unshipped.txt'), @(), @('adversarial-reviewer'), @{}, 'block', 'PublicAPI change without mechanical-fixer', '- mechanical-fixer:)(?!.*- adversarial-reviewer:'),
    @('issue-worker', @('src/Encina/X.cs', 'src/Encina/PublicAPI.Unshipped.txt'), @(), @('adversarial-reviewer', 'mechanical-fixer'), @{}, 'allow', 'code and PublicAPI with both specialists'),
    @('issue-worker', @('tests/Encina.UnitTests/XTests.cs'), @(), @(), @{}, 'allow', 'tests only need no specialist'),
    @('issue-worker', @(), @(), @(), @{}, 'allow', 'no changes'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @('adversarial-reviewer'), @{ tool = 'Task' }, 'allow', 'legacy Task tool name'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @('adversarial-reviewer'), @{ derived = $true }, 'allow', 'transcript found from transcript_path and agent_id'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ missing = $true }, 'warn', 'transcript missing: allowed with a warning'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ cwd = $gateMain; brief = $briefNamingWorktree }, 'block', 'cwd is the main checkout: worktree from the brief', 'adversarial-reviewer'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ cwd = $gateMain }, 'warn', 'cwd is the main checkout and the brief names no worktree'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ agent_type = 'mechanical-fixer' }, 'allow', 'hook inherited by another agent'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ agent_type = '-' }, 'allow', 'plain Stop input has no agent fields: not this agent stopping'),
    @('docs-writer', @('docs/en/guide.md'), @(), @(), @{}, 'block', 'docs-writer without docs-reviewer', 'docs-reviewer'),
    @('docs-writer', @('docs/en/guide.md'), @(), @('docs-reviewer', 'mechanical-fixer'), @{}, 'allow', 'docs-writer with docs-reviewer'),
    @('docs-writer', @('src/Encina/README.md'), @(), @(), @{}, 'block', 'package README without docs-reviewer', 'docs-reviewer'),
    @('docs-writer', @('changelog.d/1-x.changed.md'), @(), @(), @{}, 'allow', 'docs-writer changelog fragment needs no specialist'),
    # m1: only spawns whose tool_result is not an error count.
    @('issue-worker', @('src/Encina/X.cs'), @(), @('adversarial-reviewer!'), @{}, 'block', 'denied spawn (error tool_result) does not count', 'adversarial-reviewer'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @('adversarial-reviewer?'), @{}, 'block', 'spawn without a tool_result does not count', 'adversarial-reviewer'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @('adversarial-reviewer!', 'adversarial-reviewer'), @{}, 'allow', 'a denied spawn retried successfully counts'),
    # M3: docs/** site code is production code; images are documentation.
    @('issue-worker', @('docs/coverage/app.js'), @(), @(), @{}, 'block', 'dashboard code under docs needs adversarial-reviewer', 'adversarial-reviewer'),
    @('issue-worker', @('docs/_config.yml'), @(), @('adversarial-reviewer'), @{}, 'allow', 'docs/_config.yml with adversarial-reviewer'),
    @('docs-writer', @('docs/images/flow.png'), @(), @(), @{}, 'block', 'docs-writer image needs docs-reviewer', 'docs-reviewer'),
    # m5: worktree detection from the user messages.
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ cwd = $gateMain; brief = $briefNamingGone; messages = @("Correction: the worktree is $gateWt.") }, 'block', 'worktree from a later user message when the brief names a missing one', 'adversarial-reviewer'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ cwd = $gateMain; brief = $briefNamingEmpty; messages = @("Also see $gateWt.") }, 'block', 'the named worktree with changes wins over one without', 'adversarial-reviewer'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ cwd = $gateMain; toolOutput = "$gateWt  abc123 [feature]" }, 'warn', 'a worktree named only in tool output is ignored'),
    @('issue-worker', @('src/Encina/X.cs'), @(), @(), @{ cwd = $gateMain; brief = $briefNamingGone }, 'warn', 'the only named worktree does not exist')
)

$script:failed = 0
$script:total = 0

# Joins captured child-process output lines and collapses whitespace, so a "-match" against a multi-word
# phrase does not depend on where the host's console width wrapped it (#1380: PowerShell re-wraps a
# Write-Error ErrorRecord to the host width even when stderr is redirected to the parent, so the same phrase
# can land on one line in a wide terminal and split across two in a narrow one, on the very same commit).
function Get-FlatOutput([object[]]$Lines) {
    return (($Lines | ForEach-Object { "$_" }) -join ' ') -replace '\s+', ' '
}

# Runs one hook: $Json on stdin, -Agent when given; checks the exit code and, when given, a regex on stdout.
function Invoke-HookCase([string]$Hook, [string]$Json, [int]$Expected, [string]$Label, [string]$HookAgent, [string]$StdoutPattern) {
    $arguments = @('-NoProfile', '-File', $Hook)
    if ($HookAgent) { $arguments += @('-Agent', $HookAgent) }
    $output = $Json | pwsh @arguments 2>&1
    $code = $LASTEXITCODE
    $stdout = @($output | Where-Object { $_ -isnot [System.Management.Automation.ErrorRecord] }) -join "`n"
    $stderr = @($output | Where-Object { $_ -is [System.Management.Automation.ErrorRecord] }) -join ' '
    $ok = $code -eq $Expected
    if ($ok -and $StdoutPattern) { $ok = $stdout -match $StdoutPattern }
    $script:total++
    if (-not $ok) { $script:failed++ }
    $expectation = if ($StdoutPattern) { "$Expected, stdout /$StdoutPattern/" } else { "$Expected" }
    "{0} [{1}, expected {2}] {3}: {4}" -f ($(if ($ok) { 'PASS' } else { 'FAIL' })), $code, $expectation, (Split-Path -Leaf $Hook), $Label
    if (-not $ok) {
        if ($stderr) { "      stderr: $stderr" }
        if ($stdout) { "      stdout: $stdout" }
    }
}

function Invoke-Git { & git -C $gateWt -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }

function Write-GateFile([string]$Relative) {
    $path = Join-Path $gateWt $Relative
    New-Item -ItemType Directory -Force (Split-Path -Parent $path) | Out-Null
    Set-Content -Path $path -Value 'x'
}

# A spawn written as 'type!' gets an error tool_result (a denied spawn), 'type?' gets no tool_result at all.
# $Messages are later user text messages; $ToolOutput is a tool_result text (not a user message).
function Write-Transcript([string]$Path, [string[]]$Spawns, [string]$Brief, [string]$ToolName, [string[]]$Messages, [string]$ToolOutput) {
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add((@{ type = 'user'; message = @{ role = 'user'; content = $Brief } } | ConvertTo-Json -Compress -Depth 10))
    if ($ToolOutput) {
        $lines.Add((@{ type = 'assistant'; message = @{ role = 'assistant'; content = @(@{ type = 'tool_use'; id = 'toolu_out'; name = 'PowerShell'; input = @{ command = 'git worktree list' } }) } } | ConvertTo-Json -Compress -Depth 10))
        $lines.Add((@{ type = 'user'; message = @{ role = 'user'; content = @(@{ type = 'tool_result'; tool_use_id = 'toolu_out'; content = $ToolOutput }) } } | ConvertTo-Json -Compress -Depth 10))
    }
    $n = 0
    foreach ($s in $Spawns) {
        $n++
        $id = "toolu_spawn$n"
        $type = $s.TrimEnd('!', '?')
        $lines.Add((@{ type = 'assistant'; message = @{ role = 'assistant'; content = @(@{ type = 'tool_use'; id = $id; name = $ToolName; input = @{ subagent_type = $type; prompt = 'x' } }) } } | ConvertTo-Json -Compress -Depth 10))
        if ($s.EndsWith('?')) { continue }
        $result = @{ type = 'tool_result'; tool_use_id = $id; content = 'done' }
        if ($s.EndsWith('!')) { $result.is_error = $true; $result.content = 'Blocked by hook' }
        $lines.Add((@{ type = 'user'; message = @{ role = 'user'; content = @($result) } } | ConvertTo-Json -Compress -Depth 10))
    }
    foreach ($m in $Messages) { $lines.Add((@{ type = 'user'; message = @{ role = 'user'; content = @(@{ type = 'text'; text = $m }) } } | ConvertTo-Json -Compress -Depth 10)) }
    New-Item -ItemType Directory -Force (Split-Path -Parent $Path) | Out-Null
    Set-Content -Path $Path -Value $lines
}

Push-Location $work
try {
    foreach ($case in $cases) {
        $hook, $tool, $command, $expected, $label = $case
        $json = if ($command -eq 'not json') { 'not json' } else { @{ tool_name = $tool; cwd = $work; tool_input = @{ command = $command } } | ConvertTo-Json -Compress }
        Invoke-HookCase $hook $json $expected $label
    }

    # #1410: check-issue-template's local-draft evidence check, run against $issueRoot (see the fixtures above)
    # so Get-RepoRoot's $env:CLAUDE_PROJECT_DIR fallback resolves there instead of the real worktree.
    $caseBPath = Join-Path $issueRoot 'artifacts/local-ai/out/case-b.md'
    $localDraftCases = @(
        @("gh issue create --title `"[DEBT] x`" --body-file draft-pointer-a.md", 0, 'local-draft: accepted with pointer + ledger line'),
        @("gh issue create --title `"[DEBT] x`" --body-file `"$caseBPath`"", 0, 'local-draft: accepted when the body file is itself a ledger outFile'),
        @("gh issue create --title `"[DEBT] x`" --body-file debt-no-evidence.md", 2, 'local-draft: refused with no evidence'),
        @("gh issue create --title `"[DEBT] x`" --body-file debt-optout-empty.md", 2, 'local-draft: refused with an opt-out without a reason'),
        @("gh issue create --title `"[DEBT] x`" --body-file draft-pointer-g.md", 2, 'local-draft: pointer to a file with no ledger line refused'),
        @("gh issue create --title `"[DEBT] x`" --body-file draft-pointer-h.md", 2, 'local-draft: ledger line older than 24h refused'),
        @("gh issue create --title `"$remediationTitle`" --body-file `"$remediationTempBody`"", 0, 'local-draft: accepted for an open-remediation draft'),
        # Adversarial review of #1410: content-only remediation matching would let one legitimately drafted
        # remediation file be replayed under any other title within the 24-hour window; the title carried in
        # the draft's own header must also match --title.
        @("gh issue create --title `"[DEBT] a different finding entirely`" --body-file `"$remediationTempBody`"", 2, 'local-draft: open-remediation draft content reused under a different title refused')
    )
    $savedProjectDirForLocalDraft = $env:CLAUDE_PROJECT_DIR
    $env:CLAUDE_PROJECT_DIR = $issueRoot
    try {
        foreach ($case in $localDraftCases) {
            $command, $expected, $label = $case
            $json = @{ tool_name = 'PowerShell'; cwd = $work; tool_input = @{ command = $command } } | ConvertTo-Json -Compress
            Invoke-HookCase $issue $json $expected $label
        }
    }
    finally {
        $env:CLAUDE_PROJECT_DIR = $savedProjectDirForLocalDraft
    }

    # #1410: the opt-out route also logs a line to artifacts/local-ai/opt-outs.log; a dedicated, isolated root
    # so the assertion below reads only what this one case wrote.
    $savedProjectDirForOptOut = $env:CLAUDE_PROJECT_DIR
    $env:CLAUDE_PROJECT_DIR = $optOutRoot
    try {
        $json = @{ tool_name = 'PowerShell'; cwd = $work; tool_input = @{ command = 'gh issue create --title "[DEBT] x" --body-file debt-optout.md' } } | ConvertTo-Json -Compress
        Invoke-HookCase $issue $json 0 'local-draft: accepted with a logged opt-out (temp main root)'
        $optOutLog = Join-Path $optOutRoot 'artifacts/local-ai/opt-outs.log'
        $optOutLogOk = (Test-Path -LiteralPath $optOutLog) -and ((Get-Content -Raw -LiteralPath $optOutLog) -match 'hook test opt-out')
        $script:total++
        if (-not $optOutLogOk) { $script:failed++ }
        "{0} [n/a] check-issue-template.ps1: {1}" -f $(if ($optOutLogOk) { 'PASS' } else { 'FAIL' }), 'opt-out reason logged to artifacts/local-ai/opt-outs.log'
    }
    finally {
        $env:CLAUDE_PROJECT_DIR = $savedProjectDirForOptOut
    }

    foreach ($case in $spawnCases) {
        $hookAgent, $agentType, $subagentType, $expected, $label = $case
        $toolInput = if ($null -eq $subagentType) { @{} } else { @{ subagent_type = $subagentType } }
        $payload = @{ tool_name = 'Agent'; cwd = $work; tool_input = $toolInput }
        if ($agentType) { $payload.agent_type = $agentType; $payload.agent_id = 'a1' }
        Invoke-HookCase $spawn ($payload | ConvertTo-Json -Compress) $expected $label $hookAgent
    }
    Invoke-HookCase $spawn 'not json' 0 'malformed payload' 'issue-worker'

    foreach ($case in $writeCases) {
        $tool, $toolInput, $caseCwd, $expected, $label, $caseProject, $caseEnv, $stdoutPattern = $case
        $env:CLAUDE_PROJECT_DIR = if ($caseProject) { $caseProject } else { $main }
        $saved = @{}
        if ($caseEnv) { foreach ($k in $caseEnv.Keys) { $saved[$k] = [Environment]::GetEnvironmentVariable($k); [Environment]::SetEnvironmentVariable($k, $caseEnv[$k]) } }
        try {
            $json = if ($toolInput.command -eq 'not json') { 'not json' } else { @{ tool_name = $tool; cwd = $caseCwd; tool_input = $toolInput } | ConvertTo-Json -Compress }
            Invoke-HookCase $mainCheckout $json $expected $label $null $stdoutPattern
        }
        finally {
            foreach ($k in $saved.Keys) { [Environment]::SetEnvironmentVariable($k, $saved[$k]) }
        }
    }

    foreach ($case in $commandCases) {
        $tool, $command, $expected, $label = $case
        $json = if ($command -eq 'not json') { 'not json' } else { @{ tool_name = $tool; cwd = $work; tool_input = @{ command = $command } } | ConvertTo-Json -Compress }
        Invoke-HookCase $prohibited $json $expected $label
    }

    $env:CLAUDE_PROJECT_DIR = $main
    foreach ($case in $ownershipCases) {
        $hookAgent, $tool, $path, $caseCwd, $expected, $label, $agentType = $case
        $payload = @{ tool_name = $tool; cwd = $caseCwd; tool_input = @{ file_path = $path } }
        if ($agentType) { $payload.agent_type = $agentType; $payload.agent_id = 'a1' }
        Invoke-HookCase $ownership ($payload | ConvertTo-Json -Compress) $expected $label $hookAgent
    }
    Invoke-HookCase $ownership 'not json' 0 'malformed payload' 'issue-worker'

    # #1345: every allowed stage-artifact write above recorded its author in the sidecar, and the sidecar
    # names the CORRECT agent for each stage (not just "something" — a stale/wrong entry would defeat the
    # audit-commit-stage.ps1 check that reads it).
    $script:total++
    if (Test-Path -LiteralPath $ownershipAuthorsPath) {
        $recordedAuthors = Get-Content -LiteralPath $ownershipAuthorsPath -Raw | ConvertFrom-Json
        $expectedAuthors = @{ archivist = 'issue-archivist'; code = 'issue-auditor'; tests = 'test-auditor'; verification = 'audit-verifier'; docs = 'docs-reviewer' }
        $mismatches = @($expectedAuthors.Keys | Where-Object { [string]$recordedAuthors.$_.agent -ne $expectedAuthors[$_] })
        if ($mismatches.Count -eq 0) { 'PASS enforce-path-ownership.ps1: .authors.json records the correct agent for every stage' }
        else { $script:failed++; "FAIL enforce-path-ownership.ps1: .authors.json mismatches for $($mismatches -join ', ')" }
    }
    else { $script:failed++; 'FAIL enforce-path-ownership.ps1: .authors.json was never written despite allowed stage-artifact writes' }

    # #1345 review blocker: block-main-checkout-writes.ps1's "Edit tool only for source files" rule explicitly
    # excludes artifacts/ (Test-RepoFile), so before this fix a stage agent's own PowerShell/Bash tool could
    # fabricate another stage's artifact via Set-Content/redirection, bypassing enforce-path-ownership.ps1
    # entirely (it only ran on Write|Edit|MultiEdit|NotebookEdit). These cases exercise the hook's own
    # Bash|PowerShell handling directly.
    $shellCodePath = "$wt\artifacts\knowledge\stages\code.md"
    $shellCases = @(
        @('test-auditor', 'PowerShell', "Set-Content -LiteralPath '$shellCodePath' -Value 'fabricated'", 2, 'shell vector: Set-Content by the wrong stage agent is denied'),
        @('issue-auditor', 'PowerShell', "Set-Content -LiteralPath '$shellCodePath' -Value 'legitimate'", 0, 'shell vector: Set-Content by the correct stage agent is allowed'),
        @($null, 'PowerShell', "[IO.File]::WriteAllText('$shellCodePath', 'fabricated')", 2, 'shell vector: [IO.File]::WriteAllText by the orchestrator is denied', 'mechanical-fixer'),
        @('test-auditor', 'Bash', "echo fabricated > '$shellCodePath'", 2, 'shell vector: Bash redirection by the wrong stage agent is denied'),
        # #1447 adversarial-review fix: pr-reviewer.md originally wired enforce-path-ownership.ps1 only on the
        # Write|Edit|MultiEdit|NotebookEdit matcher, not on Bash|PowerShell; a shell write whose payload omits
        # agent_type (the exact case the frontmatter -Agent fallback exists for) fell through every elseif
        # branch to the default allow. These cases exercise the hook's own Bash|PowerShell handling for
        # pr-reviewer directly, the same way the stage-agent cases above do.
        @('pr-reviewer', 'PowerShell', "Set-Content -LiteralPath '$wt\src\Encina\X.cs' -Value 'fabricated'", 2, 'shell vector (#1447): pr-reviewer shell write outside artifacts/pr-review is denied'),
        @('pr-reviewer', 'PowerShell', "Set-Content -LiteralPath '$wt\artifacts\pr-review\1447.md' -Value 'ok'", 0, 'shell vector (#1447): pr-reviewer shell write to its own artifacts/pr-review is allowed'),
        # #1523: the coverage-scratch allowance is checked through the same Test-PathOwnership path for a shell
        # write as for a Write/Edit tool call, so redirection into artifacts/audit/coverage behaves the same as
        # the direct Write case above (allowed for test-auditor/audit-verifier, denied for anyone else).
        @('test-auditor', 'PowerShell', "Set-Content -LiteralPath '$wt\artifacts\audit\coverage\unit\shell.xml' -Value 'ok'", 0, 'shell vector (#1523): test-auditor shell write to its own coverage scratch is allowed'),
        @('audit-verifier', 'Bash', "echo ok > '$wt/artifacts/audit/coverage/verify-unit/shell.xml'", 0, 'shell vector (#1523): audit-verifier shell redirection to its own verify- coverage scratch is allowed'),
        @('issue-auditor', 'PowerShell', "Set-Content -LiteralPath '$wt\artifacts\audit\coverage\unit\shell.xml' -Value 'ok'", 2, 'shell vector (#1523): issue-auditor shell write to coverage scratch is denied'),
        # #1466: a direct shell write to the rerun-archivist marker is denied for every caller, the same as the
        # .authors.json sidecar; the sanctioned scripts' own internal writes never appear as a literal
        # Set-Content in the top-level command text, so they are unaffected (next case).
        @($null, 'PowerShell', "Set-Content -LiteralPath '$wt\artifacts\knowledge\stages\.rerun-archivist' -Value 'fabricated'", 2, 'shell vector (#1466): a direct Set-Content to .rerun-archivist is denied for every caller'),
        @('issue-archivist', 'Bash', "echo fabricated > '$wt/artifacts/knowledge/stages/.rerun-archivist'", 2, 'shell vector (#1466): Bash redirection to .rerun-archivist is denied even for issue-archivist'),
        @($null, 'PowerShell', "pwsh -NoProfile -File tools/ai/audit/audit-done.ps1", 0, 'shell vector (#1466): the sanctioned audit-done.ps1 launch itself is not denied (its internal marker write is invisible to this analysis)')
    )
    foreach ($case in $shellCases) {
        $hookAgent, $tool, $command, $expected, $label, $agentType = $case
        $payload = @{ tool_name = $tool; cwd = $wt; tool_input = @{ command = $command } }
        if ($agentType) { $payload.agent_type = $agentType; $payload.agent_id = 'a1' }
        Invoke-HookCase $ownership ($payload | ConvertTo-Json -Compress) $expected $label $hookAgent
    }

    # #1345 review blocker: git itself is a bypass vector for the stage-ownership check — `git checkout <rev>
    # -- <path>` / `git restore <path>` can overwrite a stage artifact's content without going through the
    # Write/Edit tool or any of the shell-write APIs above; `git commit`/`apply`/`am`, run as the command's own
    # top-level git invocation instead of through tools/ai/audit/audit-commit-stage.ps1 (the only script that
    # checks .authors.json), would let anyone commit a fabricated stage artifact. These cases need no real git
    # repository: Get-ShellWrites tokenises the command text; it never runs git.
    $wiaWt = Join-Path $main '.claude\worktrees\wia-777'
    $gitOwnershipCases = @(
        @('test-auditor', "git -C '$wt' checkout HEAD -- artifacts/knowledge/stages/code.md", 2, 'git vector: checkout of a stage artifact by the wrong stage agent is denied'),
        @('issue-auditor', "git -C '$wt' checkout HEAD -- artifacts/knowledge/stages/code.md", 0, 'git vector: checkout of a stage artifact by the correct stage agent is allowed'),
        @('test-auditor', "git -C '$wt' restore artifacts/knowledge/stages/code.md", 2, 'git vector: restore of a stage artifact by the wrong stage agent is denied'),
        @($null, "git -C '$wiaWt' commit -m x -m 'Stage: code'", 2, 'git vector: a bare commit inside an open audit worktree is denied for every caller'),
        @('issue-auditor', "git -C '$wiaWt' commit -m x -m 'Stage: code'", 2, 'git vector: a bare commit inside an open audit worktree is denied even for the stage''s own agent'),
        @($null, "git -C '$wiaWt' apply patch.diff", 2, 'git vector: apply inside an open audit worktree is denied'),
        @($null, "git -C '$wiaWt' am patch.mbox", 2, 'git vector: am inside an open audit worktree is denied'),
        @($null, "git -C '$wt' commit -m x", 0, 'git vector: a bare commit outside an audit worktree is not restricted'),
        @($null, "git -C '$wiaWt' add -f artifacts/knowledge", 0, 'git vector: add alone (no commit) is not restricted')
    )
    foreach ($case in $gitOwnershipCases) {
        $hookAgent, $command, $expected, $label = $case
        $payload = @{ tool_name = 'PowerShell'; cwd = $wt; tool_input = @{ command = $command } }
        if ($hookAgent) { $payload.agent_type = $hookAgent; $payload.agent_id = 'a1' }
        Invoke-HookCase $ownership ($payload | ConvertTo-Json -Compress) $expected $label $hookAgent
    }

    foreach ($case in $noBgCases) {
        $agentType, $agentId, $subagentType, $runInBackground, $expected, $label, $hookAgentArg = $case
        $payload = @{ tool_name = 'Agent'; cwd = $work; tool_input = @{ subagent_type = $subagentType; run_in_background = $runInBackground } }
        if ($agentType) { $payload.agent_type = $agentType; $payload.agent_id = $agentId }
        Invoke-HookCase $noBg ($payload | ConvertTo-Json -Compress) $expected $label $hookAgentArg
    }
    Invoke-HookCase $noBg 'not json' 0 'malformed payload'

    foreach ($case in $noBgShellCases) {
        $agentType, $agentId, $tool, $runInBackground, $expected, $label, $hookAgentArg = $case
        $payload = @{ tool_name = $tool; cwd = $work; tool_input = @{ command = 'dotnet test'; run_in_background = $runInBackground } }
        if ($agentType) { $payload.agent_type = $agentType; $payload.agent_id = $agentId }
        Invoke-HookCase $noBg ($payload | ConvertTo-Json -Compress) $expected $label $hookAgentArg
    }

    foreach ($case in $orchestratorCases) {
        $tool, $toolInput, $caseCwd, $agentId, $expected, $label, $agentType = $case
        $payload = @{ tool_name = $tool; cwd = $caseCwd; tool_input = $toolInput }
        if ($agentId) {
            $payload.agent_id = $agentId
            if ($agentType -ne '-') { $payload.agent_type = $(if ($agentType) { $agentType } else { 'issue-worker' }) }
        }
        $json = if ($toolInput.command -eq 'not json') { 'not json' } else { $payload | ConvertTo-Json -Compress }
        Invoke-HookCase $orchestrator $json $expected $label
    }

    # The Stop gate needs git; a machine without git skips these cases.
    if (Get-Command git -ErrorAction SilentlyContinue) {
        $env:CLAUDE_PROJECT_DIR = $gateMain
        New-Item -ItemType Directory -Force $gateWt | Out-Null
        New-Item -ItemType Directory -Force $gateEmpty | Out-Null
        Invoke-Git init -q -b main
        Invoke-Git commit -q --allow-empty -m base
        Invoke-Git update-ref refs/remotes/origin/main HEAD
        foreach ($case in $gateCases) {
            $hookAgent, $committed, $uncommitted, $spawns, $overrides, $outcome, $label, $reasonPattern = $case
            Invoke-Git reset -q --hard
            Invoke-Git clean -fdxq
            Invoke-Git checkout -q -B feature refs/remotes/origin/main
            foreach ($f in $committed) { Write-GateFile $f }
            if ($committed.Count -gt 0) { Invoke-Git add -A; Invoke-Git commit -q -m change }
            foreach ($f in $uncommitted) { Write-GateFile $f }

            if (Test-Path $sessionDir) { Remove-Item -Recurse -Force $sessionDir }
            $brief = if ($overrides.brief) { $overrides.brief } else { 'Issue #1. Implement the brief.' }
            $toolName = if ($overrides.tool) { $overrides.tool } else { 'Agent' }
            if (-not $overrides.missing) { Write-Transcript $agentTranscript $spawns $brief $toolName $overrides.messages $overrides.toolOutput }
            # agent_type = '-' simulates a plain Stop input (the main session stopping): per
            # https://code.claude.com/docs/en/hooks.md, that input has no agent fields at all, unlike
            # SubagentStop, which always carries agent_id/agent_type.
            $noAgentFields = $overrides.agent_type -eq '-'
            $payload = [ordered]@{
                hook_event_name  = $(if ($noAgentFields) { 'Stop' } else { 'SubagentStop' })
                stop_hook_active = [bool]$overrides.stop_hook_active
                cwd              = $(if ($overrides.cwd) { $overrides.cwd } else { $gateWt })
                transcript_path  = $sessionFile
            }
            if (-not $noAgentFields) {
                $payload.agent_id = 'g1agent'
                $payload.agent_type = $(if ($overrides.agent_type) { $overrides.agent_type } else { $hookAgent })
            }
            if (-not $overrides.derived) { $payload.agent_transcript_path = $agentTranscript }
            $pattern = switch ($outcome) {
                'block' { if ($reasonPattern) { "(?s)^(?=.*`"decision`":`"block`")(?=.*$reasonPattern)" } else { '"decision":"block"' } }
                'warn' { '"systemMessage"' }
                default { '^$' }
            }
            Invoke-HookCase $gate ($payload | ConvertTo-Json -Compress) 0 $label $hookAgent $pattern
        }

        # #1345 item 5: the diff base is the branch's own upstream fork point (`git merge-base HEAD
        # @{upstream}`), not always origin/main, so a branch stacked on another PR's branch is judged on its
        # own commits only — the parent branch's own docs change must not require docs-writer here.
        Invoke-Git checkout -q -B parent refs/remotes/origin/main
        Write-GateFile 'docs/en/guide.md'
        Invoke-Git add -A
        Invoke-Git commit -q -m 'parent change'
        Invoke-Git checkout -q -B feature-stacked parent
        Invoke-Git branch --set-upstream-to=parent feature-stacked
        Write-GateFile 'src/Encina/X.cs'
        Invoke-Git add -A
        Invoke-Git commit -q -m 'own change'
        if (Test-Path $sessionDir) { Remove-Item -Recurse -Force $sessionDir }
        Write-Transcript $agentTranscript @() 'Issue #1. Implement the brief.' 'Agent' $null $null
        $stackedPayload = [ordered]@{
            hook_event_name       = 'SubagentStop'
            stop_hook_active      = $false
            cwd                   = $gateWt
            transcript_path       = $sessionFile
            agent_id              = 'g1agent'
            agent_type            = 'issue-worker'
            agent_transcript_path = $agentTranscript
        }
        Invoke-HookCase $gate ($stackedPayload | ConvertTo-Json -Compress) 0 "require-specialists: a branch stacked on another PR's branch is judged on its own commits only" 'issue-worker' '(?s)^(?=.*"decision":"block")(?=.*adversarial-reviewer)(?!.*docs-writer)'

        # #1345 review: the normal case once GitHub deletes a merged branch is that @{upstream} no longer
        # resolves at all (not merely "moved ahead") — reproduced here by deleting the local 'parent' branch
        # after 'feature-stacked' branched from it. The fork point must still come from feature-stacked's own
        # reflog ('branch: Created from parent'), not from widening the diff to origin/main, which would pick
        # up 'parent''s own docs change again.
        Invoke-Git branch -D parent
        if (Test-Path $sessionDir) { Remove-Item -Recurse -Force $sessionDir }
        Write-Transcript $agentTranscript @() 'Issue #1. Implement the brief.' 'Agent' $null $null
        Invoke-HookCase $gate ($stackedPayload | ConvertTo-Json -Compress) 0 "require-specialists: a stacked branch whose upstream branch was deleted still uses its own reflog fork point, not origin/main" 'issue-worker' '(?s)^(?=.*"decision":"block")(?=.*adversarial-reviewer)(?!.*docs-writer)'

        Invoke-HookCase $gate 'not json' 0 'malformed payload' 'issue-worker' '^$'
    }
    else {
        'SKIP require-specialists.ps1: git is not on PATH'
    }

    # audit-stage-guard.ps1 (#1345) needs a real git repository at the "audit worktree" so it can check
    # `git log --grep "Stage: <name>"`, and its own pipeline.json (the guard never hard-codes the order).
    if (Get-Command git -ErrorAction SilentlyContinue) {
        $auditMain = Join-Path $work 'AuditMain'
        $auditN = 42
        $auditWt = Join-Path $auditMain ".claude\worktrees\wia-$auditN"
        $auditCurrentPath = Join-Path $auditMain 'artifacts\knowledge\current-audit.json'
        $defaultPipelineJson = '{"stages":[{"stage":"archivist","agent":"issue-archivist","model":"sonnet","artifact":"archivist.md"},{"stage":"code","agent":"issue-auditor","model":"sonnet","artifact":"code.md"},{"stage":"tests","agent":"test-auditor","model":"sonnet","artifact":"tests.md"},{"stage":"docs","agent":"docs-reviewer","model":"sonnet","artifact":"docs.md"},{"stage":"remediation","agent":"local-model","model":"qwen","artifact":"remediation.md"},{"stage":"verification","agent":"audit-verifier","model":"sonnet","artifact":"verification.md"}],"minModel":"sonnet","forbiddenModels":["haiku"],"verdictLine":"Verdict: PASS","lessonsHeading":"## Lessons for the pipeline"}'
        # Reordered: 'code' runs before 'archivist' — proves the guard reads pipeline.json, not a hard-coded order.
        $reorderedPipelineJson = '{"stages":[{"stage":"code","agent":"issue-auditor","model":"sonnet","artifact":"code.md"},{"stage":"archivist","agent":"issue-archivist","model":"sonnet","artifact":"archivist.md"},{"stage":"tests","agent":"test-auditor","model":"sonnet","artifact":"tests.md"},{"stage":"docs","agent":"docs-reviewer","model":"sonnet","artifact":"docs.md"},{"stage":"remediation","agent":"local-model","model":"qwen","artifact":"remediation.md"},{"stage":"verification","agent":"audit-verifier","model":"sonnet","artifact":"verification.md"}],"minModel":"sonnet","forbiddenModels":["haiku"],"verdictLine":"Verdict: PASS","lessonsHeading":"## Lessons for the pipeline"}'
        # #1345 review: the verifier artifact name must be resolved from pipeline.json (the stage whose agent
        # is audit-verifier), not hard-coded as 'verification.md' — proven by renaming it here.
        $renamedVerifierPipelineJson = '{"stages":[{"stage":"archivist","agent":"issue-archivist","model":"sonnet","artifact":"archivist.md"},{"stage":"code","agent":"issue-auditor","model":"sonnet","artifact":"code.md"},{"stage":"tests","agent":"test-auditor","model":"sonnet","artifact":"tests.md"},{"stage":"docs","agent":"docs-reviewer","model":"sonnet","artifact":"docs.md"},{"stage":"remediation","agent":"local-model","model":"qwen","artifact":"remediation.md"},{"stage":"verification","agent":"audit-verifier","model":"sonnet","artifact":"verdict.md"}],"minModel":"sonnet","forbiddenModels":["haiku"],"verdictLine":"Verdict: PASS","lessonsHeading":"## Lessons for the pipeline"}'

        function Invoke-AuditGit { & git -C $auditWt -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }

        function Initialize-AuditWorktree([string]$PipelineJson) {
            if (Test-Path $auditWt) { Remove-Item -Recurse -Force $auditWt }
            New-Item -ItemType Directory -Force $auditWt | Out-Null
            Invoke-AuditGit init -q -b main
            Invoke-AuditGit commit -q --allow-empty -m base
            New-Item -ItemType Directory -Force (Join-Path $auditWt 'tools\ai\audit') | Out-Null
            Set-Content (Join-Path $auditWt 'tools\ai\audit\pipeline.json') $PipelineJson
            New-Item -ItemType Directory -Force (Join-Path $auditWt 'artifacts\knowledge\stages') | Out-Null
        }

        function Write-AuditStage([string]$StageName, [string]$ArtifactName, [switch]$Commit) {
            Set-Content (Join-Path $auditWt "artifacts\knowledge\stages\$ArtifactName") "x`n## Lessons for the pipeline`n- none`n"
            if ($Commit) {
                Invoke-AuditGit add -f "artifacts/knowledge/stages/$ArtifactName"
                Invoke-AuditGit commit -q -m "audit #$auditN`: $StageName stage" -m "Stage: $StageName"
            }
        }

        function Set-AuditOpen([bool]$Open) {
            if ($Open) {
                New-Item -ItemType Directory -Force (Split-Path -Parent $auditCurrentPath) | Out-Null
                @{ issue = $auditN; worktree = $auditWt; branch = "audit/$auditN"; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content $auditCurrentPath
            }
            elseif (Test-Path $auditCurrentPath) { Remove-Item -Force $auditCurrentPath }
        }

        function Invoke-AuditCase([string]$Subagent, [string]$Prompt, [string]$Model, [int]$Expected, [string]$Label) {
            $toolInput = @{ subagent_type = $Subagent; prompt = $Prompt }
            if ($Model) { $toolInput.model = $Model }
            $payload = @{ tool_name = 'Agent'; cwd = $auditWt; tool_input = $toolInput }
            $env:CLAUDE_PROJECT_DIR = $auditWt
            Invoke-HookCase $auditGuard ($payload | ConvertTo-Json -Compress) $Expected $Label
        }

        Initialize-AuditWorktree $defaultPipelineJson
        Set-AuditOpen $false
        Invoke-AuditCase 'issue-archivist' "Audit #$auditN in worktree wia-$auditN." $null 2 'audit-stage-guard: no open audit'

        Set-AuditOpen $true
        Invoke-AuditCase 'issue-archivist' "Audit #$auditN, no worktree named." $null 2 'audit-stage-guard: prompt missing wia-<n>'
        Invoke-AuditCase 'issue-archivist' "Audit #$auditN in worktree wia-$auditN, also see wia-7 for comparison." $null 2 'audit-stage-guard: another wia-<m> mentioned (batching)'
        Invoke-AuditCase 'issue-archivist' "Audit #$auditN in worktree wia-$auditN." $null 0 'audit-stage-guard: first stage, correct agent'
        Invoke-AuditCase 'issue-auditor' "Audit #$auditN in worktree wia-$auditN, code stage." $null 2 'audit-stage-guard: code stage before archivist is committed'

        Write-AuditStage 'archivist' 'archivist.md'
        Invoke-AuditCase 'issue-auditor' "Audit #$auditN in worktree wia-$auditN, code stage." $null 2 'audit-stage-guard: archivist artifact exists but is not committed'

        Write-AuditStage 'archivist' 'archivist.md' -Commit
        Invoke-AuditCase 'issue-auditor' "Audit #$auditN in worktree wia-$auditN, code stage." $null 0 'audit-stage-guard: code stage after archivist is committed'
        Invoke-AuditCase 'issue-auditor' "Audit #$auditN in worktree wia-$auditN, code stage." 'haiku' 2 'audit-stage-guard: haiku is blocked even at the correct stage'

        Write-AuditStage 'code' 'code.md' -Commit
        Write-AuditStage 'tests' 'tests.md' -Commit
        Invoke-AuditCase 'docs-reviewer' "Audit #$auditN in worktree wia-$auditN, docs stage." $null 0 'audit-stage-guard: docs-reviewer at its own stage'

        Set-AuditOpen $false
        Invoke-AuditCase 'docs-reviewer' 'A normal documentation self-review, no audit context.' $null 0 'audit-stage-guard: docs-reviewer without wia-<n> is not an audit stage'
        Set-AuditOpen $true

        Invoke-AuditCase 'issue-worker' "Run the SPEC-003 audit for #$auditN end to end." $null 2 'audit-stage-guard: issue-worker SPEC-003 audit coordinator path is closed'
        Invoke-AuditCase 'general-purpose' "Run the SPEC-003 audit for #$auditN." $null 2 'audit-stage-guard: general-purpose SPEC-003 audit coordinator path is closed'

        Write-AuditStage 'docs' 'docs.md' -Commit
        Invoke-AuditCase 'audit-verifier' "Audit #$auditN in worktree wia-$auditN, verify." $null 2 'audit-stage-guard: remediation stage still pending, agent spawn is out of order'

        Write-AuditStage 'remediation' 'remediation.md' -Commit
        Set-Content (Join-Path $auditWt 'artifacts\knowledge\stages\verification.md') "Verdict: PASS`n## Lessons for the pipeline`n- none`n"
        Invoke-AuditGit add -f 'artifacts/knowledge/stages/verification.md'
        Invoke-AuditGit commit -q -m "audit #$auditN`: verification stage" -m 'Stage: verification'
        Invoke-AuditCase 'audit-verifier' "Audit #$auditN in worktree wia-$auditN, verify again." $null 2 'audit-stage-guard: all stages complete with a PASS verdict, no more spawns'

        # #1555: a stage re-committed AFTER the verifier's own last commit makes that PASS verdict stale -- the
        # verifier never inspected the new content. Recommit remediation.md (simulating the orchestrator
        # regenerating drafts with audit-draft-remediation.ps1 and re-running audit-commit-stage.ps1 -Stage
        # remediation) with a committer date strictly after "now" (when the verification commit above ran, with
        # no explicit date), and check: audit-verifier may now be re-spawned, but any OTHER out-of-order stage
        # agent stays blocked exactly as under a fresh, non-stale PASS (decision 2) -- this pair fails against
        # the unmodified guard, which has no notion of staleness and blocks every spawn once all stages are done.
        # Write-AuditStage always writes the same fixed content, so a second call would leave nothing new to
        # commit (the earlier remediation commit already has it); write distinguishable content instead so the
        # re-commit actually happens.
        Set-Content (Join-Path $auditWt 'artifacts\knowledge\stages\remediation.md') "x (regenerated)`n## Lessons for the pipeline`n- none`n"
        Invoke-AuditGit add -f 'artifacts/knowledge/stages/remediation.md'
        $staleCommitDate = [DateTimeOffset]::UtcNow.AddHours(1).ToString('yyyy-MM-ddTHH:mm:sszzz')
        $env:GIT_AUTHOR_DATE = $staleCommitDate
        $env:GIT_COMMITTER_DATE = $staleCommitDate
        Invoke-AuditGit commit -q -m "audit #$auditN`: remediation stage" -m 'Stage: remediation'
        Remove-Item Env:\GIT_AUTHOR_DATE, Env:\GIT_COMMITTER_DATE -ErrorAction SilentlyContinue
        Invoke-AuditCase 'audit-verifier' "Audit #$auditN in worktree wia-$auditN, verify again after remediation was regenerated." $null 0 'audit-stage-guard: a PASS verdict made stale by a newer remediation commit lets audit-verifier re-run (#1555)'
        Invoke-AuditCase 'issue-auditor' "Audit #$auditN in worktree wia-$auditN, redo the code stage after remediation was regenerated." $null 2 'audit-stage-guard: a stale PASS verdict still blocks any OTHER out-of-order stage agent (#1555)'

        # #1457: without a .rerun-archivist marker, issue-archivist stays blocked after a PASS verdict too
        # (the deadlock this issue closes was audit-done refusing while the guard also refused the re-spawn
        # that would fix it) -- but audit-done.ps1 writing that marker (a record-schema failure with every
        # stage already committed) is the one condition that lets it through anyway.
        Invoke-AuditCase 'issue-archivist' "Audit #$auditN in worktree wia-$auditN, redo archivist to fix the record." $null 2 'audit-stage-guard: PASS verdict, no .rerun-archivist marker, issue-archivist still blocked (#1457)'

        $rerunArchivistMarker = Join-Path $auditWt 'artifacts\knowledge\stages\.rerun-archivist'
        Set-Content $rerunArchivistMarker "knowledge-records --check failed: fixture error`n`nUTC: 2026-01-01T00:00:00Z`n"
        Invoke-AuditCase 'issue-archivist' "Audit #$auditN in worktree wia-$auditN, redo archivist to fix the record." $null 0 'audit-stage-guard: .rerun-archivist marker allows issue-archivist even after a PASS verdict (#1457)'
        Invoke-AuditCase 'issue-auditor' "Audit #$auditN in worktree wia-$auditN, redo the code stage instead." $null 2 'audit-stage-guard: the .rerun-archivist marker only exempts issue-archivist, not another stage agent (#1457)'
        Remove-Item -Force $rerunArchivistMarker

        Set-Content (Join-Path $auditWt 'artifacts\knowledge\stages\verification.md') "Verdict: FAIL`n## Lessons for the pipeline`n- none`n"
        Invoke-AuditCase 'issue-auditor' "Audit #$auditN in worktree wia-$auditN, redo the code stage." $null 0 'audit-stage-guard: earlier stage re-run allowed after a Verdict: FAIL'

        Initialize-AuditWorktree $renamedVerifierPipelineJson
        Set-AuditOpen $true
        Write-AuditStage 'archivist' 'archivist.md' -Commit
        Write-AuditStage 'code' 'code.md' -Commit
        Write-AuditStage 'tests' 'tests.md' -Commit
        Write-AuditStage 'docs' 'docs.md' -Commit
        Write-AuditStage 'remediation' 'remediation.md' -Commit
        Set-Content (Join-Path $auditWt 'artifacts\knowledge\stages\verdict.md') "Verdict: FAIL`n## Lessons for the pipeline`n- none`n"
        Invoke-AuditCase 'issue-auditor' "Audit #$auditN in worktree wia-$auditN, redo the code stage (renamed verifier artifact)." $null 0 'audit-stage-guard: FAIL verdict resolved from pipeline.json artifact name (verdict.md), re-run still allowed'

        Initialize-AuditWorktree $reorderedPipelineJson
        Set-AuditOpen $true
        Invoke-AuditCase 'issue-auditor' "Audit #$auditN in worktree wia-$auditN, code stage (reordered pipeline)." $null 0 'audit-stage-guard: reordered pipeline.json makes code the first stage'
        Invoke-AuditCase 'issue-archivist' "Audit #$auditN in worktree wia-$auditN, archivist stage (reordered pipeline)." $null 2 'audit-stage-guard: reordered pipeline.json makes archivist out of order'

        Invoke-HookCase $auditGuard 'not json' 0 'audit-stage-guard: malformed payload'

        # #1345: audit-commit-stage.ps1 refuses an artifact whose last recorded author (the sidecar
        # enforce-path-ownership.ps1 maintains) does not match the agent pipeline.json assigns to that stage.
        # A standalone repo, self-referential (its own current-audit.json points at itself), carrying its own
        # copy of the real script so $PSScriptRoot resolves inside the fixture, not the real checkout.
        $commitWt = Join-Path $work 'CommitWt'
        if (Test-Path $commitWt) { Remove-Item -Recurse -Force $commitWt }
        New-Item -ItemType Directory -Force (Join-Path $commitWt 'tools\ai\audit') | Out-Null
        Copy-Item (Join-Path $hooks '..\..\tools\ai\audit\audit-commit-stage.ps1') (Join-Path $commitWt 'tools\ai\audit\audit-commit-stage.ps1')
        Copy-Item (Join-Path $hooks '..\..\tools\ai\audit\_audit-lib.ps1') (Join-Path $commitWt 'tools\ai\audit\_audit-lib.ps1')
        Set-Content (Join-Path $commitWt 'tools\ai\audit\pipeline.json') $defaultPipelineJson
        function Invoke-CommitWtGit { & git -C $commitWt -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
        Invoke-CommitWtGit init -q -b main
        # Local (not -c, which is invocation-scoped only) repo identity: audit-commit-stage.ps1's own `git
        # commit` call intentionally passes no -c override (production assumes a configured committer, like
        # every other git call in this codebase), so this fixture must give the ephemeral repo a real identity
        # or that commit fails with "Please tell me who you are" whenever the ambient global config is not
        # visible to the child process (observed specifically when this suite runs long enough to be moved to
        # the background: #1345).
        Invoke-CommitWtGit config user.name hooks
        Invoke-CommitWtGit config user.email hooks@example.invalid
        Invoke-CommitWtGit commit -q --allow-empty -m base
        New-Item -ItemType Directory -Force (Join-Path $commitWt 'artifacts\knowledge\stages') | Out-Null
        @{ issue = 77; worktree = $commitWt; branch = 'audit/77'; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $commitWt 'artifacts\knowledge\current-audit.json')
        Set-Content (Join-Path $commitWt 'artifacts\knowledge\stages\code.md') "x`n## Lessons for the pipeline`n- none`n"

        function Invoke-CommitStage {
            $output = & pwsh -NoProfile -File (Join-Path $commitWt 'tools\ai\audit\audit-commit-stage.ps1') -Stage code 2>&1
            [pscustomobject]@{ Code = $LASTEXITCODE; Output = (Get-FlatOutput $output) }
        }
        function Test-CommitStageCase([string]$Label, [bool]$ExpectSuccess) {
            $r = Invoke-CommitStage
            $ok = if ($ExpectSuccess) { $r.Code -eq 0 } else { $r.Code -ne 0 }
            $script:total++
            if ($ok) { "PASS audit-commit-stage.ps1: $Label" } else { $script:failed++; "FAIL audit-commit-stage.ps1: $Label (exit $($r.Code)): $($r.Output)" }
        }

        Test-CommitStageCase 'refuses when .authors.json does not exist' $false
        @{ code = @{ agent = 'test-auditor'; utc = '2026-01-01T00:00:00Z' } } | ConvertTo-Json | Set-Content (Join-Path $commitWt 'artifacts\knowledge\stages\.authors.json')
        Test-CommitStageCase 'refuses when the recorded author is the wrong agent' $false
        @{ code = @{ agent = 'issue-auditor'; utc = '2026-01-01T00:00:00Z' } } | ConvertTo-Json | Set-Content (Join-Path $commitWt 'artifacts\knowledge\stages\.authors.json')
        Test-CommitStageCase 'commits when the recorded author matches pipeline.json' $true

        # ================================================================================================
        # #1457: audit-commit-stage.ps1 -Stage archivist additionally runs the real knowledge-records --check
        # against the knowledge record the archivist stage wrote (artifacts\knowledge\issues\<n>.md), refusing
        # to commit a non-schema-1 record and clearing any .rerun-archivist marker once a valid one commits.
        # A standalone fixture (its own repo, self-referential current-audit.json), like $commitWt above, but
        # carrying a real copy of .github\scripts\knowledge-records.cs so the check runs for real (no gh, no
        # local model, no network -- dotnet run --file is the same tool AGENTS.md requires for this repo).
        if (Get-Command dotnet -ErrorAction SilentlyContinue) {
            $archivistWt = Join-Path $work 'ArchivistCommitWt'
            if (Test-Path $archivistWt) { Remove-Item -Recurse -Force $archivistWt }
            New-Item -ItemType Directory -Force (Join-Path $archivistWt 'tools\ai\audit') | Out-Null
            New-Item -ItemType Directory -Force (Join-Path $archivistWt '.github\scripts') | Out-Null
            New-Item -ItemType Directory -Force (Join-Path $archivistWt 'artifacts\knowledge\stages') | Out-Null
            New-Item -ItemType Directory -Force (Join-Path $archivistWt 'artifacts\knowledge\issues') | Out-Null
            Copy-Item (Join-Path $hooks '..\..\tools\ai\audit\audit-commit-stage.ps1') (Join-Path $archivistWt 'tools\ai\audit\audit-commit-stage.ps1')
            Copy-Item (Join-Path $hooks '..\..\tools\ai\audit\_audit-lib.ps1') (Join-Path $archivistWt 'tools\ai\audit\_audit-lib.ps1')
            Copy-Item (Join-Path $repo '.github\scripts\knowledge-records.cs') (Join-Path $archivistWt '.github\scripts\knowledge-records.cs')
            Set-Content (Join-Path $archivistWt 'tools\ai\audit\pipeline.json') $defaultPipelineJson
            function Invoke-ArchivistWtGit { & git -C $archivistWt -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
            Invoke-ArchivistWtGit init -q -b main
            Invoke-ArchivistWtGit config user.name hooks
            Invoke-ArchivistWtGit config user.email hooks@example.invalid
            Invoke-ArchivistWtGit commit -q --allow-empty -m base
            $archivistN = 4242
            @{ issue = $archivistN; worktree = $archivistWt; branch = "audit/$archivistN"; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $archivistWt 'artifacts\knowledge\current-audit.json')
            Set-Content (Join-Path $archivistWt 'artifacts\knowledge\stages\archivist.md') "x`n## Lessons for the pipeline`n- none`n"
            @{ archivist = @{ agent = 'issue-archivist'; utc = '2026-01-01T00:00:00Z' } } | ConvertTo-Json | Set-Content (Join-Path $archivistWt 'artifacts\knowledge\stages\.authors.json')

            $recordPath = Join-Path $archivistWt "artifacts\knowledge\issues\$archivistN.md"
            $nonSchemaRecord = @"
---
issue: $archivistN
title: "Ad hoc record"
type: infra
outcome: delivered
closed_at: 2026-09-27
---

## Decisions
- something
"@
            $validRecord = @"
---
schema: 1
nav_exclude: true
issue: $archivistN
title: "Test knowledge record (#1457 fixture)"
closed: 2026-09-27
state_reason: completed
outcome: delivered
type: infra
area: ci-process
review: draft
packages:
prs:
linked_prs:
knowledge:
remediation:
audit:
  checklist: 0
  date: 2026-09-27
  verdict: not-audited
  record: "not written yet"
---

## Asked
Test.
## Outcome
Test.
"@

            function Invoke-ArchivistCommitStage {
                $output = & pwsh -NoProfile -File (Join-Path $archivistWt 'tools\ai\audit\audit-commit-stage.ps1') -Stage archivist 2>&1
                [pscustomobject]@{ Code = $LASTEXITCODE; Output = (Get-FlatOutput $output) }
            }
            function Test-ArchivistCommitCase([string]$Label, [bool]$ExpectSuccess, [string]$Pattern) {
                $r = Invoke-ArchivistCommitStage
                $ok = if ($ExpectSuccess) { $r.Code -eq 0 } else { $r.Code -ne 0 }
                if ($ok -and $Pattern) { $ok = $r.Output -match $Pattern }
                $script:total++
                if ($ok) { "PASS audit-commit-stage.ps1: $Label" } else { $script:failed++; "FAIL audit-commit-stage.ps1: $Label (exit $($r.Code)): $($r.Output)" }
            }

            Set-Content -LiteralPath $recordPath -Value $nonSchemaRecord
            Test-ArchivistCommitCase 'refuses to commit the archivist stage when the knowledge record fails knowledge-records --check (#1457)' $false 'knowledge-records --check'

            # A prior audit-done.ps1 run had already left the .rerun-archivist marker (a record-schema
            # failure discovered after every stage was committed) -- committing a now-valid record must clear
            # it, or audit-stage-guard.ps1 would keep allowing an issue-archivist re-spawn forever.
            $archivistRerunMarker = Join-Path $archivistWt 'artifacts\knowledge\stages\.rerun-archivist'
            Set-Content $archivistRerunMarker "knowledge-records --check failed: fixture error`n`nUTC: 2026-01-01T00:00:00Z`n"

            Set-Content -LiteralPath $recordPath -Value $validRecord
            Test-ArchivistCommitCase 'commits the archivist stage once the knowledge record passes knowledge-records --check (#1457)' $true $null

            $script:total++
            if (-not (Test-Path -LiteralPath $archivistRerunMarker)) {
                'PASS audit-commit-stage.ps1: a successful archivist commit removes the .rerun-archivist marker (#1457)'
            }
            else {
                $script:failed++
                'FAIL audit-commit-stage.ps1: .rerun-archivist marker was not removed after a successful archivist commit (#1457)'
            }

            # Adversarial review of the first #1457 diff: the marker must be removed BEFORE 'git add -f
            # artifacts/knowledge', not after the commit -- an add -f over the whole (gitignored) tree would
            # otherwise sweep the still-present marker into the archivist commit itself, leaving its later
            # on-disk removal as an unstaged, unrelated deletion that the NEXT stage's own 'git add -f' would
            # silently fold into a commit that never touched it. Checking file presence alone (the case above)
            # cannot catch that: the marker genuinely disappears from disk either way. Assert directly that no
            # commit on the branch ever carried the marker, and that the working tree is clean afterward.
            $script:total++
            $markerCommits = & git -C $archivistWt log --all --name-only --pretty=format: -- 'artifacts/knowledge/stages/.rerun-archivist' 2>&1
            $markerStatus = & git -C $archivistWt status --porcelain -- 'artifacts/knowledge' 2>&1
            if ([string]::IsNullOrWhiteSpace(($markerCommits | Select-Object -First 1)) -and [string]::IsNullOrWhiteSpace(($markerStatus | Select-Object -First 1))) {
                'PASS audit-commit-stage.ps1: the .rerun-archivist marker was never committed and the audit worktree is clean after the archivist commit (#1457)'
            }
            else {
                $script:failed++
                "FAIL audit-commit-stage.ps1: the .rerun-archivist marker leaked into git history or left the worktree dirty (#1457): commits=$(Get-FlatOutput $markerCommits); status=$(Get-FlatOutput $markerStatus)"
            }

            # #1466: the record is unchanged since the commit above, so 'git add -f' has nothing to stage and
            # 'git commit' fails with "nothing to commit". A marker present at the start of this call (written
            # here to simulate audit-done.ps1 leaving one for an unrelated reason between two archivist
            # attempts) must NOT be lost by the ordering bug this issue fixes: the old code removed the marker
            # unconditionally once the check passed, before ever finding out the commit itself would fail.
            $noCommitMarkerContent = "knowledge-records --check failed: still broken`n`nUTC: 2026-01-01T01:00:00Z`n"
            Set-Content -LiteralPath $archivistRerunMarker -Value $noCommitMarkerContent -NoNewline
            Test-ArchivistCommitCase 'refuses "nothing to commit" when the record is unchanged since the last commit (#1466)' $false 'nothing to commit'

            $script:total++
            if ((Test-Path -LiteralPath $archivistRerunMarker) -and (Get-Content -LiteralPath $archivistRerunMarker -Raw) -eq $noCommitMarkerContent) {
                'PASS audit-commit-stage.ps1: a "nothing to commit" archivist commit keeps the .rerun-archivist marker in place, unchanged (#1466)'
            }
            else {
                $script:failed++
                'FAIL audit-commit-stage.ps1: a "nothing to commit" archivist commit lost or altered the .rerun-archivist marker (#1466)'
            }
        }
        else {
            'SKIP audit-commit-stage.ps1: dotnet is not on PATH (#1457/#1466 knowledge-records --check cases)'
        }

        # ================================================================================================
        # #1374 (appended last, its own delimited block, to minimise conflicts with #1375's own audit-stage
        # cases): two concurrent enforce-path-ownership.ps1 instances writing .authors.json for two different
        # stages must never leave it invalid; an unreadable sidecar must be reported loudly by the hook and
        # by audit-commit-stage.ps1, never silently swallowed; audit-stage.ps1 -RepairAuthors is the one
        # sanctioned repair for an open audit.
        # ================================================================================================

        # (a) Two real, concurrent enforce-path-ownership.ps1 processes (Start-Process -PassThru, not two
        # sequential Invoke-HookCase calls) writing DIFFERENT stages of the same sidecar, run 5 times: the
        # result must always be one valid JSON object with both entries, never the two-concatenated-objects
        # corruption #1374 reports.
        $concurrentWt = Join-Path $work 'ConcurrentAuthorsWt'
        if (Test-Path $concurrentWt) { Remove-Item -Recurse -Force $concurrentWt }
        New-Item -ItemType Directory -Force (Join-Path $concurrentWt 'tools\ai\audit') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $concurrentWt 'artifacts\knowledge\stages') | Out-Null
        Set-Content (Join-Path $concurrentWt 'tools\ai\audit\pipeline.json') $defaultPipelineJson
        $concurrentAuthorsPath = Join-Path $concurrentWt 'artifacts\knowledge\stages\.authors.json'
        $concurrentArchivistPayload = @{ tool_name = 'Write'; cwd = $concurrentWt; tool_input = @{ file_path = "$concurrentWt\artifacts\knowledge\stages\archivist.md" }; agent_type = 'issue-archivist'; agent_id = 'a1' } | ConvertTo-Json -Compress
        $concurrentCodePayload = @{ tool_name = 'Write'; cwd = $concurrentWt; tool_input = @{ file_path = "$concurrentWt\artifacts\knowledge\stages\code.md" }; agent_type = 'issue-auditor'; agent_id = 'a2' } | ConvertTo-Json -Compress
        $concurrentArchivistStdin = Join-Path $concurrentWt 'archivist-payload.json'
        $concurrentCodeStdin = Join-Path $concurrentWt 'code-payload.json'
        Set-Content -LiteralPath $concurrentArchivistStdin -Value $concurrentArchivistPayload -NoNewline
        Set-Content -LiteralPath $concurrentCodeStdin -Value $concurrentCodePayload -NoNewline
        $env:CLAUDE_PROJECT_DIR = $concurrentWt

        $concurrentFailures = [System.Collections.Generic.List[string]]::new()
        for ($round = 1; $round -le 5; $round++) {
            if (Test-Path $concurrentAuthorsPath) { Remove-Item -Force $concurrentAuthorsPath }
            $out1 = Join-Path $concurrentWt "out1-$round.txt"
            $err1 = Join-Path $concurrentWt "err1-$round.txt"
            $out2 = Join-Path $concurrentWt "out2-$round.txt"
            $err2 = Join-Path $concurrentWt "err2-$round.txt"
            $p1 = Start-Process pwsh -ArgumentList @('-NoProfile', '-File', $ownership, '-Agent', 'issue-archivist') -RedirectStandardInput $concurrentArchivistStdin -RedirectStandardOutput $out1 -RedirectStandardError $err1 -PassThru -WindowStyle Hidden
            $p2 = Start-Process pwsh -ArgumentList @('-NoProfile', '-File', $ownership, '-Agent', 'issue-auditor') -RedirectStandardInput $concurrentCodeStdin -RedirectStandardOutput $out2 -RedirectStandardError $err2 -PassThru -WindowStyle Hidden
            # A bounded timeout, not an unbounded WaitForExit(): a hung child (e.g. a broken pwsh install)
            # must fail this case with a clear message, never block the whole suite forever.
            $exited1 = $p1.WaitForExit(15000)
            $exited2 = $p2.WaitForExit(15000)
            if (-not $exited1) { Stop-Process -Id $p1.Id -Force -ErrorAction SilentlyContinue }
            if (-not $exited2) { Stop-Process -Id $p2.Id -Force -ErrorAction SilentlyContinue }
            if (-not ($exited1 -and $exited2)) { $concurrentFailures.Add("round $round`: a hook process did not exit within 15s (killed)"); continue }
            if (-not (Test-Path $concurrentAuthorsPath)) { $concurrentFailures.Add("round $round`: sidecar missing"); continue }
            try {
                $parsed = Get-Content -LiteralPath $concurrentAuthorsPath -Raw | ConvertFrom-Json -AsHashtable
                $ok = $null -ne $parsed -and $parsed.ContainsKey('archivist') -and [string]$parsed['archivist'].agent -eq 'issue-archivist' -and $parsed.ContainsKey('code') -and [string]$parsed['code'].agent -eq 'issue-auditor'
                if (-not $ok) { $concurrentFailures.Add("round $round`: missing an entry") }
            }
            catch { $concurrentFailures.Add("round $round`: invalid JSON - $($_.Exception.Message)") }
        }
        $script:total++
        if ($concurrentFailures.Count -eq 0) { 'PASS enforce-path-ownership.ps1: 5 rounds of two concurrent instances leave one valid sidecar with both entries (#1374)' }
        else { $script:failed++; "FAIL enforce-path-ownership.ps1: concurrent sidecar writes (#1374): $($concurrentFailures -join '; ')" }

        # (b) An unparseable sidecar (two concatenated objects, as observed in #1374) denies the write with
        # the repair instruction on stderr, instead of the old silent `catch { }`.
        function Test-OwnershipDenialCase([string]$Json, [string]$HookAgent, [string]$Label, [string]$Pattern) {
            $output = $Json | pwsh -NoProfile -File $ownership -Agent $HookAgent 2>&1
            $code = $LASTEXITCODE
            $text = Get-FlatOutput $output
            $ok = ($code -eq 2) -and ($text -match $Pattern)
            $script:total++
            if ($ok) { "PASS enforce-path-ownership.ps1: $Label" } else { $script:failed++; "FAIL enforce-path-ownership.ps1: $Label (exit $code): $text" }
        }
        if (Test-Path $concurrentAuthorsPath) { Remove-Item -Force $concurrentAuthorsPath }
        Set-Content -LiteralPath $concurrentAuthorsPath -Value '{"code":{"agent":"audit-verifier","utc":"2026-01-01T00:00:00Z"}}{"code":{"agent":"audit-verifier","utc":"2026-01-01T00:00:00Z"}}'
        Test-OwnershipDenialCase $concurrentArchivistPayload 'issue-archivist' 'an unparseable sidecar denies the write with the parse error and the repair instruction (#1374)' '(?s)(?=.*not valid JSON)(?=.*RepairAuthors)'

        # (c) audit-commit-stage.ps1 distinguishes an unreadable sidecar from "no recorded author" (#1374):
        # reusing $commitWt, whose .authors.json is currently a valid entry for 'code' from the case above.
        Set-Content -LiteralPath (Join-Path $commitWt 'artifacts\knowledge\stages\.authors.json') '{"code":{"agent":"issue-auditor","utc":"2026-01-01T00:00:00Z"}}{"code":{"agent":"issue-auditor","utc":"2026-01-01T00:00:00Z"}}'
        $unreadableResult = Invoke-CommitStage
        $script:total++
        if ($unreadableResult.Code -ne 0 -and $unreadableResult.Output -match 'unreadable' -and $unreadableResult.Output -notmatch 'no recorded author') {
            'PASS audit-commit-stage.ps1: an unparseable sidecar is reported as unreadable with the parse error, not "no recorded author" (#1374)'
        }
        else {
            $script:failed++
            "FAIL audit-commit-stage.ps1: unreadable-sidecar distinction (#1374) (exit $($unreadableResult.Code)): $($unreadableResult.Output)"
        }

        # (d) audit-stage.ps1 -RepairAuthors restores the sidecar from the committed version at HEAD and lists
        # the stage entries the working-tree copy had that the committed copy lacks. A standalone, self-
        # referential audit worktree (its own current-audit.json points at itself), like $commitWt above.
        $repairWt = Join-Path $work 'RepairAuthorsWt'
        if (Test-Path $repairWt) { Remove-Item -Recurse -Force $repairWt }
        New-Item -ItemType Directory -Force (Join-Path $repairWt 'tools\ai\audit') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $repairWt 'artifacts\knowledge\stages') | Out-Null
        Copy-Item (Join-Path $hooks '..\..\tools\ai\audit\audit-stage.ps1') (Join-Path $repairWt 'tools\ai\audit\audit-stage.ps1')
        Copy-Item (Join-Path $hooks '..\..\tools\ai\audit\_audit-lib.ps1') (Join-Path $repairWt 'tools\ai\audit\_audit-lib.ps1')
        function Invoke-RepairWtGit { & git -C $repairWt -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
        Invoke-RepairWtGit init -q -b main
        Invoke-RepairWtGit config user.name hooks
        Invoke-RepairWtGit config user.email hooks@example.invalid
        Invoke-RepairWtGit commit -q --allow-empty -m base
        Set-Content (Join-Path $repairWt 'artifacts\knowledge\stages\code.md') 'x'
        @{ code = @{ agent = 'issue-auditor'; utc = '2026-01-01T00:00:00Z' } } | ConvertTo-Json | Set-Content (Join-Path $repairWt 'artifacts\knowledge\stages\.authors.json')
        Invoke-RepairWtGit add -f 'artifacts/knowledge'
        Invoke-RepairWtGit commit -q -m 'audit #78: code stage' -m 'Stage: code'
        # Corrupt the working tree: a 'tests' entry recorded but never committed, mangled into two
        # concatenated objects (the exact #1374 shape).
        $repairCorrupt = '{"code":{"agent":"issue-auditor","utc":"2026-01-01T00:00:00Z"},"tests":{"agent":"test-auditor","utc":"2026-01-02T00:00:00Z"}}{"code":{"agent":"issue-auditor","utc":"2026-01-01T00:00:00Z"},"tests":{"agent":"test-auditor","utc":"2026-01-02T00:00:00Z"}}'
        Set-Content (Join-Path $repairWt 'artifacts\knowledge\stages\.authors.json') $repairCorrupt
        @{ issue = 78; worktree = $repairWt; branch = 'audit/78'; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $repairWt 'artifacts\knowledge\current-audit.json')
        $repairOutput = & pwsh -NoProfile -File (Join-Path $repairWt 'tools\ai\audit\audit-stage.ps1') -RepairAuthors 2>&1
        $repairCode = $LASTEXITCODE
        $repairResultRaw = Get-Content -LiteralPath (Join-Path $repairWt 'artifacts\knowledge\stages\.authors.json') -Raw
        $repairOk = $false
        try {
            $repairParsed = $repairResultRaw | ConvertFrom-Json -AsHashtable
            $repairOk = $repairCode -eq 0 -and $null -ne $repairParsed -and $repairParsed.Count -eq 1 -and [string]$repairParsed['code'].agent -eq 'issue-auditor' -and (Get-FlatOutput $repairOutput) -match 'tests'
        }
        catch { $repairOk = $false }
        $script:total++
        if ($repairOk) { 'PASS audit-stage.ps1: -RepairAuthors restores the committed sidecar and lists the dropped stage entries (#1374)' }
        else { $script:failed++; "FAIL audit-stage.ps1: -RepairAuthors (#1374) (exit $repairCode): $(Get-FlatOutput $repairOutput); sidecar now: $repairResultRaw" }

        # -RepairAuthors refuses when no audit is open, same as -Next.
        Remove-Item -Force (Join-Path $repairWt 'artifacts\knowledge\current-audit.json')
        $noAuditOutput = & pwsh -NoProfile -File (Join-Path $repairWt 'tools\ai\audit\audit-stage.ps1') -RepairAuthors 2>&1
        $script:total++
        if ($LASTEXITCODE -ne 0) { 'PASS audit-stage.ps1: -RepairAuthors refuses when no audit is open' }
        else { $script:failed++; "FAIL audit-stage.ps1: -RepairAuthors should refuse when no audit is open: $noAuditOutput" }
    }
    else {
        'SKIP audit-stage-guard.ps1: git is not on PATH'
    }
    $env:CLAUDE_PROJECT_DIR = $repo

    # ---- #1375: audit-draft-remediation.ps1 (Split-Findings, per-finding template routing, -DryRun) ----
    # One delimited block, appended after the audit-stage-guard.ps1 cases above so it stays clear of #1374's
    # edits nearby. Exercises the real _audit-lib.ps1/audit-draft-remediation.ps1 directly (not through a
    # hook): -DryRun -NoGh never calls the local model or `gh`, so this suite stays free and offline.
    . (Join-Path $repo 'tools\ai\audit\_audit-lib.ps1')

    function Test-RemediationCase([string]$Label, [scriptblock]$Check) {
        $script:total++
        try {
            if (& $Check) { "PASS audit-draft-remediation: $Label" }
            else { $script:failed++; "FAIL audit-draft-remediation: $Label" }
        }
        catch {
            $script:failed++
            "FAIL audit-draft-remediation: $Label ($($_.Exception.Message))"
        }
    }

    # ---- #1555: Get-StaleStageAfterVerification (shared _audit-lib.ps1 helper) and audit-done.ps1's use of it ----
    if (Get-Command git -ErrorAction SilentlyContinue) {
        # A tiny throwaway repo with explicit GIT_AUTHOR_DATE/GIT_COMMITTER_DATE per commit, so the tie case
        # does not depend on real elapsed time between two git calls on a possibly slow machine.
        $staleWt = Join-Path $work 'StaleHelperWt'
        if (Test-Path $staleWt) { Remove-Item -Recurse -Force $staleWt }
        New-Item -ItemType Directory -Force (Join-Path $staleWt 'artifacts\knowledge\stages') | Out-Null
        & git -C $staleWt -c user.name=hooks -c user.email=hooks@example.invalid init -q -b main 2>&1 | Out-Null
        & git -C $staleWt -c user.name=hooks -c user.email=hooks@example.invalid commit -q --allow-empty -m base 2>&1 | Out-Null
        $stalePipeline = $defaultPipelineJson | ConvertFrom-Json

        # A dedicated wrapper (not the reused Test-RemediationCase) so these cases report under their own
        # component name instead of misleadingly under 'audit-draft-remediation:'.
        function Test-StaleCase([string]$Label, [scriptblock]$Check) {
            $script:total++
            try {
                if (& $Check) { "PASS Get-StaleStageAfterVerification: $Label" }
                else { $script:failed++; "FAIL Get-StaleStageAfterVerification: $Label" }
            }
            catch {
                $script:failed++
                "FAIL Get-StaleStageAfterVerification: $Label ($($_.Exception.Message))"
            }
        }

        function Set-StaleCommit([string]$RelativePath, [string]$Content, [int]$UnixSeconds) {
            $full = Join-Path $staleWt ($RelativePath -replace '/', '\')
            New-Item -ItemType Directory -Force (Split-Path -Parent $full) | Out-Null
            Set-Content -LiteralPath $full -Value $Content
            & git -C $staleWt add -f $RelativePath 2>&1 | Out-Null
            $env:GIT_AUTHOR_DATE = "@$UnixSeconds +0000"
            $env:GIT_COMMITTER_DATE = "@$UnixSeconds +0000"
            & git -C $staleWt -c user.name=hooks -c user.email=hooks@example.invalid commit -q -m "commit at $UnixSeconds" 2>&1 | Out-Null
            Remove-Item Env:\GIT_AUTHOR_DATE, Env:\GIT_COMMITTER_DATE -ErrorAction SilentlyContinue
        }

        Set-StaleCommit 'artifacts/knowledge/stages/remediation.md' 'v1' 1000
        Set-StaleCommit 'artifacts/knowledge/stages/verification.md' 'Verdict: PASS' 2000
        $notStale = Get-StaleStageAfterVerification $staleWt $stalePipeline
        Test-StaleCase 'verification committed after remediation -- not stale' { $null -eq $notStale }

        Set-StaleCommit 'artifacts/knowledge/stages/remediation.md' 'v2' 3000
        $staleAfter = Get-StaleStageAfterVerification $staleWt $stalePipeline
        Test-StaleCase 'remediation re-committed after verification -- stale, names the remediation stage' { $null -ne $staleAfter -and $staleAfter.stage -eq 'remediation' }

        # Distinguishable content ('Verdict: PASS (recheck)') so this actually creates a new commit at the same
        # second as remediation's last one above -- identical content would leave nothing to commit and the
        # verification commit time would stay at 2000, silently passing this case for the wrong reason.
        Set-StaleCommit 'artifacts/knowledge/stages/verification.md' "Verdict: PASS`n(recheck)" 3000
        $tieResult = Get-StaleStageAfterVerification $staleWt $stalePipeline
        Test-StaleCase 'a tied commit second is NOT stale (decision 1)' { $null -eq $tieResult }

        # audit-done.ps1's own use of the helper: a standalone, self-referential repo (its own current-audit.json
        # points at itself, like $commitWt/$archivistWt above) with NO .github\scripts\knowledge-records.cs, so
        # every run refuses for that unrelated reason regardless of staleness -- letting these two cases isolate
        # the #1555 check alone (present vs. absent in the output) without needing dotnet or a real knowledge
        # record. Both fail against the unmodified audit-done.ps1, which never checks staleness at all.
        $doneWt = Join-Path $work 'AuditDoneWt'
        $doneN = 5555
        function Initialize-DoneWorktree {
            if (Test-Path $doneWt) { Remove-Item -Recurse -Force $doneWt }
            New-Item -ItemType Directory -Force (Join-Path $doneWt 'tools\ai\audit') | Out-Null
            New-Item -ItemType Directory -Force (Join-Path $doneWt 'artifacts\knowledge\stages') | Out-Null
            New-Item -ItemType Directory -Force (Join-Path $doneWt 'artifacts\knowledge\issues') | Out-Null
            Copy-Item (Join-Path $repo 'tools\ai\audit\audit-done.ps1') (Join-Path $doneWt 'tools\ai\audit\audit-done.ps1')
            Copy-Item (Join-Path $repo 'tools\ai\audit\_audit-lib.ps1') (Join-Path $doneWt 'tools\ai\audit\_audit-lib.ps1')
            Set-Content (Join-Path $doneWt 'tools\ai\audit\pipeline.json') $defaultPipelineJson
            & git -C $doneWt -c user.name=hooks -c user.email=hooks@example.invalid init -q -b main 2>&1 | Out-Null
            & git -C $doneWt -c user.name=hooks -c user.email=hooks@example.invalid commit -q --allow-empty -m base 2>&1 | Out-Null
            @{ issue = $doneN; worktree = $doneWt; branch = "audit/$doneN"; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $doneWt 'artifacts\knowledge\current-audit.json')
        }
        function Invoke-DoneGit { & git -C $doneWt -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
        function Write-DoneStage([string]$StageName, [string]$ArtifactName, [string]$Content, [int]$UnixSeconds) {
            Set-Content (Join-Path $doneWt "artifacts\knowledge\stages\$ArtifactName") $Content
            Invoke-DoneGit add -f "artifacts/knowledge/stages/$ArtifactName"
            $env:GIT_AUTHOR_DATE = "@$UnixSeconds +0000"
            $env:GIT_COMMITTER_DATE = "@$UnixSeconds +0000"
            Invoke-DoneGit commit -q -m "audit #$doneN`: $StageName stage" -m "Stage: $StageName"
            Remove-Item Env:\GIT_AUTHOR_DATE, Env:\GIT_COMMITTER_DATE -ErrorAction SilentlyContinue
        }
        function Test-DoneCase([string]$Label, [string]$Pattern, [string]$NotPattern) {
            $output = & pwsh -NoProfile -File (Join-Path $doneWt 'tools\ai\audit\audit-done.ps1') 2>&1
            $flat = Get-FlatOutput $output
            $ok = $LASTEXITCODE -ne 0
            if ($ok -and $Pattern) { $ok = $flat -match $Pattern }
            if ($ok -and $NotPattern) { $ok = $flat -notmatch $NotPattern }
            $script:total++
            if ($ok) { "PASS audit-done.ps1: $Label" } else { $script:failed++; "FAIL audit-done.ps1: $Label (exit $LASTEXITCODE): $flat" }
        }

        Initialize-DoneWorktree
        Write-DoneStage 'archivist' 'archivist.md' "x`n## Lessons for the pipeline`n- none`n" 1000
        Write-DoneStage 'code' 'code.md' "x`n## Lessons for the pipeline`n- none`n" 1001
        Write-DoneStage 'tests' 'tests.md' "x`n## Lessons for the pipeline`n- none`n" 1002
        Write-DoneStage 'docs' 'docs.md' "x`n## Lessons for the pipeline`n- none`n" 1003
        Write-DoneStage 'remediation' 'remediation.md' 'v1' 1004
        Write-DoneStage 'verification' 'verification.md' "Verdict: PASS`n## Lessons for the pipeline`n- none`n" 1005
        Set-Content (Join-Path $doneWt 'artifacts\knowledge\stages\lessons.md') "no lessons`n"
        Write-DoneStage 'remediation' 'remediation.md' 'v2 regenerated' 2000
        Test-DoneCase 'refuses to close when remediation was re-committed after the verification PASS, naming the stale stage (#1555)' 'stale.*remediation' $null

        Initialize-DoneWorktree
        Write-DoneStage 'archivist' 'archivist.md' "x`n## Lessons for the pipeline`n- none`n" 1000
        Write-DoneStage 'code' 'code.md' "x`n## Lessons for the pipeline`n- none`n" 1001
        Write-DoneStage 'tests' 'tests.md' "x`n## Lessons for the pipeline`n- none`n" 1002
        Write-DoneStage 'docs' 'docs.md' "x`n## Lessons for the pipeline`n- none`n" 1003
        Write-DoneStage 'remediation' 'remediation.md' 'v1' 1004
        Write-DoneStage 'verification' 'verification.md' "Verdict: PASS`n## Lessons for the pipeline`n- none`n" 1005
        Set-Content (Join-Path $doneWt 'artifacts\knowledge\stages\lessons.md') "no lessons`n"
        Test-DoneCase 'does not flag staleness when nothing was committed after the verification PASS (#1555)' $null 'stale'
    }
    else {
        'SKIP #1555: git is not on PATH'
    }

    # (a) Split-Findings splits the numbered "N. **Severity** -- ..." paragraphs the stage agents write,
    # including a finding with a continuation line and a blank-line-separated finding (issue-auditor's style).
    $codeFindingsText = "1. **Blocker** -- ``src/A.cs:1`` first.`nmore.`n`n2. **Major** -- ``src/B.cs:2`` second.`n`n3. **Minor** -- ``src/C.cs:3`` third."
    $codeSplit = @(Split-Findings 'code' $codeFindingsText)
    Test-RemediationCase 'Split-Findings: 3 code findings with the right severities, in order' { $codeSplit.Count -eq 3 -and $codeSplit[0].Severity -eq 'Blocker' -and $codeSplit[1].Severity -eq 'Major' -and $codeSplit[2].Severity -eq 'Minor' -and $codeSplit[0].Text -match 'more\.' }

    $testsFindingsText = "1. **Major** -- ``tests/X.cs:1`` a.`n2. **Minor** -- ``tests/Y.cs:2`` b."
    $testsSplit = @(Split-Findings 'tests' $testsFindingsText)
    Test-RemediationCase 'Split-Findings: 2 tests findings with the right severities' { $testsSplit.Count -eq 2 -and $testsSplit[0].Severity -eq 'Major' -and $testsSplit[1].Severity -eq 'Minor' }

    $docsFindingsText = "1. **Blocker** -- ``docs/a.md:1`` a.`n2. **Major** -- ``docs/b.md:2`` b."
    $docsSplit = @(Split-Findings 'docs' $docsFindingsText)
    Test-RemediationCase 'Split-Findings: 2 docs findings with the right severities' { $docsSplit.Count -eq 2 -and $docsSplit[0].Severity -eq 'Blocker' -and $docsSplit[1].Severity -eq 'Major' }

    # (d) "- none" (the stage agents' own convention for "nothing survives review") yields zero findings, but a
    # non-empty section the parser cannot recognize never yields zero silently: one 'Unknown' finding instead.
    Test-RemediationCase 'Split-Findings: "- none" yields zero findings' { @(Split-Findings 'code' '- none').Count -eq 0 }
    $unknownSplit = @(Split-Findings 'code' 'Some free-form paragraph with no numbered severity line at all.')
    Test-RemediationCase 'Split-Findings: an unrecognized non-empty section yields one Unknown finding, never silently zero' { $unknownSplit.Count -eq 1 -and $unknownSplit[0].Severity -eq 'Unknown' }

    # CodeRabbit review of PR #1378 (thread 1): a repeated finding number within one stage is a malformed
    # artifact and must be an explicit error, never a silent overwrite of the first finding.
    Test-RemediationCase 'Split-Findings: a duplicate finding id within one stage throws' {
        $threw = $false
        try { [void](Split-Findings 'code' "1. **Blocker** -- a.`n1. **Major** -- b.") }
        catch { $threw = $_.Exception.Message -match "stage 'code'" -and $_.Exception.Message -match "'1'" }
        $threw
    }

    # CodeRabbit review of PR #1378 (thread 2): a numbered paragraph whose bold token is not one of
    # Blocker/Major/Minor (a typo like **Critical**) still starts a NEW finding with Severity 'Unknown',
    # whether it appears after a recognized finding or as the very first line -- never appended to the
    # previous finding's body, and never discarded when nothing has started yet.
    $midStreamSplit = @(Split-Findings 'code' "1. **Blocker** -- a.`n2. **Critical** -- b.`n3. **Minor** -- c.")
    Test-RemediationCase 'Split-Findings: an unrecognized severity token mid-stream starts a new Unknown finding, not appended to the previous one' {
        $midStreamSplit.Count -eq 3 -and $midStreamSplit[0].Severity -eq 'Blocker' -and $midStreamSplit[0].Text -notmatch 'Critical|b\.' -and $midStreamSplit[1].Severity -eq 'Unknown' -and $midStreamSplit[1].Text -match 'b\.' -and $midStreamSplit[2].Severity -eq 'Minor'
    }
    $firstLineUnknownSplit = @(Split-Findings 'code' "1. **Critical** -- first.`n2. **Major** -- second.")
    Test-RemediationCase 'Split-Findings: an unrecognized severity token as the first line still starts a finding, not discarded' {
        $firstLineUnknownSplit.Count -eq 2 -and $firstLineUnknownSplit[0].Severity -eq 'Unknown' -and $firstLineUnknownSplit[0].Text -match 'first\.' -and $firstLineUnknownSplit[1].Severity -eq 'Major'
    }

    # CodeRabbit review of PR #1378 (thread 7): Test-ValidDuplicate is extracted from audit-draft-remediation.ps1
    # so it can be unit-tested directly -- the classification/dedup path itself needs the local model and is
    # unreachable from -DryRun, so this is the "unit-style case around the validation helper" alternative.
    $draftScriptText = Get-Content (Join-Path $repo 'tools\ai\audit\audit-draft-remediation.ps1') -Raw
    $validDuplicateFuncMatch = [regex]::Match($draftScriptText, '(?ms)^function Test-ValidDuplicate.*?^\}')
    Test-RemediationCase 'audit-draft-remediation.ps1 still defines Test-ValidDuplicate (extraction target found)' { $validDuplicateFuncMatch.Success }
    if ($validDuplicateFuncMatch.Success) {
        Invoke-Expression $validDuplicateFuncMatch.Value
        Test-RemediationCase 'Test-ValidDuplicate: a duplicate-of number that IS one of the candidates is valid' { Test-ValidDuplicate '42' @('1', '42', '99') }
        Test-RemediationCase 'Test-ValidDuplicate: a duplicate-of number that is NOT one of the candidates is rejected' { -not (Test-ValidDuplicate '7' @('1', '42', '99')) }
        Test-RemediationCase 'Test-ValidDuplicate: no candidates at all rejects any duplicate-of' { -not (Test-ValidDuplicate '1' @()) }
    }

    if (Get-Command git -ErrorAction SilentlyContinue) {
        # A self-contained repo (its own '.git', so Get-MainRoot resolves to itself -- the same trick $auditWt
        # and $commitWt use above) carrying its own copies of the real scripts, so $PSScriptRoot resolves
        # inside the fixture, not the real checkout.
        $remWt = Join-Path $work 'RemediationWt'
        if (Test-Path $remWt) { Remove-Item -Recurse -Force $remWt }
        New-Item -ItemType Directory -Force (Join-Path $remWt 'tools\ai\audit') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $remWt 'artifacts\knowledge\stages') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $remWt '.github\ISSUE_TEMPLATE') | Out-Null
        Copy-Item (Join-Path $repo 'tools\ai\audit\pipeline.json') (Join-Path $remWt 'tools\ai\audit\pipeline.json')
        Copy-Item (Join-Path $repo 'tools\ai\audit\_audit-lib.ps1') (Join-Path $remWt 'tools\ai\audit\_audit-lib.ps1')
        # #1388: audit-draft-remediation.ps1 now also dot-sources _remediation-checks.ps1 (Get-FindingAnchors,
        # Test-DuplicateEvidence, Remove-OuterFence, Find-TemplatePlaceholders; #1424 added
        # Find-DuplicateAmongCandidates), so this fixture needs its own copy too, exactly like _audit-lib.ps1
        # above.
        Copy-Item (Join-Path $repo 'tools\ai\audit\_remediation-checks.ps1') (Join-Path $remWt 'tools\ai\audit\_remediation-checks.ps1')
        Copy-Item (Join-Path $repo 'tools\ai\audit\audit-draft-remediation.ps1') (Join-Path $remWt 'tools\ai\audit\audit-draft-remediation.ps1')
        foreach ($t in 'bug_report.md', 'test_implementation.md', 'technical_debt.md') {
            Copy-Item (Join-Path $repo ".github\ISSUE_TEMPLATE\$t") (Join-Path $remWt ".github\ISSUE_TEMPLATE\$t")
        }
        function Invoke-RemGit { & git -C $remWt -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
        Invoke-RemGit init -q -b main
        Invoke-RemGit commit -q --allow-empty -m base

        Set-Content (Join-Path $remWt 'artifacts\knowledge\stages\code.md') "## Findings`n$codeFindingsText`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt 'artifacts\knowledge\stages\tests.md') "## Findings`n$testsFindingsText`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt 'artifacts\knowledge\stages\docs.md') "## Findings`n$docsFindingsText`n## Lessons for the pipeline`n- none`n"
        $remN = 4242
        @{ issue = $remN; worktree = $remWt; branch = "audit/$remN"; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $remWt 'artifacts\knowledge\current-audit.json')

        $remOutput = & pwsh -NoProfile -File (Join-Path $remWt 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh 2>&1
        $remExit = $LASTEXITCODE
        Test-RemediationCase '-DryRun -NoGh exits 0 and never calls the model or gh' { $remExit -eq 0 }

        $dryDir = Join-Path $remWt "artifacts\knowledge\remediation\_dryrun-$remN"
        $inputFiles = @(Get-ChildItem $dryDir -Filter '*-input.md' -ErrorAction SilentlyContinue)
        $briefFiles = @(Get-ChildItem $dryDir -Filter '*-brief.md' -ErrorAction SilentlyContinue)
        # (b) 7 findings (3 + 2 + 2) -> 7 input files and 7 briefs.
        Test-RemediationCase '-DryRun -NoGh writes 7 per-finding input files' { $inputFiles.Count -eq 7 }
        Test-RemediationCase '-DryRun -NoGh writes 7 per-finding briefs' { $briefFiles.Count -eq 7 }

        # Deterministic fallback routing (decision 5): tests stage -> test; docs stage -> docs; a code Blocker
        # -> bug; everything else -> debt. Each brief's first '## ' header must match its routed template's.
        $expectedFirstHeader = @{
            'code-1-brief.md'  = '## Description'          # bug_report.md
            'code-2-brief.md'  = '## Type'                  # technical_debt.md (Major, not a Blocker)
            'code-3-brief.md'  = '## Type'                  # technical_debt.md (Minor)
            'tests-1-brief.md' = '## Test Category'          # test_implementation.md
            'tests-2-brief.md' = '## Test Category'
            'docs-1-brief.md'  = '## Type'                  # technical_debt.md (docs kind)
            'docs-2-brief.md'  = '## Type'
        }
        foreach ($fileName in $expectedFirstHeader.Keys) {
            $path = Join-Path $dryDir $fileName
            $firstHeader = if (Test-Path -LiteralPath $path) { @(Get-Content -LiteralPath $path | Where-Object { $_ -match '^##\s' })[0] } else { $null }
            Test-RemediationCase "-DryRun brief '$fileName' routes to the template whose first header is '$($expectedFirstHeader[$fileName])'" { $firstHeader -eq $expectedFirstHeader[$fileName] }
        }

        # (c) stages/remediation.md lists one line per finding (7), plus a real Lessons section -- -DryRun
        # previews it too (see the script's own comment: not a model call, always regenerated for real later).
        $remStageLines = @(Get-Content (Join-Path $remWt 'artifacts\knowledge\stages\remediation.md') | Where-Object { $_ -match '^-\s+\w+\s+\d+\s+\(' })
        Test-RemediationCase 'stages/remediation.md lists 7 finding lines' { $remStageLines.Count -eq 7 }

        # A regression in the em-dash/label construction would still pass every check above (they only look at
        # the first '## ' header); check the actual header-comment content of one bug-routed and one
        # docs-routed brief so the milestone and label lines are verified, not just the routed template.
        $bugBriefText = Get-Content -LiteralPath (Join-Path $dryDir 'code-1-brief.md') -Raw
        Test-RemediationCase "bug-routed brief 'code-1-brief.md' carries the real Hardening milestone with its em dash" { $bugBriefText -match [regex]::Escape("milestone: v0.14.0 $([char]0x2014) Hardening") }
        Test-RemediationCase "bug-routed brief 'code-1-brief.md' carries the 'bug' label and [BUG] prefix" { $bugBriefText -match 'labels:\s*bug\b' -and $bugBriefText -match 'title:\s*\[BUG\]' }
        $docsBriefText = Get-Content -LiteralPath (Join-Path $dryDir 'docs-1-brief.md') -Raw
        Test-RemediationCase "docs-routed brief 'docs-1-brief.md' carries the area-documentation label, [DEBT] prefix and an empty milestone" { $docsBriefText -match 'labels:\s*technical-debt,\s*area-documentation' -and $docsBriefText -match 'title:\s*\[DEBT\]' -and $docsBriefText -match '(?m)^milestone:\s*$' }

        # CodeRabbit review of PR #1378 (thread 5): re-running the stage for the same audit must remove its own
        # previous outputs (here, the _dryrun-<n> preview from the run above) before writing fresh ones, and
        # print what it removed, rather than accumulating stale files across re-runs.
        $remOutput2 = & pwsh -NoProfile -File (Join-Path $remWt 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh 2>&1
        $remExit2 = $LASTEXITCODE
        Test-RemediationCase 're-running -DryRun -NoGh exits 0 and prints that it removed the previous _dryrun output' { $remExit2 -eq 0 -and (Get-FlatOutput $remOutput2) -match "removed previous output _dryrun-$remN" }
        $inputFilesAfterRerun = @(Get-ChildItem $dryDir -Filter '*-input.md' -ErrorAction SilentlyContinue)
        Test-RemediationCase 're-running -DryRun -NoGh does not accumulate stale files (still 7 input files, not 14)' { $inputFilesAfterRerun.Count -eq 7 }

        # CodeRabbit review of PR #1378 (thread 4): a stage artifact whose '## Findings' header is missing
        # entirely is an error, distinct from a header present with an explicit '- none' body -- restore the
        # file afterwards so it does not affect any later case that reuses $remWt.
        $testsStageFile = Join-Path $remWt 'artifacts\knowledge\stages\tests.md'
        $testsStageBackup = Get-Content -LiteralPath $testsStageFile -Raw
        Set-Content -LiteralPath $testsStageFile -Encoding utf8 -Value "No '## Findings' header here, just prose.`n## Lessons for the pipeline`n- none`n"
        $missingHeaderOutput = & pwsh -NoProfile -File (Join-Path $remWt 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh 2>&1
        $missingHeaderExit = $LASTEXITCODE
        Test-RemediationCase "a stage artifact missing the '## Findings' header is an error (exit non-zero, names the file)" { $missingHeaderExit -ne 0 -and (Get-FlatOutput $missingHeaderOutput) -match [regex]::Escape('tests.md') -and (Get-FlatOutput $missingHeaderOutput) -match "## Findings' header" }
        Set-Content -LiteralPath $testsStageFile -Encoding utf8 -Value $testsStageBackup

        # CodeRabbit review of PR #1378 (thread 1, end to end): a duplicate finding id within one stage
        # artifact surfaces as Write-Error + a non-zero exit, not a silently overwritten finding.
        Set-Content -LiteralPath $testsStageFile -Encoding utf8 -Value "## Findings`n1. **Major** -- ``tests/X.cs:1`` first.`n1. **Minor** -- ``tests/Y.cs:2`` duplicate id.`n## Lessons for the pipeline`n- none`n"
        $dupIdOutput = & pwsh -NoProfile -File (Join-Path $remWt 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh 2>&1
        $dupIdExit = $LASTEXITCODE
        Test-RemediationCase "a duplicate finding id within one stage is an error end to end (exit non-zero, names the stage and id)" { $dupIdExit -ne 0 -and (Get-FlatOutput $dupIdOutput) -match "stage 'tests'" -and (Get-FlatOutput $dupIdOutput) -match "'1'" }
        Set-Content -LiteralPath $testsStageFile -Encoding utf8 -Value $testsStageBackup
    }
    else {
        'SKIP audit-draft-remediation.ps1: git is not on PATH'
    }
    # ---- end #1375 block ----

    # ---- #1388: tools/ai/audit/_remediation-checks.ps1 (duplicate evidence, outer-fence stripping, template
    # placeholder detection) -- the three defect classes audit #16's remediation stage produced (a duplicate
    # claim with no shared evidence, a fenced draft, a draft that kept the template's own placeholder text).
    # Exercises the real _remediation-checks.ps1 directly: no `gh` and no local model, so this suite stays
    # free and offline; the duplicate-evidence cases instead replay real fixture text captured once from
    # `gh issue view` (fixtures/1388/issue-*.json) and the real code/docs stage finding paragraphs of audit #16
    # (fixtures/1388/finding-*.md), and the placeholder cases replay the real drafts audit #16 produced
    # (fixtures/1388/16-tests-*.md).
    . (Join-Path $repo 'tools\ai\audit\_remediation-checks.ps1')
    $fixtures1388 = Join-Path $repo '.claude\hooks\tests\fixtures\1388'

    function Test-RemediationChecksCase([string]$Label, [scriptblock]$Check) {
        $script:total++
        try {
            if (& $Check) { "PASS remediation-checks: $Label" }
            else { $script:failed++; "FAIL remediation-checks: $Label" }
        }
        catch {
            $script:failed++
            "FAIL remediation-checks: $Label ($($_.Exception.Message))"
        }
    }

    $findingCode1 = Get-Content -LiteralPath (Join-Path $fixtures1388 'finding-code-1.md') -Raw
    $findingCode2 = Get-Content -LiteralPath (Join-Path $fixtures1388 'finding-code-2.md') -Raw
    $findingCode4 = Get-Content -LiteralPath (Join-Path $fixtures1388 'finding-code-4.md') -Raw
    $findingDocs9 = Get-Content -LiteralPath (Join-Path $fixtures1388 'finding-docs-9.md') -Raw
    $issue1333 = Get-Content -LiteralPath (Join-Path $fixtures1388 'issue-1333.json') -Raw | ConvertFrom-Json
    $issue1328 = Get-Content -LiteralPath (Join-Path $fixtures1388 'issue-1328.json') -Raw | ConvertFrom-Json
    $issue1170 = Get-Content -LiteralPath (Join-Path $fixtures1388 'issue-1170.json') -Raw | ConvertFrom-Json
    $issue1177 = Get-Content -LiteralPath (Join-Path $fixtures1388 'issue-1177.json') -Raw | ConvertFrom-Json

    # (a) The four real duplicate claims from audit #16: three false (code 1 -> #1333, code 2 -> #1328,
    # docs 9 -> #1177) and one true (code 4 -> #1170). Exact expected outcome per finding, matching the
    # orchestrator's own confirmation in #1388's issue body.
    Test-RemediationChecksCase 'Test-DuplicateEvidence: code finding 1 vs #1333 is REJECTED (false duplicate claim)' {
        -not (Test-DuplicateEvidence $findingCode1 "$($issue1333.title)`n$($issue1333.body)")
    }
    Test-RemediationChecksCase 'Test-DuplicateEvidence: code finding 2 vs #1328 is REJECTED (false duplicate claim)' {
        -not (Test-DuplicateEvidence $findingCode2 "$($issue1328.title)`n$($issue1328.body)")
    }
    Test-RemediationChecksCase 'Test-DuplicateEvidence: code finding 4 vs #1170 is ACCEPTED (the one true duplicate claim)' {
        Test-DuplicateEvidence $findingCode4 "$($issue1170.title)`n$($issue1170.body)"
    }
    Test-RemediationChecksCase 'Test-DuplicateEvidence: docs finding 9 vs #1177 is REJECTED (false duplicate claim)' {
        -not (Test-DuplicateEvidence $findingDocs9 "$($issue1177.title)`n$($issue1177.body)")
    }

    # (b) A finding with a file anchor but no backticked symbol at all can never be auto-accepted, even against
    # a candidate that repeats the file path and every other word of the finding.
    $noSymbolFinding = 'src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs has a defect, but this sentence backticks nothing.'
    $echoingCandidate = 'title: src/Encina.ADO.SqlServer/Sagas/SagaStoreADO.cs has a defect, but this sentence backticks nothing, word for word.'
    Test-RemediationChecksCase 'Test-DuplicateEvidence: a finding with no backticked symbol is never accepted' {
        -not (Test-DuplicateEvidence $noSymbolFinding $echoingCandidate)
    }

    # (c) Remove-OuterFence strips exactly one outer ```markdown fence (the real 16-tests-1 draft, #1388's own
    # reproduction of the defect) and one bare ``` fence, but leaves an inner fence and an already-unfenced
    # draft (the real 16-tests-3 draft) untouched.
    $fencedDraft = Get-Content -LiteralPath (Join-Path $fixtures1388 '16-tests-1-outer-fence.md') -Raw
    $defencedDraft = Remove-OuterFence $fencedDraft
    Test-RemediationChecksCase 'Remove-OuterFence: strips the real 16-tests-1 outer ```markdown fence' {
        $defencedDraft.TrimStart().StartsWith('<!--') -and -not $defencedDraft.TrimEnd().EndsWith('```')
    }
    $tripleBacktick = [string]::new([char]0x60, 3)
    $bareFenced = ($tripleBacktick, 'plain content', 'more content', $tripleBacktick) -join "`n"
    $bareFencedExpected = ('plain content', 'more content') -join "`n"
    Test-RemediationChecksCase 'Remove-OuterFence: strips a bare outer triple-backtick fence (no markdown tag)' {
        (Remove-OuterFence $bareFenced) -eq $bareFencedExpected
    }
    $innerFenceOnly = ('## Description', '', 'Some text.', '', $tripleBacktick + 'csharp', 'code sample', $tripleBacktick, '', 'More text.') -join "`n"
    Test-RemediationChecksCase 'Remove-OuterFence: leaves an inner code fence untouched' {
        (Remove-OuterFence $innerFenceOnly) -eq $innerFenceOnly
    }
    $unfencedDraft = Get-Content -LiteralPath (Join-Path $fixtures1388 '16-tests-3-placeholders.md') -Raw
    Test-RemediationChecksCase 'Remove-OuterFence: leaves an already-unfenced draft (the real 16-tests-3) untouched' {
        (Remove-OuterFence $unfencedDraft) -eq $unfencedDraft
    }

    # (d) Find-TemplatePlaceholders finds the five placeholder lines #1388's issue body names in the real
    # 16-tests-3 draft, and nothing at all in the real 16-tests-1 draft once its outer fence is stripped (a
    # clean, fully-filled draft). Both drafts route to test_implementation.md ([TEST] prefix); #1400 changed
    # the function's signature to take that ONE routed template's text, not the whole templates directory.
    $testTemplateTextFor1388 = Get-Content -LiteralPath (Join-Path $repo '.github\ISSUE_TEMPLATE\test_implementation.md') -Raw
    $foundPlaceholders = Find-TemplatePlaceholders $testTemplateTextFor1388 $unfencedDraft
    $expectedPlaceholderSubstrings = @(
        '[e.g., Encina.Dapper.SqlServer, Encina.ADO.PostgreSQL]',
        '| Example.Package | 62.3% | 85% | -22.7% |',
        '[ ] Test 1: Description',
        '[e.g., `ADO-PostgreSQL`, `Dapper-SqlServer`, `EFCore-MySQL`]',
        '#___ - Description'
    )
    foreach ($expected in $expectedPlaceholderSubstrings) {
        Test-RemediationChecksCase "Find-TemplatePlaceholders: the real 16-tests-3 draft still has '$expected'" {
            @($foundPlaceholders | Where-Object { $_.Contains($expected) }).Count -gt 0
        }
    }
    Test-RemediationChecksCase 'Find-TemplatePlaceholders: a clean, fully-filled draft (the real 16-tests-1, defenced) has none' {
        (Find-TemplatePlaceholders $testTemplateTextFor1388 $defencedDraft).Count -eq 0
    }
    # ---- end #1388 block ----

    # ---- #1400: tools/ai/audit/_remediation-checks.ps1 -- the coverage rule for a duplicate-of claim (every
    # file anchor, not just one), template-derived placeholders (a template's own instruction sentence, not
    # only its hand-written markers) and the Related Issues sanitizer (Limit-RelatedIssues), fixing the three
    # defect classes audit #16's verifier still found after #1388: a partial-duplicate claim accepted, a
    # template instruction line left in a draft, and an invented Related Issues entry. No `gh` and no local
    # model here either; fixtures replay real audit #16 material captured once (fixtures/1400/).
    $fixtures1400 = Join-Path $repo '.claude\hooks\tests\fixtures\1400'

    # (a) The real code finding 3 of audit #16 vs the real #1343: #1343 only covers `SagaRunner.cs`, but the
    # finding's own leading location clause also names `SagaOrchestrator.cs`, a second file #1343 never
    # mentions -- exactly the partial-duplicate defect #1400 was filed to fix. Test-DuplicateEvidence now
    # rejects it (previously accepted it, #1400's own reproduction), and Test-PartialDuplicateEvidence reports
    # true because the `SagaRunner.cs` anchor DID match.
    $findingCode3For1400 = Get-Content -LiteralPath (Join-Path $fixtures1400 'finding-code-3.md') -Raw
    $issue1343 = Get-Content -LiteralPath (Join-Path $fixtures1400 'issue-1343.json') -Raw | ConvertFrom-Json
    $candidate1343Text = "$($issue1343.title)`n$($issue1343.body)"
    Test-RemediationChecksCase 'Test-DuplicateEvidence: code finding 3 vs #1343 is REJECTED (partial duplicate, not the same finding)' {
        -not (Test-DuplicateEvidence $findingCode3For1400 $candidate1343Text)
    }
    Test-RemediationChecksCase 'Test-PartialDuplicateEvidence: code finding 3 vs #1343 is a partial match (SagaRunner.cs matched, SagaOrchestrator.cs did not)' {
        Test-PartialDuplicateEvidence $findingCode3For1400 $candidate1343Text
    }

    # (b) The four real #1388 duplicate claims still give the same outcomes as before -- #1400's stricter
    # "every file anchor" rule does not regress the one true duplicate (code finding 4 vs #1170, whose leading
    # location clause is exactly its 3 SagaStoreADO.cs citations, all matched by candidate #1170's own brace-
    # expanded file list) nor the three already-rejected false claims.
    $findingCode1For1400 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\finding-code-1.md') -Raw
    $findingCode2For1400 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\finding-code-2.md') -Raw
    $findingCode4For1400 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\finding-code-4.md') -Raw
    $findingDocs9For1400 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\finding-docs-9.md') -Raw
    $issue1333For1400 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\issue-1333.json') -Raw | ConvertFrom-Json
    $issue1328For1400 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\issue-1328.json') -Raw | ConvertFrom-Json
    $issue1170For1400 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\issue-1170.json') -Raw | ConvertFrom-Json
    $issue1177For1400 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\issue-1177.json') -Raw | ConvertFrom-Json
    Test-RemediationChecksCase '#1400 regression: code finding 1 vs #1333 is still REJECTED' {
        -not (Test-DuplicateEvidence $findingCode1For1400 "$($issue1333For1400.title)`n$($issue1333For1400.body)")
    }
    Test-RemediationChecksCase '#1400 regression: code finding 2 vs #1328 is still REJECTED' {
        -not (Test-DuplicateEvidence $findingCode2For1400 "$($issue1328For1400.title)`n$($issue1328For1400.body)")
    }
    Test-RemediationChecksCase '#1400 regression: code finding 4 vs #1170 is still ACCEPTED (the one true duplicate)' {
        Test-DuplicateEvidence $findingCode4For1400 "$($issue1170For1400.title)`n$($issue1170For1400.body)"
    }
    Test-RemediationChecksCase '#1400 regression: docs finding 9 vs #1177 is still REJECTED' {
        -not (Test-DuplicateEvidence $findingDocs9For1400 "$($issue1177For1400.title)`n$($issue1177For1400.body)")
    }

    # (c) Find-TemplatePlaceholders, given the ONE routed template's own text: the real 16-docs-1 draft still
    # has technical_debt.md's Related Issues instruction sentence "Link any related issues here." verbatim --
    # #1388's hand-written marker list never knew this line; a clean draft (the real, defenced 16-tests-1) has
    # none, against its own routed template (test_implementation.md).
    $technicalDebtTemplateText = Get-Content -LiteralPath (Join-Path $repo '.github\ISSUE_TEMPLATE\technical_debt.md') -Raw
    $docs1Draft = Get-Content -LiteralPath (Join-Path $fixtures1400 '16-docs-1-draft.md') -Raw
    $docs1Placeholders = Find-TemplatePlaceholders $technicalDebtTemplateText $docs1Draft
    Test-RemediationChecksCase 'Find-TemplatePlaceholders: the real 16-docs-1 draft still has "Link any related issues here."' {
        @($docs1Placeholders | Where-Object { $_ -eq 'Link any related issues here.' }).Count -gt 0
    }
    Test-RemediationChecksCase 'Find-TemplatePlaceholders: a clean draft (the real 16-tests-1, defenced) has none, against its own routed template' {
        (Find-TemplatePlaceholders $testTemplateTextFor1388 $defencedDraft).Count -eq 0
    }

    # (d) Limit-RelatedIssues on the real 16-code-5 draft (bug_report.md's '- **Related Issues**:' bullet
    # convention, since bug_report.md has no dedicated header): #699/#696/#181 are real open issues the model
    # invented a relation to -- they are absent from both the real finding text and an (empty, for this case)
    # candidate list -- while #16, the audited issue itself, is always kept.
    $findingCode5 = Get-Content -LiteralPath (Join-Path $fixtures1400 'finding-code-5.md') -Raw
    $code5Draft = Get-Content -LiteralPath (Join-Path $fixtures1400 '16-code-5-draft.md') -Raw
    $limited = Limit-RelatedIssues $code5Draft '16' $findingCode5 @()
    Test-RemediationChecksCase 'Limit-RelatedIssues: removes #699, #696 and #181 from the real 16-code-5 draft (not in the finding or candidates)' {
        (@($limited.Removed) | Sort-Object) -join ',' -eq '181,696,699'
    }
    Test-RemediationChecksCase 'Limit-RelatedIssues: keeps the allowed #16 (the audited issue itself) in the sanitized draft' {
        $limited.Text -match '#16 \(This issue\)'
    }
    Test-RemediationChecksCase 'Limit-RelatedIssues: drops the unverified lines from the sanitized draft text' {
        $limited.Text -notmatch '#699' -and $limited.Text -notmatch '#696' -and $limited.Text -notmatch '#181'
    }

    # (e) adversarial review finding 1: Add-RelatedIssuesLine places the "partially related" note inside the
    # real 16-code-5 draft's own bold-bullet Related Issues section (bug_report.md has no H2 header), not
    # detached at the end of the file -- so Limit-RelatedIssues (which runs right after, in
    # audit-draft-remediation.ps1) can actually see and verify it.
    $withNote = Add-RelatedIssuesLine $code5Draft '- #1343 - partially related (it covers only part of this finding)'
    Test-RemediationChecksCase 'Add-RelatedIssuesLine: finds the real 16-code-5 draft''s bold-bullet Related Issues section' { $withNote.Found }
    Test-RemediationChecksCase 'Add-RelatedIssuesLine: inserts the note as an indented sub-bullet right after the bold-bullet header' {
        ($withNote.Text -split "`r?`n") -contains '  - #1343 - partially related (it covers only part of this finding)'
    }

    # (f) adversarial review finding 2: Limit-RelatedIssues' bold-bullet convention also tolerates two other
    # plausible model outputs the single 16-code-5 fixture does not exercise -- unindented sibling bullets at
    # the same list level, and a header with no trailing colon.
    $unindentedSiblingsDraft = "## Additional Context`n`n- **Related Issues**:`n- #16 (This issue)`n- #699: unrelated`n"
    $unindentedResult = Limit-RelatedIssues $unindentedSiblingsDraft '16' '' @()
    Test-RemediationChecksCase 'Limit-RelatedIssues: removes an unverified reference from an unindented sibling bullet list' {
        (@($unindentedResult.Removed)) -contains '699'
    }
    $colonlessHeaderDraft = "## Additional Context`n`n- **Related Issues**`n  - #16 (This issue)`n  - #699: unrelated`n"
    $colonlessResult = Limit-RelatedIssues $colonlessHeaderDraft '16' '' @()
    Test-RemediationChecksCase 'Limit-RelatedIssues: recognizes a bold-bullet header with no trailing colon' {
        (@($colonlessResult.Removed)) -contains '699'
    }
    # ---- end #1400 block ----

    # ---- #1535: tools/ai/audit/_remediation-checks.ps1 -- Limit-RelatedIssues no longer leaves a broken list
    # item when it strips the issue number a Related Issues bullet is ABOUT. Before this fix, removing only the
    # '#n' token left three distinct broken shapes, all found in audit #18's live drafts: a doubled marker
    # ('- - [TEST] title', 18-tests-1..4), a dangling colon ('-: [DEBT] title', 18-docs-13/14) and a bare '-'
    # (18-docs-5). The fix drops the WHOLE bullet line when the removed reference is the bullet's own leading or
    # sole reference; ordinary prose (the reference is not the first thing after the bullet marker) still gets
    # only the token stripped, as before. A Related Issues section left with no bullets at all after this gets
    # "None." under its own header, instead of an empty section.

    # (a) "- #n - title" -> the whole bullet, not "- - title" (18-tests-1..4's own shape).
    $dashShapeDraft = "## Related Issues`n`n- #18 - [Bug] Scope-vs-singleton lifetime bug`n- #910 - [TEST] Increase coverage for 8 mejorable modules`n"
    $dashShapeResult = Limit-RelatedIssues $dashShapeDraft '18' '' @()
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: a "- #n - title" bullet whose #n is disallowed is dropped whole, not left as "- - title"' {
        $dashShapeResult.Text -notmatch '(?m)^-\s+-\s'
    }
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: the "- #n - title" case still records 910 as removed' {
        (@($dashShapeResult.Removed)) -contains '910'
    }
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: the "- #n - title" case keeps the allowed #18 bullet' {
        $dashShapeResult.Text -match '#18 - \[Bug\] Scope-vs-singleton lifetime bug'
    }

    # (b) "- #n: title" -> the whole bullet, not "-: title" (18-docs-13/14's own shape).
    $colonShapeDraft = "## Related Issues`n`n- #18: The original issue documenting the design decision.`n- #1400: [DEBT] Documentation drift found while writing the guide`n"
    $colonShapeResult = Limit-RelatedIssues $colonShapeDraft '18' '' @()
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: a "- #n: title" bullet whose #n is disallowed is dropped whole, not left as "-: title"' {
        $colonShapeResult.Text -notmatch '(?m)^-:\s'
    }
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: the "- #n: title" case still records 1400 as removed' {
        (@($colonShapeResult.Removed)) -contains '1400'
    }

    # (c) a bare "- #n" -> the whole bullet, not a bare "-" (18-docs-5's own shape).
    $bareShapeDraft = "## Related Issues`n`n- #18`n- #1414`n"
    $bareShapeResult = Limit-RelatedIssues $bareShapeDraft '18' '' @()
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: a bare "- #n" bullet whose #n is disallowed is dropped whole, not left as a bare "-"' {
        $bareShapeResult.Text -notmatch '(?m)^-\s*$'
    }
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: the bare "- #n" case still records 1414 as removed' {
        (@($bareShapeResult.Removed)) -contains '1414'
    }

    # (d) a bold-bullet "- **#n**: title" shape is dropped whole too. Asserts the EXACT surviving line set (not
    # just the absence of '**910**') so this discriminates the fix: the pre-#1535 script reduces this input to
    # a different, also-broken shape ('- ****: some unverified title', four orphaned asterisks with no link), and
    # a looser "does not contain '**910**' or a bare '-:' line" assertion is true against BOTH the old and the
    # new output (adversarial review of #1535 finding 1) -- it would never fail if this branch of the fix broke.
    $boldShapeDraft = "## Related Issues`n`n- #18 (This issue)`n- **#910**: some unverified title`n"
    $boldShapeResult = Limit-RelatedIssues $boldShapeDraft '18' '' @()
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: a "- **#n**: title" bullet whose #n is disallowed is dropped whole, leaving only the allowed #18 bullet' {
        (@($boldShapeResult.Text -split "`r?`n") | Where-Object { $_ -match '^-' }) -join "`n" -eq '- #18 (This issue)'
    }
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: the bold-bullet case never leaves the orphaned "****" shape the pre-fix script produced' {
        $boldShapeResult.Text -notmatch '\*\*\*\*'
    }

    # (e) prose (the disallowed reference is not the bullet's own leading token) still keeps only token removal.
    $proseShapeDraft = "## Additional Context`n`nThis defect is related to #999 in some unrelated way, but the rest of the sentence stays.`n"
    $proseShapeResult = Limit-RelatedIssues $proseShapeDraft '18' '' @()
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: a prose reference (not the bullet''s leading token) keeps token-only removal' {
        $proseShapeResult.Text -match 'This defect is related to in some unrelated way, but the rest of the sentence stays\.'
    }
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: a prose reference still records the number as removed' {
        (@($proseShapeResult.Removed)) -contains '999'
    }

    # (f) every bullet of a Related Issues section stripped -> "None." under the header, header kept.
    $emptySectionDraft = "## Related Issues`n`n- #910 - [TEST] some unverified title`n- #920: [DEBT] another unverified title`n"
    $emptySectionResult = Limit-RelatedIssues $emptySectionDraft '18' '' @()
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: a Related Issues section with every bullet stripped gets "None."' {
        ($emptySectionResult.Text -split "`r?`n") -contains 'None.'
    }
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: the "## Related Issues" header survives when the section is emptied' {
        $emptySectionResult.Text -match '(?m)^## Related Issues\s*$'
    }
    Test-RemediationChecksCase '#1535 Limit-RelatedIssues: a Related Issues section that still has a kept bullet (#18) never gets "None."' {
        ($dashShapeResult.Text -split "`r?`n") -notcontains 'None.'
    }

    # (g) pr-reviewer finding on PR #1550 (MAJOR): the whole-bullet drop must fire ONLY inside a Related Issues
    # region. Limit-RelatedIssues scans the whole draft body on purpose (#1492 decision 2), and a bulleted prose
    # line elsewhere in the draft (Additional Context, Root Cause, Proposed Fix) that merely STARTS with a
    # disallowed reference is supporting evidence, not a Related Issues list item -- dropping it whole would
    # silently delete real content the model wrote. Both plain and bold-leading-token shapes are covered.
    $additionalContextProseDraft = "## Additional Context`n`n- #1502 already fixed a similar regex escape issue; apply the same pattern here.`n"
    $additionalContextProseResult = Limit-RelatedIssues $additionalContextProseDraft '18' '' @()
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: a "- #n text" bullet OUTSIDE any Related Issues region keeps token-only removal' {
        $additionalContextProseResult.Text -match '(?m)^-\s+already fixed a similar regex escape issue; apply the same pattern here\.\s*$'
    }
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: the out-of-region "- #n text" case still records the number as removed' {
        (@($additionalContextProseResult.Removed)) -contains '1502'
    }
    $rootCauseProseDraft = "## Root Cause`n`n- **#1330** -- similar pattern found there too.`n"
    $rootCauseProseResult = Limit-RelatedIssues $rootCauseProseDraft '18' '' @()
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: a "- **#n** -- text" bullet OUTSIDE any Related Issues region keeps token-only removal (only the "#1330" token is gone, the rest of the line survives)' {
        $rootCauseProseResult.Text -match '(?m)^-\s+\*\*\*\*\s+--\s+similar pattern found there too\.\s*$'
    }
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: the out-of-region bold "- **#n** -- text" case still records the number as removed' {
        (@($rootCauseProseResult.Removed)) -contains '1330'
    }

    # (h) the whole-bullet drop still fires INSIDE the bold-bullet '- **Related Issues**:' region (bug_report.md
    # drafts have no H2 header, only this convention).
    $boldRegionDraft = "## Additional Context`n`n- **Related Issues**:`n  - #18 (This issue)`n  - #910 - [TEST] some unverified title`n"
    $boldRegionResult = Limit-RelatedIssues $boldRegionDraft '18' '' @()
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: a disallowed leading "#n" bullet INSIDE the bold-bullet Related Issues region is dropped whole' {
        $boldRegionResult.Text -notmatch '(?m)^\s*-\s+-\s' -and (@($boldRegionResult.Removed)) -contains '910'
    }
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: the bold-bullet region case keeps the allowed #18 sub-bullet' {
        $boldRegionResult.Text -match '#18 \(This issue\)'
    }

    # (i) the whole-bullet drop still fires INSIDE the plain 'Related Issues:' line region (#1428's own form).
    $plainRegionDraft = "## Additional Context`n`nRelated Issues:`n- #18 (This issue)`n- #920: another unverified title`n"
    $plainRegionResult = Limit-RelatedIssues $plainRegionDraft '18' '' @()
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: a disallowed leading "#n" bullet INSIDE the plain "Related Issues:" region is dropped whole' {
        $plainRegionResult.Text -notmatch '(?m)^-:\s' -and (@($plainRegionResult.Removed)) -contains '920'
    }
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: the plain-line region case keeps the allowed #18 bullet' {
        $plainRegionResult.Text -match '#18 \(This issue\)'
    }

    # (j) second review round of #1550 (MAJOR, still concrete): the field-region extension must stop as soon as
    # a following bullet is LESS indented than the field's own list -- a real, unrelated prose bullet that
    # happens to follow the bold-bullet field's indented sub-bullets, with no separating heading, must NOT be
    # swallowed into the region and whole-line-dropped. Reproduced exactly as the review found it: an indented
    # sub-bullet ("  - #18") establishes the list's own indentation, then an unindented bullet follows directly.
    $overExtensionDraft = "## Additional Context`n`n- **Related Issues**:`n  - #18 (This issue)`n- #1502 already reported this exact behavior in a similar library upgrade discussion`n"
    $overExtensionResult = Limit-RelatedIssues $overExtensionDraft '18' '' @()
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: a less-indented bullet following the bold-bullet field''s own indented sub-bullets is OUTSIDE the region and keeps its content' {
        $overExtensionResult.Text -match '(?m)^-\s+already reported this exact behavior in a similar library upgrade discussion\s*$'
    }
    Test-RemediationChecksCase '#1535/#1550 Limit-RelatedIssues: the over-extension case still records 1502 as removed and keeps the indented #18 sub-bullet' {
        (@($overExtensionResult.Removed)) -contains '1502' -and $overExtensionResult.Text -match '  - #18 \(This issue\)'
    }
    # ---- end #1535 block ----

    # ---- #1409: tools/ai/audit/_remediation-checks.ps1 -- Set-BugEnvironment (Get-EncinaVersion,
    # Get-PackageFromFindingText, Test-PlaceholderEnvironmentValue) fills bug_report.md's own '## Environment'
    # section deterministically after the model replies, so a code-stage finding routed to [BUG] never keeps
    # the template's Encina/.NET Version and OS placeholders forever -- the local model has no way to know
    # those facts, and even the one re-ask left them unfilled on audit #16's own 16-code-5 draft (this issue's
    # own reproduction). No `gh` and no local model here either; Get-EncinaVersion reads a small throwaway
    # Directory.Build.props fixture under $work, never the real repository's.
    $bugReportTemplateTextFor1409 = Get-Content -LiteralPath (Join-Path $repo '.github\ISSUE_TEMPLATE\bug_report.md') -Raw

    $envFixtureRoot = Join-Path $work 'EnvFixture1409'
    New-Item -ItemType Directory -Force $envFixtureRoot | Out-Null
    Set-Content -LiteralPath (Join-Path $envFixtureRoot 'Directory.Build.props') -Value @'
<Project>
  <PropertyGroup>
    <VersionPrefix>0.14.0</VersionPrefix>
    <VersionSuffix>dev</VersionSuffix>
  </PropertyGroup>
</Project>
'@

    $findingCode1409 = 'Finding cites `src/Encina.MongoDB/Sagas/SagaStoreMongoDB.cs:129-132` as the defect location.'

    # (a) a draft with all three template placeholders in '## Environment' -> filled, none left afterward
    # against the real bug_report.md template.
    $draftAllPlaceholders = @'
## Description

Real description text goes here, filled in by the model.

## Environment

- **Encina Version**: [e.g., 0.9.0]
- **.NET Version**: [e.g., .NET 10.0]
- **OS**: [e.g., Windows 11, Ubuntu 24.04]
- **Package(s) Affected**: [e.g., Encina.EntityFrameworkCore, Encina.Dapper.SqlServer]

## Code Sample

some real code sample text
'@
    $filledAll = Set-BugEnvironment $draftAllPlaceholders $envFixtureRoot $findingCode1409
    Test-RemediationChecksCase 'Set-BugEnvironment: fills the Encina Version, .NET Version and OS placeholders' {
        $filledAll -match [regex]::Escape('- **Encina Version**: 0.14.0-dev') -and
        $filledAll -match [regex]::Escape('- **.NET Version**: .NET 10') -and
        $filledAll -match [regex]::Escape('- **OS**: Not applicable (found by static review of the code, not at runtime)')
    }
    Test-RemediationChecksCase "Set-BugEnvironment: derives Package(s) Affected from the finding's src/ path when the model left a placeholder" {
        $filledAll -match [regex]::Escape('- **Package(s) Affected**: Encina.MongoDB')
    }
    Test-RemediationChecksCase 'Set-BugEnvironment: leaves the rest of the draft (Description, Code Sample) untouched' {
        $filledAll -match 'Real description text goes here' -and $filledAll -match 'some real code sample text'
    }
    Test-RemediationChecksCase 'Find-TemplatePlaceholders: a bug_report.md draft with Set-BugEnvironment applied has no Environment placeholders left' {
        $found = Find-TemplatePlaceholders $bugReportTemplateTextFor1409 $filledAll
        @($found | Where-Object { $_ -match '\[e\.g\.,' }).Count -eq 0
    }

    # (b) the model's own Package(s) Affected value is kept when it is not itself one of the template's
    # bracketed placeholder shapes.
    $draftRealPackage = @'
## Environment

- **Encina Version**: [e.g., 0.9.0]
- **.NET Version**: [e.g., .NET 10.0]
- **OS**: [e.g., Windows 11, Ubuntu 24.04]
- **Package(s) Affected**: Encina.Dapper.SqlServer

## Code Sample
'@
    $filledRealPackage = Set-BugEnvironment $draftRealPackage $envFixtureRoot $findingCode1409
    Test-RemediationChecksCase "Set-BugEnvironment: keeps the model's own Package(s) Affected value when it is not a placeholder" {
        $filledRealPackage -match [regex]::Escape('- **Package(s) Affected**: Encina.Dapper.SqlServer')
    }

    # (c) Get-EncinaVersion: with and without a VersionSuffix, and when Directory.Build.props is missing.
    Test-RemediationChecksCase 'Get-EncinaVersion: appends the suffix with a single dash when VersionSuffix is present' {
        (Get-EncinaVersion $envFixtureRoot) -eq '0.14.0-dev'
    }
    $envFixtureNoSuffix = Join-Path $work 'EnvFixture1409NoSuffix'
    New-Item -ItemType Directory -Force $envFixtureNoSuffix | Out-Null
    Set-Content -LiteralPath (Join-Path $envFixtureNoSuffix 'Directory.Build.props') -Value @'
<Project>
  <PropertyGroup>
    <VersionPrefix>1.0.0</VersionPrefix>
    <VersionSuffix></VersionSuffix>
  </PropertyGroup>
</Project>
'@
    Test-RemediationChecksCase 'Get-EncinaVersion: omits the "-suffix" entirely when VersionSuffix is empty' {
        (Get-EncinaVersion $envFixtureNoSuffix) -eq '1.0.0'
    }
    Test-RemediationChecksCase 'Get-EncinaVersion: returns "Not determined" when Directory.Build.props is missing' {
        (Get-EncinaVersion (Join-Path $work 'NoSuchRoot1409')) -eq 'Not determined'
    }

    # (d) a non-bug draft (no '## Environment' header at all -- the technical_debt.md/test_implementation.md
    # shape) is returned completely untouched. Set-BugEnvironment is only ever called for bug_report.md-routed
    # drafts from audit-draft-remediation.ps1's Repair-Draft, but this verifies the function itself is inert on
    # a draft it was never meant to touch.
    $debtDraft = @'
## Type

- [x] Code Quality

## Description

Some debt description.
'@
    $debtResult = Set-BugEnvironment $debtDraft $envFixtureRoot $findingCode1409
    Test-RemediationChecksCase 'Set-BugEnvironment: a draft with no "## Environment" header is returned unchanged' {
        $debtResult -eq $debtDraft
    }
    # ---- end #1409 block ----

    # ---- #1424: tools/ai/audit/_remediation-checks.ps1 -- Limit-RelatedIssues no longer trusts the duplicate
    # search's own candidate list as evidence of a real relation (Defect A: audit #16 verification pass 4 found
    # 5 drafts citing real-but-unrelated issues that were only ever a search candidate, never mentioned by the
    # finding or the script's own anchor-checked notes), and Find-DuplicateAmongCandidates makes duplicate-vs-new
    # deterministic by checking EVERY candidate the search returns, not only the one the model happened to name
    # (Defect B: the real finding 16-code-4 classified as duplicate-of-#1170 in one run and as new in the next,
    # with #1170 unchanged in between, because the model's own reply -- not the deterministic evidence --
    # decided). No `gh` and no local model here either; reuses the real #1388/#1400 fixtures captured from
    # audit #16.

    # (a) a number that was only ever a search candidate -- never cited by the finding's own text and never
    # named in a script note -- is removed just like any other unverified number: being a candidate offered to
    # the classifier is no longer, on its own, evidence of a relation (Defect A's exact false-positive shape).
    $onlyCandidateDraft = "## Related Issues`n`n- #16 (This issue)`n- #1234 - a real open issue, offered as a search candidate but never mentioned by the finding`n"
    $onlyCandidateResult = Limit-RelatedIssues $onlyCandidateDraft '16' 'The finding text discusses an unrelated cache eviction bug and cites no other issue number.' @()
    Test-RemediationChecksCase '#1424 Limit-RelatedIssues: a number that was only a search candidate (never cited by the finding or a script note) is removed' {
        (@($onlyCandidateResult.Removed)) -contains '1234'
    }

    # (b) a number named only in one of the script's own partially-related/possibly-related note lines is kept,
    # even though the finding text itself never mentions it -- those notes are already anchor-checked by
    # Test-PartialDuplicateEvidence before Limit-RelatedIssues ever sees them.
    $scriptNoteDraft = "## Related Issues`n`n- #16 (This issue)`n- #1343 - partially related (it covers only part of this finding)`n"
    $scriptNoteResult = Limit-RelatedIssues $scriptNoteDraft '16' 'The finding text discusses an unrelated cache eviction bug and cites no other issue number.' @('- #1343 - partially related (it covers only part of this finding)')
    Test-RemediationChecksCase '#1424 Limit-RelatedIssues: a number named only in a script note line (partially related) is kept' {
        (@($scriptNoteResult.Removed)) -notcontains '1343'
    }

    # (c) Find-DuplicateAmongCandidates: given every candidate the duplicate search returned for the real code
    # finding 4 -- the one true duplicate #1170 alongside two real-but-false candidates #1333/#1328 -- it
    # returns #1170. This function never looks at any model output at all, so it returns the same answer
    # whether a "model" would have answered duplicate-of-#1170, duplicate-of-something-else, or "new".
    $findingCode4For1424 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\finding-code-4.md') -Raw
    $issue1170For1424 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\issue-1170.json') -Raw | ConvertFrom-Json
    $issue1333For1424 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\issue-1333.json') -Raw | ConvertFrom-Json
    $issue1328For1424 = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1388\issue-1328.json') -Raw | ConvertFrom-Json
    $candidates1424 = @(
        [pscustomobject]@{ Number = '1333'; TitleAndBody = "$($issue1333For1424.title)`n$($issue1333For1424.body)" }
        [pscustomobject]@{ Number = '1170'; TitleAndBody = "$($issue1170For1424.title)`n$($issue1170For1424.body)" }
        [pscustomobject]@{ Number = '1328'; TitleAndBody = "$($issue1328For1424.title)`n$($issue1328For1424.body)" }
    )
    Test-RemediationChecksCase '#1424 Find-DuplicateAmongCandidates: finds the one true duplicate (#1170) among false candidates, whatever a "model" would have answered' {
        (Find-DuplicateAmongCandidates $findingCode4For1424 $candidates1424) -eq '1170'
    }

    # (d) the same finding and the same candidate set classify identically twice, in either order -- ties are
    # always broken by the lowest issue number, never by list order, so a re-run of the same audit against the
    # same open issues can never flip a finding from duplicate to new or vice versa (the exact instability
    # Defect B reported: 16-code-4 vs #1170, unchanged between two runs, classified differently each time).
    $candidates1424Reordered = @($candidates1424[2], $candidates1424[0], $candidates1424[1])
    Test-RemediationChecksCase '#1424 Find-DuplicateAmongCandidates: the same finding and candidates classify identically twice, in either order (deterministic)' {
        (Find-DuplicateAmongCandidates $findingCode4For1424 $candidates1424) -eq (Find-DuplicateAmongCandidates $findingCode4For1424 $candidates1424Reordered)
    }
    $tieCandidatesLowFirst = @(
        [pscustomobject]@{ Number = '100'; TitleAndBody = "$($issue1170For1424.title)`n$($issue1170For1424.body)" }
        [pscustomobject]@{ Number = '9999'; TitleAndBody = "$($issue1170For1424.title)`n$($issue1170For1424.body)" }
    )
    $tieCandidatesHighFirst = @($tieCandidatesLowFirst[1], $tieCandidatesLowFirst[0])
    Test-RemediationChecksCase '#1424 Find-DuplicateAmongCandidates: when several candidates pass, the lowest issue number always wins, not list order' {
        (Find-DuplicateAmongCandidates $findingCode4For1424 $tieCandidatesLowFirst) -eq '100' -and
        (Find-DuplicateAmongCandidates $findingCode4For1424 $tieCandidatesHighFirst) -eq '100'
    }

    # (e) no passing candidate at all (the two false candidates alone, #1170 excluded) -> $null, never a
    # fabricated duplicate.
    Test-RemediationChecksCase '#1424 Find-DuplicateAmongCandidates: returns $null when no candidate passes the evidence check' {
        $null -eq (Find-DuplicateAmongCandidates $findingCode4For1424 @($candidates1424[0], $candidates1424[2]))
    }
    # ---- end #1424 block ----

    # ---- #1428: tools/ai/audit/_remediation-checks.ps1 -- Limit-RelatedIssues recognizes a plain 'Related
    # Issues:' line (no '##' header, no bold bullet) as a fourth section-start form, and, for a draft routed to
    # bug_report.md, also sanitizes any '#n' reference found anywhere under '## Additional Context', because
    # that template gives the model no structural marker at all for "related issues" -- it only says, in
    # 'Additional Context', "Add any other context about the problem here (screenshots, logs, related issues)."
    # Audit #16's real 16-code-5 draft (verification passes 4/5) used exactly this plain-line shape and kept
    # #699/#696/#181 (real, but unrelated) across two remediation re-runs. No `gh` and no local model here
    # either.

    # (a) the plain 'Related Issues:' line, on its own (not inside a bug_report.md draft -- $IsBugReportDraft
    # defaults to $false), still ends the section at a blank line followed by prose (not another bullet), and
    # leaves that trailing prose untouched.
    $plainLineDraft = @'
## Additional Context

Related Issues:
- #16 (This issue)
- #699: unrelated

More prose after the list must survive untouched.
'@
    $plainLineResult = Limit-RelatedIssues $plainLineDraft '16' '' @()
    Test-RemediationChecksCase '#1428 Limit-RelatedIssues: recognizes a plain "Related Issues:" line and removes the unverified #699' {
        (@($plainLineResult.Removed)) -contains '699'
    }
    Test-RemediationChecksCase '#1428 Limit-RelatedIssues: keeps the allowed #16 under the plain-line form' {
        $plainLineResult.Text -match '#16 \(This issue\)'
    }
    Test-RemediationChecksCase '#1428 Limit-RelatedIssues: keeps the trailing prose after the plain-line section untouched' {
        $plainLineResult.Text -match 'More prose after the list must survive untouched\.'
    }

    # (b) the plain form is case-insensitive and tolerates a missing trailing colon ('Related issues', no ':').
    $plainNoColonDraft = "## Additional Context`n`nRelated issues`n- #16 (This issue)`n- #699: unrelated`n"
    $plainNoColonResult = Limit-RelatedIssues $plainNoColonDraft '16' '' @()
    Test-RemediationChecksCase '#1428 Limit-RelatedIssues: recognizes "Related issues" (lower-case, no trailing colon)' {
        (@($plainNoColonResult.Removed)) -contains '699'
    }

    # (c) a reference to an unverified issue sitting under '## Additional Context' with NO "Related Issues"
    # label at all used to be untouched when the draft was not routed to bug_report.md ($IsBugReportDraft =
    # $false, the default) -- decision 2 (as first landed) was additive only for bug drafts, never a general
    # Additional Context scan. #1492 decision 2 supersedes this: the allowed-set rule now applies to every '#n'
    # reference anywhere in the draft body regardless of $IsBugReportDraft, so the bare, unlabelled #699
    # reference is removed for a non-bug draft too -- only the unverified TOKEN, leaving the rest of that
    # bullet's own prose (and the sibling "#16 is the source issue." line) readable.
    $bareAdditionalContextDraft = "## Additional Context`n`n- See also #699 for context.`n- #16 is the source issue.`n"
    $bareNonBugResult = Limit-RelatedIssues $bareAdditionalContextDraft '16' '' @()
    Test-RemediationChecksCase '#1492 Limit-RelatedIssues: a bare "#n" under Additional Context is now removed even for a non-bug draft (decision 2 is global, not bug-only)' {
        (@($bareNonBugResult.Removed)) -contains '699'
    }
    Test-RemediationChecksCase '#1492 Limit-RelatedIssues: keeps the allowed #16 and the rest of the sentence when the draft is not bug-routed' {
        $bareNonBugResult.Text -match '#16 is the source issue\.' -and $bareNonBugResult.Text -match 'See also\s+for context\.'
    }

    # (d) the same bare reference IS sanitized when the draft is routed to bug_report.md ($IsBugReportDraft =
    # $true) -- decision 2's whole-Additional-Context scan, with no "Related Issues" label needed at all.
    $bareBugResult = Limit-RelatedIssues $bareAdditionalContextDraft '16' '' @() $true
    Test-RemediationChecksCase '#1428 Limit-RelatedIssues: a bare "#n" under Additional Context is removed for a bug_report.md-routed draft' {
        (@($bareBugResult.Removed)) -contains '699'
    }
    Test-RemediationChecksCase '#1428 Limit-RelatedIssues: keeps the allowed #16 in the bug-routed bare-reference case' {
        $bareBugResult.Text -match '#16 is the source issue\.'
    }

    # (e) the real 16-code-5 Additional Context text (copied verbatim from
    # artifacts/knowledge/remediation/16-code-5-*.md, the actual bug_report.md-routed draft audit #16 wrote and
    # kept unresolved across verification passes 4 and 5): its own plain 'Related Issues:' line, inside
    # 'Additional Context', names #16 (allowed), #699, #696 and #181 (all real but unrelated). Both decision 1
    # (the plain-line form) and decision 2 (the bug-routed whole-section scan) agree on the same outcome here.
    $code5AdditionalContext = Get-Content -LiteralPath (Join-Path $repo '.claude\hooks\tests\fixtures\1428\16-code-5-additional-context.md') -Raw
    $code5AcResult = Limit-RelatedIssues $code5AdditionalContext '16' $findingCode5 @() $true
    Test-RemediationChecksCase '#1428 Limit-RelatedIssues: removes #699, #696 and #181 from the real 16-code-5 Additional Context text' {
        (@($code5AcResult.Removed) | Sort-Object) -join ',' -eq '181,696,699'
    }
    Test-RemediationChecksCase '#1428 Limit-RelatedIssues: keeps the allowed #16 in the real 16-code-5 Additional Context text' {
        $code5AcResult.Text -match '#16: Original audit issue'
    }
    Test-RemediationChecksCase '#1428 Limit-RelatedIssues: drops the unverified lines from the real 16-code-5 Additional Context text' {
        $code5AcResult.Text -notmatch '#699' -and $code5AcResult.Text -notmatch '#696' -and $code5AcResult.Text -notmatch '#181'
    }

    # (f) Add-RelatedIssuesLine must recognize the same plain-line form Limit-RelatedIssues does (adversarial
    # review of #1428): before this fix, a rejected duplicate-of note for a plain-line draft fell through to
    # Add-RelatedIssuesLine's own "not found" path and was appended detached at the end of the file -- the
    # exact structurally-malformed-draft bug #1400 fixed for the header/bold-bullet forms, reintroduced for the
    # plain-line form this issue adds.
    $plainLineForNote = "## Additional Context`n`nRelated Issues:`n- #16 (This issue)`n"
    $withPlainLineNote = Add-RelatedIssuesLine $plainLineForNote '- #1343 - partially related (it covers only part of this finding)'
    Test-RemediationChecksCase '#1428 Add-RelatedIssuesLine: finds a plain "Related Issues:" line' { $withPlainLineNote.Found }
    Test-RemediationChecksCase '#1428 Add-RelatedIssuesLine: inserts the note right after the plain "Related Issues:" line, not detached at the end' {
        $plainNoteLines = @($withPlainLineNote.Text -split "`r?`n")
        $relatedIdx = [array]::IndexOf($plainNoteLines, 'Related Issues:')
        $noteIdx = [array]::IndexOf($plainNoteLines, '- #1343 - partially related (it covers only part of this finding)')
        $relatedIdx -ge 0 -and $noteIdx -eq $relatedIdx + 1
    }

    # (g) regression: the earlier '## Related Issues' H2 and '- **Related Issues**:' bold-bullet forms (#1400,
    # #1424) still work exactly as before -- adding the plain-line form and the bug-routed whole-section scan
    # never changed their own section-boundary logic.
    Test-RemediationChecksCase '#1428 regression: the real 16-code-5 bold-bullet draft (#1400 fixture) still removes #699/#696/#181' {
        $regressionLimited = Limit-RelatedIssues $code5Draft '16' $findingCode5 @()
        (@($regressionLimited.Removed) | Sort-Object) -join ',' -eq '181,696,699'
    }
    Test-RemediationChecksCase '#1428 regression: an "## Related Issues" H2 header draft still removes an unverified number' {
        $h2Draft = "## Related Issues`n`n- #16 (This issue)`n- #699: unrelated`n"
        (@((Limit-RelatedIssues $h2Draft '16' '' @()).Removed)) -contains '699'
    }
    # ---- end #1428 block ----

    # ---- #1492: tools/ai/audit/_remediation-checks.ps1 and audit-draft-remediation.ps1 -- the remediation
    # FAIL loop did not converge (audit #17): regenerating every draft to fix one detail re-rolled every other
    # draft's own already-correct model choices (the Type checkbox, and a Related Issues reference living
    # outside any labelled section). Fixes: (1) the script ticks '## Type' itself, deterministically, instead
    # of the model; (2) Limit-RelatedIssues' allowed-set rule now covers the whole draft body, not just a
    # labelled section (covered by the updated #1428 test (c) above and the Description-prose case below); (3)
    # -Only regenerates a single finding's draft without touching any other finding's own output. No `gh` and
    # no local model anywhere in this block.

    # (a) Get-DeterministicDebtType: the finding's stage decides for docs/tests; the classifier's own kind
    # decides for code, each with its own keyword override -- the exact two audit #17 pass-3/pass-4 failures.
    Test-RemediationChecksCase '#1492 Get-DeterministicDebtType: a docs-stage finding always ticks "Documentation gap"' {
        (Get-DeterministicDebtType 'docs' 'debt' 'A stale sentence in the README.') -eq 'Documentation gap'
    }
    Test-RemediationChecksCase '#1492 Get-DeterministicDebtType: a tests-stage finding ticks "Missing tests" by default' {
        (Get-DeterministicDebtType 'tests' 'test' 'No coverage exists for the new branch.') -eq 'Missing tests'
    }
    Test-RemediationChecksCase '#1492 Get-DeterministicDebtType: a tests-stage finding about duplicate/consolidated tests ticks "Refactoring needed" instead (audit #17 pass 3)' {
        (Get-DeterministicDebtType 'tests' 'debt' 'Two test classes duplicate the same setup and should be consolidated.') -eq 'Refactoring needed'
    }
    Test-RemediationChecksCase '#1492 Get-DeterministicDebtType: a code-stage "debt" finding ticks the real "Code quality (warnings, analyzers)" label by default' {
        (Get-DeterministicDebtType 'code' 'debt' 'The method has unnecessary cyclomatic complexity.') -eq 'Code quality (warnings, analyzers)'
    }
    Test-RemediationChecksCase '#1492 Get-DeterministicDebtType: a code-stage "debt" finding about a stale label ticks "Documentation gap" instead (audit #17 pass 4)' {
        (Get-DeterministicDebtType 'code' 'debt' 'The `.vscode/tasks.json` task carries a stale label that no longer matches the command it runs.') -eq 'Documentation gap'
    }
    Test-RemediationChecksCase '#1492 Get-DeterministicDebtType: a code-stage finding the classifier itself called "docs" ticks "Documentation gap"' {
        (Get-DeterministicDebtType 'code' 'docs' 'A comment describing the old behavior was left in place.') -eq 'Documentation gap'
    }

    # (b) Set-DebtType: clears whatever the model itself ticked and ticks exactly the deterministic box.
    $debtTypeDraft = @'
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

Two SagaStoreADO test classes duplicate the same setup.
'@
    $debtTypeResult = Set-DebtType $debtTypeDraft 'Refactoring needed'
    Test-RemediationChecksCase '#1492 Set-DebtType: ticks exactly the deterministic box' {
        (@($debtTypeResult -split "`r?`n")) -contains '- [x] Refactoring needed'
    }
    Test-RemediationChecksCase "#1492 Set-DebtType: clears the model's own, different tick" {
        (@($debtTypeResult -split "`r?`n")) -contains '- [ ] Documentation gap'
    }
    Test-RemediationChecksCase '#1492 Set-DebtType: leaves the rest of the draft (Description) untouched' {
        $debtTypeResult -match 'Two SagaStoreADO test classes duplicate the same setup\.'
    }

    # Adversarial review of #1492: the first version of Get-DeterministicDebtType returned the bare 'Code
    # quality' label, which never matches technical_debt.md's real 'Code quality (warnings, analyzers)' checkbox
    # text -- Set-DebtType compares by exact equality, so that mismatch silently ticked NOTHING at all instead
    # of one box. This exercises the exact real label Get-DeterministicDebtType now returns for the code-stage
    # "debt" default, against a draft carrying the real checkbox line, and proves the box actually gets ticked.
    $debtTypeCodeQualityResult = Set-DebtType $debtTypeDraft 'Code quality (warnings, analyzers)'
    Test-RemediationChecksCase '#1492 Set-DebtType: the real "Code quality (warnings, analyzers)" label (Get-DeterministicDebtType''s own default) actually ticks that box' {
        (@($debtTypeCodeQualityResult -split "`r?`n")) -contains '- [x] Code quality (warnings, analyzers)'
    }
    Test-RemediationChecksCase '#1492 Set-DebtType: ticking "Code quality (warnings, analyzers)" leaves every other box unticked, never all-blank' {
        $ticked = @(($debtTypeCodeQualityResult -split "`r?`n") | Where-Object { $_ -match '^-\s*\[x\]' })
        $ticked.Count -eq 1 -and $ticked[0] -eq '- [x] Code quality (warnings, analyzers)'
    }
    Test-RemediationChecksCase '#1492 Set-DebtType: a draft with no "## Type" header is returned unchanged' {
        (Set-DebtType "## Description`n`nNo Type section here." 'Code quality (warnings, analyzers)') -eq "## Description`n`nNo Type section here."
    }

    # (c) Limit-RelatedIssues decision 2: a reference living in plain body prose (Description), not inside any
    # Related Issues section at all, is now caught and removed too -- the audit #17 pass 4 case: a draft cited
    # #1372, an unrelated package-count issue, in its own Description prose, which the previous section-scoped
    # version of this function never looked at.
    $bodyProseDraft = "## Description`n`nThe defect also resembles the pattern fixed in #1372, though that issue is unrelated to this finding.`n`n## Location`n`n- **File(s)**: ``src/A.cs```n"
    $bodyProseResult = Limit-RelatedIssues $bodyProseDraft '17' 'The finding text cites no other issue number.' @()
    Test-RemediationChecksCase '#1492 Limit-RelatedIssues: a reference in plain Description prose (no Related Issues section at all) is removed' {
        (@($bodyProseResult.Removed)) -contains '1372'
    }
    Test-RemediationChecksCase '#1492 Limit-RelatedIssues: keeps the rest of the Description sentence readable and #1372 gone' {
        $bodyProseResult.Text -match 'The defect also resembles the pattern fixed in' -and $bodyProseResult.Text -notmatch '#1372'
    }
    Test-RemediationChecksCase '#1492 Limit-RelatedIssues: the audited issue (#17) is not in scope here, and a number the finding itself cites survives' {
        $findingCitesOther = 'The finding text cites #1234 as the origin of the pattern.'
        $citedResult = Limit-RelatedIssues "## Description`n`nSee #1234 and #9999 for background.`n" '17' $findingCitesOther @()
        (@($citedResult.Removed)) -eq @('9999') -and $citedResult.Text -match '#1234'
    }

    if (Get-Command git -ErrorAction SilentlyContinue) {
        # (d) -Only end to end: a self-contained fixture repo (own '.git', mirroring the #1375 $remWt pattern
        # above but kept separate so this block never depends on $remWt's own later mutations) with 2 code
        # findings. -DryRun -NoGh never calls the model or `gh`, matching decision 4's own requirement.
        $remWt1492 = Join-Path $work 'RemediationOnlyWt'
        if (Test-Path $remWt1492) { Remove-Item -Recurse -Force $remWt1492 }
        New-Item -ItemType Directory -Force (Join-Path $remWt1492 'tools\ai\audit') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $remWt1492 'artifacts\knowledge\stages') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $remWt1492 '.github\ISSUE_TEMPLATE') | Out-Null
        Copy-Item (Join-Path $repo 'tools\ai\audit\pipeline.json') (Join-Path $remWt1492 'tools\ai\audit\pipeline.json')
        Copy-Item (Join-Path $repo 'tools\ai\audit\_audit-lib.ps1') (Join-Path $remWt1492 'tools\ai\audit\_audit-lib.ps1')
        Copy-Item (Join-Path $repo 'tools\ai\audit\_remediation-checks.ps1') (Join-Path $remWt1492 'tools\ai\audit\_remediation-checks.ps1')
        Copy-Item (Join-Path $repo 'tools\ai\audit\audit-draft-remediation.ps1') (Join-Path $remWt1492 'tools\ai\audit\audit-draft-remediation.ps1')
        foreach ($t in 'bug_report.md', 'test_implementation.md', 'technical_debt.md') {
            Copy-Item (Join-Path $repo ".github\ISSUE_TEMPLATE\$t") (Join-Path $remWt1492 ".github\ISSUE_TEMPLATE\$t")
        }
        function Invoke-RemGit1492 { & git -C $remWt1492 -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
        Invoke-RemGit1492 init -q -b main
        Invoke-RemGit1492 commit -q --allow-empty -m base

        $codeFindingsText1492 = "1. **Major** -- ``src/X.cs:10`` first finding.`n2. **Minor** -- ``src/Y.cs:20`` second finding."
        Set-Content (Join-Path $remWt1492 'artifacts\knowledge\stages\code.md') "## Findings`n$codeFindingsText1492`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1492 'artifacts\knowledge\stages\tests.md') "## Findings`n- none`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1492 'artifacts\knowledge\stages\docs.md') "## Findings`n- none`n## Lessons for the pipeline`n- none`n"
        $remN1492 = 4343
        @{ issue = $remN1492; worktree = $remWt1492; branch = "audit/$remN1492"; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $remWt1492 'artifacts\knowledge\current-audit.json')

        $baselineOutput = & pwsh -NoProfile -File (Join-Path $remWt1492 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh 2>&1
        $baselineExit = $LASTEXITCODE
        Test-RemediationCase '#1492 -Only fixture: the baseline (no -Only) full run exits 0' { $baselineExit -eq 0 }

        $dryDir1492 = Join-Path $remWt1492 "artifacts\knowledge\remediation\_dryrun-$remN1492"
        $untouchedFile1492 = Join-Path $dryDir1492 'code-2-brief.md'
        Test-RemediationCase '#1492 -Only fixture: the baseline run wrote both findings'' brief files' {
            (Test-Path -LiteralPath (Join-Path $dryDir1492 'code-1-brief.md')) -and (Test-Path -LiteralPath $untouchedFile1492)
        }

        # Backdate the finding-2 brief file's mtime and capture its bytes, so "-Only 'code 1'" leaving it
        # untouched can be proven by more than "the deterministic content happens to match again."
        $backdated1492 = [DateTime]::new(2020, 1, 1, 0, 0, 0, [DateTimeKind]::Utc)
        (Get-Item -LiteralPath $untouchedFile1492).LastWriteTimeUtc = $backdated1492
        $untouchedContentBefore1492 = Get-Content -LiteralPath $untouchedFile1492 -Raw
        $untouchedInputFile1492 = Join-Path $dryDir1492 'code-2-input.md'
        (Get-Item -LiteralPath $untouchedInputFile1492).LastWriteTimeUtc = $backdated1492

        $onlyOutput1492 = & pwsh -NoProfile -File (Join-Path $remWt1492 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -Only 'code 1' 2>&1
        $onlyExit1492 = $LASTEXITCODE
        Test-RemediationCase '#1492 -Only "code 1" exits 0 and never calls the model or gh' { $onlyExit1492 -eq 0 }

        $untouchedAfter1492 = Get-Item -LiteralPath $untouchedFile1492
        Test-RemediationCase '#1492 -Only "code 1" leaves finding code-2''s own dry-run brief file with an unchanged mtime (never rewritten)' {
            $untouchedAfter1492.LastWriteTimeUtc -eq $backdated1492
        }
        Test-RemediationCase '#1492 -Only "code 1" leaves finding code-2''s own dry-run brief file byte-identical' {
            (Get-Content -LiteralPath $untouchedFile1492 -Raw) -eq $untouchedContentBefore1492
        }
        Test-RemediationCase '#1492 -Only "code 1" leaves finding code-2''s own dry-run input file with an unchanged mtime (never rewritten)' {
            (Get-Item -LiteralPath $untouchedInputFile1492).LastWriteTimeUtc -eq $backdated1492
        }
        Test-RemediationCase '#1492 -Only "code 1" never logs removing finding code-2''s own output' {
            (Get-FlatOutput $onlyOutput1492) -notmatch [regex]::Escape('code-2-brief.md') -and (Get-FlatOutput $onlyOutput1492) -notmatch [regex]::Escape('code-2-input.md')
        }

        $remStageLines1492 = @(Get-Content (Join-Path $remWt1492 'artifacts\knowledge\stages\remediation.md') | Where-Object { $_ -match '^-\s+\w+\s+\d+\s+\(' })
        Test-RemediationCase '#1492 -Only "code 1": stages\remediation.md still lists both findings'' lines after the -Only run' {
            $remStageLines1492.Count -eq 2
        }

        # -Only with a stage/id that does not match any currently-parsed finding is an error, not a silent no-op.
        $badOnlyOutput1492 = & pwsh -NoProfile -File (Join-Path $remWt1492 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -Only 'code 99' 2>&1
        $badOnlyExit1492 = $LASTEXITCODE
        Test-RemediationCase "#1492 -Only 'code 99' (no matching finding) is an error, not a silent no-op" {
            $badOnlyExit1492 -ne 0 -and (Get-FlatOutput $badOnlyOutput1492) -match "does not match a finding"
        }

        # -Only against an audit that has never had a full regeneration (no stages\remediation.md yet) is also
        # an error, never a guess at what the other findings' lines should say.
        $remWt1492NoBaseline = Join-Path $work 'RemediationOnlyWtNoBaseline'
        if (Test-Path $remWt1492NoBaseline) { Remove-Item -Recurse -Force $remWt1492NoBaseline }
        New-Item -ItemType Directory -Force $remWt1492NoBaseline | Out-Null
        Copy-Item -Recurse (Join-Path $remWt1492 'tools') (Join-Path $remWt1492NoBaseline 'tools')
        Copy-Item -Recurse (Join-Path $remWt1492 '.github') (Join-Path $remWt1492NoBaseline '.github')
        New-Item -ItemType Directory -Force (Join-Path $remWt1492NoBaseline 'artifacts\knowledge\stages') | Out-Null
        Set-Content (Join-Path $remWt1492NoBaseline 'artifacts\knowledge\stages\code.md') "## Findings`n$codeFindingsText1492`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1492NoBaseline 'artifacts\knowledge\stages\tests.md') "## Findings`n- none`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1492NoBaseline 'artifacts\knowledge\stages\docs.md') "## Findings`n- none`n## Lessons for the pipeline`n- none`n"
        $remN1492NoBaseline = 4344
        @{ issue = $remN1492NoBaseline; worktree = $remWt1492NoBaseline; branch = "audit/$remN1492NoBaseline"; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $remWt1492NoBaseline 'artifacts\knowledge\current-audit.json')
        function Invoke-RemGit1492NoBaseline { & git -C $remWt1492NoBaseline -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
        Invoke-RemGit1492NoBaseline init -q -b main
        Invoke-RemGit1492NoBaseline commit -q --allow-empty -m base
        $noBaselineOutput = & pwsh -NoProfile -File (Join-Path $remWt1492NoBaseline 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -Only 'code 1' 2>&1
        $noBaselineExit = $LASTEXITCODE
        Test-RemediationCase '#1492 -Only without a prior full regeneration is an error, not a guess' {
            $noBaselineExit -ne 0 -and (Get-FlatOutput $noBaselineOutput) -match 'requires an existing'
        }

        # (e) #1492 adversarial-review regression: -Only "code 1" must never touch a DOUBLE-DIGIT sibling
        # finding's own leftover output. Split-Findings takes a finding's Id straight from the markdown's own
        # leading digits (not from its position in the list), so the fixture above -- findings "1" and "2" --
        # could never have exposed a numeric-PREFIX collision even in the unfixed code: "code 1" never collided
        # with "code 2". This fixture numbers its two findings "1." and "10." instead, so "-Only 'code 1'" runs
        # directly against a "code 10" sibling and can prove the fix at audit-draft-remediation.ps1:339 (every
        # narrow pattern has a literal separator immediately after $keyId, so a bare "$keyId*.md" wildcard can
        # no longer swallow "${keyId}0...").
        $remWt1492c = Join-Path $work 'RemediationOnlyWtCollision'
        if (Test-Path $remWt1492c) { Remove-Item -Recurse -Force $remWt1492c }
        New-Item -ItemType Directory -Force $remWt1492c | Out-Null
        Copy-Item -Recurse (Join-Path $remWt1492 'tools') (Join-Path $remWt1492c 'tools')
        Copy-Item -Recurse (Join-Path $remWt1492 '.github') (Join-Path $remWt1492c '.github')
        New-Item -ItemType Directory -Force (Join-Path $remWt1492c 'artifacts\knowledge\stages') | Out-Null
        $collisionFindingsText1492 = "1. **Major** -- ``src/X.cs:10`` first finding.`n10. **Minor** -- ``src/Y.cs:20`` tenth finding."
        Set-Content (Join-Path $remWt1492c 'artifacts\knowledge\stages\code.md') "## Findings`n$collisionFindingsText1492`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1492c 'artifacts\knowledge\stages\tests.md') "## Findings`n- none`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1492c 'artifacts\knowledge\stages\docs.md') "## Findings`n- none`n## Lessons for the pipeline`n- none`n"
        $remN1492c = 4345
        @{ issue = $remN1492c; worktree = $remWt1492c; branch = "audit/$remN1492c"; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $remWt1492c 'artifacts\knowledge\current-audit.json')
        function Invoke-RemGit1492c { & git -C $remWt1492c -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
        Invoke-RemGit1492c init -q -b main
        Invoke-RemGit1492c commit -q --allow-empty -m base

        $collisionBaselineOutput = & pwsh -NoProfile -File (Join-Path $remWt1492c 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh 2>&1
        $collisionBaselineExit = $LASTEXITCODE
        Test-RemediationCase '#1492 double-digit fixture: the baseline (no -Only) full run exits 0 with findings code 1 and code 10' { $collisionBaselineExit -eq 0 }

        # -DryRun's own preview files for code-10 (written by the baseline run above), backdated the same way
        # as the single-digit case, so "-Only 'code 1'" leaving them untouched is proven by more than
        # "the deterministic content happens to match again."
        $backdated1492c = [DateTime]::new(2020, 1, 1, 0, 0, 0, [DateTimeKind]::Utc)
        $collisionDryDir = Join-Path $remWt1492c "artifacts\knowledge\remediation\_dryrun-$remN1492c"
        $code10DryFiles = @{
            'dryrun-brief' = Join-Path $collisionDryDir 'code-10-brief.md'
            'dryrun-input' = Join-Path $collisionDryDir 'code-10-input.md'
        }
        $code10DryContentBefore = @{}
        foreach ($key in $code10DryFiles.Keys) {
            (Get-Item -LiteralPath $code10DryFiles[$key]).LastWriteTimeUtc = $backdated1492c
            $code10DryContentBefore[$key] = Get-Content -LiteralPath $code10DryFiles[$key] -Raw
        }

        # Simulate a previous REAL (non -DryRun) run's leftover output for finding "code 10" -- exactly the
        # files "-Only 'code 1'"'s cleanup step (audit-draft-remediation.ps1:339-346) walks regardless of
        # -DryRun, and exactly the files the pre-fix single "_brief-...-$keyId*.md" pattern could delete by
        # accident (matching "_brief-<n>-code-10.md" and its "-reask" variant too).
        $collisionRemDir = Join-Path $remWt1492c 'artifacts\knowledge\remediation'
        $code10Leftovers = @{
            'input'          = "_input-$remN1492c-code-10.md"
            'classify-brief' = "_classify-brief-$remN1492c-code-10.md"
            'classify'       = "_classify-$remN1492c-code-10.md"
            'brief'          = "_brief-$remN1492c-code-10.md"
            'brief-reask'    = "_brief-$remN1492c-code-10-reask.md"
            'brief-reask-checks' = "_brief-$remN1492c-code-10-reask-checks.md"
            'draft'          = "$remN1492c-code-10-tenth-finding.md"
        }
        $code10Paths = @{}
        $code10ContentBefore = @{}
        foreach ($key in $code10Leftovers.Keys) {
            $path = Join-Path $collisionRemDir $code10Leftovers[$key]
            Set-Content -LiteralPath $path -Encoding utf8 -Value "leftover content for code-10 $key"
            (Get-Item -LiteralPath $path).LastWriteTimeUtc = $backdated1492c
            $code10Paths[$key] = $path
            $code10ContentBefore[$key] = Get-Content -LiteralPath $path -Raw
        }

        # #1565: the targeted finding's OWN combined re-ask brief is removed by -Only, like its other briefs.
        $code1ReaskChecks = Join-Path $collisionRemDir "_brief-$remN1492c-code-1-reask-checks.md"
        Set-Content -LiteralPath $code1ReaskChecks -Encoding utf8 -Value 'leftover content for code-1 reask-checks'

        $collisionOnlyOutput = & pwsh -NoProfile -File (Join-Path $remWt1492c 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -Only 'code 1' 2>&1
        $collisionOnlyExit = $LASTEXITCODE
        Test-RemediationCase '#1492 -Only "code 1" against a code-10 sibling exits 0' { $collisionOnlyExit -eq 0 }

        # The child pwsh process above (its own OS process, separate from this test) does the actual file
        # removal; on Windows, a just-created/just-renamed file's visibility to a SIBLING process's directory
        # enumeration can lag the write by a few milliseconds (filesystem cache/AV scan settle time). A file
        # that is genuinely gone stays gone through every retry, so this loop cannot mask a real regression --
        # it only protects against a false failure from reading the directory microseconds too early.
        function Wait-FileState1492c([string]$Path) {
            for ($attempt = 0; $attempt -lt 10; $attempt++) {
                if (Test-Path -LiteralPath $Path) { return $true }
                Start-Sleep -Milliseconds 50
            }
            return $false
        }

        # Also assert the run's own log never claims to have removed one of code-10's files -- the same
        # style the pre-existing single-digit case above uses (line ~2422), extended here with the never-
        # deleted output-text check the double-digit case was missing.
        Test-RemediationCase '#1492 -Only "code 1" never logs removing any of code-10''s own leftover output' {
            $flatCollisionOutput = Get-FlatOutput $collisionOnlyOutput
            $flatCollisionOutput -notmatch [regex]::Escape('code-10-brief.md') -and $flatCollisionOutput -notmatch [regex]::Escape('code-10-input.md') -and
            (($code10Leftovers.Values | ForEach-Object { $flatCollisionOutput -notmatch [regex]::Escape($_) }) -notcontains $false)
        }

        Test-RemediationCase "#1565 -Only 'code 1' removes code 1's own -reask-checks brief" {
            -not (Test-Path -LiteralPath $code1ReaskChecks)
        }

        foreach ($key in $code10DryFiles.Keys) {
            $path = $code10DryFiles[$key]
            Test-RemediationCase "#1492 -Only 'code 1' leaves code-10's own $key dry-run preview file with an unchanged mtime (double-digit prefix collision)" {
                (Get-Item -LiteralPath $path).LastWriteTimeUtc -eq $backdated1492c
            }
            Test-RemediationCase "#1492 -Only 'code 1' leaves code-10's own $key dry-run preview file byte-identical" {
                (Get-Content -LiteralPath $path -Raw) -eq $code10DryContentBefore[$key]
            }
        }

        foreach ($key in $code10Leftovers.Keys) {
            $path = $code10Paths[$key]
            $survived = Wait-FileState1492c $path
            Test-RemediationCase "#1492 -Only 'code 1' leaves code-10's own $key file present after the run (double-digit prefix collision)" {
                $survived
            }
            if ($survived) {
                Test-RemediationCase "#1492 -Only 'code 1' leaves code-10's own $key file with an unchanged mtime" {
                    (Get-Item -LiteralPath $path).LastWriteTimeUtc -eq $backdated1492c
                }
                Test-RemediationCase "#1492 -Only 'code 1' leaves code-10's own $key file byte-identical" {
                    (Get-Content -LiteralPath $path -Raw) -eq $code10ContentBefore[$key]
                }
            }
        }

        $collisionStageLines = Get-Content (Join-Path $remWt1492c 'artifacts\knowledge\stages\remediation.md')
        $code10Line = @($collisionStageLines | Where-Object { $_ -match '^-\s+code\s+10\s+\(' })
        Test-RemediationCase '#1492 -Only "code 1": stages\remediation.md keeps code 10''s own line after the run' {
            $code10Line.Count -eq 1
        }
    }
    else {
        'SKIP #1492 -Only fixture: git is not on PATH'
    }
    # ---- end #1492 block ----

    # ---- #1491: tools/ai/audit/_remediation-checks.ps1 (Get-FindingLeadingAnchor, Test-SameLocationAnchor,
    # Group-FindingsByLocation, Get-GroupPrimary, Add-ReportedByLine) and audit-draft-remediation.ps1 -- the
    # remediation stage drafted one remediation issue per finding even when two stages (code, tests, docs)
    # reported the SAME defect (audit #17 verification pass 2: docs finding 7 and code finding 4 both cited
    # `src/Encina.DomainModeling/AggregateBase.cs:20`). Findings are now grouped by their leading location
    # anchor before drafting: same file and overlapping/equal line (range) = one group, one draft, from the
    # group's highest-severity finding; every OTHER member's own line in stages/remediation.md says which draft
    # covers it instead of getting a second draft of its own.

    # (a) Get-FindingLeadingAnchor: only a citation with an explicit line number counts as an anchor.
    Test-RemediationChecksCase '#1491 Get-FindingLeadingAnchor: a leading `file:line` citation is the anchor' {
        $a = Get-FindingLeadingAnchor '`src/A.cs:20` a stale comment.'
        $null -ne $a -and $a.FullPath -eq 'src/A.cs' -and $a.StartLine -eq 20 -and $a.EndLine -eq 20
    }
    Test-RemediationChecksCase '#1491 Get-FindingLeadingAnchor: a leading `file:start-end` range citation keeps both ends' {
        $a = Get-FindingLeadingAnchor '`src/A.cs:20-25` a stale comment.'
        $null -ne $a -and $a.StartLine -eq 20 -and $a.EndLine -eq 25
    }
    Test-RemediationChecksCase '#1491 Get-FindingLeadingAnchor: a file citation with NO line number is not an anchor' {
        $null -eq (Get-FindingLeadingAnchor '`src/A.cs` a general observation about the whole file.')
    }
    Test-RemediationChecksCase '#1491 Get-FindingLeadingAnchor: no file citation at all is not an anchor' {
        $null -eq (Get-FindingLeadingAnchor 'A general observation with no file citation whatsoever.')
    }

    # (b) Test-SameLocationAnchor: same file and overlapping/equal ranges match; different lines of the same
    # file, or a different file, do not; a $null anchor never matches anything (never grouped).
    $anchor20 = Get-FindingLeadingAnchor '`src/A.cs:20` x.'
    $anchor20b = Get-FindingLeadingAnchor '`src/A.cs:20` y, described differently.'
    $anchor18to22 = Get-FindingLeadingAnchor '`src/A.cs:18-22` z.'
    $anchor99 = Get-FindingLeadingAnchor '`src/A.cs:99` w.'
    $anchorOther = Get-FindingLeadingAnchor '`src/B.cs:20` v.'
    Test-RemediationChecksCase '#1491 Test-SameLocationAnchor: same file, equal line, matches' { Test-SameLocationAnchor $anchor20 $anchor20b }
    Test-RemediationChecksCase '#1491 Test-SameLocationAnchor: same file, overlapping range, matches' { Test-SameLocationAnchor $anchor20 $anchor18to22 }
    Test-RemediationChecksCase '#1491 Test-SameLocationAnchor: same file, DIFFERENT line, does not match' { -not (Test-SameLocationAnchor $anchor20 $anchor99) }
    Test-RemediationChecksCase '#1491 Test-SameLocationAnchor: different file, same line number, does not match' { -not (Test-SameLocationAnchor $anchor20 $anchorOther) }
    Test-RemediationChecksCase '#1491 Test-SameLocationAnchor: a $null anchor never matches anything' { (-not (Test-SameLocationAnchor $null $anchor20)) -and (-not (Test-SameLocationAnchor $anchor20 $null)) }

    # (c) Group-FindingsByLocation + Get-GroupPrimary: a finding with no anchor is always its own singleton
    # group (decision 1's "a finding with no file anchor is never grouped"); two findings at the same location
    # group together with the highest-severity one as primary; a tie is broken by the members' own order
    # (stage order code/tests/docs, since $Members is given in that order).
    $groupFindingCode1 = [pscustomobject]@{ Stage = 'code'; Id = '1'; Severity = 'Major'; Text = '`src/A.cs:20` stale doc comment (code stage).' }
    $groupFindingDocs1 = [pscustomobject]@{ Stage = 'docs'; Id = '1'; Severity = 'Minor'; Text = '`src/A.cs:20` the same stale doc comment (docs stage).' }
    $groupFindingDocs2 = [pscustomobject]@{ Stage = 'docs'; Id = '2'; Severity = 'Major'; Text = '`src/A.cs:99` an unrelated defect, same file, different line.' }
    $groupFindingTests1 = [pscustomobject]@{ Stage = 'tests'; Id = '1'; Severity = 'Minor'; Text = 'A general observation with no file citation at all.' }
    $testGroups = Group-FindingsByLocation @($groupFindingCode1, $groupFindingDocs1, $groupFindingDocs2, $groupFindingTests1)
    Test-RemediationChecksCase '#1491 Group-FindingsByLocation: 4 findings, 2 at the same location, yield 3 groups' { $testGroups.Count -eq 3 }
    $sameLocationGroup = @($testGroups | Where-Object { $_.Members.Count -eq 2 })
    Test-RemediationChecksCase '#1491 Group-FindingsByLocation: exactly one group has the 2 same-location findings' { $sameLocationGroup.Count -eq 1 }
    Test-RemediationChecksCase '#1491 Group-FindingsByLocation: the different-line finding (docs 2) is its own group' {
        @($testGroups | Where-Object { $_.Members.Count -eq 1 -and $_.Members[0].Stage -eq 'docs' -and $_.Members[0].Id -eq '2' }).Count -eq 1
    }
    Test-RemediationChecksCase '#1491 Group-FindingsByLocation: the no-anchor finding (tests 1) is its own group, never merged' {
        @($testGroups | Where-Object { $_.Members.Count -eq 1 -and $_.Members[0].Stage -eq 'tests' -and $_.Members[0].Id -eq '1' }).Count -eq 1
    }
    Test-RemediationChecksCase '#1491 Get-GroupPrimary: the higher-severity member (code 1, Major) is the primary over the lower one (docs 1, Minor)' {
        $primary = Get-GroupPrimary $sameLocationGroup[0].Members
        $primary.Stage -eq 'code' -and $primary.Id -eq '1'
    }
    Test-RemediationChecksCase '#1491 Get-GroupPrimary: a severity tie is broken by the members'' own order (stage order code before docs)' {
        $tieMembers = @(
            [pscustomobject]@{ Stage = 'code'; Id = '2'; Severity = 'Major' },
            [pscustomobject]@{ Stage = 'docs'; Id = '9'; Severity = 'Major' }
        )
        $tiePrimary = Get-GroupPrimary $tieMembers
        $tiePrimary.Stage -eq 'code' -and $tiePrimary.Id -eq '2'
    }

    # (d) Add-ReportedByLine: inserts right after '## Description', leaves the rest of the draft untouched.
    $reportedByDraft = "## Type`n`n- [ ] Documentation gap`n`n## Description`n`nA stale comment describing old behavior.`n`n## Location`n`n- **File(s)**: ``src/A.cs```n"
    $reportedByResult = Add-ReportedByLine $reportedByDraft 'Reported by: code 1, docs 1.'
    Test-RemediationChecksCase '#1491 Add-ReportedByLine: found the header and inserted the line' { $reportedByResult.Found }
    Test-RemediationChecksCase '#1491 Add-ReportedByLine: the line lands right after "## Description"' {
        $rbLines = @($reportedByResult.Text -split "`r?`n")
        $descIdx = [array]::IndexOf($rbLines, '## Description')
        $descIdx -ge 0 -and $rbLines[$descIdx + 2] -eq 'Reported by: code 1, docs 1.'
    }
    Test-RemediationChecksCase '#1491 Add-ReportedByLine: leaves the rest of the draft (the original Description sentence) untouched' {
        $reportedByResult.Text -match 'A stale comment describing old behavior\.'
    }
    Test-RemediationChecksCase '#1491 Add-ReportedByLine: a draft with no "## Description" header is returned unchanged, Found = $false' {
        $noDescResult = Add-ReportedByLine "## Type`n`nNo Description header here." 'Reported by: code 1.'
        (-not $noDescResult.Found) -and $noDescResult.Text -eq "## Type`n`nNo Description header here."
    }

    # Adversarial review of #1491: Build-DraftBrief's own $groupNote asks the model to write its own "Reported
    # by: ..." line at the start of Description -- a model that complies must not end up with the line TWICE.
    $modelWroteOwnDraft = "## Description`n`nReported by: code 1, docs 1.`n`nA stale comment describing old behavior.`n"
    $modelWroteOwnResult = Add-ReportedByLine $modelWroteOwnDraft 'Reported by: code 1, docs 1.'
    Test-RemediationChecksCase '#1491 Add-ReportedByLine: when the model already wrote its own "Reported by:" line, the result has exactly ONE such line, never two' {
        @(($modelWroteOwnResult.Text -split "`r?`n") | Where-Object { $_ -match '(?i)^Reported by:' }).Count -eq 1
    }
    Test-RemediationChecksCase '#1491 Add-ReportedByLine: replacing the model''s own line still keeps the rest of the Description' {
        $modelWroteOwnResult.Text -match 'A stale comment describing old behavior\.'
    }
    $modelWroteDifferentWordingDraft = "## Description`n`nReported by: code 1 and docs 1, same defect.`n`nA stale comment.`n"
    $modelWroteDifferentWordingResult = Add-ReportedByLine $modelWroteDifferentWordingDraft 'Reported by: code 1, docs 1.'
    Test-RemediationChecksCase '#1491 Add-ReportedByLine: the model''s own DIFFERENTLY WORDED "Reported by:" line is replaced by the deterministic one, not kept alongside it' {
        $rbLines2 = @($modelWroteDifferentWordingResult.Text -split "`r?`n")
        (@($rbLines2 | Where-Object { $_ -match '(?i)^Reported by:' })).Count -eq 1 -and ($rbLines2 -contains 'Reported by: code 1, docs 1.')
    }

    if (Get-Command git -ErrorAction SilentlyContinue) {
        # A self-contained fixture repo (own '.git', the same pattern the #1375/#1492 fixtures above use) with
        # 4 findings across the 3 stages: code-1 and docs-1 cite the SAME file:line (`src/A.cs:20`, code-1 is
        # Major and must be the drafted primary); docs-2 cites a DIFFERENT line of the SAME file (`src/A.cs:99`,
        # its own group); tests-1 cites no file at all (never grouped with anything).
        $remWt1491 = Join-Path $work 'RemediationGroupingWt'
        if (Test-Path $remWt1491) { Remove-Item -Recurse -Force $remWt1491 }
        New-Item -ItemType Directory -Force (Join-Path $remWt1491 'tools\ai\audit') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $remWt1491 'artifacts\knowledge\stages') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $remWt1491 '.github\ISSUE_TEMPLATE') | Out-Null
        Copy-Item (Join-Path $repo 'tools\ai\audit\pipeline.json') (Join-Path $remWt1491 'tools\ai\audit\pipeline.json')
        Copy-Item (Join-Path $repo 'tools\ai\audit\_audit-lib.ps1') (Join-Path $remWt1491 'tools\ai\audit\_audit-lib.ps1')
        Copy-Item (Join-Path $repo 'tools\ai\audit\_remediation-checks.ps1') (Join-Path $remWt1491 'tools\ai\audit\_remediation-checks.ps1')
        Copy-Item (Join-Path $repo 'tools\ai\audit\audit-draft-remediation.ps1') (Join-Path $remWt1491 'tools\ai\audit\audit-draft-remediation.ps1')
        foreach ($t in 'bug_report.md', 'test_implementation.md', 'technical_debt.md') {
            Copy-Item (Join-Path $repo ".github\ISSUE_TEMPLATE\$t") (Join-Path $remWt1491 ".github\ISSUE_TEMPLATE\$t")
        }
        function Invoke-RemGit1491 { & git -C $remWt1491 -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
        Invoke-RemGit1491 init -q -b main
        Invoke-RemGit1491 commit -q --allow-empty -m base

        Set-Content (Join-Path $remWt1491 'artifacts\knowledge\stages\code.md') "## Findings`n1. **Major** -- ``src/A.cs:20`` stale doc comment (code stage).`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1491 'artifacts\knowledge\stages\tests.md') "## Findings`n1. **Minor** -- A general observation with no file citation at all.`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1491 'artifacts\knowledge\stages\docs.md') "## Findings`n1. **Minor** -- ``src/A.cs:20`` the same stale doc comment noted from the docs side.`n2. **Major** -- ``src/A.cs:99`` an unrelated defect, same file, different line.`n## Lessons for the pipeline`n- none`n"
        $remN1491 = 4646
        @{ issue = $remN1491; worktree = $remWt1491; branch = "audit/$remN1491"; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $remWt1491 'artifacts\knowledge\current-audit.json')

        $groupingOutput = & pwsh -NoProfile -File (Join-Path $remWt1491 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh 2>&1
        $groupingExit = $LASTEXITCODE
        Test-RemediationCase '#1491 grouping fixture: the full run exits 0' { $groupingExit -eq 0 }

        $groupingDryDir = Join-Path $remWt1491 "artifacts\knowledge\remediation\_dryrun-$remN1491"
        # Two stages reporting the same file:line (code 1, docs 1) produce ONE draft: only code-1 (the
        # higher-severity member) gets its own dry-run preview files; docs-1 gets none.
        Test-RemediationCase '#1491 grouping fixture: the same-location primary (code 1, Major) got its own dry-run brief' {
            Test-Path -LiteralPath (Join-Path $groupingDryDir 'code-1-brief.md')
        }
        Test-RemediationCase '#1491 grouping fixture: the merged sibling (docs 1, Minor, same location) never got its own dry-run brief' {
            -not (Test-Path -LiteralPath (Join-Path $groupingDryDir 'docs-1-brief.md'))
        }
        # Different lines of the same file (docs 2 vs. code 1/docs 1) are different groups: docs-2 drafts its own.
        Test-RemediationCase '#1491 grouping fixture: a different line of the same file (docs 2) got its own dry-run brief' {
            Test-Path -LiteralPath (Join-Path $groupingDryDir 'docs-2-brief.md')
        }
        # A finding with no anchor (tests 1) is never merged with anything and always drafts its own.
        Test-RemediationCase '#1491 grouping fixture: the no-anchor finding (tests 1) got its own dry-run brief' {
            Test-Path -LiteralPath (Join-Path $groupingDryDir 'tests-1-brief.md')
        }
        $groupingBriefs = @(Get-ChildItem $groupingDryDir -Filter '*-brief.md' -ErrorAction SilentlyContinue)
        Test-RemediationCase '#1491 grouping fixture: exactly 3 drafts total for 4 findings (one group merged)' { $groupingBriefs.Count -eq 3 }

        $groupingStageLines = Get-Content (Join-Path $remWt1491 'artifacts\knowledge\stages\remediation.md')
        Test-RemediationCase '#1491 grouping fixture: stages/remediation.md still lists all 4 findings' {
            @($groupingStageLines | Where-Object { $_ -match '^-\s+\w+\s+\d+\s+\(' }).Count -eq 4
        }
        Test-RemediationCase '#1491 grouping fixture: docs 1''s own line says it merged into code 1 (same location)' {
            @($groupingStageLines | Where-Object { $_ -match '^-\s+docs\s+1\s+\(Minor\):\s+merged into code 1 \(same location\)$' }).Count -eq 1
        }
        Test-RemediationCase '#1491 grouping fixture: docs 2 and tests 1 are NOT reported as merged (each drafted its own)' {
            (@($groupingStageLines | Where-Object { $_ -match '^-\s+docs\s+2\s+\(' }) -notmatch 'merged into') -and
            (@($groupingStageLines | Where-Object { $_ -match '^-\s+tests\s+1\s+\(' }) -notmatch 'merged into')
        }

        # -Only on the MERGED (non-primary) finding docs-1 regenerates the group's one draft (code-1's own),
        # never tries to draft docs-1 on its own (decision 4).
        $onlyMergedOutput = & pwsh -NoProfile -File (Join-Path $remWt1491 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -Only 'docs 1' 2>&1
        $onlyMergedExit = $LASTEXITCODE
        Test-RemediationCase '#1491 -Only "docs 1" (a merged, non-primary finding) exits 0' { $onlyMergedExit -eq 0 }
        Test-RemediationCase '#1491 -Only "docs 1" regenerates the group''s own primary brief (code-1), not a "docs-1-brief.md" of its own' {
            (Test-Path -LiteralPath (Join-Path $groupingDryDir 'code-1-brief.md')) -and (-not (Test-Path -LiteralPath (Join-Path $groupingDryDir 'docs-1-brief.md')))
        }
        $onlyMergedStageLines = Get-Content (Join-Path $remWt1491 'artifacts\knowledge\stages\remediation.md')
        Test-RemediationCase '#1491 -Only "docs 1": stages/remediation.md still lists all 4 findings after the -Only run' {
            @($onlyMergedStageLines | Where-Object { $_ -match '^-\s+\w+\s+\d+\s+\(' }).Count -eq 4
        }
        Test-RemediationCase '#1491 -Only "docs 1": docs 1''s own line still says merged into code 1' {
            @($onlyMergedStageLines | Where-Object { $_ -match '^-\s+docs\s+1\s+\(Minor\):\s+merged into code 1 \(same location\)$' }).Count -eq 1
        }
    }
    else {
        'SKIP #1491 grouping fixture: git is not on PATH'
    }
    # ---- end #1491 block ----

    # ---- #1534: audit-draft-remediation.ps1 -Duplicateof -- an explicit, logged override that records a
    # finding as a duplicate of an OPEN issue confirmed by audit-verifier or the orchestrator, drafting nothing
    # for it. Real case: audit #18's docs finding 12 duplicates #1177's own drift report, but #1177 only lists
    # it as one item of a numbered list inside its Description section, which #1393's own evidence check
    # deliberately excludes -- Test-DuplicateEvidence is unchanged by this issue (decision 6); the override is
    # the fix. -DryRun -NoGh never calls the model or `gh`, so this block stays free and offline like #1491/#1492
    # above; the -DuplicateOf OPEN-state check itself is skipped entirely under -NoGh (the script's own #1534
    # comment block), so this suite never needs a real GitHub issue.

    # (a) Format validation: a malformed entry is a fail-fast error, no file touched.
    Test-RemediationCase '#1534 -DuplicateOf: a malformed entry (no "=<issue>") is an error, not a silent no-op' {
        $badFormatOutput = & pwsh -NoProfile -File (Join-Path $repo 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -DuplicateOf 'docs 12' 2>&1
        $badFormatExit = $LASTEXITCODE
        $badFormatExit -ne 0 -and (Get-FlatOutput $badFormatOutput) -match "must be '<stage> <n>=<issue>'"
    }
    Test-RemediationCase '#1534 -DuplicateOf: a non-numeric issue number is an error' {
        $badIssueOutput = & pwsh -NoProfile -File (Join-Path $repo 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -DuplicateOf 'docs 12=abc' 2>&1
        $badIssueExit = $LASTEXITCODE
        $badIssueExit -ne 0 -and (Get-FlatOutput $badIssueOutput) -match "must be '<stage> <n>=<issue>'"
    }

    if (Get-Command git -ErrorAction SilentlyContinue) {
        # A self-contained fixture repo (own '.git', the #1491/#1492 pattern) with 3 findings: code-1 and
        # docs-1 cite the SAME file:line (`src/A.cs:20`, code-1 is Major and is the group's own primary) --
        # proves decision 4 (overriding the primary records the WHOLE group as the duplicate); docs-12 is a
        # standalone finding with no shared location -- mirrors the real audit #18 case and the acceptance
        # command's own "docs 12" key.
        $remWt1534 = Join-Path $work 'RemediationDuplicateOfWt'
        if (Test-Path $remWt1534) { Remove-Item -Recurse -Force $remWt1534 }
        New-Item -ItemType Directory -Force (Join-Path $remWt1534 'tools\ai\audit') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $remWt1534 'artifacts\knowledge\stages') | Out-Null
        New-Item -ItemType Directory -Force (Join-Path $remWt1534 '.github\ISSUE_TEMPLATE') | Out-Null
        Copy-Item (Join-Path $repo 'tools\ai\audit\pipeline.json') (Join-Path $remWt1534 'tools\ai\audit\pipeline.json')
        Copy-Item (Join-Path $repo 'tools\ai\audit\_audit-lib.ps1') (Join-Path $remWt1534 'tools\ai\audit\_audit-lib.ps1')
        Copy-Item (Join-Path $repo 'tools\ai\audit\_remediation-checks.ps1') (Join-Path $remWt1534 'tools\ai\audit\_remediation-checks.ps1')
        Copy-Item (Join-Path $repo 'tools\ai\audit\audit-draft-remediation.ps1') (Join-Path $remWt1534 'tools\ai\audit\audit-draft-remediation.ps1')
        foreach ($t in 'bug_report.md', 'test_implementation.md', 'technical_debt.md') {
            Copy-Item (Join-Path $repo ".github\ISSUE_TEMPLATE\$t") (Join-Path $remWt1534 ".github\ISSUE_TEMPLATE\$t")
        }
        function Invoke-RemGit1534 { & git -C $remWt1534 -c user.name=hooks -c user.email=hooks@example.invalid @args 2>&1 | Out-Null }
        Invoke-RemGit1534 init -q -b main
        Invoke-RemGit1534 commit -q --allow-empty -m base

        Set-Content (Join-Path $remWt1534 'artifacts\knowledge\stages\code.md') "## Findings`n1. **Major** -- ``src/A.cs:20`` stale doc comment (code stage).`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1534 'artifacts\knowledge\stages\tests.md') "## Findings`n- none`n## Lessons for the pipeline`n- none`n"
        Set-Content (Join-Path $remWt1534 'artifacts\knowledge\stages\docs.md') "## Findings`n1. **Minor** -- ``src/A.cs:20`` the same stale doc comment noted from the docs side.`n12. **Blocker** -- ``docs/messaging/index.md:45`` references the removed package ``Encina.Dapper.Oracle``.`n## Lessons for the pipeline`n- none`n"
        $remN1534 = 1818
        @{ issue = $remN1534; worktree = $remWt1534; branch = "audit/$remN1534"; startedUtc = '2026-01-01T00:00:00Z' } | ConvertTo-Json | Set-Content (Join-Path $remWt1534 'artifacts\knowledge\current-audit.json')

        # Baseline (no -DuplicateOf): a full run, so a later -Only + -DuplicateOf run has a stages/remediation.md
        # to fall back to for the findings it does not touch.
        $baselineOutput1534 = & pwsh -NoProfile -File (Join-Path $remWt1534 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh 2>&1
        $baselineExit1534 = $LASTEXITCODE
        Test-RemediationCase '#1534 fixture: the baseline (no -DuplicateOf) full run exits 0' { $baselineExit1534 -eq 0 }

        $dryDir1534 = Join-Path $remWt1534 "artifacts\knowledge\remediation\_dryrun-$remN1534"
        Test-RemediationCase '#1534 fixture: the baseline run wrote a dry-run brief for docs 12' {
            Test-Path -LiteralPath (Join-Path $dryDir1534 'docs-12-brief.md')
        }

        # (c) unknown key: a -DuplicateOf entry naming a finding not parsed from the stages is an error.
        $unknownKeyOutput1534 = & pwsh -NoProfile -File (Join-Path $remWt1534 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -DuplicateOf 'docs 99=1177' 2>&1
        $unknownKeyExit1534 = $LASTEXITCODE
        Test-RemediationCase "#1534 -DuplicateOf 'docs 99=1177' (no matching finding) is an error, not a silent no-op" {
            $unknownKeyExit1534 -ne 0 -and (Get-FlatOutput $unknownKeyOutput1534) -match 'does not match a finding'
        }

        # (b) the acceptance command's own typical pairing: -Only 'docs 12' -DuplicateOf 'docs 12=1177'. Expect
        # no draft, the duplicate line (with " (manual override)"), and no dry-run brief written for docs 12.
        if (Test-Path -LiteralPath $dryDir1534) { Remove-Item -LiteralPath (Join-Path $dryDir1534 'docs-12-brief.md') -Force -ErrorAction SilentlyContinue }
        $onlyDupOutput1534 = & pwsh -NoProfile -File (Join-Path $remWt1534 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -Only 'docs 12' -DuplicateOf 'docs 12=1177' 2>&1
        $onlyDupExit1534 = $LASTEXITCODE
        Test-RemediationCase "#1534 -Only 'docs 12' -DuplicateOf 'docs 12=1177' exits 0" { $onlyDupExit1534 -eq 0 }
        Test-RemediationCase "#1534 -Only 'docs 12' -DuplicateOf 'docs 12=1177' writes no dry-run brief for docs 12 (no draft)" {
            -not (Test-Path -LiteralPath (Join-Path $dryDir1534 'docs-12-brief.md'))
        }
        $onlyDupStageLines = Get-Content (Join-Path $remWt1534 'artifacts\knowledge\stages\remediation.md')
        Test-RemediationCase "#1534 -Only 'docs 12' -DuplicateOf 'docs 12=1177': docs 12''s own line reads like an automatic duplicate, plus (manual override)" {
            @($onlyDupStageLines | Where-Object { $_ -match '^-\s+docs\s+12\s+\(Blocker\):\s+duplicate of #1177 \(manual override\)$' }).Count -eq 1
        }
        Test-RemediationCase "#1534 -Only 'docs 12' -DuplicateOf 'docs 12=1177': stages\remediation.md still lists all 3 findings" {
            @($onlyDupStageLines | Where-Object { $_ -match '^-\s+\w+\s+\d+\s+\(' }).Count -eq 3
        }
        Test-RemediationCase "#1534 -Only 'docs 12' -DuplicateOf 'docs 12=1177': logs the override under Lessons for the pipeline" {
            (Get-FlatOutput (Get-Content (Join-Path $remWt1534 'artifacts\knowledge\stages\remediation.md'))) -match [regex]::Escape('docs 12: recorded as duplicate of #1177 by manual override')
        }
        Test-RemediationCase "#1534 -Only 'docs 12' -DuplicateOf 'docs 12=1177': code 1 and docs 1 (untouched) keep their own lines" {
            (@($onlyDupStageLines | Where-Object { $_ -match '^-\s+code\s+1\s+\(' })).Count -eq 1 -and
            (@($onlyDupStageLines | Where-Object { $_ -match '^-\s+docs\s+1\s+\(' })).Count -eq 1
        }

        # (d) overriding the PRIMARY of a #1491 same-location group records the WHOLE group as the duplicate --
        # every member's own line, never a "merged into ..." line for the non-primary sibling. Scoped with
        # -Only 'code 1' (the group's own primary key) so docs 12's own line from run (b), still untouched
        # here, is proven to survive via stages/remediation.md's own existing-line fallback.
        $groupDupOutput1534 = & pwsh -NoProfile -File (Join-Path $remWt1534 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -Only 'code 1' -DuplicateOf 'code 1=999' 2>&1
        $groupDupExit1534 = $LASTEXITCODE
        Test-RemediationCase "#1534 -Only 'code 1' -DuplicateOf 'code 1=999' (group primary) exits 0" { $groupDupExit1534 -eq 0 }
        Test-RemediationCase "#1534 -DuplicateOf 'code 1=999' writes no dry-run brief for code 1 or its merged sibling docs 1" {
            (-not (Test-Path -LiteralPath (Join-Path $dryDir1534 'code-1-brief.md'))) -and (-not (Test-Path -LiteralPath (Join-Path $dryDir1534 'docs-1-brief.md')))
        }
        $groupDupStageLines = Get-Content (Join-Path $remWt1534 'artifacts\knowledge\stages\remediation.md')
        Test-RemediationCase "#1534 -DuplicateOf 'code 1=999': the primary's own line (code 1) says duplicate of #999 (manual override)" {
            @($groupDupStageLines | Where-Object { $_ -match '^-\s+code\s+1\s+\(Major\):\s+duplicate of #999 \(manual override\)$' }).Count -eq 1
        }
        Test-RemediationCase "#1534 -DuplicateOf 'code 1=999': the merged sibling's own line (docs 1) ALSO says duplicate of #999 (manual override), never 'merged into ...'" {
            @($groupDupStageLines | Where-Object { $_ -match '^-\s+docs\s+1\s+\(Minor\):\s+duplicate of #999 \(manual override\)$' }).Count -eq 1
        }
        Test-RemediationCase "#1534 -DuplicateOf 'code 1=999': docs 12 (untouched by this run) still drafts, keeping its own earlier duplicate-override line" {
            @($groupDupStageLines | Where-Object { $_ -match '^-\s+docs\s+12\s+\(Blocker\):\s+duplicate of #1177 \(manual override\)$' }).Count -eq 1
        }

        # (e) #1535: overriding a NON-primary member of the SAME #1491 group (docs 1, merged into code 1 above)
        # records the WHOLE group as the duplicate too -- not only the primary. Scoped with -Only 'docs 1' (the
        # non-primary member's own key) and a different target issue (#998) so its own stage line is
        # distinguishable from run (d)'s #999.
        $nonPrimaryDupOutput1534 = & pwsh -NoProfile -File (Join-Path $remWt1534 'tools\ai\audit\audit-draft-remediation.ps1') -DryRun -NoGh -Only 'docs 1' -DuplicateOf 'docs 1=998' 2>&1
        $nonPrimaryDupExit1534 = $LASTEXITCODE
        Test-RemediationCase "#1535 -Only 'docs 1' -DuplicateOf 'docs 1=998' (group NON-primary member) exits 0" { $nonPrimaryDupExit1534 -eq 0 }
        Test-RemediationCase "#1535 -DuplicateOf 'docs 1=998' writes no dry-run brief for docs 1 or its group primary code 1" {
            (-not (Test-Path -LiteralPath (Join-Path $dryDir1534 'docs-1-brief.md'))) -and (-not (Test-Path -LiteralPath (Join-Path $dryDir1534 'code-1-brief.md')))
        }
        $nonPrimaryDupStageLines = Get-Content (Join-Path $remWt1534 'artifacts\knowledge\stages\remediation.md')
        Test-RemediationCase "#1535 -DuplicateOf 'docs 1=998': the non-primary member's own line (docs 1) says duplicate of #998 (manual override)" {
            @($nonPrimaryDupStageLines | Where-Object { $_ -match '^-\s+docs\s+1\s+\(Minor\):\s+duplicate of #998 \(manual override\)$' }).Count -eq 1
        }
        Test-RemediationCase "#1535 -DuplicateOf 'docs 1=998': the group PRIMARY's own line (code 1) ALSO says duplicate of #998 (manual override), same as naming the primary directly" {
            @($nonPrimaryDupStageLines | Where-Object { $_ -match '^-\s+code\s+1\s+\(Major\):\s+duplicate of #998 \(manual override\)$' }).Count -eq 1
        }
    }
    else {
        'SKIP #1534 fixture: git is not on PATH'
    }
    # ---- end #1534 block ----

    # ---- #1565: tools/ai/audit/_remediation-checks.ps1 and audit-draft-remediation.ps1 -- the five defect classes
    # audit #18's verifier loop kept finding in the local model's drafts, each decided deterministically from the
    # finding's own text: (1) packages dropped (a five-package finding drafted as NATS-only), (2) the wrong Test
    # Category (a unit-flag gap drafted with Integration ticked), (3) a coverage figure the finding never gave
    # (0% against a measured 71.1%), (4) "throws" for a Left-returning RequestAsync, (5) pipeline meta-text in a
    # draft ("not provided in finding"). Pure functions only: the model and `gh` are never called here, and the
    # fixtures live under a temporary root, never the repository.
    $pkgRoot1565 = Join-Path $work 'Pkg1565'
    if (Test-Path $pkgRoot1565) { Remove-Item -Recurse -Force $pkgRoot1565 }
    foreach ($pkg1565 in 'Encina.NATS', 'Encina.RabbitMQ', 'Encina.MQTT', 'Encina.AzureServiceBus', 'Encina.Kafka') {
        New-Item -ItemType Directory -Force (Join-Path $pkgRoot1565 "src\$pkg1565") | Out-Null
    }
    New-Item -ItemType Directory -Force (Join-Path $pkgRoot1565 '.github\coverage-manifest') | Out-Null
    Set-Content -LiteralPath (Join-Path $pkgRoot1565 '.github\coverage-manifest\Encina.NATS.json') -Value '{ "package": "Encina.NATS", "targets": { "guard": 15, "unit": 55 } }'
    $manifestDir1565 = Join-Path $pkgRoot1565 '.github\coverage-manifest'

    $finding1565 = 'Five transport packages (`src/Encina.NATS/NATSMessagePublisher.cs`, `src/Encina.RabbitMQ/RabbitMQMessagePublisher.cs`, `Encina.MQTT/MqttMessagePublisher.cs`, Encina.AzureServiceBus and `src/Encina.Kafka/`) are below the unit flag target: unit and guard flags cover 71.1% of the lines. `NatsMessagePublisher.RequestAsync` returns `Left` when the request times out. Not related: Encina.Invented, Encina.Messaging.Outbox.'
    $tplTest1565 = (Get-Content (Join-Path $repo '.github\ISSUE_TEMPLATE\test_implementation.md') -Raw) -replace '(?s)^---.*?---\r?\n', ''

    # (1) packages
    $packages1565 = @(Get-FindingPackages $finding1565 $pkgRoot1565)
    Test-RemediationChecksCase '#1565 Get-FindingPackages: derives the five existing packages, sorted, from src/ paths, Encina.<X>/<file> paths and bare tokens' {
        ($packages1565 -join ',') -eq 'Encina.AzureServiceBus,Encina.Kafka,Encina.MQTT,Encina.NATS,Encina.RabbitMQ'
    }
    Test-RemediationChecksCase '#1565 Get-FindingPackages: an invented name and a namespace-only token never pass' {
        ($packages1565 -notcontains 'Encina.Invented') -and ($packages1565 -notcontains 'Encina.Messaging.Outbox') -and ($packages1565 -notcontains 'Encina.Messaging')
    }
    Test-RemediationChecksCase '#1565 Get-FindingPackages: a finding with no repo root or no package yields an empty set' {
        @(Get-FindingPackages 'No package here.' $pkgRoot1565).Count -eq 0 -and @(Get-FindingPackages $finding1565 '').Count -eq 0
    }
    $natsOnlyDraft1565 = "## Packages / Providers Affected`n`n- **Package(s)**: Encina.NATS`n- **Provider(s)**: NATS`n`n## Description`n`nEncina.NATS has no unit tests for NatsMessagePublisher.`n"
    Test-RemediationChecksCase '#1565 Get-MissingPackages (failing case): a five-package finding drafted as NATS-only misses the other four' {
        ((@(Get-MissingPackages $natsOnlyDraft1565 $packages1565)) -join ',') -eq 'Encina.AzureServiceBus,Encina.Kafka,Encina.MQTT,Encina.RabbitMQ'
    }
    Test-RemediationChecksCase '#1565 Get-MissingPackages (passing case): a draft naming every package in its body misses none, and the package line itself does not count' {
        $fullDraft = $natsOnlyDraft1565 + "`nAlso Encina.RabbitMQ, src/Encina.MQTT/X.cs, Encina.AzureServiceBus and Encina.Kafka.Extensions.`n"
        (@(Get-MissingPackages $fullDraft $packages1565)).Count -eq 0
    }
    Test-RemediationChecksCase '#1565 Get-MissingPackages: a package that is only a prefix of a longer name is still missing' {
        (@(Get-MissingPackages 'Only Encina.NATSExtras is named.' @('Encina.NATS'))) -contains 'Encina.NATS'
    }
    $setPkgTest1565 = Set-PackageLine $natsOnlyDraft1565 $packages1565 'test_implementation.md'
    Test-RemediationChecksCase '#1565 Set-PackageLine: writes the sorted comma-separated set into the test_implementation.md Package(s) line' {
        (@($setPkgTest1565 -split "`n")) -contains '- **Package(s)**: Encina.AzureServiceBus, Encina.Kafka, Encina.MQTT, Encina.NATS, Encina.RabbitMQ'
    }
    Test-RemediationChecksCase '#1565 Set-PackageLine: writes the set into the bug_report.md Package(s) Affected line and leaves other lines untouched' {
        $bugDraft = "## Environment`n`n- **OS**: Windows`n- **Package(s) Affected**: [e.g., Encina.Dapper.SqlServer]`n"
        $r = @((Set-PackageLine $bugDraft @('Encina.Kafka', 'Encina.NATS') 'bug_report.md') -split "`n")
        ($r -contains '- **Package(s) Affected**: Encina.Kafka, Encina.NATS') -and ($r -contains '- **OS**: Windows')
    }
    Test-RemediationChecksCase '#1565 Set-PackageLine: an empty set leaves the draft unchanged' {
        (Set-PackageLine $natsOnlyDraft1565 @() 'test_implementation.md') -eq $natsOnlyDraft1565
    }

    # (2) test category
    $badCategoryDraft1565 = $tplTest1565 -replace '- \[ \] Integration Tests \(Docker/Testcontainers\)', '- [x] Integration Tests (Docker/Testcontainers)' `
        -replace '- \[ \] Docker / Testcontainers', '- [x] Docker / Testcontainers' `
        -replace '(?m)^- \*\*Collection\*\*:.*$', '- **Collection**: `ADO-PostgreSQL`' `
        -replace '(?m)^- \*\*Fixture\*\*:.*$', '- **Fixture**: `PostgreSqlFixture`'
    $unitFlagFinding1565 = 'The unit flag of `src/Encina.NATS/NATSMessagePublisher.cs` is below target; the guard flag too.'
    $fixedCategory1565 = @((Set-TestCategory $badCategoryDraft1565 $unitFlagFinding1565) -split "`n")
    Test-RemediationChecksCase '#1565 Get-TestCategoryTicks: the unit/guard flag ticks Unit Tests and Guard Clause Tests, never Integration Tests' {
        $ticks = @(Get-TestCategoryTicks $unitFlagFinding1565)
        ($ticks -contains 'Unit Tests') -and ($ticks -contains 'Guard Clause Tests') -and ($ticks -notcontains 'Integration Tests') -and ($ticks -contains 'Coverage Gap')
    }
    Test-RemediationChecksCase '#1565 Set-TestCategory (failing case): a unit-flag gap drafted with Integration ticked is rewritten to Unit only' {
        ($fixedCategory1565 -contains '- [x] Unit Tests') -and ($fixedCategory1565 -contains '- [ ] Integration Tests (Docker/Testcontainers)') -and ($fixedCategory1565 -contains '- [x] Guard Clause Tests')
    }
    Test-RemediationChecksCase '#1565 Set-TestCategory: without Integration, Infrastructure ticks only "None (pure unit tests)"' {
        $infraTicked = @($fixedCategory1565 | Where-Object { $_ -match '^- \[x\] (Docker|Real database|Message broker|NBomber|BenchmarkDotNet|None)' })
        $infraTicked.Count -eq 1 -and $infraTicked[0] -eq '- [x] None (pure unit tests)'
    }
    Test-RemediationChecksCase '#1565 Set-TestCategory: without Integration, the Collection Fixture body is exactly "Not applicable" and keeps the header and quoted AGENTS.md line' {
        $text = $fixedCategory1565 -join "`n"
        $text -match '(?s)## Collection Fixture \(Integration Tests Only\)\n\n> Per `AGENTS\.md` §9[^\n]*\n\nNot applicable: no integration tests\.\n\n## Related Issues' -and $text -notmatch 'ADO-PostgreSQL'
    }
    $integrationFinding1565 = 'The integration flag is 0%: no Testcontainers test exercises `src/Encina.NATS/NATSMessagePublisher.cs` against a real broker container.'
    $keptIntegration1565 = @((Set-TestCategory $badCategoryDraft1565 $integrationFinding1565) -split "`n")
    Test-RemediationChecksCase '#1565 Set-TestCategory (passing case): a finding about the integration flag/Testcontainers keeps Integration ticked and leaves Infrastructure and Collection Fixture alone' {
        ($keptIntegration1565 -contains '- [x] Integration Tests (Docker/Testcontainers)') -and ($keptIntegration1565 -contains '- [x] Docker / Testcontainers') -and
            ($keptIntegration1565 -contains '- **Collection**: `ADO-PostgreSQL`') -and ($keptIntegration1565 -notcontains '- [x] Unit Tests')
    }
    Test-RemediationChecksCase '#1565 Get-TestCategoryTicks: "Unit of Work" alone does not tick Unit Tests, and other boxes tick only when named' {
        $uow = @(Get-TestCategoryTicks 'The Unit of Work leaks a connection.')
        $named = @(Get-TestCategoryTicks 'Add property-based tests (FsCheck) and a benchmark; the contract flag is empty.')
        $uow.Count -eq 0 -and ($named -contains 'Property-Based Tests') -and ($named -contains 'Benchmark Tests') -and ($named -contains 'Contract Tests') -and ($named -notcontains 'Unit Tests')
    }
    Test-RemediationChecksCase '#1565 Set-TestCategory: a draft without the sections is returned unchanged' {
        (Set-TestCategory "## Description`n`nNothing.`n" $unitFlagFinding1565) -eq "## Description`n`nNothing.`n"
    }

    # (3) figures
    Test-RemediationChecksCase '#1565 Get-UnsupportedFigures (failing case): 0% against a measured 71.1% is flagged' {
        $flagged = @(Get-UnsupportedFigures '| Encina.NATS | 0% | 85% | -85% |' $finding1565 $tplTest1565 $packages1565 $manifestDir1565)
        $flagged.Count -eq 1 -and $flagged[0] -eq '0%'
    }
    Test-RemediationChecksCase '#1565 Get-UnsupportedFigures (passing case): the finding figure, the template figure and a manifest target are all accepted' {
        (@(Get-UnsupportedFigures 'Measured 71.1%; template target 85%; manifest unit target 55% and guard target 15%.' $finding1565 $tplTest1565 $packages1565 $manifestDir1565)).Count -eq 0
    }
    Test-RemediationChecksCase '#1565 Get-UnsupportedFigures: a manifest target only counts for a package of the set' {
        (@(Get-UnsupportedFigures 'Unit target 55%.' 'No figures.' '' @('Encina.Kafka') $manifestDir1565)) -contains '55%'
    }
    Test-RemediationChecksCase '#1565 Get-UnsupportedFigures: a rounded figure (71%) is not the finding''s 71.1%, and repeats are listed once' {
        (@(Get-UnsupportedFigures 'About 71% and 71%.' $finding1565 '' $packages1565 $manifestDir1565)).Count -eq 1
    }

    # (4) Either semantics
    $eitherFinding1565 = '`NatsMessagePublisher.RequestAsync` returns `Left` (an `EncinaError`) when the request times out instead of surfacing the failure.'
    Test-RemediationChecksCase '#1565 Get-EitherSemanticsViolations (failing case): a Left-returning RequestAsync drafted as "throws" is flagged' {
        $v = @(Get-EitherSemanticsViolations $eitherFinding1565 "RequestAsync throws a TimeoutException on timeout.`nIt should return a Left.")
        $v.Count -eq 1 -and $v[0] -match 'throws a TimeoutException'
    }
    Test-RemediationChecksCase '#1565 Get-EitherSemanticsViolations (passing case): "throw" on a line naming ArgumentNullException is a legitimate guard clause' {
        (@(Get-EitherSemanticsViolations $eitherFinding1565 'The guard clause throws ArgumentNullException for a null request; a timeout returns Left.')).Count -eq 0
    }
    Test-RemediationChecksCase '#1565 Get-EitherSemanticsViolations: no violation when the finding itself says throws, or never mentions Left/Either' {
        $withThrow = Get-EitherSemanticsViolations 'RequestAsync returns Left, but a null argument throws.' 'It throws on null.'
        $noEither = Get-EitherSemanticsViolations 'RequestAsync mishandles a timeout.' 'It throws on timeout.'
        @($withThrow).Count -eq 0 -and @($noEither).Count -eq 0
    }

    # (5) meta-text
    Test-RemediationChecksCase '#1565 Get-MetaTextLines (failing case): a Location of "Specific file path not provided in finding" is flagged, case-insensitively' {
        $v = @(Get-MetaTextLines "## Location`n`n- **File(s)**: Specific file path NOT PROVIDED IN FINDING`n- #12 (the local model proposed it as a duplicate; the evidence check rejected it)")
        $v.Count -eq 2
    }
    Test-RemediationChecksCase '#1565 Get-MetaTextLines (passing case): a clean draft has no meta-text' {
        (@(Get-MetaTextLines "## Location`n`n- **File(s)**: ``src/Encina.NATS/NATSMessagePublisher.cs:93-141```n- #12 - possibly related (a similar open issue)")).Count -eq 0
    }

    Test-RemediationChecksCase '#1565 Get-MetaTextLines: a phrase the finding itself uses (an audit of the local model or the evidence check) is not meta-text' {
        $aboutTooling = 'The evidence check accepts a duplicate that the local model named without a shared symbol.'
        (@(Get-MetaTextLines 'The evidence check never looks at the local model reply.' $aboutTooling)).Count -eq 0 -and
            (@(Get-MetaTextLines 'Location: not provided in finding; the evidence check rejected it.' $aboutTooling)).Count -eq 1
    }
    Test-RemediationChecksCase '#1565 Get-FindingPackages: a src/Encina.<X> citation without a trailing slash still counts' {
        (@(Get-FindingPackages 'See `src/Encina.NATS` and src/Encina.Kafka.' $pkgRoot1565) -join ',') -eq 'Encina.Kafka,Encina.NATS'
    }

    # coordinator follow-up (#1565): cases that could mark a CORRECT draft and stop the verifier loop converging
    Test-RemediationChecksCase '#1565 Get-EitherSemanticsViolations: a plain sentence-initial "Either" does not switch the check on, but `Either<...>` and a backticked `Either` do' {
        $plain = Get-EitherSemanticsViolations 'Either option may be null for the caller.' 'RequestAsync throws InvalidOperationException.'
        $generic = Get-EitherSemanticsViolations 'RequestAsync returns Either<EncinaError, string> on timeout.' 'RequestAsync throws InvalidOperationException.'
        $ticked = Get-EitherSemanticsViolations 'RequestAsync returns an `Either` on timeout.' 'RequestAsync throws InvalidOperationException.'
        @($plain).Count -eq 0 -and @($generic).Count -eq 1 -and @($ticked).Count -eq 1
    }
    Test-RemediationChecksCase '#1565 Get-EitherSemanticsViolations: a negated throw word is not a violation (never, does not, rather than, instead of, without, no longer)' {
        $negated = "The method never throws.`nIt does not throw on timeout.`nIt returns Left rather than throwing.`nIt returns Left instead of throwing.`nIt fails without throwing.`nIt no longer throws."
        (@(Get-EitherSemanticsViolations $eitherFinding1565 $negated)).Count -eq 0
    }
    Test-RemediationChecksCase '#1565 Get-EitherSemanticsViolations: an un-negated throw on a line that also contains a negation elsewhere is still flagged' {
        $v = @(Get-EitherSemanticsViolations $eitherFinding1565 "It never returns Left; RequestAsync throws instead.`nA timeout is thrown as an exception.")
        $v.Count -eq 2
    }
    Test-RemediationChecksCase '#1565 Get-TestCategoryTicks: negated mentions do not tick ("no integration tests", "are not needed", "without Docker")' {
        (@(Get-TestCategoryTicks 'The unit flag is low; no integration tests are involved.')) -notcontains 'Integration Tests' -and
            (@(Get-TestCategoryTicks 'The unit flag is low. Integration tests are not needed here.')) -notcontains 'Integration Tests' -and
            (@(Get-TestCategoryTicks 'The unit flag is low; add tests without Testcontainers.')) -notcontains 'Integration Tests' -and
            (@(Get-TestCategoryTicks 'The unit flag is low; no integration tests are involved.')) -contains 'Unit Tests'
    }
    Test-RemediationChecksCase '#1565 Get-TestCategoryTicks: an un-negated integration or Testcontainers mention still ticks Integration; Docker or compose alone does not' {
        (@(Get-TestCategoryTicks 'The integration flag is 0%.')) -contains 'Integration Tests' -and
            (@(Get-TestCategoryTicks 'Needs Testcontainers coverage.')) -contains 'Integration Tests' -and
            (@(Get-TestCategoryTicks 'The unit flag is low; docker compose is how CI starts services.')) -notcontains 'Integration Tests'
    }
    Test-RemediationChecksCase '#1565 Get-TestCategoryTicks: a path segment such as Encina.UnitTests counts as a unit mention' {
        (@(Get-TestCategoryTicks 'Missing cases in `tests/Encina.UnitTests/NatsTests.cs`.')) -contains 'Unit Tests' -and
            (@(Get-TestCategoryTicks 'Nothing about test projects here.')) -notcontains 'Unit Tests'
    }
    Test-RemediationChecksCase '#1565 Get-UnsupportedFigures: "71.1 %", "71.1%" and "71.1 percent" are the same figure on both sides' {
        $spaced = 'Measured 71.1 % of lines.'
        $percentWord = 'Measured 71.1 percent of lines.'
        (@(Get-UnsupportedFigures 'Covers 71.1%.' $spaced '' @() '')).Count -eq 0 -and (@(Get-UnsupportedFigures 'Covers 71.1 %.' $percentWord '' @() '')).Count -eq 0 -and
            (@(Get-UnsupportedFigures 'Covers 71.1 percent.' 'Measured 71.1%.' '' @() '')).Count -eq 0
    }
    Test-RemediationChecksCase '#1565 Get-UnsupportedFigures: a different spaced or worded figure is still flagged' {
        $v = @(Get-UnsupportedFigures 'Covers 0 % and 12 percent.' 'Measured 71.1%.' '' @() '')
        $v.Count -eq 2
    }
    Test-RemediationChecksCase '#1565 Get-TestCategoryTicks: a MISSING-tests statement ("no integration tests exist", "not covered by integration tests") still ticks the category' {
        (@(Get-TestCategoryTicks 'There are no integration tests for the saga store.')) -contains 'Integration Tests' -and
            (@(Get-TestCategoryTicks 'No unit tests exist for SagaRunner.')) -contains 'Unit Tests' -and
            (@(Get-TestCategoryTicks 'The branch is not covered by integration tests.')) -contains 'Integration Tests' -and
            (@(Get-TestCategoryTicks 'The store does not have integration tests.')) -contains 'Integration Tests' -and
            (@(Get-TestCategoryTicks 'No integration tests needed for this change.')) -notcontains 'Integration Tests'
    }
    Test-RemediationChecksCase '#1565 Get-EitherSemanticsViolations: a negation of ANOTHER verb before "and throws" does not hide the violation' {
        $v = @(Get-EitherSemanticsViolations $eitherFinding1565 "It does not retry and throws InvalidOperationException.`nIt cannot recover but throws.`nNo exception is thrown.")
        $v.Count -eq 2 -and $v[0] -match 'does not retry' -and $v[1] -match 'cannot recover'
    }
    Test-RemediationChecksCase '#1565 Get-EitherSemanticsViolations: "throws no exception" and "throws nothing" are not violations' {
        (@(Get-EitherSemanticsViolations $eitherFinding1565 "The method throws no exception on timeout.`nIt throws nothing; it returns Left.")).Count -eq 0
    }
    $nsRoot1565 = Join-Path $work 'PkgNs1565'
    if (Test-Path $nsRoot1565) { Remove-Item -Recurse -Force $nsRoot1565 }
    foreach ($nsPkg in 'Encina.Messaging', 'Encina.ADO.SqlServer') { New-Item -ItemType Directory -Force (Join-Path $nsRoot1565 "src\$nsPkg") | Out-Null }
    Test-RemediationChecksCase '#1565 Get-FindingPackages: a dotted type or namespace token resolves to its longest dotted prefix that is a src/ directory' {
        $r = @(Get-FindingPackages 'See `Encina.Messaging.Sagas.SagaRunner` and Encina.ADO.SqlServer.Sagas.SagaStoreADO.' $nsRoot1565)
        ($r -join ',') -eq 'Encina.ADO.SqlServer,Encina.Messaging'
    }
    Test-RemediationChecksCase '#1565 Get-FindingPackages: the package takes the directory''s real casing, and a token with no existing prefix still yields nothing' {
        $r = @(Get-FindingPackages 'See Encina.messaging.Outbox and Encina.Invented.Thing.' $nsRoot1565)
        ($r -join ',') -ceq 'Encina.Messaging'
    }

    # combined pass, marks and the re-ask note
    $badAll1565 = $natsOnlyDraft1565 + "`nCoverage is 0%. RequestAsync throws on timeout. Specific file path not provided in finding.`n"
    $violations1565 = Get-DraftViolations $badAll1565 $eitherFinding1565 $tplTest1565 $packages1565 $manifestDir1565
    Test-RemediationChecksCase '#1565 Get-DraftViolations: one pass finds every class together' {
        $violations1565.Any -and $violations1565.MissingPackages.Count -eq 4 -and $violations1565.Figures.Count -eq 1 -and $violations1565.ThrowLines.Count -eq 1 -and $violations1565.MetaLines.Count -eq 1
    }
    Test-RemediationChecksCase '#1565 Get-DraftViolationMarks: names each surviving class (PACKAGES MISSING, FIGURES NOT IN FINDING, SEMANTICS, META-TEXT LEFT)' {
        $marks = (@(Get-DraftViolationMarks $violations1565)) -join ' | '
        $marks -match 'PACKAGES MISSING: Encina\.AzureServiceBus, Encina\.Kafka, Encina\.MQTT, Encina\.RabbitMQ' -and $marks -match 'FIGURES NOT IN FINDING: 0%' -and
            $marks -match 'SEMANTICS: throws vs Either' -and $marks -match 'META-TEXT LEFT'
    }
    Test-RemediationChecksCase '#1565 Format-DraftViolationNote: ONE note names every violation for the single combined re-ask' {
        $note = Format-DraftViolationNote $violations1565
        $note -match 'Encina\.Kafka' -and $note -match '0%' -and $note -match 'throws on timeout' -and $note -match 'not provided in finding'
    }
    Test-RemediationChecksCase '#1565 Get-DraftViolations (passing case): a clean draft has no violation, no marks and an empty note' {
        $cleanDraft = $natsOnlyDraft1565 + "`nEncina.RabbitMQ, Encina.MQTT, Encina.AzureServiceBus and Encina.Kafka share the 71.1% unit coverage; a timeout returns Left.`n"
        $clean = Get-DraftViolations $cleanDraft $eitherFinding1565 $tplTest1565 $packages1565 $manifestDir1565
        $cleanNoFig = Get-DraftViolations ($cleanDraft -replace '71\.1%', 'low') $eitherFinding1565 $tplTest1565 $packages1565 $manifestDir1565
        -not $cleanNoFig.Any -and @(Get-DraftViolationMarks $cleanNoFig).Count -eq 0 -and (Format-DraftViolationNote $cleanNoFig) -eq '' -and $clean.Figures.Count -eq 1
    }

    # wiring: audit-draft-remediation.ps1 applies the checks inside Repair-Draft, re-asks once with every violation,
    # marks the finding's line and exits 1; no real run happens here (audit #18 is open: #1540).
    $draftScript1565 = Get-Content (Join-Path $repo 'tools\ai\audit\audit-draft-remediation.ps1') -Raw
    Test-RemediationChecksCase '#1565 audit-draft-remediation.ps1 applies Set-PackageLine/Set-TestCategory in Repair-Draft and makes ONE combined re-ask' {
        $draftScript1565 -match 'Set-PackageLine \$repaired \$Packages \$RouteTemplateFile' -and $draftScript1565 -match 'Set-TestCategory \$repaired \$FindingText' -and
            ([regex]::Matches($draftScript1565, 'Format-DraftViolationNote')).Count -eq 1 -and $draftScript1565 -match 'reask-checks'
    }
    Test-RemediationChecksCase '#1565 audit-draft-remediation.ps1 marks surviving violations and fails the run like PLACEHOLDERS LEFT' {
        $draftScript1565 -match 'Get-DraftViolationMarks' -and $draftScript1565 -match '\$checkFailures\.Count -gt 0\) \{ exit 1 \}'
    }
    Test-RemediationChecksCase '#1565 the possibly-related note the script appends to a draft is itself free of meta-text' {
        $noteMatch = [regex]::Match($draftScript1565, '"- #\$duplicateOf - possibly related \([^"]*\)"')
        $noteMatch.Success -and (@(Get-MetaTextLines $noteMatch.Value)).Count -eq 0
    }
    # ---- end #1565 block ----

    # ---- #1393: tools/ai/audit/_remediation-checks.ps1 -- a candidate counts as the same defect only when it is
    # ABOUT the finding's location and symbol (its title and location sections: Location, Current/Actual
    # Behavior, Code Sample, ...), not when it merely MENTIONS them in its Description, Root Cause, Proposed Fix,
    # Additional Context or Related Issues; folders, line references and AGENTS.md/CLAUDE.md house-rule tokens
    # are never symbol evidence. Audit #16 verification pass 5 found four false duplicates once #1424 ran the
    # evidence check over every candidate. Real fixtures captured once (fixtures/1393/: the findings as
    # Split-Findings extracts them from the audit #16 stage files, and `gh issue view --json title,body` of
    # #1393, #1299 and #592); #1343 and #1170 reuse the #1400/#1388 fixtures, unchanged on GitHub since.
    $fixtures1393 = Join-Path $repo '.claude\hooks\tests\fixtures\1393'
    function Get-Fixture1393Candidate([string]$Path) {
        $issue = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
        return "$($issue.title)`n$($issue.body)"
    }
    $findingCode2For1393 = Get-Content -LiteralPath (Join-Path $fixtures1393 'finding-code-2.md') -Raw
    $findingCode4For1393 = Get-Content -LiteralPath (Join-Path $fixtures1393 'finding-code-4.md') -Raw
    $findingDocs1For1393 = Get-Content -LiteralPath (Join-Path $fixtures1393 'finding-docs-1.md') -Raw
    $findingDocs2For1393 = Get-Content -LiteralPath (Join-Path $fixtures1393 'finding-docs-2.md') -Raw
    $findingDocs4For1393 = Get-Content -LiteralPath (Join-Path $fixtures1393 'finding-docs-4.md') -Raw
    $candidate1393 = Get-Fixture1393Candidate (Join-Path $fixtures1393 'issue-1393.json')
    $candidate1299 = Get-Fixture1393Candidate (Join-Path $fixtures1393 'issue-1299.json')
    $candidate592 = Get-Fixture1393Candidate (Join-Path $fixtures1393 'issue-592.json')
    $candidate1343For1393 = Get-Fixture1393Candidate (Join-Path $repo '.claude\hooks\tests\fixtures\1400\issue-1343.json')
    $candidate1170For1393 = Get-Fixture1393Candidate (Join-Path $repo '.claude\hooks\tests\fixtures\1388\issue-1170.json')

    # (a) the four false duplicates of verification pass 5, and the one true duplicate that must keep passing.
    Test-RemediationChecksCase '#1393 16-code-2 vs #1393 is NOT a duplicate (#1393 cites InstrumentedSagaStore.cs and EncinaError.Message only as an example)' {
        -not (Test-DuplicateEvidence $findingCode2For1393 $candidate1393)
    }
    Test-RemediationChecksCase '#1393 16-docs-1 vs #1343 is NOT a duplicate (only a generic `src/` token and a "Sagas/" directory segment matched)' {
        -not (Test-DuplicateEvidence $findingDocs1For1393 $candidate1343For1393)
    }
    Test-RemediationChecksCase '#1393 16-docs-4 vs #1299 is NOT a duplicate (only a generic `src/` token and a bare README.md in prose matched)' {
        -not (Test-DuplicateEvidence $findingDocs4For1393 $candidate1299)
    }
    Test-RemediationChecksCase '#1393 16-docs-2 vs #592 is partially related, NOT a duplicate (#592 covers IChoreographyStateStore only)' {
        (-not (Test-DuplicateEvidence $findingDocs2For1393 $candidate592)) -and (Test-PartialDuplicateEvidence $findingDocs2For1393 $candidate592)
    }
    Test-RemediationChecksCase '#1393 16-code-4 vs #1170 is still a duplicate (the one true duplicate)' {
        Test-DuplicateEvidence $findingCode4For1393 $candidate1170For1393
    }

    # (b) #1393's own synthetic case: a finding and a candidate that share only a coincidental file citation
    # (in the candidate's own Location) and a verbatim house-rule quote (`EncinaError.Message`) are never the
    # same defect; the same pair that also shares a defect-specific symbol still is, so the rule is not simply
    # rejecting everything.
    $syntheticFinding1393 = '`src/Encina.Messaging/Shared/SharedStore.cs:40`: `EncinaError.Message` is written to the log when the retry budget runs out. Fails AGENTS.md: "`EncinaError.Message` NEVER reaches logs".'
    $syntheticCandidate1393 = "[BUG] SharedStore leaks the error text into the activity status`n## Description`n`nAnother leak of the same house rule.`n`n## Location`n`n- **File(s)**: ``src/Encina.Messaging/Shared/SharedStore.cs```n`n## Current Behavior`n`n``EncinaError.Message`` reaches the activity status in ``UpdateAsync``.`n"
    Test-RemediationChecksCase '#1393 synthetic: shared file + shared house-rule quote only is NOT a duplicate' {
        -not (Test-DuplicateEvidence $syntheticFinding1393 $syntheticCandidate1393)
    }
    $syntheticFindingSpecific1393 = '`src/Encina.Messaging/Shared/SharedStore.cs:40`: `UpdateAsync` writes `EncinaError.Message` into the activity status.'
    Test-RemediationChecksCase '#1393 synthetic control: shared file + shared specific symbol (UpdateAsync) in the candidate''s Current Behavior IS a duplicate' {
        Test-DuplicateEvidence $syntheticFindingSpecific1393 $syntheticCandidate1393
    }
    $syntheticCandidateMentionOnly1393 = "[DEBT] Unrelated tooling issue`n## Description`n`nFor example ``src/Encina.Messaging/Shared/SharedStore.cs`` calls ``UpdateAsync``.`n`n## Location`n`n- **File(s)**: ``tools/ai/audit/_remediation-checks.ps1```n"
    Test-RemediationChecksCase '#1393 synthetic: the same file and symbol named only in the candidate''s Description are NOT duplicate evidence' {
        -not (Test-DuplicateEvidence $syntheticFindingSpecific1393 $syntheticCandidateMentionOnly1393)
    }

    # (c) Find-DuplicateAmongCandidates over all five real candidates for all five real findings: only
    # 16-code-4 resolves to a duplicate (#1170); the other four are drafted as new.
    $candidates1393 = @(
        [pscustomobject]@{ Number = '592'; TitleAndBody = $candidate592 }
        [pscustomobject]@{ Number = '1170'; TitleAndBody = $candidate1170For1393 }
        [pscustomobject]@{ Number = '1299'; TitleAndBody = $candidate1299 }
        [pscustomobject]@{ Number = '1343'; TitleAndBody = $candidate1343For1393 }
        [pscustomobject]@{ Number = '1393'; TitleAndBody = $candidate1393 }
    )
    Test-RemediationChecksCase '#1393 Find-DuplicateAmongCandidates: over all five real candidates, only 16-code-4 is a duplicate (#1170)' {
        (Find-DuplicateAmongCandidates $findingCode4For1393 $candidates1393) -eq '1170' -and
        $null -eq (Find-DuplicateAmongCandidates $findingCode2For1393 $candidates1393) -and
        $null -eq (Find-DuplicateAmongCandidates $findingDocs1For1393 $candidates1393) -and
        $null -eq (Find-DuplicateAmongCandidates $findingDocs2For1393 $candidates1393) -and
        $null -eq (Find-DuplicateAmongCandidates $findingDocs4For1393 $candidates1393)
    }

    # (d) the building blocks: the location text of #1393 keeps its Location and Current Behavior but not the
    # Description that names InstrumentedSagaStore.cs; the house-rule token set is read from AGENTS.md.
    $location1393 = Get-CandidateLocationText $candidate1393
    Test-RemediationChecksCase '#1393 Get-CandidateLocationText: keeps the title, Location and Current Behavior of #1393 and drops its Description' {
        $location1393.Contains('[DEBT] Test-DuplicateEvidence still treats') -and
        $location1393.Contains('tools/ai/audit/_remediation-checks.ps1') -and
        $location1393.Contains('extracts every backticked token as a symbol anchor') -and
        -not $location1393.Contains('InstrumentedSagaStore')
    }
    Test-RemediationChecksCase '#1393 house-rule tokens: read from AGENTS.md (EncinaError.Message and TimeProvider are excluded as symbol evidence)' {
        $anchors = Get-FindingAnchors '`src/Encina/Foo.cs`: `EncinaError.Message`, `TimeProvider`, `src/`, `:12-14` and `SpecificSymbol`.'
        (@($anchors.SymbolAnchors) -join ',') -eq 'SpecificSymbol'
    }

    # (e) adversarial review of #1393: the same store file name exists once per provider, so a bare file name
    # or a brace pattern naming only ANOTHER provider is not the finding's file; a bold field under an excluded
    # section (Root Cause, Additional Context) is a mention; emphasis around a heading name is ignored.
    $sqlServerOutboxFinding1393 = '`src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs:80`: `GetAsync` swallows a Left result and reports success.'
    $mySqlBareNameCandidate1393 = "[BUG] MySQL outbox retry counter never increments`n## Location`n`n- **File(s)**: ``OutboxStoreADO.cs`` (MySQL provider)`n`n## Current Behavior`n`n``GetAsync`` never increments ``RetryCount`` in the MySQL store.`n"
    Test-RemediationChecksCase '#1393 a bare store file name (the same name exists in every provider) is NOT the finding''s file' {
        -not (Test-DuplicateEvidence $sqlServerOutboxFinding1393 $mySqlBareNameCandidate1393)
    }
    $mySqlBraceCandidate1393 = "[BUG] MySQL stores swallow errors`n## Location`n`n- **File(s)**: ``src/Encina.ADO.MySQL/{Outbox/OutboxStoreADO,Inbox/InboxStoreADO}.cs```n`n## Current Behavior`n`n``GetAsync`` reports success on a Left.`n"
    Test-RemediationChecksCase '#1393 a brace pattern that expands only to another provider''s file is NOT the finding''s file' {
        -not (Test-DuplicateEvidence $sqlServerOutboxFinding1393 $mySqlBraceCandidate1393)
    }
    $allProvidersBraceCandidate1393 = "[BUG] ADO stores swallow errors`n## Location`n`n- **File(s)**: ``src/Encina.ADO.{SqlServer,PostgreSQL,MySQL}/Outbox/OutboxStoreADO.cs```n`n## Current Behavior`n`n``GetAsync`` reports success on a Left.`n"
    Test-RemediationChecksCase '#1393 control: a brace pattern that expands to the finding''s own path, plus a shared symbol, IS a duplicate' {
        Test-DuplicateEvidence $sqlServerOutboxFinding1393 $allProvidersBraceCandidate1393
    }
    foreach ($excludedSection in 'Root Cause', 'Additional Context') {
        $boldUnderExcluded = "[BUG] Unrelated`n## $excludedSection`n`n- **File(s)**: ``src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs```n`n## Current Behavior`n`n``GetAsync`` reports success on a Left.`n"
        Test-RemediationChecksCase "#1393 a **File(s)** bold field under '## $excludedSection' is NOT location evidence" {
            -not (Test-DuplicateEvidence $sqlServerOutboxFinding1393 $boldUnderExcluded)
        }
    }
    $boldBeforeHeadings1393 = "[BUG] ADO outbox store swallows errors`n**Location**: ``src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs```n`n``GetAsync`` reports success on a Left.`n`n## Additional Context`n`nNone.`n"
    Test-RemediationChecksCase '#1393 a bold **Location** field before the first heading IS location evidence (issue written without the template headers)' {
        (Get-CandidateLocationText $boldBeforeHeadings1393).Contains('src/Encina.ADO.SqlServer/Outbox/OutboxStoreADO.cs')
    }
    Test-RemediationChecksCase '#1393 an emphasized heading (## **Location**) is still the Location section' {
        (Get-CandidateLocationText "Title`n## **Location**`n`n- File: ``src/Encina.Foo/Bar/Widget.cs```n").Contains('src/Encina.Foo/Bar/Widget.cs')
    }
    # ---- end #1393 block ----

    # ================================================================================================
    # #1368/#1380: the Scripts write-API/reference heuristic (_write-targets.ps1: Test-ScriptHasWriteApi /
    # Test-ScriptReferencesPath) must not block the pipeline's own sanctioned scripts (Test-ScriptIsSanctioned)
    # even though they legitimately mention src/ or tests/ while writing only under artifacts/; a script that
    # merely LOOKS like a sanctioned one, placed outside the repository or outside the allowlist, still goes
    # through the heuristic (#1368). Get-FlatOutput (added above) makes a captured-output "-match" insensitive
    # to console width (#1380).
    # ================================================================================================

    # (a)/(b): the orchestrator's own sanctioned commands, against the REAL scripts of this checkout (no
    # execution happens here — guard-orchestrator-writes.ps1 only reads the named script's text).
    $realTestHooksPath = Join-Path $repo '.claude\hooks\tests\Test-Hooks.ps1'
    $realRemediationPath = Join-Path $repo 'tools\ai\audit\audit-draft-remediation.ps1'
    Invoke-HookCase $orchestrator (@{ tool_name = 'PowerShell'; cwd = $repo; tool_input = @{ command = "pwsh -NoProfile -File '$realTestHooksPath'" } } | ConvertTo-Json -Compress) 0 'orchestrator: pwsh -File of the hook test suite itself is allowed (#1368)'
    Invoke-HookCase $orchestrator (@{ tool_name = 'PowerShell'; cwd = $repo; tool_input = @{ command = "pwsh -NoProfile -File '$realRemediationPath'" } } | ConvertTo-Json -Compress) 0 'orchestrator: pwsh -File of audit-draft-remediation.ps1 is allowed (#1380)'

    # (c): an issue-worker's own sanctioned command, from its worktree. block-main-checkout-writes.ps1 only
    # applies the Scripts write-API/reference check when the launching statement's Base resolves to the main
    # checkout, so a fixture at $wt (a worktree under the fake $main) is needed to exercise it: the worker's raw
    # tool-call cwd resets to the main checkout between calls (see the worker protocol), so "pwsh -File
    # <worktree absolute path>\...\Test-Hooks.ps1" with no prior Set-Location is the exact false-positive shape;
    # "Set-Location <worktree>; pwsh -File ..." already worked before this fix (Base tracks the Set-Location).
    $workerFixturePath = Join-Path $wt '.claude\hooks\tests\Test-Hooks.ps1'
    New-Item -ItemType Directory -Force (Split-Path -Parent $workerFixturePath) | Out-Null
    Set-Content -LiteralPath $workerFixturePath -Value "# Mentions src/Foo.cs and tests/Bar.cs in guidance text.`nSet-Content 'x.log' 'y'`n"
    $env:CLAUDE_PROJECT_DIR = $main
    Invoke-HookCase $mainCheckout (@{ tool_name = 'PowerShell'; cwd = $main; tool_input = @{ command = "Set-Location '$wt'; pwsh -NoProfile -File '$workerFixturePath'" } } | ConvertTo-Json -Compress) 0 'issue-worker: Set-Location into the worktree then pwsh -File of Test-Hooks.ps1 is allowed'
    Invoke-HookCase $mainCheckout (@{ tool_name = 'PowerShell'; cwd = $main; tool_input = @{ command = "pwsh -NoProfile -File '$workerFixturePath'" } } | ConvertTo-Json -Compress) 0 'issue-worker: pwsh -File of Test-Hooks.ps1 with no Set-Location (raw cwd is the main checkout) is allowed (#1368)'
    $env:CLAUDE_PROJECT_DIR = $repo

    # (d): a script under <repo>\artifacts\ that is NOT one of the sanctioned paths, but does write files and
    # reference src/, must still be blocked — the exemption is a narrow allowlist, not a blanket pass for
    # anything under the orchestrator's own tooling folders (regression guard for the #1181 rule).
    $nonSanctionedFixturePath = Join-Path $repo 'artifacts\x-1368-fixture.ps1'
    New-Item -ItemType Directory -Force (Split-Path -Parent $nonSanctionedFixturePath) | Out-Null
    Set-Content -LiteralPath $nonSanctionedFixturePath -Value "# Mentions src/Foo.cs in guidance text.`nSet-Content 'x.log' 'y'`n"
    Invoke-HookCase $orchestrator (@{ tool_name = 'PowerShell'; cwd = $repo; tool_input = @{ command = "pwsh -NoProfile -File '$nonSanctionedFixturePath'" } } | ConvertTo-Json -Compress) 2 'orchestrator: a script under artifacts/ that writes and references src/ is still blocked (#1368 regression guard)'
    Remove-Item -Force $nonSanctionedFixturePath -ErrorAction SilentlyContinue

    # (e): a script whose name and relative shape match a sanctioned pattern, but which lives outside the
    # repository entirely (a temp fixture root, not a worktree), still goes through the heuristic: the allowlist
    # is deliberately a repository-relative path check, not a name match (decision 5).
    $outsideLookalikePath = Join-Path $work 'tools\ai\audit\lookalike.ps1'
    New-Item -ItemType Directory -Force (Split-Path -Parent $outsideLookalikePath) | Out-Null
    Set-Content -LiteralPath $outsideLookalikePath -Value "# Mentions tests/Bar.cs in guidance text.`nSet-Content 'x.log' 'y'`n"
    Invoke-HookCase $orchestrator (@{ tool_name = 'PowerShell'; cwd = $repo; tool_input = @{ command = "pwsh -NoProfile -File '$outsideLookalikePath'" } } | ConvertTo-Json -Compress) 2 'orchestrator: a script outside the repository is not exempt just because its path looks sanctioned (#1368 decision 5)'

    # (f): Get-FlatOutput reconstructs a phrase that a narrow console wrapped across two output lines, so the
    # "-match" cases above (and the #1374/#1375 blocks) do not depend on the host's console width (#1380).
    $script:total++
    $wrappedLines = @("some prefix text ## Findings'", "header some suffix text")
    if ((Get-FlatOutput $wrappedLines) -match "## Findings' header") {
        "PASS Get-FlatOutput: a phrase wrapped across two console lines still matches after joining and collapsing whitespace (#1380)"
    }
    else {
        $script:failed++
        "FAIL Get-FlatOutput: expected the wrapped phrase to match after flattening, got '$(Get-FlatOutput $wrappedLines)'"
    }
    # ---- end #1368/#1380 block ----

    # Agent frontmatter and settings.json wiring: structure, models, and hook scripts that exist.
    function Test-Wiring([string]$Label, [string[]]$Problems) {
        $script:total++
        if ($Problems.Count -gt 0) { $script:failed++; "FAIL $Label`: $($Problems -join '; ')" } else { "PASS $Label" }
    }
    foreach ($file in (Get-ChildItem (Join-Path $repo '.claude\agents') -Filter *.md | Where-Object Name -ne 'README.md')) {
        $lines = Get-Content $file.FullName
        $problems = [System.Collections.Generic.List[string]]::new()
        $end = if ($lines[0] -eq '---') { [Array]::IndexOf($lines, '---', 1) } else { -1 }
        if ($end -lt 1) { Test-Wiring "frontmatter of $($file.Name)" @('no --- block'); continue }
        $front = $lines[1..($end - 1)]
        if ($front -match "`t") { $problems.Add('tab in frontmatter') }
        $keys = @{}
        foreach ($l in $front) { if ($l -match '^(?<k>[A-Za-z]+):\s*(?<v>.*)$') { $keys[$Matches.k] = $Matches.v } elseif ($l -notmatch '^\s+\S|^\s*$') { $problems.Add("unexpected line '$l'") } }
        if ($keys.name -ne $file.BaseName) { $problems.Add("name '$($keys.name)' differs from the file name") }
        if ($keys.model -notin 'haiku', 'sonnet', 'opus', 'inherit') { $problems.Add("model '$($keys.model)'") }
        foreach ($l in ($front -match 'command:')) {
            $m = [regex]::Match($l, '\.claude/hooks/(?<h>[\w-]+\.ps1)"(?:\s+-Agent\s+(?<a>[\w-]+))?')
            if (-not $m.Success) { $problems.Add("hook command not recognised: $l"); continue }
            if (-not (Test-Path (Join-Path $hooks $m.Groups['h'].Value))) { $problems.Add("missing hook $($m.Groups['h'].Value)") }
            if ($m.Groups['a'].Success -and $m.Groups['a'].Value -ne $file.BaseName) { $problems.Add("-Agent $($m.Groups['a'].Value) in $($file.Name)") }
        }
        if ($file.BaseName -in 'issue-worker', 'mechanical-fixer', 'docs-writer', 'docs-reviewer', 'site-steward', 'pr-reviewer' -and -not ($front -match 'block-worker-publish\.ps1')) { $problems.Add('block-worker-publish is not wired') }
        Test-Wiring "frontmatter of $($file.Name)" $problems
    }
    $settingsProblems = [System.Collections.Generic.List[string]]::new()
    try {
        $settings = Get-Content (Join-Path $repo '.claude\settings.json') -Raw | ConvertFrom-Json
        $commands = @($settings.hooks.PreToolUse | ForEach-Object { $_.hooks } | ForEach-Object { $_.command })
        foreach ($c in $commands) {
            $m = [regex]::Match($c, '\.claude/hooks/(?<h>[\w-]+\.ps1)')
            if (-not $m.Success -or -not (Test-Path (Join-Path $hooks $m.Groups['h'].Value))) { $settingsProblems.Add("missing hook in '$c'") }
        }
        $agentHook = @($settings.hooks.PreToolUse | Where-Object { 'Agent' -match "^($($_.matcher))$" } | ForEach-Object { $_.hooks.command } | Where-Object { $_ -match 'block-worker-spawn\.ps1" -Agent orchestrator' })
        if ($agentHook.Count -eq 0) { $settingsProblems.Add('no Agent hook with block-worker-spawn -Agent orchestrator') }
    }
    catch { $settingsProblems.Add("settings.json is not valid JSON: $($_.Exception.Message)") }
    Test-Wiring 'settings.json wiring' $settingsProblems

    # A2: every extension the source-file rule covers is named in issue-worker.md.
    $undocumented = @($hookExtensions | Where-Object { $_ -and $workerDefinition -notmatch "\.$([regex]::Escape($_))\b" })
    $script:total++
    if ($undocumented.Count -gt 0) { $script:failed++; "FAIL issue-worker.md does not name these source extensions of block-main-checkout-writes.ps1: $($undocumented -join ', ')" }
    else { "PASS issue-worker.md names every source extension of block-main-checkout-writes.ps1 ($($hookExtensions.Count))" }
}
finally {
    Pop-Location
    Remove-Item -Recurse -Force $work
}

"{0} cases, {1} failed" -f $script:total, $script:failed
exit ([int]($script:failed -gt 0))
