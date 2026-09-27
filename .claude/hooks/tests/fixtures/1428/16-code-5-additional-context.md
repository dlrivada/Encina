## Additional Context

The defect is isolated to the stuck-batch misclassification. A `TimedOut` saga cannot also appear in the "expired" batch on MongoDB because `GetExpiredSagasAsync` (`SagaStoreMongoDB.cs:155`) restricts results to `Running`/`Compensating` by construction. The two batches are mutually exclusive regarding the `TimedOut` status, but the stuck-batch logic incorrectly includes them.

Related Issues:
- #16: Original audit issue from which this finding was extracted.
- #699: [FEATURE] Add distributed cache for Choreography State Store lookups (related context on MongoDB provider behavior).
- #696: [FEATURE] Add distributed cache layer to Saga Store for state lookups (related context on Saga Store performance/behavior).
- #181: [FEATURE] Observability: Create Encina.HealthChecks Package (related to provider coherence and health monitoring).
