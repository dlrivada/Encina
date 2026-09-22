# ADR-029: Recoverability Classifies Failures as Transient, Permanent or Unknown

## Status

**Accepted** - decided in issue #39 (2025); recorded as an ADR on 2026-09-22 from the project history.

## Context

Message handling fails for reasons with opposite remedies: a database timeout deserves a retry, a deserialization error never will succeed, and some exceptions cannot be told apart in advance. Retrying everything wastes resources and delays dead-lettering; retrying nothing loses recoverable work. The behaviour had to be uniform across the Outbox, Inbox and transport consumers.

## Decision

The recoverability pipeline classifies every failure through `IErrorClassifier` into three categories (#39): **Transient** (retry: immediate retries, then delayed retries with backoff), **Permanent** (no retry: the message goes to the dead-letter queue as a `FailedMessage` record with its context), and **Unknown** (treated conservatively; the default classifier decides by exception type). `DefaultErrorClassifier` ships the baseline mapping; applications extend or replace it through DI.

## Rationale

- The three categories cover the decision the pipeline must make (retry now, retry later, stop) without a taxonomy that no one maintains.
- A dead-letter record with the failure context is the only honest outcome for a permanent failure; silent drops and infinite retries were both observed in earlier designs.
- Classification is injectable so that provider-specific exceptions (transient SQL error codes, broker disconnects) can be mapped without touching the pipeline.

## Alternatives rejected

- **Retry every failure with a cap:** rejected; permanent failures then burn the whole retry budget before reaching the dead-letter queue.
- **Per-handler retry policies only:** rejected; the same classification logic would be duplicated per handler and per provider.

## Consequences

- Every messaging store exposes the dead-letter surface (`IDeadLetterStore`; CDC has its own `ICdcDeadLetterStore` because its records differ, #631).
- Resilience integrations (Polly) sit around the classifier, not instead of it: a Polly policy decides how to retry, the classifier decides whether.
- New exception types from providers must be mapped in the classifier or they fall into Unknown.

## References

- Issue #39 (and #631 for the CDC dead-letter store); `CLAUDE.md`, "Cross-Cutting Integration Rule" (Resilience, Idempotency); `docs/engineering/PROJECT-HISTORY.md`, "Messaging patterns and transports".
