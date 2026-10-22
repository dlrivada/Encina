---
nav_exclude: true
---

# GitHub API quota: GraphQL versus REST

**Status:** reference note for the maintainer and for contributors of `tools/ai`. **Issues:** #2568, #2661.

The maintainer account has one GraphQL budget of 5000 points per hour, shared by every Claude session, watcher and script. On 2026-10-21 it reached `remaining 0` while REST still had 4995 of 5000, and every `gh pr`, `gh issue` and `gh label` call failed. This page lists what spends GraphQL, what the repository tools do about it, and how to check.

## What uses which API

Rule: an operation with a REST equivalent uses REST (`gh api repos/...`), always.

| API | Operations |
| --- | --- |
| REST | List and read issues, pull requests, labels, checks (check runs and commit status), comments, reviews, milestones and sub-issues; create issues and pull requests, add labels, create labels, search issues by title (`search/issues`, 30 requests per minute) |
| GraphQL-backed `gh` commands (avoid) | `gh pr view`, `gh pr checks`, `gh pr list`, `gh issue view`, `gh issue list`, `gh label list`, `gh project ...` |
| GraphQL only | Review threads (`isResolved`) and `resolveReviewThread`, `enablePullRequestAutoMerge`, Projects v2 (every `gh project` call), the bulk query of `collect-issues`, the bulk closed-issue reads with comments of `fetch-closed-issue-data` and `historian-extract-closed` (REST would need one extra call per issue), and `closedByPullRequestsReferences` |

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
| `audit-next -Issue`, `classify-scope`, `qwen-predraft`, `audit-done` pull request creation and lookup, `open-remediation` issue creation and title search | per run | 1 per call; failed at 0 | 0 (REST) | per run |
| `build-board-stats`, `reconcile-board` windows | per run | `pr list --search` and `issue list --search`: 3 to 6 per run | 0 (REST list endpoints, newest first, paged until the window start; search is not used because it caps at 1000 results) | per run |
| `fetch-closed-issue-data`, `historian-extract-closed` bulk reads | per run | 1 per page of issues with comments | unchanged (bulk GraphQL), behind `Wait-GhGraphQLQuota` and `Write-GhQuotaUsage` | 0 |
| `gh project` calls of `open-remediation` and `check-project-membership` | per run | 1 each | unchanged (Projects v2 has no REST API), behind the helper with `-NoWait`: a low quota fails the add with the retry command and never blocks issue creation | 0 |
| `collect-issues` | per run | 1 per page | 1 per page, behind `Wait-GhGraphQLQuota` and `Write-GhQuotaUsage` | 0 |

The watchers read check runs and commit status only when the head sha changed or the previous read had pending or failed checks (a failed job can be re-run on the same sha); reviews and the comment lists are read every poll (the comment lists with `since`; reviews are filtered by `submitted_at`). Limit: a re-run of a job that passed on the same sha is not seen until the sha changes; failed and pending checks are re-read every poll. REST has its own budget of 5000 requests per hour, shared like GraphQL, so several watchers on the same PR should not run at once.

The only GraphQL polling left is the review-thread read of `watch-open-prs.ps1`. It runs when the PR's `updated_at` changed and `remaining >= 300`; when skipped it prints one `WARN` line per reset window.

## What is left

None: every remaining GraphQL call is behind the helper (`Wait-GhGraphQLQuota` before, `Write-GhQuotaUsage` after). They are the review-thread read of `watch-open-prs`, the bulk query of `collect-issues`, the bulk closed-issue reads of `fetch-closed-issue-data` and `historian-extract-closed`, `closedByPullRequestsReferences`, and the `gh project` calls.

## How to prove it

```powershell
pwsh -NoProfile -File tools/ai/gh-quota-selftest.ps1
pwsh -NoProfile -File tools/ai/gh-graphql-down-selftest.ps1
```

See also: [AI development model](AI-DEVELOPMENT-MODEL.md).
