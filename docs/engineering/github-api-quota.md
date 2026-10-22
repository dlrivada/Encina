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
| `Wait-GhGraphQLQuota` | `-Caller`, `-MinRemaining` (default 200), `-NoWait`. Waits until `resetAt` plus 5 s, re-checking at most once per 60 s; with `-NoWait` it returns `$false` instead of waiting |
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

These numbers are **calculated** from GitHub's documented GraphQL cost formula (points = connection requests / 100, rounded, minimum 1; a `first:N` under a parent counts N times), **not measured**. Measure with the command above to confirm.

| Call | Calculated cost (points) |
| --- | --- |
| `gh pr view --json statusCheckRollup` | about 1 |
| `gh pr checks` | about 1 |
| `gh pr view --json state,mergeCommit` | about 1 |
| `reviewThreads(first:100)` with `comments(first:1)` (202 requests) | 2 |
| `gh label list --limit 400` (4 pages) | about 4 to 8 |
| `gh issue view --json title,body` | 1 |
| `gh issue list --search x --limit 8` | about 1 |
| One `collect-issues` page (100 issues, `labels(first:30)`; 3101 requests) | about 31 (three pages, about 93, if the open backlog is about 300 issues) |

## Hourly cost and intervals

Same caveat: estimates, not measurements.

| Caller | Interval | Before | After |
| --- | --- | --- | --- |
| `watch-open-prs` (6 open PRs) | 60 s | 3 per PR per poll, about 1080 per hour | checks over REST (0); threads only when a PR's `updated_at` changed (assuming 4 changes per hour per PR, about 48 per hour) plus about 12 on the first poll |
| `watch-pr-events` (per watched PR) | 45 s | about 2 per poll, about 160 per hour | 0 (REST) |
| `pr-watcher` agent | 60 s | about 2 per poll, about 120 per hour | 0 (REST) |
| `check-target-issue-template` hook | one read per worker spawn | 1 per spawn; failed closed at 0 quota | REST |
| Label listings (`audit-draft-remediation -Prepare`, `open-remediation`, `apply-priority-labels`) | per run | 1 to 8 per run; failed at 0 | REST |
| `audit-draft-remediation` duplicate search and issue reads | per run | GraphQL | REST (the search API has its own limit of 30 requests per minute) |
| `score-issues -Issue` | per run | GraphQL | REST |
| `collect-issues` | per run | GraphQL | GraphQL, behind `Wait-GhGraphQLQuota` and `Write-GhQuotaUsage`; about 31 per page |

The only GraphQL polling left is the review-thread read of `watch-open-prs.ps1`. It runs when the PR's `updated_at` changed and `remaining >= 300`; when skipped it prints one `WARN` line per reset window.

## What is left

The scripts not yet migrated are `fetch-closed-issue-data`, `historian-*`, `build-board-stats`, `reconcile-board`, `check-project-membership`, `open-remediation` beyond labels, `qwen-predraft`, `classify-scope` and `audit-next`. They are tracked in a follow-up issue.

## How to prove it

```powershell
pwsh -NoProfile -File tools/ai/gh-quota-selftest.ps1
pwsh -NoProfile -File tools/ai/gh-graphql-down-selftest.ps1
```

See also: [AI development model](AI-DEVELOPMENT-MODEL.md).
