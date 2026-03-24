# Coverage Baseline Report

**Date**: 2026-03-24 21:45 UTC
**SonarCloud Scan**: Run #23511615353
**Overall Coverage**: 65.2% (target: ≥80%)
**Total NCLOC**: 240,467
**Total Uncovered Lines**: 18,054

## Summary by Coverage Tier

| Tier | Packages | NCLOC | Uncovered | Avg Coverage |
|------|:--------:|------:|----------:|:------------:|
| 🟢 Excellent (≥90%) | 14 | 5,337 | 131 | ~96% |
| 🟡 Good (80-89%) | 12 | 11,606 | 953 | ~84% |
| 🟠 Needs Improvement (60-79%) | 20 | 53,363 | 7,050 | ~70% |
| 🔴 Low (1-59%) | 12 | 22,861 | 7,476 | ~42% |
| ⚫ Zero (0%) | 11 | 8,294 | 2,444 | 0% |
| ⬛ N/A (not measured) | 39 | 139,006 | 0* | N/A |
| **TOTAL** | **108** | **240,467** | **18,054** | **65.2%** |

\* N/A packages show 0 uncovered because SonarCloud did not measure them (providers tested via integration tests only).

## 🟢 Excellent (≥90%) — 14 packages

| Package | Cov% | NCLOC | Uncovered |
|---------|:----:|------:|----------:|
| DistributedLock | 100% | 75 | 0 |
| GuardClauses | 100% | 242 | 0 |
| MiniValidator | 100% | 39 | 0 |
| Security.Secrets.GoogleCloudSecretManager | 97.7% | 269 | 3 |
| Caching.Memory | 96.5% | 598 | 6 |
| AzureServiceBus | 96.3% | 301 | 6 |
| Security.Secrets.HashiCorpVault | 95.8% | 252 | 5 |
| AmazonSQS | 95.1% | 392 | 11 |
| Security | 94.1% | 766 | 14 |
| Extensions.Resilience | 93.1% | 248 | 10 |
| RabbitMQ | 93% | 269 | 11 |
| Polly | 92.1% | 1,350 | 44 |
| DistributedLock.InMemory | 91.1% | 252 | 7 |
| Security.Secrets.AwsSecretsManager | 89.5% | 279 | 9 |

## 🟡 Good (80-89%) — 12 packages

| Package | Cov% | NCLOC | Uncovered |
|---------|:----:|------:|----------:|
| Caching.Valkey | 87.5% | 47 | 2 |
| Caching.KeyDB | 87.5% | 47 | 2 |
| Caching.Garnet | 87.5% | 47 | 2 |
| Caching.Dragonfly | 87.5% | 47 | 2 |
| Security.Secrets.AzureKeyVault | 86.9% | 234 | 8 |
| DataAnnotations | 86.5% | 58 | 2 |
| IdGeneration | 86% | 938 | 76 |
| Kafka | 85.4% | 303 | 22 |
| Compliance.PrivacyByDesign | 84.7% | 1,720 | 112 |
| Security.Secrets | 84.7% | 2,931 | 224 |
| Compliance.DPIA | 82.6% | 3,439 | 339 |
| Security.PII | 81.5% | 1,795 | 162 |

## 🟠 Needs Improvement (60-79%) — 20 packages

| Package | Cov% | NCLOC | Uncovered |
|---------|:----:|------:|----------:|
| Compliance.NIS2 | 81.2% | 2,568 | 244 |
| Security.Sanitization | 80.3% | 1,390 | 134 |
| Messaging | 78.3% | 9,104 | 765 |
| Security.Encryption | 77.3% | 1,058 | 125 |
| Compliance.AIAct | 77.1% | 1,381 | 195 |
| Compliance.Consent | 76.9% | 1,739 | 258 |
| Caching.Redis | 75.9% | 729 | 84 |
| NATS | 75.1% | 297 | 38 |
| Security.ABAC | 73% | 8,412 | 1,256 |
| Messaging.Encryption | 72.9% | 959 | 117 |
| Caching | 72.6% | 1,659 | 224 |
| Encina (core) | 72.1% | 13,042 | 1,725 |
| Hangfire | 72.1% | 261 | 29 |
| Quartz | 69.7% | 303 | 45 |
| Caching.Hybrid | 69% | 323 | 43 |
| Compliance.GDPR | 67.3% | 1,694 | 317 |
| Security.Audit | 66.8% | 2,554 | 420 |
| Compliance.BreachNotification | 64.1% | 2,793 | 507 |
| Cdc | 63.5% | 2,698 | 440 |
| Tenancy.AspNetCore | 62.8% | 397 | 66 |
| Security.AntiTampering | 61.9% | 1,114 | 204 |
| Compliance.ProcessorAgreements | 60.4% | 2,777 | 612 |

## 🔴 Low (1-59%) — 12 packages

| Package | Cov% | NCLOC | Uncovered |
|---------|:----:|------:|----------:|
| Compliance.Anonymization | 58.3% | 2,969 | 601 |
| Compliance.DataSubjectRights | 51.7% | 2,898 | 787 |
| Compliance.Retention | 51% | 3,459 | 826 |
| Compliance.CrossBorderTransfer | 47.9% | 2,979 | 991 |
| Compliance.Attestation | 48.6% | 1,445 | 444 |
| MQTT | 45.6% | 399 | 112 |
| Tenancy | 45% | 461 | 110 |
| Compliance.LawfulBasis | 39.4% | 2,172 | 736 |
| Compliance.DataResidency | 37.7% | 3,004 | 1,009 |
| Cdc.Debezium | 36.8% | 971 | 311 |
| OpenTelemetry | 30.2% | 3,434 | 1,260 |
| DistributedLock.Redis | 25.5% | 338 | 130 |
| DistributedLock.SqlServer | 25.1% | 372 | 159 |

## ⚫ Zero Coverage (0%) — 11 packages

| Package | Cov% | NCLOC | Uncovered |
|---------|:----:|------:|----------:|
| **DomainModeling** | **0%** | **5,660** | **1,525** |
| Security.ABAC.Analyzers | 0% | 434 | 175 |
| Cdc.PostgreSql | 0% | 387 | 135 |
| Cdc.MySql | 0% | 341 | 128 |
| Cdc.SqlServer | 0% | 329 | 111 |
| Cdc.MongoDb | 0% | 300 | 102 |
| Messaging.Encryption.AzureKeyVault | 0% | 197 | 68 |
| Messaging.Encryption.AwsKms | 0% | 176 | 53 |
| FluentValidation | 0% | 123 | 50 |
| Messaging.Encryption.DataProtection | 0% | 123 | 33 |
| Redis.PubSub | 0% | 224 | 64 |

## ⬛ N/A (not measured — integration tests only) — 39 packages

| Package | NCLOC |
|---------|------:|
| EntityFrameworkCore | 12,842 |
| ADO.SqlServer | 11,476 |
| ADO.PostgreSQL | 11,467 |
| ADO.MySQL | 10,746 |
| ADO.Sqlite | 10,576 |
| MongoDB | 10,600 |
| Dapper.SqlServer | 9,486 |
| Dapper.PostgreSQL | 9,487 |
| Dapper.MySQL | 9,002 |
| Dapper.Sqlite | 8,890 |
| Testing | 5,694 |
| Marten | 4,130 |
| Marten.GDPR | 2,320 |
| Audit.Marten | 2,279 |
| AspNetCore | 1,752 |
| Cli | 1,699 |
| Testing.Pact | 1,200 |
| AzureFunctions | 1,138 |
| Testing.Architecture | 843 |
| Testing.Bogus | 801 |
| Testing.Shouldly | 727 |
| AwsLambda | 650 |
| Testing.WireMock | 635 |
| Testing.Respawn | 487 |
| Aspire.Testing | 479 |
| Testing.Testcontainers | 442 |
| SignalR | 436 |
| Testing.Verify | 400 |
| Testing.TUnit | 374 |
| Testing.FsCheck | 370 |
| gRPC | 368 |
| GraphQL | 340 |
| InMemory | 283 |
| Refit | 163 |

## To Reach 80% Global Coverage

- **Current**: 65.2% → 18,054 uncovered lines out of ~240K measured
- **Target**: 80% → max ~11,300 uncovered lines
- **Gap**: ~6,750 lines need coverage

### Highest-Impact Targets

1. **DomainModeling** (0% → investigate possible bug — 1,525 lines)
2. **OpenTelemetry** (30.2% → 80% = ~1,000 lines gained)
3. **8 Compliance modules in 🔴** (avg 45% → 80% = ~3,000 lines gained)
4. **Encina core** (72.1% → 80% = ~500 lines gained)
5. **Security.ABAC** (73% → 80% = ~400 lines gained)
