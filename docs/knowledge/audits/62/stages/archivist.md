## Scope

Closing commit f0eff2ffbeec76140eb439796f0eedb96f9113fc (timeline `referenced` event; the `closed` event has no commit; the issue was closed 2025-12-26T23:17:15Z, the commit is 2025-12-27 00:26 +01:00). No PR.

Source today (all exist): `src/Encina.Messaging/RoutingSlip/` - IRoutingSlipRunner, IRoutingSlipState, IRoutingSlipStore, RoutingSlipActivityEntry, RoutingSlipBuilder, RoutingSlipContext, RoutingSlipErrorCodes, RoutingSlipOptions, RoutingSlipResult, RoutingSlipRunner, RoutingSlipStatus, RoutingSlipStepBuilder, RoutingSlipStepDefinition. Also `src/Encina.Messaging/MessagingConfiguration.cs` (UseRoutingSlips, RoutingSlipOptions), `MessagingServiceCollectionExtensions.cs` (registration; also used by `AddEncinaEntityFrameworkCore` since #2711), `src/Encina.Messaging/PublicAPI.Unshipped.txt`, and `src/Encina/Diagnostics/EventIdRanges.cs` (Messaging 2800-2999; the runner's EventIds are 2872-2887, NOT 400-415 as the close comment says).

Tests: the commit added 6 files under `tests/Encina.Tests/` (RoutingSlipGuardsTests, RoutingSlipPropertyTests, RoutingSlipBuilderTests, RoutingSlipContextTests, RoutingSlipRunnerTests, RoutingSlipStepBuilderTests). Consolidation 658263027 deleted the per-package project. Today: `tests/Encina.UnitTests/Messaging/RoutingSlip/` (Builder, Context, Options, Result, RunnerDiRegistration, Runner tests), `tests/Encina.GuardTests/Messaging/RoutingSlip/RoutingSlipRunnerGuardTests.cs`, plus mentions in `MessagingConfigurationPatternFlagsTests`, `MessagingServiceCollectionExtensionsCoreEquivalenceTests` and the EF Core recoverability-routers tests. No property or contract tests remain; RoutingSlipStepBuilderTests has no successor file. Docs from the commit: CHANGELOG.md (exists), ROADMAP.md (exists, lists the pattern), `docs/history/2025-12.md` (deleted by d098e5425; not a regression of this issue). No dedicated docs page for routing slips; docs/INVENTORY.md and a diagram in docs/features/transactional-outbox.md only mention it.

## Destinations

- Builder/inline-step decision: present, `RoutingSlipBuilderTests.cs`.
- Dynamic route modification (AddStep, AddStepNext, InsertStep, RemoveStepAt, ClearRemainingSteps all exist in RoutingSlipContext): present, `RoutingSlipContextTests.cs`.
- Reverse-order compensation + ROP errors: present, `RoutingSlipRunnerTests.cs`.
- Opt-in flag UseRoutingSlips: present, `RoutingSlipRunnerDiRegistrationTests.cs`.
- Property tests for step ordering/compensation invariants: missing (file not in today's tree).
- Contract tests for IRoutingSlipStep: missing; the interface was never built (grep over src for `IRoutingSlipStep` finds nothing). OnCompletion exists as a builder delegate, not as `OnCompletion<NotifyCustomer>()`.
- `IEncina.Execute(routingSlip, data)` from the issue sample: not built.
- Persistence/completion tracking: IRoutingSlipStore/IRoutingSlipState exist; no implementation and no caller in src (RoutingSlipRunner has no Store reference). Pending work tracked only indirectly by #701.
- Pre-draft items dropped: "spec-invariant" destination and the "Rules and lessons" (85% coverage, contract tests required) came from the issue's acceptance criteria/test matrix, not decisions.
- Pre-draft corrections: `closed_at: 12/26/2025` normalised to 2025-12-26; `linked_prs: []` kept empty (no PR); EventIds 400-415 claim was false.

## Successor and duplicate issues

No successor or duplicate. Related, verified via REST: #701 OPEN "[FEATURE] Add distributed cache for Routing Slip Store state lookups" (assumes a store that has no implementation); #2826 OPEN "[DEBT] MongoDB registration omits recoverability, routing slip, content router and scatter-gather services"; #2711 CLOSED 2026-10-29 (EF Core now honours UseRoutingSlips).

## Lessons for the pipeline

- A close comment's EventId range (400-415) can be wrong; check the runner's `EventId =` values and EventIdRanges.cs.
- A builder-only delivery can hide a missing interface from the issue's code sample (IRoutingSlipStep) and unused persistence abstractions; grep each named symbol and each interface for implementers and callers.
- The worktree had no predraft; read it from the main checkout.
