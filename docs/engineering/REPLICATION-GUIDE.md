# Replicating the Encina working setup, from a clean machine

> The step-by-step companion of [`HOW-ENCINA-IS-BUILT.md`](HOW-ENCINA-IS-BUILT.md) §7: what to install, what to copy from this repository, in what order, with the exact commands and what "done" looks like at each step. It targets an engineer who wants the same setup for another project, or a new Encina contributor who wants the full toolchain rather than the minimum of the [contributor guide](../contributing/README.md). Tracking: #1107 (EPIC #1102). Times are indicative; the first run of anything that downloads or compiles takes longer.
>
> Everything the guide tells you to copy exists in this repository at the path given. The scripts under `tools/ai/` are the ones used for the work described in `HOW-ENCINA-IS-BUILT.md` §6.

## 0. What you get, and what it needs

At the end you have: a repository whose `main` cannot receive a change that does not build, pass its tests and pass static analysis; a coverage system that measures each test type separately against per-package targets; weekly mutation testing that survives its own tooling; documentation that cites measured numbers instead of typing them; a pull-request flow that merges by itself on green; a paid AI assistant that specifies, decides and verifies; a free local model that does the bulk reading and drafting; and the ledgers that tell you what each of them cost.

Hardware: anything runs the repository and the paid-AI side. The local model needs a GPU: the reference setup is an RTX 4090 (24 GB), on which a 27B model at 4-bit with a 49k context uses about 22 GB (`ai-task-routing.md` §2.1). With less VRAM, pick a smaller GGUF and a shorter context; the rest of the guide does not change.

| Step | Time (first time) | Skippable? |
|---|---|---|
| 1 Base tooling | 30 min | No |
| 2 Repository conventions | 1 h to write, then ongoing | No |
| 3 Quality system | 2 to 4 h to wire, minutes to copy | Partially (start with coverage) |
| 4 GitHub automation | 2 h | No |
| 5 Paid AI | 30 min | Yes, if you have no budget: the local model still works with a human as the gate |
| 6 Local AI | 1 to 3 h (build and model download dominate) | Yes, if you have no GPU: everything routes to the paid tier or to you |
| 7 First run | 1 day | No |

## 1. Base tooling

Install and verify, in this order.

| Tool | Install | Verify |
|---|---|---|
| .NET SDK 10.0.x | [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/10.0) | `dotnet --version` starts with `10.0` |
| Docker Desktop (Windows/macOS) or Docker Engine + Compose v2 (Linux) | docker.com | `docker compose version` |
| PowerShell 7 | `winget install Microsoft.PowerShell` on Windows; packages on Linux/macOS | `pwsh --version` |
| Git | git-scm.com | `git --version` (worktrees need 2.5+) |
| GitHub CLI | cli.github.com, then `gh auth login` with the `repo`, `workflow` and `read:org` scopes | `gh auth status` |

Clone and prove the build, exactly as a contributor would (about 15 min the first time):

```bash
git clone https://github.com/dlrivada/Encina.git
cd Encina
dotnet restore Encina.slnx
dotnet build Encina.slnx --configuration Release --no-restore
dotnet format Encina.slnx --verify-no-changes
docker compose --profile core up -d
dotnet test tests/Encina.GuardTests/Encina.GuardTests.csproj --configuration Release --no-build --results-directory artifacts/test-results
```

Done when: build ends with 0 warnings and 0 errors, format reports no changes, the guard tests pass. If `dotnet build` crashes with an internal CLR error, keep `Directory.Build.rsp` (it limits MSBuild to one node for that reason).

For your own project, copy from the root: `Directory.Build.props` (analyzers, `TreatWarningsAsErrors`, nullable, `LangVersion`), `Directory.Packages.props` (central package management), `Directory.Build.rsp`, `.editorconfig`, `docker-compose.yml` with its profiles, `codecov.yml`, `.coderabbit.yaml`.

## 2. Repository conventions

These are files, not habits; the automation of steps 3 and 4 depends on them.

1. **The rules file.** `CLAUDE.md` at the root is read by the paid assistant on every session and is the contract for every contributor: philosophy, the scripting policy (PowerShell or C# file-based apps only), provider rules, cross-cutting rule, naming, testing standards, EventId registry, issue process. Start yours from this one and delete what does not apply; keep it in English.
2. **Issue templates and prefixes.** Copy `.github/ISSUE_TEMPLATE/` (nine templates: `bug_report.md`, `feature_request.md`, `technical_debt.md`, `test_implementation.md`, `architecture_spike.md`, `epic.md`, `refactoring.md`, `infrastructure.md`, `config.yml`). Titles carry the prefix (`[BUG]`, `[FEATURE]`, `[DEBT]`, `[TEST]`, `[SPIKE]`, `[EPIC]`, `[REFACTOR]`, `[INFRA]`).
3. **Labels.** Create at least: `epic`, `ai:local-candidate`, `ai:claude-required`, `deferred-1.0`, `p0-mandatory`, `p1-recommended`, `p2-post-1.0`, `p3-obsolete`, plus your area labels. `tools/ai/apply-priority-labels.ps1` creates the four priority labels if missing.
4. **Specifications and decisions.** Create `docs/specifications/` with a `README.md` index and write `SPEC-000` first: what the next release is, requirements `REQ-`, acceptance criteria `AC-`, invariants `INV-`, and a decision table `DEC-` with options, recommendation and the human's choice. Use [`SPEC-000`](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) as the template. Create `docs/architecture/adr/` with an `index.md`; number ADRs sequentially.
5. **The method documents.** Copy `docs/engineering/AI-DEVELOPMENT-MODEL.md` (roles, finding classes, human gate, provenance rules) and `docs/engineering/ai-task-routing.md` (which tier does what; the brief template) and edit them to your project. They are the operating manual of steps 5 and 6.
6. **Outputs.** Everything generated goes under `artifacts/` (gitignored); nothing at the root. `.gitignore` has `artifacts/` and `.claude/*` with `!.claude/agents/`.

## 3. The quality system, piece by piece

Wire these in the order below; each one is independently useful.

### 3.1 Zero warnings and public API tracking (30 min)

Already in `Directory.Build.props`: analyzers, `TreatWarningsAsErrors`, `WarningsAsErrors` including NuGet audit codes (`NU*`), nullable everywhere. Add `Microsoft.CodeAnalysis.PublicApiAnalyzers` per package with `PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt` (every package under `src/` has them; copy an empty pair to a new package). Done when a new public member fails the build until it is declared.

### 3.2 Coverage as obligations (2 h)

Files to copy:

- `.github/coverage-manifest/<Package>.json`, one per package (108 here): which test types apply to each source file and the target percentage per flag.
- `.github/scripts/coverage-report.cs`: reads the Cobertura XML of each test type and computes per-flag obligations. Usage: `dotnet run .github/scripts/coverage-report.cs -- --input <dir with one subfolder per flag> --output <dir>`.
- `.github/scripts/coverage-history.cs`: appends a snapshot to `history.json`.
- `docs/coverage/` (`index.html`, `app.js`, `data/`): the dashboard published to GitHub Pages by `publish-coverage.yml`.
- `codecov.yml`: flags per test type, patch target, `wait_for_ci`.

Wire: one test project per test type (`tests/Encina.UnitTests`, `GuardTests`, `ContractTests`, `PropertyTests`, `IntegrationTests`), each run separately with `--collect "XPlat Code Coverage"` and a distinct results folder; `ci-full.yml` does this weekly and on tags and feeds the report. Done when the dashboard shows a package green only if every applicable flag reaches its target, and a reflection-only test adds zero coverage.

### 3.3 Mutation testing (1 h to copy, weekly to run)

Files: `.github/stryker-config.json` (coverage analysis off, `test-case-filter`, thresholds, mutate globs), `.github/workflows/mutation-tests.yml` (17-shard matrix with per-folder test filters patched into the config before each shard), `.github/scripts/mutation-history.cs` (merges shards and carries forward per-file results), `.github/scripts/mut-docs-render.cs` (expands `mutref` markers), `docs/mutations/` (dashboard). Local run: `dotnet run --file .github/scripts/run-stryker.cs`. Read `docs/testing/mutation-measurement-methodology.md` before changing anything: two upstream bugs (xUnit v3 per-test coverage, filter only from the config file) shape the design.

### 3.4 Citations instead of numbers (30 min)

`.github/scripts/perf-docs-render.cs` and `mut-docs-render.cs` expand marker blocks (`docref-table`, `mutref-table`, inline `docref:`/`mutref:`) from the published `docref-index.json` files; the publish workflows run them and commit the rendered docs back. Adopt the rule before you have data: any measured number in a document goes through a marker. Coverage citations are specified in [`SPEC-001`](../specifications/SPEC-001-coverage-docref-citations.md) and not yet implemented (#1092).

### 3.5 Architecture tests (30 min)

`src/Encina.Testing.Architecture` (ArchUnitNET) with the `EventIdUniquenessRule`: every `[LoggerMessage]` event id belongs to a range registered in `src/Encina/Diagnostics/EventIdRanges.cs`; ranges do not overlap. Copy the registry pattern and the rule; add your own layering rules there.

### 3.6 Tests that cost nothing to keep

Integration tests use shared xUnit `[Collection]` fixtures (one container per provider, `tests/Encina.IntegrationTests/**/Collections.cs`) and Testcontainers; load tests and benchmarks live in `tests/Encina.LoadTests`, `tests/Encina.NBomber` and `tests/Encina.BenchmarkTests` and run only on schedule or dispatch, never on pull requests.

## 4. GitHub automation

### 4.1 Workflows

Copy `.github/workflows/` and adapt names and paths. What each one does and when it runs (as of #1110, where benchmarks moved from pull requests to push on `main`):

| Workflow file | Name | Runs on | What it does | Tier |
| :--- | :--- | :--- | :--- | :--- |
| benchmark-chatops.yml | Benchmark ChatOps | issue_comment | Parses a benchmark request in an issue comment and dispatches the run | event |
| benchmarks.yml | Benchmarks | push to main (path-filtered); schedule; workflow_dispatch | Determines the changed benchmark projects by fingerprint, runs them, aggregates | slow (main/nightly) |
| ci-full.yml | CI Full | schedule (Monday 03:17 UTC); push of `v*` tags; workflow_dispatch | Build, every test type separately with coverage, coverage report, pack | slow (main/nightly) |
| ci.yml | CI | push to main; pull_request; workflow_dispatch | `changes` filter, build, unit/integration/contract/property/guard shards, EF providers, `ci-result` gate | fast (PR) |
| codeql.yml | CodeQL | push to main; pull_request; schedule (Monday 03:00 UTC); workflow_dispatch | C# analysis in source mode (`build-mode: none`) | fast (PR) |
| conventional-commits.yml | Conventional Commits | pull_request_target | Validates the PR title type | fast (PR) |
| docs.yml | Documentation | push to main; pull_request; workflow_dispatch | Builds the docs site (DocFX API + Jekyll) and deploys on main | fast (PR) |
| link-check.yml | Link Check | push (md/config paths); pull_request (md paths); schedule (Sunday 03:00 UTC); workflow_dispatch | lychee: internal links only on PRs, full scan otherwise | fast (PR) |
| load-tests.yml | Load Tests | workflow_dispatch; schedule | Load-test suites per area | slow (main/nightly) |
| mutation-tests.yml | Mutation Tests | workflow_dispatch; schedule (Friday 03:00 UTC) | Baseline tests, 17-shard Stryker matrix, aggregate | slow (main/nightly) |
| publish-benchmarks.yml, publish-coverage.yml, publish-load-tests.yml, publish-mutations.yml | Publish … Data | workflow_run of the producing workflow; workflow_dispatch | Render citations, publish dashboards to Pages, persist data back | event |
| release-on-milestone.yml | Release on Milestone Close | milestone closed | Version bump, tag, GitHub release (broken by branch protection, see #1100) | event |
| sbom.yml | SBOM | push of `v*` tags; workflow_dispatch | Software bill of materials | slow |
| sonarcloud.yml | SonarCloud Analysis | push to main; pull_request; workflow_dispatch | Static analysis only (coverage comes from Codecov) | fast (PR) |
| testing-dogfooding-validation.yml | Testing Dogfooding Validation | pull_request; push to main; workflow_dispatch | Validates examples and testing packages, audits test dependencies, migration progress | fast (PR) |

The `ci-result` job in `ci.yml` is the one piece to copy exactly: `needs` every job, `if: always()`, fails when any upstream result is `failure` or `cancelled`. It lets branch protection require one stable check instead of matrix job names.

### 4.2 Branch protection (5 min, needs admin)

Apply with the GitHub API; the payload used here is in `HOW-ENCINA-IS-BUILT.md` §4.5 and SPEC-000 REQ-020:

```bash
gh api -X PUT repos/<owner>/<repo>/branches/main/protection --input protection.json
```

with `protection.json` containing: `required_status_checks: { strict: false, contexts: ["ci-result", "Analyze"] }`, `enforce_admins: true`, `required_pull_request_reviews: { required_approving_review_count: 0, dismiss_stale_reviews: true, require_code_owner_reviews: false }`, `required_linear_history: true`, `required_conversation_resolution: true`, `allow_force_pushes: false`, `allow_deletions: false`, `restrictions: null`. Set `required_approving_review_count` to 1 or more only if someone other than the author can approve. Keep `strict: false` unless you want every merge to force a rebase of every other open PR.

Then, repository settings: allow auto-merge, squash merge only, delete branches on merge (`gh api -X PATCH repos/<owner>/<repo> -F allow_auto_merge=true -F allow_squash_merge=true -F allow_merge_commit=false -F allow_rebase_merge=false -F delete_branch_on_merge=true`).

Audit every workflow that pushes to `main` afterwards; a release workflow that commits directly stops working (#1100).

### 4.3 Bots

- **Codecov:** install the GitHub app, add `CODECOV_TOKEN`, keep `codecov.yml`. Patch coverage is informational until all shards upload.
- **SonarCloud:** create the project, add `SONAR_TOKEN` (a user token works; it expires, and an expired token fails the workflow with an authentication error).
- **CodeQL:** enabled by the workflow; needs `security-events: write`.
- **CodeRabbit:** install the app, keep `.coderabbit.yaml` with `request_changes_workflow: false` (otherwise its review blocks auto-merge until a human dismisses it). On the free plan for small repositories, request reviews explicitly with `@coderabbitai review`, one per hour; every push cancels the review in progress.
- **Dependabot:** `.github/dependabot.yml` splits NuGet into `src/`, `tests/`, `tools/` jobs with grouping and cooldowns, because a single job over 150+ projects hits the updater timeout.

## 5. The paid AI

Claude Code (desktop app or CLI) opened on the repository root. What to copy:

- `CLAUDE.md` (step 2) is the session contract.
- `.claude/agents/` (versioned; `.gitignore` excludes the rest of `.claude/`): `pr-watcher` (Haiku, low effort), `ci-diagnoser` (Sonnet, medium), `mechanical-fixer` (Sonnet, low), `adversarial-reviewer` (Opus, high). Each file pins model, effort, tools and whether it may write. See [`.claude/agents/README.md`](../../.claude/agents/README.md).
- `tools/ai/watch-pr-events.ps1`: the token-free pull-request watcher. Run it under a monitor (or a terminal) as `pwsh -NoProfile -File tools/ai/watch-pr-events.ps1 -Pr <n>`; it emits one line per event (`CHECK-FAIL`, `REVIEW-COMMENT`, `REVIEW`, `CHECKS-DONE`, `PR-MERGED`).
- The assistant's persistent memory lives outside the repository (its own project directory); keep a "handoff" note there with branches, open PRs, pending commands and next steps, so that a cut session resumes cleanly.

Working rules that make the cost predictable: the main session specifies, decides and gates; watchers and diagnosers run on the cheapest adequate model; one push per round of corrections; auto-merge on green; every problem found becomes an issue immediately.

## 6. The local AI

Reference setup: llama.cpp `llama-server` serving Qwen 3.8 27B (Unsloth `UD-Q4_K_XL` GGUF, about 16 GB) with CUDA; opencode as the agent orchestrator; a small C# script for direct calls. Verified on Windows 11 with an RTX 4090; the limits below were measured, not assumed (`ai-task-routing.md` §2.1).

### 6.1 Server (1 to 3 h, dominated by build and download)

1. Get llama.cpp: build from source with CUDA (`cmake -B build -DGGML_CUDA=ON && cmake --build build --config Release`) or download a CUDA release build. On Windows the CUDA runtime DLLs must be on `PATH`; a `STATUS_DLL_NOT_FOUND` exit means they are not.
2. Download the GGUF manually (the `-hf` downloader needs an OpenSSL build). Put it under a models folder.
3. Start the server from a terminal you can watch (tokens per second and per-request token counts are printed there):

```powershell
.\llama-server.exe -m <models>\Qwen3.8-27B-UD-Q4_K_XL.gguf --alias qwen3.8-27b --no-mmproj -c 49152 -fa on -ctk q8_0 -ctv q8_0 -b 256 -ub 128 -np 1 --spec-type draft-mtp --spec-draft-n-max 4 --jinja --no-reasoning-preserve --temp 1.0 --top-k 20 --top-p 0.95 --min-p 0.0 -n 8192 --host 127.0.0.1 --port 8080
```

Flags that matter: `-c 49152` is the safe context for 24 GB; `-np 1` means one request at a time (scripts must not run in parallel); `--spec-draft-n-max 4` was the measured optimum for MTP speculative decoding (roughly 1.8× to 2× generation speed); the q8_0 KV cache showed no quality loss. Done when `Invoke-RestMethod http://127.0.0.1:8080/health` returns `status: ok`.

4. Disable "thinking" per request, not in the template: the direct-call script sends `chat_template_kwargs: { enable_thinking: false }`; in opencode set `extraBody.chat_template_kwargs.enable_thinking=false` on the provider. Reasoning blocks otherwise consume the context in agentic use.

### 6.2 Direct calls with a usage ledger (10 min)

`tools/ai/local-ai-ask.cs` posts one brief (plus optional input files) to the server and writes the reply and a CSV line with prompt tokens, completion tokens, seconds and tokens per second:

```bash
dotnet run tools/ai/local-ai-ask.cs -- --task my-task --brief tools/ai/briefs/my-brief.md --input docs/some-source.md --out artifacts/local-ai/out/my-task.md --max-tokens 4096
```

Keep `artifacts/local-ai/ledger.csv` (written by the script) and a `ledger-notes.md` next to it with the paid-side cost and a verdict per task; that pair is what "how much does the local model save" is answered from.

### 6.3 Agentic runs with opencode (30 min)

Install opencode 1.18.31 or later (older versions loop on context overflow). Configure a provider for the server (OpenAI-compatible, base URL `http://127.0.0.1:8080/v1`, no key, model `qwen3.8-27b`) in your global opencode config; the repository ships `opencode.json` (default model), `.opencode/agents/` (`encina-docs`, `encina-review`, `encina-test`) and `.opencode/skills/` (`cross-cutting-check`, `eventid-allocation`, `provider-coherence`, `release-checklist`, `test-workflow`). Launch a bounded task on its own worktree:

```powershell
opencode run -m llamacpp/qwen3.8-27b --agent encina-test --dir <worktree> "<one-line instruction>" -f <brief.md>
```

The message goes before `-f` or it is swallowed. On Windows launch it through `cmd /c opencode.cmd …` when you need it detached, and kill leftover `opencode` processes after the run.

### 6.4 Briefs: the part that decides whether it works

Copy `tools/ai/briefs/` as templates. Rules learned the measured way (`HOW-ENCINA-IS-BUILT.md` §6.4):

- One bounded task per brief, numbered points, "without omitting any", the exact output path, "stop when the file is written".
- Put the facts in the brief; do not ask it to read `src/` (a five-file reading brief overflowed the context and delivered nothing).
- Name every collaborator a test must mock; it will not discover them.
- Format rules that must not be dropped go in the system prompt and in the first line of the brief; validate the output with a script (JSON shape, citation set, bullet count) and keep the best of up to three attempts.
- Never expose internal identifiers next to the numbers it must cite; it will cite the wrong ones.
- Expect faithful content and choppy prose; the rewrite is the paid tier's job.

### 6.5 The batch pipelines (ready to run)

All under `tools/ai/`, each reading its inputs from `artifacts/local-ai/` and writing there:

| Script | Purpose | Input | Output |
|---|---|---|---|
| `classify-issues.ps1` | P0 to P3 backlog classification in batches of 30 | `issues/open-issues.json` (from `gh issue list --json …`), `briefs/classify-issues-rules.md` | `issues/classification.csv` |
| `apply-priority-labels.ps1` | Creates the priority labels and applies the approved classification (`-DryRun` first) | `issues/classification.csv` | labels on GitHub |
| `historian-extract.ps1` | Evidence per open issue: body, human comments, referencing PRs, `git grep` hits for the title's identifiers | GitHub API, repository | `historian/evidence.json` |
| `historian-run.ps1` | "Does this already exist?" verdict per issue with cited evidence, batches of 12 | `historian/evidence.json`, `briefs/historian-rules.md` | `historian/historian.csv` |
| `historian-extract-closed.ps1` and `archaeology-run.ps1` | Durable knowledge from closed issues with provenance | GitHub API; `briefs/archaeology-rules.md` | `historian/archaeology.csv`, `historian/knowledge.csv` |
| `consolidate-run.ps1` and `history-sections-run.ps1` | Area assignment, duplicate marking, one drafted section per area with validated citations | `historian/knowledge.csv`, `briefs/consolidate-areas-rules.md`, `briefs/history-section-rules.md` | `historian/knowledge-areas.csv`, `out/history/*.md` |
| `renumber-milestones-repair.ps1` | Milestone renumbering after classification (`-Execute` to apply) | `issues/classification-final.csv` | milestones on GitHub |

Every verdict that leads to closing an issue, promoting a rule or moving work is reviewed by a person before it is applied; the scripts never close, label or move on their own except `apply-priority-labels.ps1` and the milestone script, which run only after explicit approval.

## 7. First run

1. Write `SPEC-000` for your project and take its decisions with the human, one by one; record them in the decision table.
2. Open the first issue with a typed prefix, branch per task, verify locally, open the PR with the contract (`Fixes #N`, verification, cross-cutting outcomes), request the bot review once, arm auto-merge, start the next task.
3. Export the open issues and run the classifier; review the distribution and the patterns with the human; apply labels; renumber milestones.
4. Run the Historian passes (open, then closed); review the positives; close what is closed; publish the project history with its candidates for promotion.
5. Record the first ledger lines and the first PR timings; they are the baseline the next lessons are measured against.

## 8. What to expect

- **Pull-request latency** after the fast lane: build plus the slowest shard, 15 to 25 minutes; CodeQL in source mode about 10; a docs-only PR under 5. Before the fast lane it was 40 minutes plus a 36-minute CodeQL on the critical path (`HOW-ENCINA-IS-BUILT.md` §6.1).
- **Local model throughput:** 60 to 110 tokens per second generation on the reference GPU; a 30-issue classification batch in 15 to 20 seconds; a 1,000-word cited draft in 25 to 40 seconds; a 560-issue Historian pass in 11 minutes.
- **Local model failure modes:** dropped instructions on long briefs, prose in paragraphs when bullets were asked, citations of the wrong identifier if any other identifier is visible, re-exploration loops on open-ended research. All are handled by bounded briefs, automatic validation and best-of-N; none by trusting the output.
- **Bots:** CodeRabbit quota and cancelled reviews on push; Codecov patch evaluated per upload; external link checks flaky by nature; an expired Sonar token. Treat every bot signal as data to verify, never as a gate on its own.
- **If you have no GPU:** skip step 6; the briefs still make the paid model cheaper to steer, and the pipelines of 6.5 run on any OpenAI-compatible endpoint by changing `--url`.
- **If you have no paid budget:** skip step 5; the human takes the Specifier, Verifier and gate roles, and the local model does everything mechanical. It is slower, not impossible; the quality system of step 3 does not care which tier produced the change.
