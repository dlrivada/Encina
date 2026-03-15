# LoadTests - Security Secrets Management

## Status: Not Implemented

## Justification

### 1. Infrequent Operations
Secret retrieval happens at application startup or during key rotation (minutes/hours frequency, not per-request). Load testing infrequent operations provides no actionable insights.

### 2. External Provider Dependency
Actual load behavior is determined by the cloud provider (Azure Key Vault, AWS SM, HashiCorp Vault, GCP SM) — rate limits, throttling, and latency are provider-controlled, not Encina-controlled.

### 3. Cached Results
`ISecretProvider` results are cached in `IKeyProvider` at the application level. After initial load, secrets are served from memory. The hot path is the cache, not the provider.

### 4. Adequate Coverage from Other Test Types
- **Unit Tests**: Cover all provider operations, error handling, rotation logic
- **Integration Tests**: Cover cloud provider emulators (to be created)
- **Guard Tests**: Verify parameter validation for all 4 providers

### 5. Recommended Alternative
If secret rotation under load becomes a concern, test the specific cloud provider SDK's throughput using their own benchmarking tools, not Encina's load test infrastructure.

## Related Files
- `src/Encina.Security.Secrets/` — Source
- `src/Encina.Security.Secrets.AzureKeyVault/` — Azure provider
- `src/Encina.Security.Secrets.AwsSecretsManager/` — AWS provider
- `src/Encina.Security.Secrets.HashiCorpVault/` — HashiCorp provider
- `src/Encina.Security.Secrets.GoogleCloudSecretManager/` — GCP provider

## Date: 2026-03-15
