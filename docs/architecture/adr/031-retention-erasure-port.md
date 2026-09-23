# ADR-031: Retention Enforcement Erases Through Its Own Category-Scoped Port

## Status

**Accepted** - decided in issue #1160 (2026-09-23). Supersedes decision 3 ("Deletion Mechanism") of `docs/plans/retention-implementation-plan-406.md`, which delegated erasure to `IDataErasureExecutor` from `Encina.Compliance.DataSubjectRights`.

## Context

`Encina.Compliance.Retention` creates one retention record per tracking call; each record carries an entity, a data category, a tenant and a module. The same entity can have several records with different periods (the reference use case of SPEC-002 keeps a patient's contact data for one year and the clinical record for at least five, Ley 41/2002, art. 17), and also several records in the same category, for example one per clinical episode, each with its own expiry.

The enforcement cycle erased expired data through `IDataErasureExecutor.EraseAsync(subjectId, ErasureScope)` from the data subject rights package, passing the record's `EntityId` as the subject id and an `ErasureScope` with no categories. The executor keeps every location when no category is given, so the expiry of any one record erased every category of the entity (#1160).

The two models do not line up:

- **Identity.** A retention record is keyed by the tracked entity (the `Id`/`EntityId` of a command response). The DSR executor locates data by data subject. An entity may belong to one subject, to several or to none; the DSR locators cannot find data by entity.
- **Category.** A retention category is a free-form string (`"clinical-record"`, `"marketing-consent"`, or the response type name). A DSR category is the `PersonalDataCategory` enum (`Contact`, `Health`, ...). The mapping is many-to-many: two retention categories with different periods can both be `Health`, so scoping by the enum would still erase data whose period has not expired.

## Decision

- Retention defines its own port, `IRetentionDataEraser`, whose single method receives a `RetentionErasureTarget` (record id, entity id, retention data category, expiry, tenant id, module id). The enforcement service calls it once per expired, non-held record and marks the record `Deleted` only after `Right`.
- The contract: erase only the target's category for the target's entity, return `Left` for any partial failure (the record stays `Expired` and is retried), and be idempotent.
- The port erases a whole category for an entity, so it must not run while another record of the same entity, category, tenant and module (a sibling) still retains that data. While any sibling is `Active` and not yet past its expiry, or `UnderLegalHold`, the expired record stays `Expired`, nothing is erased and no `DataDeleted` event is raised (the cycle reports it as deferred, EventId 8591). When the last sibling expires, the category is erased once and every sibling whose period has elapsed is marked `Deleted` together with it. `DataDeleted` is only ever raised for a record whose data was erased. If the siblings cannot be read, nothing is erased (fail closed). Added after the adversarial review of PR #1185.
- The eraser scopes the erasure by the target's tenant (and module) explicitly: the enforcement service runs in a background scope with no ambient tenant. It returns `Left` when it cannot establish that scope. The retention pipeline behavior records the tenant and module of the request on each record so that the target carries them.
- The application implements the port; Encina ships no default. Without a registration nothing is erased and records stay `Expired` (unchanged from #1142).
- `Encina.Compliance.Retention` no longer references `Encina.Compliance.DataSubjectRights`. An application whose entities are data subjects may implement the port by calling `IDataErasureExecutor` with its own category mapping.

## Rationale

- The port speaks the retention model's own terms (entity and retention category), so there is no lossy translation and no identity confusion inside the library.
- Only the application knows where the data of a retention category lives and how it must be removed (row deletion, field nullification, anonymization, crypto-shredding, a remote call).
- Removing the package reference removes a coupling that produced the bug and that no other retention feature needed (pay for what you use).

## Alternatives rejected

- **Pass a `PersonalDataCategory` mapping in `ErasureScope`:** rejected; the retention-to-enum mapping is many-to-many, so it still over-erases, and it leaves the entity/subject mismatch in place.
- **Add entity-scoped location to `IPersonalDataLocator`:** rejected; every locator implementation would need a second lookup path, and the category mismatch would remain.
- **Sibling records: a new `Superseded` status or event for the earlier record:** rejected; the earlier record's data is not erased when it is superseded, so a terminal status would claim a disposal that did not happen, and it would add a lifecycle state every consumer must handle. Keeping it `Expired` until the category is actually erased keeps the audit trail honest with the existing states.
- **Sibling records: erase per record instead of per category:** rejected; only the application knows which rows belong to which tracking call, and requiring the eraser to map record ids to data would push record bookkeeping into every implementation.
- **Record a subject id on each retention record:** rejected for this change; it needs a subject extractor in the retention pipeline, new events and a new read-model field, and still leaves the category mismatch.

## Consequences

- Breaking change (pre-1.0): applications that relied on the DSR executor being picked up by retention enforcement must register an `IRetentionDataEraser`. Until they do, expired records stay `Expired` and a warning (EventId 8519) is logged once per cycle, so nothing is erased by surprise.
- Retention enforcement can be tested with an in-memory eraser, without the DSR locator and strategy stack.
- An expired record whose category is still retained by a sibling stays `Expired` and is selected again by every cycle until the last sibling expires. The cost is one extra read of the entity's records per such record and cycle.
- Legal holds are still keyed by entity id only, across tenants; making them tenant-scoped is tracked separately (SPEC-002 REQ-061).
- Related: #1144 (crypto-shredding erases every key of a subject) has the same root problem on the crypto-shredding side and is not changed here.
