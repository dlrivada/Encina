# tools/ai/audit/qwen-predraft.ps1 [-Issue <n> | -QueueFile <path>] [-Force] (#1345; moved from the
# unversioned artifacts/knowledge/)
#
# Pre-drafts the KNOWLEDGE part of SPEC-003 records with the free local model, ahead of the per-issue
# auditors. For each closed issue: fetch body + comments + linked PR/commit titles, ask Qwen for a draft
# record, save it. -Issue drafts a single issue (audit-next.ps1 calls it this way, so the pre-draft exists
# before the archivist stage starts); with no -Issue, drafts every entry of -QueueFile (default
# artifacts/knowledge/predraft-queue.txt) not already drafted.

param([int]$Issue, [string]$QueueFile, [switch]$Force)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot '_audit-lib.ps1')

$root = Get-MainRoot $PSScriptRoot

if ($Issue) {
    $Issues = @($Issue)
}
else {
    if ([string]::IsNullOrWhiteSpace($QueueFile)) { $QueueFile = Join-Path $root 'artifacts\knowledge\predraft-queue.txt' }
    $Issues = (Get-Content $QueueFile -Raw) -split '[,\s]+' | Where-Object { $_ } | ForEach-Object { [int]$_ }
}

$out = Join-Path $root 'artifacts\knowledge\predraft'
$raw = Join-Path $out 'raw'
New-Item -ItemType Directory -Force $out, $raw | Out-Null
$brief = Join-Path $out '_brief.md'
Set-Content $brief -Encoding utf8 -Value @'
You pre-draft the KNOWLEDGE part of a record for one closed GitHub issue of the Encina .NET library (SPEC-003). Use ONLY the input (issue body, comments, linked PRs/commits). Never invent facts; if something is not in the input, write "unknown". Output Markdown only:

---
issue: <number>
title: "<title>"
type: <feature|bug|debt|test|infra|spike|epic|refactor|docs|other>
outcome: <delivered|partial|rejected-reasoned|rejected-unexplained|superseded|duplicate|moved|no-evidence>
closed_at: <date or unknown>
linked_prs: [<numbers>]
packages: [<package names mentioned>]
---

## Decisions
- <what was decided, with source: issue body / comment by X / PR #n>
## Rejected alternatives
- <...> (or "none")
## Rules and lessons
- <durable rules or lessons, with source>
## Candidate destinations
- <adr | claude-md | regression-test | reviewer-checklist | docs | readme | roadmap | benchmark | coverage-manifest | spec-invariant | none>: <why>
## Code touched (from the input)
- <files or areas named in the PRs/commits, or "unknown">
## Open questions for the auditor
- <what the auditor must verify in the code>
'@
foreach ($n in $Issues) {
    $dst = Join-Path $out "$n.md"
    if ((Test-Path $dst) -and -not $Force) { continue }
    $in = Join-Path $raw "$n.txt"
    $v = gh issue view $n --repo dlrivada/Encina --json number,title,state,closedAt,labels,body,comments 2>$null | ConvertFrom-Json
    if (-not $v -or $v.state -ne 'CLOSED') { continue }
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine("ISSUE #$($v.number): $($v.title)`nclosed_at: $($v.closedAt)`nlabels: $(($v.labels | ForEach-Object name) -join ', ')`n`nBODY:`n$($v.body)`n")
    foreach ($c in $v.comments) { if ($c.author.login -notmatch 'bot|coderabbit') { $b = $c.body; if ($b.Length -gt 3000) { $b = $b.Substring(0, 3000) + ' [...]' }; [void]$sb.AppendLine("COMMENT by $($c.author.login):`n$b`n") } }
    $tl = gh api "repos/dlrivada/Encina/issues/$n/timeline" --paginate 2>$null | ConvertFrom-Json
    foreach ($e in @($tl)) {
        if ($e.event -eq 'cross-referenced' -and $e.source.issue.pull_request) { [void]$sb.AppendLine("LINKED PR #$($e.source.issue.number): $($e.source.issue.title) (state $($e.source.issue.state))") }
        if ($e.event -in 'referenced', 'closed' -and $e.commit_id) { $msg = git -C $root log -1 --format=%s $e.commit_id 2>$null; [void]$sb.AppendLine("COMMIT $($e.commit_id.Substring(0, 8)) ($($e.event)): $msg") }
    }
    $text = $sb.ToString(); if ($text.Length -gt 60000) { $text = $text.Substring(0, 60000) + "`n[truncated]" }
    Set-Content $in $text -Encoding utf8
    Push-Location $root
    try {
        dotnet run (Join-Path $root 'tools\ai\local-ai-ask.cs') -- --task "predraft-$n" --brief $brief --input $in --out $dst | Out-Null
    }
    finally {
        Pop-Location
    }
    "predrafted #$n"
}
