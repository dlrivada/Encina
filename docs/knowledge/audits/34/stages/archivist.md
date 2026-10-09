## Scope

Issue #34 "[REFACTOR] Replace object? Details with ImmutableDictionary in EncinaError" (created 2025-12-24T13:23:01Z, closed 2025-12-25T09:39:47Z, COMPLETED, milestone v0.03.0). No linked PR; the only timeline commit is 2e46745e (authored 2025-12-26 00:50 +0100, `Closes #34`), which changed 4 files:

- `src/Encina/Results/EncinaErrors.cs` (today the same path; history via `git log --follow` goes through #1319, #1918 and others). It still holds `EncinaErrors.Create/FromException` (and later `Forbidden/Unauthorized/Unauthenticated`) with `IReadOnlyDictionary<string, object?>? details`, internal `EncinaException.Details` (default `ImmutableDictionary<string, object?>.Empty`), and `EncinaErrorExtensions.GetDetails()/GetMetadata()` returning `IReadOnlyDictionary<string, object?>`.
- `src/Encina/PublicAPI.Unshipped.txt` (lines 208-209 carry the GetDetails/GetMetadata signatures).
- Tests `tests/Encina.Tests/EncinaTests.cs` and `tests/Encina.Tests/MediatorErrorExtensionsTests.cs`: that project no longer exists; error-details assertions now live in `tests/Encina.UnitTests/**` (for example `Compliance/Retention/RetentionErrorsMetadataTests.cs`, `AspNetCore/AuthorizationPipelineBehaviorTests.cs`).
- Consumers of the changed API today: `src/Encina.Testing.Verify/EncinaErrorConverter.cs`, `src/Encina.Testing/Assertions/EitherAssertions.cs`, `EitherCollectionAssertions.cs`, `Handlers/ScenarioResult.cs`, `Handlers/HandlerSpecification.cs`, `src/Encina.Testing.Shouldly/StreamingShouldlyExtensions.cs`, and READMEs of `Encina.EntityFrameworkCore` and `Encina.MongoDB`.

No code was removed on purpose. The shipped public type is `IReadOnlyDictionary`, not the `ImmutableDictionary` of the title.

## Destinations

- Details type change (BREAKING): present in `CHANGELOG.md:9616` and `docs/releases/v0.11.0/CHANGELOG-DETAILS.md`, `docs/releases/pre-v0.10.0/README.md:829`.
- GetDetails returns an empty dictionary instead of Option: present in `docs/engineering/PROJECT-HISTORY.md:63` ("Using Option<object> for empty details was rejected ... (#34)") and in `src/Encina/PublicAPI.Unshipped.txt:208`.
- Internal EncinaException normalization: present in `src/Encina/Results/EncinaErrors.cs:88-97`.
- ADR: none exists for this decision (the index was not found to cover details/metadata typing; searched "GetDetails", "Option<object>", "IReadOnlyDictionary<string, object" in the tree outside CHANGELOG/releases/PROJECT-HISTORY). No AGENTS.md rule carries it; not required for a small type change.
- Pre-draft candidate "reviewer-checklist: details use dictionaries" has no source in the issue and was dropped. No dedicated regression test for "GetDetails returns empty when absent" was located by name (usage is broad across unit tests); left to the test stage.
- `src/Encina.EntityFrameworkCore/README.md:1152` and `src/Encina.MongoDB/README.md:144` use `error.GetDetails()`; their snippet text was not verified for the new return type (docs stage).

## Successor and duplicate issues

None. The issue is delivered, not rejected or duplicated.

## Lessons for the pipeline

- The issue title promises a type (ImmutableDictionary) that differs from what shipped (IReadOnlyDictionary); the body is a post-hoc "Implemented" note with no problem statement. Compare title against the shipped signature, not the body only.
- The closing commit is authored (2025-12-25T23:50Z) after the issue's closed_at (09:39Z): the close was manual and the commit referenced it later; do not derive the close date from the commit.
- The pre-draft had `closed_at: 12/25/2025 09:39:47` and `linked_prs: []` in the old format; normalised to ISO and schema 1. The checker also requires `audit.checklist: 1` (not a label) and links on every `paraphrase:` source, and rejects `kind: public-api` for destinations.
