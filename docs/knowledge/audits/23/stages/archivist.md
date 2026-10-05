## Scope
Issue #23 itself changed no code (closed by comment "Reverted - issue created in error"; only timeline commit 2b50a1ec touched CLAUDE.md, ROADMAP.md, docs/history/2025-12.md, none of it Encina.Testing). Today's surface for the feature it asked for, delivered under #44 (commit 929046c5):
- `src/Encina.Testing/EncinaFixture.cs` (exists), `src/Encina.Testing/Assertions/EitherAssertions.cs` (`ShouldBeSuccess`, `ShouldBeSuccessAnd`; exists)
- `src/Encina.Testing/EventSourcing/AggregateTestBase.cs` (exists; added after #44)
- `tests/Encina.Testing.Tests` was in 929046c5; the consolidated test projects are under `tests/` (e.g. `Encina.UnitTests`). Not re-traced here.
- Sibling packages `src/Encina.Testing.*` (Architecture, Bogus, Fakes, FsCheck, Pact, Respawn, Shouldly, Testcontainers, TUnit, Verify, WireMock) exist today but belong to other issues.
Nothing was removed on purpose.

## Destinations
- Decision "#23 was a mistaken duplicate of #44": no destination needed (`none`); the knowledge entry is `current: no`.
- The Encina.Testing package itself: present (see Scope).
- Pre-draft claim "Why was it created in error?" resolved: same-titled #44 was created and closed completed hours later.

## Successor and duplicate issues
- #44 "[FEATURE] Encina.Testing - test fixtures and fluent assertions": verified CLOSED, state_reason COMPLETED, closed 2025-12-24T18:37:04Z, closed by commit 929046c5 ("Closes #44") which added EncinaFixture, EitherAssertions and 32 tests. Recorded as `superseded_by: 44`.
- Related, not duplicates: #444 (Enhanced Testing Fixtures, CLOSED), #498 (dogfooding EPIC, CLOSED).

## Lessons for the pipeline
- The pre-draft outcome `rejected-unexplained` was wrong: its open question ("why created in error") is answered by searching same-titled issues (`gh issue list --state all --search "<title> in:title"`), which found #44. GitHub's state_reason COMPLETED is not evidence of delivery.
- A fresh wia worktree has no `artifacts/knowledge/issues` or predraft; read the pre-draft from the main checkout and create the issues directory before writing.
