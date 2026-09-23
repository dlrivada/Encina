# ADR-031: Retention Enforcement Erases Through Its Own Category-Scoped Port

## Status

**Accepted** - decided in issue #1160 (2026-09-23). Supersedes decision 3 ("Deletion Mechanism") of `docs/plans/retention-implementation-plan-406.md`, which delegated erasure to `IDataErasureExecutor` from `Encina.Compliance.DataSubjectRights`.

## Context

`Encina.Compliance.Retention` tracks one retention record per entity and data category. The same entity can have several records with different periods; the reference use case of SPEC-002 keeps a patient's contact data for one year and the clinical record for at least five (Ley 41/2002, art. 17).

The enforcement cycle erased expired data through `IDataErasureExecutor.EraseAsync(subjectId, ErasureScope)` from the data subject rights package, passing the record's `EntityId` as the subject id and an `ErasureScope` with no categories. The executor keeps every location when no category is given, so the expiry of any one record erased every category of the entity (#1160).

The two models do not line up:

- **Identity.** A retention record is keyed by the tracked entity (the `Id`/`EntityId` of a command response). The DSR executor locates data by data subject. An entity may belong to one subject, to several or to none; the DSR locators cannot find data by entity.
- **Category.** A retention category is a free-form string (`"clinical-record"`, `"marketing-consent"`, or the response type name). A DSR category is the `PersonalDataCategory` enum (`Contact`, `Health`, ...). The mapping is many-to-many: two retention categories with different periods can both be `Health`, so scoping by the enum would still erase data whose period has not expired.

## Decision

- Retention defines its own port, `IRetentionDataEraser`, whose single method receives a `RetentionErasureTarget` (record id, entity id, retention data category, expiry, tenant id, module id). The enforcement service calls it once per expired, non-held record and marks the record `Deleted` only after `Right`.
- The contract: erase only the target's category for the target's entity, return `Left` for any partial failure (the record stays `Expired` and is retried), and be idempotent.
- The application implements the port; Encina ships no default. Without a registration nothing is erased and records stay `Expired` (unchanged from #1142).
- `Encina.Compliance.Retention` no longer references `Encina.Compliance.DataSubjectRights`. An application whose entities are data subjects may implement the port by calling `IDataErasureExecutor` with its own category mapping.

## Rationale

- The port speaks the retention model's own terms (entity and retention category), so there is no lossy translation and no identity confusion inside the library.
- Only the application knows where the data of a retention category lives and how it must be removed (row deletion, field nullification, anonymization, crypto-shredding, a remote call).
- Removing the package reference removes a coupling that produced the bug and that no other retention feature needed (pay for what you use).

## Alternatives rejected

- **Pass a `PersonalDataCategory` mapping in `ErasureScope`:** rejected; the retention-to-enum mapping is many-to-many, so it still over-erases, and it leaves the entity/subject mismatch in place.
- **Add entity-scoped location to `IPersonalDataLocator`:** rejected; every locator implementation would need a second lookup path, and the category mismatch would remain.
- **Record a subject id on each retention record:** rejected for this change; it needs a subject extractor in the retention pipeline, new events and a new read-model field, and still leaves the category mismatch.

## Consequences

- Breaking change (pre-1.0): applications that relied on the DSR executor being picked up by retention enforcement must register an `IRetentionDataEraser`. Until they do, expired records stay `Expired` and a warning (EventId 8519) is logged once per cycle, so nothing is erased by surprise.
- Retention enforcement can be tested with an in-memory eraser, without the DSR locator and strategy stack.
- Related: #1144 (crypto-shredding erases every key of a subject) has the same root problem on the crypto-shredding side and is not changed here.
