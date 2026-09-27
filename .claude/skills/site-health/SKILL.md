---
name: site-health
description: Checks every published Encina site and feed listed in tools/ai/sites.json (documentation, DocFX API reference, coverage/mutation/benchmark/load-test dashboards), prepares the control board's dash/* and stats/* snapshots, and routes a failure to a comment on its tracking issue or a new issue draft. Use at the start of every session, right after the llama-server check, and whenever a site or a board tab is suspected stale or broken.
---

# Site health

Encina publishes several sites and feeds; `tools/ai/sites.json` (schema 1) is the one list of them
(`baseUrl` plus a `sites` array of `id`, `name`, `kind` [`docs`|`api`|`dashboard`], `path`,
`expectHtml`, and the optional `dataPath`, `timestampField`, `publisher`, `maxAgeDays`,
`trackingIssue`). Adding a site means adding one entry here; this skill, the daily freshness check
(`.github/scripts/dashboard-freshness.cs`) and the post-deploy smoke check (`.github/workflows/docs.yml`)
all read it (#1382).

Run this skill in the main session (it can post) or delegate it to the `site-steward` agent (Haiku,
read-only on the repository except its own `artifacts/site-health/**`; it never pushes, comments or
opens anything — it drafts, and the orchestrator acts). Either way the steps are the same.

## 1. When it runs

- At the start of every session, immediately after the llama-server health check (`session-start-llama.md`
  memory note): the two are the first two things a session verifies before doing anything else.
- On demand, whenever a published site, a data feed or a control-board tab is suspected stale, broken or
  wiped (the maintainer asks, or a PR touches `tools/ai/sites.json`, `dashboard-freshness.cs`,
  `docs.yml`'s smoke check, or `tools/ai/board/*`).
- Before trusting a coverage/mutation citation (SPEC-001) or a board snapshot for a report.

## 2. Freshness and smoke checks (per registry entry)

Read `tools/ai/sites.json` once; do not hard-code the site list.

1. **Freshness** (data staleness): run the existing check as is —

   ```powershell
   dotnet run --file .github/scripts/dashboard-freshness.cs -- --registry tools/ai/sites.json
   ```

   Exit 0 = every dashboard with a `maxAgeDays` threshold is fresh; exit 1 = at least one is STALE or
   ERROR (the output names it and, when the entry carries `trackingIssue`, prints
   `(tracked in #n)`); exit 2 = bad arguments or a registry `--check-registry` would reject — treat 2 as
   a skill bug, not a site failure, and stop to report it instead of drafting anything.

2. **Smoke check** (reachability and content type), mirroring `docs.yml`'s post-deploy step in
   PowerShell (AGENTS.md §2: no `curl`, no bash) for every entry's `path` and, when present, `dataPath`:

   ```powershell
   $registry = Get-Content tools/ai/sites.json -Raw | ConvertFrom-Json
   foreach ($site in $registry.sites) {
       $url = "$($registry.baseUrl)/$($site.path)"
       $response = Invoke-WebRequest -Uri $url -Method Get -MaximumRedirection 5
       if ($response.StatusCode -ne 200) { <# record a failure for $site.id #> }
       $contentType = $response.Headers['Content-Type'] -join ';'
       if ($site.expectHtml -and $contentType -notmatch 'text/html') { <# record a failure #> }
       if ($site.dataPath) {
           $dataUrl = "$($registry.baseUrl)/$($site.dataPath)"
           $dataResponse = Invoke-WebRequest -Uri $dataUrl -Method Get -MaximumRedirection 5
           if ($dataResponse.StatusCode -ne 200) { <# record a failure #> }
       }
   }
   ```

   Retry each request once after a short wait before calling it a failure (the CI smoke check retries
   too); a timeout or a non-200/wrong-content-type after the retry is a failure for that entry.

3. Write the combined result (per entry: OK, STALE, ERROR, UNREACHABLE, WRONG-CONTENT-TYPE, with the
   tracking issue when the registry names one) to `artifacts/site-health/report.md` (site-steward's
   only writable path; the orchestrator running the skill directly may write anywhere, but the same path
   keeps the two runs comparable).

## 3. Control-board snapshots

Prepare the board's `dash/*` and `stats/*` inputs with the script part 1/3 of #1382 versioned, with
`-OutDir` always pointed under `artifacts/site-health/` — `site-steward` may write nowhere else, and
`enforce-path-ownership` cannot see through a launched `pwsh -File` script to catch a stray default:

```powershell
pwsh -File tools/ai/board/build-board-stats.ps1 -Since <yyyy-MM-dd> -OutDir artifacts/site-health/board
```

Inputs it needs:

- `-Since` (mandatory): the ISO date the report starts from — reuse the board's last known "as of" date,
  or the start of the current week.
- `-OutDir` (always pass it, as above; the script's own default is `artifacts/board`, outside
  `site-steward`'s allowlist): where `db-summary.json`, `db-days.json` and `db-sessions.json` land.
- `-ProjectDir` (optional): the Claude Code project folder; defaults to the folder derived from the main
  checkout's own path, so it is normally left unset.
- `-SessionMeta` / `-Plan` (optional, private, never committed): the maintainer's own session titles and
  plan-usage file, when they exist under `artifacts/`.

The script never publishes anything (subagents and scripts cannot use the board's artifact tools): copy
`db-summary.json`, `db-days.json` and `db-sessions.json` from `-OutDir` into the report so the orchestrator
can push them to the board itself.

## 4. Route every failure

For each entry the freshness or smoke check reported as not OK:

- **The registry entry carries `trackingIssue`**: the failure is already tracked. Draft the comment text
  (what failed, since when, the check's exact output) and:
  - running as the orchestrator directly: post it with `gh issue comment <trackingIssue> --body-file <path>`;
  - running as `site-steward`: never post — save the draft to
    `artifacts/site-health/comments/<trackingIssue>-<yyyyMMdd>.md` and list it in the report for the
    orchestrator to post.
- **No `trackingIssue`**: draft a new issue in the `open-issue` skill's format (the matching template's
  headers verbatim and in order — almost always `bug_report.md` for a broken publisher or smoke check,
  `technical_debt.md` for a stale threshold that needs raising). `site-steward` writes the file under
  `artifacts/site-health/issues/<slug>.md` (never `artifacts/issues/`, which is outside its allowlist); the
  orchestrator running the skill directly may open it right away with `gh issue create` through the
  `open-issue` skill instead of drafting a file.
- A failing **publishing workflow** (the run that populates a dashboard's data or deploys the site) is not
  diagnosed here: hand it to `ci-diagnoser` (the only agent `site-steward` may spawn) with the run id and
  the registry entry's `publisher`, and include its verdict in the report.

## 5. Report

One line per registry entry (OK, or the failure and where it was routed — comment, new issue draft, or
`ci-diagnoser` verdict), the board snapshot files produced (or why they were skipped), and the exit code of
each check run. Never fix a workflow, a hook or a page from inside this skill: that goes through the normal
path (issue, brief, worker, PR, review).
