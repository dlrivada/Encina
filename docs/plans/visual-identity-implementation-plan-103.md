# Implementation Plan: Visual Identity — logo, package icon and branding assets ("Holm Oak")

> **Issue**: [#103](https://github.com/dlrivada/Encina/issues/103)
> **Type**: Feature
> **Complexity**: Low in code (none), medium in review (the artwork needs the maintainer's approval)
> **Estimated Scope**: about 15 binary and SVG files under `assets/`, no production code, one documentation page and a README banner reference

---

## Summary

Produce and commit the visual identity of Encina ("encina" means holm oak): logo variants, the NuGet package icon and the social and marketing images, so that [#102](https://github.com/dlrivada/Encina/issues/102) can declare `PackageIcon` on every package (SPEC-000 REQ-018, "license, readme and icon metadata").

Measured on `origin/main` 7a0049c07: there is no `assets/` directory, no `src/` project sets `PackageIcon` (0 of 107), and the only brand image is `docs/assets/Encina Framework Identidad Visual.png` (1,224,848 bytes), which `README.md` does not reference. `README.md` has badges but no banner.

Deliverables, from the issue:

| Folder | Files | Purpose |
|--------|-------|---------|
| `assets/logo/` | `encina-logo-horizontal.{svg,png}`, `encina-logo-square.{svg,png}`, `encina-logo-mono-dark.svg`, `encina-logo-mono-light.svg` | Logo variants |
| `assets/icons/` | `package-icon.svg` (source), `package-icon-128.png`, `package-icon-256.png` | NuGet icon; the 128 px PNG is the one #102 packs as `icon.png` |
| `assets/social/` | `readme-banner.png`, `github-social-preview.png` (1280x640), `twitter-card.png` | README, GitHub social preview, social cards |
| palette | Oak Green `#2D5016`, Bark Brown `#5C4033`, Leaf Gold `#D4A017`, Paper White `#FAFAF8` | Documented in `docs/` |

**Scope**: image assets and documentation only; no code, no public API, no `PublicAPI.*.txt`. Affected: the core `Encina` package, every package through #102, `README.md` and the repository social preview.

**Provider category**: none (`AGENTS.md` section 5; the issue is image assets only).

**Consistency with the #102 plan** (`docs/plans/package-metadata-implementation-plan-102.md`, Design Choice 8 and Phase 5): this plan owns the files `assets/icons/package-icon-128.png` (at most 1 MB, PNG) and `assets/icons/package-icon.svg`; the #102 plan owns `PackageIcon`, the packed item and the name `icon.png` inside the package. Neither plan adds a placeholder.

**Split** (each row one branch and pull request, at most 15 files and 1500 changed lines):

| PR | Phase | Content | Files |
|----|-------|---------|-------|
| 1 | 1 | `assets/logo/` (6 files) and `assets/icons/` (3 files); unblocks #102 Phase 5 | 9 |
| 2 | 2 | `assets/social/` (3 files) | 3 |
| 3 | 3 | Palette and asset page under `docs/`, README banner reference, changelog fragment | 3 |

---

## Design Choices

<details>
<summary><strong>1. Location — a new top-level <code>assets/</code> folder</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Top-level `assets/` with `logo/`, `icons/`, `social/`** | The layout the issue proposes; one stable path for `README.md`, #102 and the repository settings | A new top-level folder |
| **B) Extend `docs/assets/`** | Exists already | Mixes brand sources with documentation images; the documentation site would publish them |

### Chosen Option: **A — top-level `assets/`**

### Rationale

- `docs/assets/` keeps documentation-only images (it holds `badges/` and the current identity image); the brand sources are consumed by the build (#102), not by the site.
- The path is fixed by the issue and referenced by the #102 plan.

**Class:** Trivial
**Settled by:** #103 (Proposed Solution: the `assets/` layout) and the #102 plan, Design Choice 8

</details>

<details>
<summary><strong>2. Package icon format — PNG 128x128 at most 1 MB, SVG as the source</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) `package-icon-128.png` (plus a 256 px PNG for high DPI), SVG source** | The format and size NuGet recommends; the SVG regenerates every size | Two PNGs to keep in step with the SVG |
| **B) SVG as the package icon** | One file | NuGet accepts only PNG and JPG |
| **C) JPG** | Smaller files | No transparency, artefacts on a flat logo |

### Chosen Option: **A — PNG 128x128 with an SVG source**

### Rationale

- NuGet accepts PNG or JPG up to 1 MB, 128x128 recommended; the acceptance criterion of the issue repeats the 1 MB limit.
- The icon must stay recognisable at 16x16 (acceptance criterion), so the design is simple and geometric.

**Class:** Settled
**Settled by:** NuGet nuspec `icon` reference — <https://learn.microsoft.com/en-us/nuget/reference/nuspec#icon>; #103 acceptance criteria

</details>

<details>
<summary><strong>3. Who authors the artwork — the maintainer or a designer, AI-assisted generation, or a commission</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The maintainer (or a designer they choose) draws the SVG; agents only export, check and place the files** | Clear authorship and licence of the mark (it ships in 107 packages); the maintainer approves the artwork as the issue states | Needs the maintainer's time |
| **B) An image model generates concepts, the maintainer picks and a designer cleans up the vector** | Fast first concepts | Unclear copyright of a generated image, and raster output must be redrawn as clean SVG before it can scale to 16x16 |
| **C) Commission an external designer** | Professional result | Cost and delay; the issue gives no budget |

### Chosen Option: **A — the maintainer or a designer they choose authors the SVG**; decided 2026-11-11: the mark is a temporary, simple vector made with AI assistance within the plan's technical constraints, and a help-wanted issue invites a designer to refine it later by replacing the files in place

### Rationale

- The project has no designer today, so the temporary mark is made with AI assistance; the help-wanted issue keeps the path to a designer-authored mark open, and the files are replaced in place without changing their paths.
- Option A keeps the rights to the mark unambiguous for a logo that every package carries.
- Option B wins if the maintainer wants options to react to before drawing anything; the concepts would then stay out of the repository and only the redrawn vector is committed.
- Option C wins if no one on the project can draw the vector.
- In every case the final artwork is approved by the maintainer when reviewing the pull request; no agent decides the look.

**Class:** Dilemma

</details>

<details>
<summary><strong>4. Delivery — icon first, or all brand assets in one pull request</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) Logo and icon first (PR 1), social images next (PR 2), documentation last (PR 3)** | The files #102 waits for land first; each review is small | Three reviews of one identity |
| **B) One pull request with every asset and the documentation** | The identity is reviewed as a whole | 15 binary files plus docs in one review; #102 Phase 5 waits for the social images too |

### Chosen Option: **A — icon first** (recommended, pending the maintainer)

### Rationale

- #102 Phase 5 only needs `assets/icons/package-icon-128.png`; releasing it first shortens the critical path of SPEC-000 REQ-018.
- Option B wins if the maintainer prefers to judge logo, banner and social card side by side before anything is merged; the 12 asset files plus the 3 documentation files are exactly 15, the limit of one pull request, with no margin.

**Class:** Dilemma

</details>

<details>
<summary><strong>5. Export — the designer's tool exports the PNGs; no render script is added</strong></summary>

### Options Considered

| Option | Pros | Cons |
|--------|------|------|
| **A) The SVG is the source; the author exports the PNGs and commits both; a PowerShell check verifies dimensions and size** | No new toolchain dependency; the check is a few lines | A PNG can drift from its SVG until the next export |
| **B) A script renders every PNG from the SVG** | Reproducible | Needs an SVG renderer the repository does not declare; a new dependency for six images |

### Chosen Option: **A — export by the author, verify by command**

### Rationale

- Assets change rarely; a render pipeline costs more than it saves.
- The verification commands in Phase 1 read each PNG header and file size, so the acceptance criteria are checked by command and not by eye.

**Class:** Trivial
**Settled by:** `AGENTS.md` section 2 (scripts only in PowerShell or C# file-based apps; no new tooling is introduced)

</details>

---

## Implementation Phases

### Phase 1: Logo and Package Icon (PR 1)

<details>
<summary>Tasks</summary>

1. Create `assets/logo/encina-logo-horizontal.svg` and `.png`, `encina-logo-square.svg` and `.png`, `encina-logo-mono-dark.svg` and `encina-logo-mono-light.svg`.
2. Create `assets/icons/package-icon.svg`, `package-icon-128.png` and `package-icon-256.png`.
3. Use the issue's palette (Oak Green `#2D5016`, Bark Brown `#5C4033`, Leaf Gold `#D4A017`, Paper White `#FAFAF8`); the symbol is a stylised oak tree or leaf silhouette, clean and geometric, readable at 16x16.
4. Verify by command from the repository root:
   - `[System.IO.FileInfo]::new('assets/icons/package-icon-128.png').Length` is below 1048576;
   - the PNG header (bytes 16 to 23 of the file, big-endian width and height) reads 128 x 128 for the 128 px icon and 256 x 256 for the other.
5. Check by eye at 16x16, 128x128 and 256x256, and record the result in the pull request.
6. The maintainer approves the artwork in the pull request (acceptance criterion).

</details>

<details>
<summary>Prompt for AI Agents — Phase 1</summary>

```text
CONTEXT: Encina is a pre-1.0 .NET library whose name means "holm oak". SPEC-000 REQ-018 needs an icon on every package; issue #102 wires PackageIcon and packs assets/icons/package-icon-128.png as icon.png. No assets/ folder exists yet. Issue #103, plan Design Choices 1 to 5.

TASK: Place the maintainer-approved artwork under assets/logo/ and assets/icons/ with the exact file names of the plan. Do NOT invent or generate artwork: if the SVG sources are not supplied by the maintainer (Design Choice 3), stop and report. Run the size and dimension checks and report their output.

KEY RULES: Files are added with the Write/copy tools, never by editing PNG bytes in a script. package-icon-128.png is 128x128 and at most 1 MB. No placeholder images. At most 15 files and 1500 changed lines.

REFERENCE FILES: docs/plans/package-metadata-implementation-plan-102.md (Phase 5), docs/assets/ (what stays documentation-only), https://learn.microsoft.com/en-us/nuget/reference/nuspec#icon.
```

</details>

### Phase 2: Social and Marketing Images (PR 2)

<details>
<summary>Tasks</summary>

1. Create `assets/social/readme-banner.png`, `github-social-preview.png` (exactly 1280x640) and `twitter-card.png`.
2. Reuse the logo and palette of Phase 1; keep the text short and in English.
3. Verify the 1280x640 dimensions with the PNG header check of Phase 1.
4. Uploading the social preview to the repository is a settings change in GitHub (Settings, Social preview); it is not a file change and is done by the maintainer.

</details>

<details>
<summary>Prompt for AI Agents — Phase 2</summary>

```text
CONTEXT: Phase 1 merged the logo and icon files. The repository needs a README banner and social cards. Issue #103.

TASK: Place the maintainer-approved readme-banner.png, github-social-preview.png (1280x640) and twitter-card.png under assets/social/ and verify the dimensions by command.

KEY RULES: No generated artwork without the maintainer's approval (Design Choice 3). Do not change repository settings. Keep each PNG reasonably small (GitHub social preview limit is 1 MB).

REFERENCE FILES: assets/logo/, https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/customizing-your-repository/customizing-your-repositorys-social-media-preview.
```

</details>

### Phase 3: Documentation and Finalization (PR 3)

<details>
<summary>Tasks</summary>

1. `docs-writer` writes a short page under `docs/` that documents the palette (the four colours and their use) and the asset locations, and references the banner from `README.md`.
2. `mechanical-fixer` writes the changelog fragment `changelog.d/103-visual-identity.added.md`.
3. No ADR (no architectural decision) and no `PublicAPI.*.txt` entry.
4. Verification: the links in the page and in `README.md` resolve to existing files; `dotnet build` is unaffected (no code).
5. Comment on #103 with the merged paths so #102 Phase 5 can start.

</details>

<details>
<summary>Prompt for AI Agents — Phase 3</summary>

```text
CONTEXT: The logo, icon and social assets are merged under assets/. Issue #103 asks for the palette and asset locations to be documented and the banner referenced from README.md.

TASK: Write the palette and asset-locations page (docs-writer, encina-docs skill) and the README banner reference; write the changelog fragment through mechanical-fixer.

KEY RULES: Documentation only through docs-writer; never edit CHANGELOG.md. English, junior-readable, no emojis, no hand-typed coverage figures. Link check must pass.

REFERENCE FILES: .claude/skills/encina-docs/SKILL.md, changelog.d/README.md, README.md, assets/.
```

</details>

---

## Research

### Relevant Standards & Specifications

| Source | What it requires | Use here |
|--------|------------------|----------|
| SPEC-000 REQ-018, AC-018 | Icon metadata on every package | The reason for the icon and its 1 MB bound |
| NuGet nuspec `icon` | PNG or JPG, up to 1 MB, 128x128 recommended | Format of `package-icon-128.png` |
| GitHub social preview | 1280x640 recommended, under 1 MB | Format of `github-social-preview.png` |
| #102 plan, Design Choice 8 | Packs `assets/icons/package-icon-128.png` as `icon.png` | File name contract between the two issues |

### Existing Encina Infrastructure to Leverage

| Component | Location | Usage in this feature |
|-----------|----------|-----------------------|
| Documentation-only images | `docs/assets/` | Stays as is; the new brand sources go to `assets/` |
| Current identity image | `docs/assets/Encina Framework Identidad Visual.png` | Visual reference for the design direction; not used by `README.md` |
| Package metadata plan | `docs/plans/package-metadata-implementation-plan-102.md` | Consumes the icon in its Phase 5 |
| README badges | `README.md` | Receives the banner reference |

### Event ID Allocation

| Package | Range | Notes |
|---------|-------|-------|
| none | none | Image assets only; nothing is registered in `src/Encina/Diagnostics/EventIdRanges.cs` |

### Estimated File Count

| Category | Files | Notes |
|----------|-------|-------|
| Logo files | 6 | 4 SVG and 2 PNG in `assets/logo/` |
| Icon files | 3 | SVG source and two PNGs in `assets/icons/` |
| Social images | 3 | PNG in `assets/social/` |
| Documentation | 2 | Palette and assets page, `README.md` banner reference |
| Changelog fragment | 1 | `changelog.d/103-visual-identity.added.md` |
| Tests | 0 | No code; dimensions and sizes are verified by command |

---

## Combined AI Agent Prompts

<details>
<summary>Combined prompt for all phases</summary>

```text
PROJECT CONTEXT: Encina is a pre-1.0 .NET 10 library; "encina" means holm oak. SPEC-000 REQ-018 needs an icon on every package. There is no assets/ folder; the only brand image is docs/assets/Encina Framework Identidad Visual.png. Issue #103 produces the files; issue #102 wires PackageIcon and packs assets/icons/package-icon-128.png as icon.png.

IMPLEMENTATION OVERVIEW: Phase 1 adds assets/logo (6 files) and assets/icons (3 files). Phase 2 adds assets/social (3 files). Phase 3 documents the palette and asset locations (docs-writer), references the banner from README.md and adds a changelog fragment (mechanical-fixer). Three pull requests, at most 15 files each.

KEY PATTERNS: The maintainer or a designer authors the SVG (Design Choice 3 is pending the maintainer); agents place, verify and document, and never invent artwork. package-icon-128.png is 128x128 and at most 1 MB. Palette: Oak Green #2D5016, Bark Brown #5C4033, Leaf Gold #D4A017, Paper White #FAFAF8. Scripts only in PowerShell or C#; no placeholder images.

REFERENCE FILES: docs/plans/package-metadata-implementation-plan-102.md, docs/assets/, README.md, changelog.d/README.md, .claude/skills/encina-docs/SKILL.md, AGENTS.md sections 2 and 11.
```

</details>

---

## Cross-Cutting Integration Matrix

| # | Function | Status | Notes |
|---|----------|--------|-------|
| 1 | Caching | ❌ | Image files read nothing that `ICacheProvider` could cache. |
| 2 | OpenTelemetry | ❌ | No runtime operation exists to trace or measure. |
| 3 | Structured Logging | ❌ | No code is added, so no `[LoggerMessage]` and no EventId range. |
| 4 | Health Checks | ❌ | There is no runtime dependency to check. |
| 5 | Validation | ❌ | No input is received; size and dimension checks are manual acceptance commands. |
| 6 | Resilience | ❌ | No external system is called. |
| 7 | Distributed Locks | ❌ | No shared state is accessed concurrently. |
| 8 | Transactions | ❌ | No multi-operation atomicity is needed. |
| 9 | Idempotency | ❌ | Static files cannot arrive twice. |
| 10 | Multi-Tenancy | ❌ | No tenant data is stored. |
| 11 | Module Isolation | ❌ | The assets belong to no module. |
| 12 | Audit Trail | ❌ | No compliance or security effect; the licence of the artwork is covered by Design Choice 3. |

---

## Prerequisites & Dependencies

| Item | State | Effect |
|------|-------|--------|
| [#102](https://github.com/dlrivada/Encina/issues/102) package metadata | open | Consumes `package-icon-128.png` in its Phase 5; this plan does not wait for it |
| Maintainer artwork or designer choice (Design Choice 3) | pending | Phase 1 cannot start without SVG sources |

---

## Next Steps

1. When work on #103 starts, ask the maintainer Design Choices 3 (who authors the artwork) and 4 (icon first or one pull request), and record the answers in a last section, `Maintainer Decisions`.
2. Write the worker brief for PR 1 once the SVG sources exist; a worker only places and verifies them.
3. After PR 1 merges, tell #102 that its Phase 5 can start, and comment on #103 with the link to this plan.

## Maintainer Decisions

**D3** (2026-11-11): temporary AI-assisted vector mark now; a help-wanted issue asks a contributor with design skills to refine it, replacing the files in place.
