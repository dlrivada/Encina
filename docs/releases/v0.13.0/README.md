# v0.13.0 - Security & Compliance

> **Milestone**: [v0.13.0 - Security & Compliance](https://github.com/dlrivada/Encina/milestone/10)
> **Status**: In Progress

This document captures the detailed implementation history for v0.13.0 (February-March 2026).

## Milestone Overview

v0.13.0 focuses on security infrastructure and regulatory compliance, providing GDPR-compliant data processing, consent management, data subject rights, anonymization, retention policies, secrets management, and data residency enforcement across all 10 database providers.

### Issues in Milestone

| Issue | Feature | Status |
|-------|---------|--------|
| #394 | Core Security | Completed |
| #402 | GDPR Core (RoPA) | Completed |
| #403 | Consent Management | Completed |
| #413 | Lawful Basis Validation | Completed |
| #404 | Data Subject Rights | Completed |
| #407 | Anonymization | Completed |
| #406 | Data Retention | Completed |
| #408 | Secrets Management | Completed |
| #405 | Data Residency | Completed |
| — | Field-Level Encryption | Planned |
| — | PII Masking | Planned |
| — | NIS2 Directive | Planned |
| #415 | AI Act Compliance | Completed |

---

## March 2026

### AI Act Compliance (#415)

**Issue**: [#415 - AI Act Compliance](https://github.com/dlrivada/Encina/issues/415)

Implemented EU AI Act (EU 2024/1689) compliance with declarative, attribute-based risk classification, prohibited practices detection (Art. 5), human oversight enforcement (Art. 14), and transparency obligations (Art. 13/50) at the CQRS pipeline level.

**Package**: `Encina.Compliance.AIAct`

#### Key Features

- **`[HighRiskAI]` attribute** — Declarative high-risk AI system metadata with category and optional system ID (Art. 6)
- **`[RequireHumanOversight]` attribute** — Human review requirement (Art. 14)
- **`[AITransparency]` attribute** — Transparency disclosure obligations (Art. 13/50)
- **`AIActCompliancePipelineBehavior`** — Automatic enforcement in the request pipeline
- **Risk classification** — 4 EU AI Act risk tiers (Prohibited, HighRisk, LimitedRisk, MinimalRisk)
- **12 high-risk categories** — Annex III categories (Biometric, Critical Infrastructure, Employment, etc.)
- **8 prohibited practices** — Art. 5 practices (Social Scoring, Real-Time Biometric, Predictive Policing, etc.)
- **Auto-registration** — Scan assemblies for AI Act attributes at startup
- **Enforcement modes** — Block, Warn, or Disabled
- **OpenTelemetry** — Tracing, metrics, and structured logging (EventId 9400-9449)
- **Health check** — Registry population, service registration, and enforcement mode verification
- **3 domain notifications** — Reclassification, prohibited use blocked, human oversight required

---

### Data Residency (#405)

**Issue**: [#405 - Data Residency](https://github.com/dlrivada/Encina/issues/405)

Implemented data sovereignty and residency enforcement with GDPR Chapter V (Art. 44-49) cross-border transfer validation, 50+ pre-defined regions, adequacy decisions, and fluent policy configuration across all 10 database providers.

**Package**: `Encina.Compliance.DataResidency`

#### Key Features

- **`[DataResidency]` attribute** — Declarative region assignment for commands/queries
- **`[NoCrossBorderTransfer]` attribute** — Strict prohibition of cross-border data transfers
- **`DataResidencyPipelineBehavior`** — Automatic enforcement in the request pipeline
- **GDPR Chapter V (Art. 44-49) cross-border transfer validation** — 5-step hierarchy for lawful transfers
- **`RegionRegistry`** — 50+ pre-defined regions with jurisdiction metadata
- **`ResidencyPolicyBuilder`** — Fluent API for composing complex residency policies
- **Adequacy decisions** — EU adequacy decision enforcement (Art. 45)
- **SCCs/BCRs** — Standard Contractual Clauses and Binding Corporate Rules support
- **Enforcement modes** — Block, Warn, or Disabled per policy
- **OpenTelemetry** — Tracing and metrics for residency enforcement
- **Health check** — Residency policy health monitoring
- **Audit trail** — Full audit logging of residency decisions
- **13 database providers** — ADO.NET (4), Dapper (4), EF Core (4), MongoDB (1)

---

### Data Retention (#406)

**Issue**: [#406 - Data Retention](https://github.com/dlrivada/Encina/issues/406)

Implemented GDPR Art. 5(1)(e) data retention and automatic deletion with `[RetentionPeriod]` attribute, legal hold support, and retention enforcement across all 10 database providers.

**Package**: `Encina.Compliance.Retention`

#### Key Features

- **`[RetentionPeriod]` attribute** — Declarative retention periods for entities
- **Legal holds** — Suspend deletion for litigation or regulatory holds
- **Automatic enforcement** — Background retention policy execution
- **13 database providers** — ADO.NET (4), Dapper (4), EF Core (4), MongoDB (1)

---

### Anonymization (#407)

**Issue**: [#407 - Anonymization](https://github.com/dlrivada/Encina/issues/407)

Implemented GDPR Art. 4(5) data anonymization, pseudonymization, and tokenization with k-anonymity, l-diversity, t-closeness, and differential privacy across all 10 database providers.

**Package**: `Encina.Compliance.Anonymization`

#### Key Features

- **`[Anonymizable]` attribute** — Declarative anonymization for entity properties
- **k-anonymity, l-diversity, t-closeness** — Statistical privacy guarantees
- **Differential privacy** — Noise injection for aggregate queries
- **Pseudonymization and tokenization** — Reversible and irreversible techniques
- **13 database providers** — ADO.NET (4), Dapper (4), EF Core (4), MongoDB (1)

---

### Secrets Management (#408)

**Issue**: [#408 - Secrets Management](https://github.com/dlrivada/Encina/issues/408)

Implemented multi-cloud secrets management with unified abstraction over Azure Key Vault, AWS Secrets Manager, HashiCorp Vault, and Google Cloud Secret Manager.

**Package**: `Encina.Compliance.Secrets`

#### Key Features

- **Azure Key Vault** integration
- **AWS Secrets Manager** integration
- **HashiCorp Vault** integration
- **Google Cloud Secret Manager** integration
- **Unified abstraction** — Single interface for all providers

---

## February 2026

### Core Security (#394)

**Issue**: [#394 - Core Security](https://github.com/dlrivada/Encina/issues/394)

Implemented core security infrastructure with 7 security attributes, `SecurityPipelineBehavior`, RBAC and permission-based authorization, and OpenTelemetry instrumentation.

**Package**: `Encina.Security`

#### Key Features

- **7 security attributes** — Declarative security enforcement on commands/queries
- **`SecurityPipelineBehavior`** — Automatic authorization enforcement in the request pipeline
- **RBAC** — Role-based access control
- **Permission-based authorization** — Fine-grained permission checks
- **OpenTelemetry** — Tracing and metrics for security operations

---

### GDPR Core - RoPA (#402)

**Issue**: [#402 - GDPR Core (RoPA)](https://github.com/dlrivada/Encina/issues/402)

Implemented GDPR Records of Processing Activities with `[ProcessingActivity]` attribute, `GDPRCompliancePipelineBehavior`, and JSON/CSV export for regulatory reporting.

**Package**: `Encina.Compliance.GDPR`

#### Key Features

- **`[ProcessingActivity]` attribute** — Declarative processing activity registration
- **`GDPRCompliancePipelineBehavior`** — Automatic compliance enforcement
- **RoPA export** — JSON and CSV export for Art. 30 compliance

---

### Consent Management (#403)

**Issue**: [#403 - Consent Management](https://github.com/dlrivada/Encina/issues/403)

Implemented consent management with `[RequireConsent]` attribute, consent versioning, and consent enforcement across all 10 database providers.

**Package**: `Encina.Compliance.Consent`

#### Key Features

- **`[RequireConsent]` attribute** — Declarative consent requirements
- **Consent versioning** — Track consent changes over time
- **13 database providers** — ADO.NET (4), Dapper (4), EF Core (4), MongoDB (1)

---

### Lawful Basis Validation (#413)

**Issue**: [#413 - Lawful Basis Validation](https://github.com/dlrivada/Encina/issues/413)

Implemented `[LawfulBasis]` attribute with Legitimate Interest Assessment (LIA) support, EDPB three-part test, and validation across all 10 database providers.

**Package**: `Encina.Compliance.GDPR`

#### Key Features

- **`[LawfulBasis]` attribute** — Declarative lawful basis assignment
- **Legitimate Interest Assessment (LIA)** — Structured assessment workflow
- **EDPB three-part test** — Purpose, necessity, and balancing tests
- **13 database providers** — ADO.NET (4), Dapper (4), EF Core (4), MongoDB (1)

---

### Data Subject Rights (#404)

**Issue**: [#404 - Data Subject Rights](https://github.com/dlrivada/Encina/issues/404)

Implemented DSR pipeline behavior with restriction enforcement, personal data discovery, erasure (right to be forgotten), and data portability across all 10 database providers.

**Package**: `Encina.Compliance.GDPR`

#### Key Features

- **DSR pipeline behavior** — Automatic data subject request handling
- **Restriction enforcement** — Processing restriction per Art. 18
- **Personal data discovery** — Scan and identify personal data fields
- **Erasure** — Right to be forgotten (Art. 17)
- **Data portability** — Export personal data in machine-readable format (Art. 20)
- **13 database providers** — ADO.NET (4), Dapper (4), EF Core (4), MongoDB (1)
