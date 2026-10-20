---
title: "v1.0.0-rc.1 release checklist"
layout: default
nav_exclude: true
---

# v1.0.0-rc.1 release checklist

> **Profile**: `rc`. Rendered by `.github/scripts/release-readiness.cs --render-checklist` from `.github/release-checklist.json`. The gate checks the automatic items itself; answer each manual item below by ticking it and adding an evidence link. A ticked line without a link counts as unanswered.

## Automatic items (checked by the gate)

| Id | Item | Severity | Source |
| --- | --- | --- | --- |
| `ci-full-on-sha` | CI Full is green on the release commit | block | SPEC-000 AC-001 |
| `law-map-date` | The law map verification date is at most 30 days old | block | SPEC-002 AC-025 |
| `codeql-high-alerts` | No open CodeQL alert at high or higher | block | SPEC-000 AC-002 |
| `unshipped-empty` | Every PublicAPI.Unshipped.txt is empty | warn | SPEC-000 AC-003 |
| `secret-scanning-no-alerts` | No open secret-scanning alert | block | #104 |

## Manual items

### Blocking

- [ ] attest-secret-scanning — Attestation: secret-scanning alerts reviewed by hand (only when the automatic fact failed) — evidence: <link>
- [ ] adversarial-review — The adversarial review report is attached with every finding dispositioned — evidence: <link>

## Milestone allow-list

One line per issue that may stay open in the release milestone:

<!-- - allow-open: #123 — reason -->

## Medium-finding dispositions

One line per medium finding listed in the evidence report (`codeql#<number>` or `sonar:<key>`):

<!-- - disposition: codeql#12 — accepted: reason -->
