---
nav_exclude: true
---

# GitHub API quota: GraphQL versus REST

**Status:** reference note for the maintainer and for contributors of `tools/ai`. **Issue:** #2568.

The maintainer account has one GraphQL budget of 5000 points per hour, shared by every Claude session, watcher and script. On 2026-10-21 it reached `remaining 0` while REST still had 4995 of 5000, and every `gh pr`, `gh issue` and `gh label` call failed. This page lists what spends GraphQL, what the repository tools do about it, and how to check.

## What uses which API

Rule: an operation with a REST equivalent uses REST (`gh api repos/...`), always.

| API | Operations |
| --- | --- |
| REST | List and read issues, pull requests, labels, checks (check runs and commit status), comments, reviews, milestones and sub-issues; create issues and pull requests (possible over REST; `open-remediation` still calls `gh issue create`, see "What is left") |
| GraphQL-backed `gh` commands (avoid) | `gh pr view`, `gh pr checks`, `gh pr list`, `gh issue view`, `gh issue list`, `gh label list`, `gh project ...` |
| GraphQL only | Review threads (`isResolved`) and `resolveReviewThread`, `enablePullRequestAutoMerge`, Projects v2, the bulk query of `collect-issues` |

The GraphQL-only operations go through the shared helper below.

## The helper

`tools/ai/gh-quota.ps1` is dot-sourceable (`. (Join-Path $PSScriptRoot 'gh-quota.ps1')`).

| Function | Behavior |
| --- | --- |
| `Get-GhGraphQLQuota` | Reads `gh api graphql -f query='{ rateLimit { remaining resetAt } }'`, never `gh api rate_limit` (its graphql numbers were stale on 2026-10-21). Cached 60 s in `artifacts/gh-quota/graphql-ratelimit.json`. An unreadable quota is unknown (`$null`) and treated as "proceed" |
| `Wait-GhGraphQLQuota` | `-Caller`, `-MinRemaining` (default 200), `-NoWait`. Re-checks the quota every 60 s until the reading is at or above `-MinRemaining`; with `-NoWait` it returns `$false` instead of waiting |
| `Test-GhGraphQLLow` | `$true` when the known remaining is below `-MinRemaining` (default 200) |
| `Write-GhQuotaUsage` | Appends `timestampUtc,caller,operation,api,count` to `artifacts/gh-quota/usage.csv` |
| `Get-GhRepoLabelNames` | Label names over REST, never cached |
| `Get-GhCheckBuckets` | Check runs and commit status of a head sha over REST, as `Name`, `Bucket` (`pass`, `fail`, `pending`, `skipping`), `Url` |

Thresholds: 200 by default, 300 for the review-thread read of `watch-open-prs.ps1`. A quota error is never retried sooner than 60 s (`Invoke-GhWithRetry` in `tools/ai/audit/_remediation-checks.ps1`).

To read the quota by hand:

```powershell
gh api graphql -f query='{ rateLimit { cost remaining resetAt } }'
```

## Cost per call

Measured on 2026-10-22 against this repository: raw queries report their own `rateLimit { cost }`; `gh` subcommands were measured as the drop in `remaining` across the call. Reading `rateLimit` alone costs 0 points.

| Call | Measured cost (points) |
| --- | --- |
| `gh pr view --json statusCheckRollup` (or the same query raw) | 1 |
| `gh pr checks` | 1 |
| `gh pr view --json state,mergeCommit` | 1 |
| `reviewThreads(first:100)` with `comments(first:1)` | 1 |
| `gh label list --limit 400` | 1 per page of 100 labels; 3 for the 243 labels of today |
| `gh issue view --json title,body` | 1 |
| `gh issue list --search x --limit 8` | 1 |
| One `collect-issues` page (100 issues, `labels(first:30)`) | 1 |

Every call costs about 1 point: the quota runs out through the **number** of calls, not through expensive queries. Polling is what matters.

The `remaining` value is not consistent between consecutive reads: on 2026-10-22 back-to-back reads alternated between about 3480 and about 4130. A single drop measurement is therefore noisy; the costs above were confirmed with the query's own `cost` field.

## Hourly cost and intervals

From the measured cost of 1 point per call.

| Caller | Interval | Before | After | REST per hour (after) |
| --- | --- | --- | --- | --- |
| `watch-open-prs` (6 open PRs) | 60 s | 2 per PR per poll (check rollup and threads), 720 per hour per running watcher; several sessions each ran one | checks over REST (0); threads only when a PR's `updated_at` changed and `remaining >= 300` (about 4 changes per PR per hour: about 24 per hour) plus 6 on the first poll; nothing else changes | 60 per hour for the pulls list, plus 2 per PR per poll only while checks are pending or after a push |
| `watch-pr-events` (per watched PR) | 60 s | 2 per poll, 160 per hour | 0 (REST); nothing else changes | about 240 per watched PR when checks are settled (4 calls per poll), up to 360 while checks are pending (6 per poll) |
| `pr-watcher` agent (per watched PR) | 60 s | 2 per poll, 120 per hour | 0 (REST); nothing else changes | about 180 per watched PR when checks are settled (3 calls per poll), about 300 while checks are pending (5 per poll) |
| `check-target-issue-template` hook | one read per worker spawn | 1 per spawn; at 0 quota it blocked every spawn (fails closed) | 0 (REST) | per run |
| Label listings (`audit-draft-remediation -Prepare`, `open-remediation`, `apply-priority-labels`) | per run | 3 per run; failed at 0 | 0 (REST) | per run |
| `audit-draft-remediation` duplicate search and issue reads | per run | 1 per search or read | 0 (REST; the search API has its own limit of 30 requests per minute) | per run |
| `score-issues -Issue` | per run | 1 | 0 (REST) | per run |
| `collect-issues` | per run | 1 per page | 1 per page, behind `Wait-GhGraphQLQuota` and `Write-GhQuotaUsage` | 0 |

The watchers read check runs and commit status only when the head sha changed or the previous read had pending or failed checks (a failed job can be re-run on the same sha); reviews and the comment lists are read every poll (the comment lists with `since`; reviews are filtered by `submitted_at`). Limit: once checks on a head sha are settled they are not read again, so a re-run of a failed job on the same sha is not announced until the sha changes or the PR state changes; read the run list by hand after a re-run. REST has its own budget of 5000 requests per hour, shared like GraphQL, so several watchers on the same PR should not run at once.

The only GraphQL polling left is the review-thread read of `watch-open-prs.ps1`. It runs when the PR's `updated_at` changed and `remaining >= 300`; when skipped it prints one `WARN` line per reset window.

## What is left

The scripts not yet migrated are `fetch-closed-issue-data`, `historian-*`, `build-board-stats`, `reconcile-board`, `check-project-membership`, `open-remediation` beyond labels, `qwen-predraft`, `classify-scope` and `audit-next`. They are tracked in a follow-up issue.

## How to prove it

```powershell
pwsh -NoProfile -File tools/ai/gh-quota-selftest.ps1
pwsh -NoProfile -File tools/ai/gh-graphql-down-selftest.ps1
```

See also: [AI development model](AI-DEVELOPMENT-MODEL.md).
