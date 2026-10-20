---
title: "How to run the release readiness gate"
layout: default
nav_exclude: true
---

# How to run the release readiness gate

This guide shows the maintainer how to work with the release readiness gate: add or retire a checklist item, answer a manual item, run the gate on your machine and re-verify the SPEC-002 law map before a tag. It assumes you can use `git`, `gh` and `dotnet`, and that you know the release steps in [How to cut a release by hand](RELEASE-PROCESS.md), which also covers the profiles and the GitHub App setup.

Every command runs from the repository root. The tool is `.github/scripts/release-readiness.cs`; its header comment is the complete list of flags. The registry it reads is `.github/release-checklist.json`. The reason for this design is [ADR-038](../architecture/adr/038-release-readiness-gate.md).

## Add an item to the registry

Open `.github/release-checklist.json` and add an object to `items`. Copy a neighbour of the same kind and change it. An automatic item that reads a file, for example `dependabot-configured`, looks like this:

```json
{
  "id": "dependabot-configured",
  "title": ".github/dependabot.yml exists",
  "source": ["#104"],
  "owner": [],
  "kind": "auto",
  "group": "Defects and security",
  "check": { "name": "file-exists", "params": { "path": ".github/dependabot.yml" } },
  "profiles": { "minor": "skip", "rc": "block", "final": "block" }
}
```

The fields, as `--check-registry` enforces them:

| Field | Rule |
| --- | --- |
| `id` | Unique and kebab-case. |
| `title` | Required. Say what is true when the item passes. |
| `source` | At least one reference: a SPEC acceptance criterion written as `SPEC-000 AC-005` or `SPEC-002 AC-025`, a requirement such as `SPEC-000 REQ-005`, or `#104`. |
| `owner` | The issues that deliver the item, as numbers. May be empty. |
| `kind` | `auto` (the gate reads a fact), `issue-state` (the state of GitHub issues) or `manual` (a person answers). |
| `group` | One of the seven SPEC-000 section 8 groups: `Inventory`, `Build and quality gates`, `Defects and security`, `API and docs`, `Release`, `Evidence`, `Regulatory compliance and readiness`. |
| `check` | Required for `auto` and `issue-state`; forbidden for `manual`. `name` is one of the evaluators and `params` are the parameters that evaluator declares. A missing or unknown parameter is an error. An `issue-state` item must use the `issue-state` evaluator. |
| `profiles` | A severity (`block`, `warn` or `skip`) for each of `minor`, `rc` and `final`. |
| `attests` | Only on a `manual` item: the id of the `auto` item it can replace when that item's fact could not be collected. Used by `attest-secret-scanning` and `attest-dependabot`. |

The evaluators and their parameters are the entries of `Evaluators.Build()` in the tool: `ci-run`, `workflow-latest`, `code-scanning-alerts`, `dependabot-alerts`, `secret-scanning-alerts`, `sonar-issues`, `issue-state`, `bug-gate`, `skip-scan`, `file-exists`, `file-regex`, `version-props`, `changelog-section`, `public-api-unshipped-empty`, `pages-freshness`, `coverage-flags-green`, `branch-protection`, `ci-result-needs`, `spec-verified-date`, `milestone-clean`, `nuget-listing`, `release-asset`, `exclusive-dirs` and `alias`. An `alias` item reuses the answer of another active `auto` item, for example `architecture-tests-green` follows `ci-full-on-sha`. In a path or pattern, `{version}` stands for the release version.

A new evaluator is a code change to the tool, not a registry edit.

Then check the registry:

```powershell
dotnet run --file .github/scripts/release-readiness.cs -- --check-registry
```

How to check it worked: the command prints `Registry OK: <path> (N items, M retired)` and exits with 0. On an error it prints one line per problem and exits with 2.

The check also reads the specs listed under `specs` in the registry and fails when an acceptance criterion of a listed spec has no item and no retired record (`SPEC-000 AC-003 has no registry entry or retired record`), or when a `source` names an acceptance criterion the spec does not contain. Adding a requirement to SPEC-000 therefore forces a registry entry in the same change.

## Retire an item

Never delete an item. A deleted item loses the reason the requirement was dropped, and the spec-coverage check would report its acceptance criterion as uncovered. Add a `retired` object with a reason and a reference instead, and drop `check` and `profiles`:

```json
{
  "id": "retired-git-secrets",
  "title": "No secrets in code, checked with git-secrets (issue #104 text)",
  "source": ["#104"],
  "owner": [],
  "kind": "auto",
  "group": "Defects and security",
  "retired": {
    "reason": "Replaced by GitHub secret scanning with push protection, which is enabled on the repository; its open alerts are checked by secret-scanning-no-alerts.",
    "ref": "repository security settings; SPEC-000 REQ-009"
  }
}
```

A retired item needs both `reason` and `ref`, is not evaluated and is not rendered into the checklist, and is counted in the `--check-registry` summary. Name the item that replaces it in the reason when there is one. Run `--check-registry` afterwards.

## Answer a manual item

The sign-off file for a release is `docs/releases/v<version>/checklist.md`, rendered for the profile of the release:

```powershell
dotnet run --file .github/scripts/release-readiness.cs -- --render-checklist --profile minor --version 0.14.0 --out docs/releases/v0.14.0/checklist.md
```

It lists one line per manual item that the profile does not skip:

```text
- [ ] law-map-reverified-through-pr — <title> — evidence: <link>
```

To answer an item, replace the space in `[ ]` with `x` and replace `<link>` with an `https://` URL or an issue reference such as `#<number>`:

```text
- [x] law-map-reverified-through-pr — <title> — evidence: https://github.com/dlrivada/Encina/pull/<number>
```

A ticked line without an evidence link counts as unanswered, and so does an unticked line. An unanswered `block` item makes the gate fail with `PENDING`; an unanswered `warn` item is reported as `WARN`.

The same file carries two more kinds of line. They are shown in the file as HTML comments, which the tool ignores, so copy the line out of the comment before you fill it in:

```text
- allow-open: #123 — reason
- disposition: codeql#12 — accepted: reason
```

- `allow-open` lists an issue that may stay open in the release milestone. The `milestone-clean` item fails on any other open issue of the milestone.
- `disposition` records what you decided about a medium finding from the evidence report, identified as `codeql#<number>` or `sonar:<key>`. The `medium-findings-dispositioned` item and the evidence report use it.

When the automatic secret-scanning or Dependabot fact could not be collected, the matching attestation item (`attest-secret-scanning`, `attest-dependabot`) is the only way to continue. Tick it with a link to the review you did by hand; the evaluation output states that the attestation replaced the fact. If the fact was collected, the attestation is ignored.

How to check it worked: the evaluation prints `PASS <id>` for the item, with the evidence link in the reason (see the next section).

## Run the gate on your machine

Collect the facts, evaluate them, and write the evidence report. The profile comes from the branch name, so pass the branch you are releasing from:

```powershell
dotnet run --file .github/scripts/release-readiness.cs -- --collect --from-branch release/v0.14.0 --out docs/releases/v0.14.0/facts.json
dotnet run --file .github/scripts/release-readiness.cs -- --evaluate --facts docs/releases/v0.14.0/facts.json --from-branch release/v0.14.0 --signoff docs/releases/v0.14.0/checklist.md
dotnet run --file .github/scripts/release-readiness.cs -- --evidence --facts docs/releases/v0.14.0/facts.json --signoff docs/releases/v0.14.0/checklist.md --out docs/releases/v0.14.0/evidence.md
```

`--collect` needs `gh` on the path, logged in to the repository. The alert facts need `RELEASE_GATE_APP_TOKEN` in the environment ([the setup](RELEASE-PROCESS.md#set-up-the-github-app-for-alert-facts)); without it those two items fail closed. Use `--version <v> --profile <p>` instead of `--from-branch` when no branch exists yet; give one or the other, not both, and a `--profile` that contradicts the branch is an error. `--collect` reads the commit from `git rev-parse HEAD` unless you pass `--sha <40 hex>`.

The evaluation prints one line per item and a `SUMMARY` line:

```text
PASS <id> <title> (<sources>) — <reason>
FAIL <id> ...
WARN <id> ...
PENDING <id> ...
SKIP <id> — profile minor skips this item
SUMMARY profile=minor version=0.14.0 pass=... fail=... warn=... pending=... skip=... result=PASS
```

The exit code is 0 when no item is `FAIL` or `PENDING`, 1 when at least one is, or when the facts are rejected (collected for another version or profile, older than `--max-facts-age-hours`, 24 by default, or unreadable), and 2 for a usage error or an invalid registry.

Useful options of `--evaluate`:

| Option | Use |
| --- | --- |
| `--only <id,id>` | Evaluate only the named items. An unknown id is a usage error. |
| `--max-facts-age-hours <n>` | Accept older facts than the default 24 hours. |
| `--now <ISO-8601>` | Evaluate as of a given time. `--evidence` uses the collection time unless you pass it, so the committed `evidence.md` can be regenerated byte for byte from the committed `facts.json`. |
| `--profile <p>` | Check that the facts were collected for that profile. Without it the profile is derived from the version or the branch. |

Check that the tool itself works, without network access:

```powershell
dotnet run --file .github/scripts/release-readiness.cs -- --self-test
```

How to check it worked: every line starts with `SELFTEST ok` and the last line is `SELFTEST summary: N ok, 0 failed`.

## Re-verify the law map before a tag

[SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md) section 3 is the map of the laws Encina's compliance modules answer to. REQ-025 and AC-025 require it to be re-verified before each minor tag of the 1.0 sequence and before `1.0.0-rc.1`, with its verification date within 30 days of the tag, through a pull request. Section 3.6 of the specification defines the cadence and the events that trigger an earlier review. Two items enforce the step, and both block in the `minor`, `rc` and `final` profiles:

| Item | Kind | What it checks |
| --- | --- | --- |
| `law-map-date` | auto | The `Verified on **yyyy-MM-dd**` line of SPEC-002 is at most 30 days old. Every `rc.N` is checked against the same 30-day limit. |
| `law-map-reverified-through-pr` | manual | You re-verified section 3 and updated the date through a pull request. The evidence is that pull request. |

To satisfy them:

1. Re-verify section 3 of SPEC-002 against the sources it cites, as section 3.6 describes, and change the date on the `Verified on **yyyy-MM-dd**` line.
2. Open the pull request, let it merge into `main`, and only then create the release branch. The date is read from the checkout of the release branch.
3. In `docs/releases/v<version>/checklist.md`, tick `law-map-reverified-through-pr` with the link to that pull request, as shown in [Answer a manual item](#answer-a-manual-item).

How to check it worked: the evaluation prints `PASS law-map-date` and `PASS law-map-reverified-through-pr`. A date 31 days old prints `FAIL law-map-date`.

## Related

- [How to cut a release by hand](RELEASE-PROCESS.md): the steps around the gate, the profiles and the GitHub App setup.
- [ADR-038](../architecture/adr/038-release-readiness-gate.md): the decision behind the gate.
- [SPEC-000](../specifications/SPEC-000-encina-1.0-baseline-and-release-scope.md) and [SPEC-002](../specifications/SPEC-002-eu-regulatory-readiness.md): the acceptance criteria the registry traces to.
- [Implementation plan for #104](../plans/pre-release-checklist-implementation-plan-104.md).
