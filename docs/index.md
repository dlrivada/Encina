---
title: Home
layout: default
nav_order: 1
---

# Encina

**Build resilient, regulation-ready .NET 10 applications with Railway Oriented Programming.**
{: .fs-6 .fw-300 }

[![.NET CI](https://github.com/dlrivada/Encina/actions/workflows/ci.yml/badge.svg)](https://github.com/dlrivada/Encina/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/dlrivada/Encina/graph/badge.svg)](https://codecov.io/gh/dlrivada/Encina)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://github.com/dlrivada/Encina/blob/main/LICENSE)
![.NET 10.0](https://img.shields.io/badge/.NET-10.0-512BD4.svg)
![Status](https://img.shields.io/badge/status-pre--1.0-blue.svg)

> **112 packages** · **13,000+ tests** · **13 database providers** · **8 cache providers** · **10 messaging transports** · **15 compliance modules**

---

## What is Encina?

Encina is a comprehensive toolkit for building robust .NET 10 applications. Built on [LanguageExt](https://github.com/louthy/language-ext), it provides explicit error handling through `Either<EncinaError, T>`, CQRS patterns, enterprise messaging, multi-provider database access, built-in GDPR/NIS2/AI Act compliance, and composable pipeline behaviors — all opt-in, pay-for-what-you-use.

## Quick Start

```bash
dotnet add package Encina
```

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEncina(typeof(Program).Assembly);
var app = builder.Build();

app.MapPost("/orders", async (IEncina encina, CancellationToken ct) =>
{
    var result = await encina.Send(new CreateOrder(Guid.NewGuid(), 99.99m), ct);
    return result.Match(
        Left: error => Results.BadRequest(error),
        Right: orderId => Results.Created($"/orders/{orderId}", orderId));
});

app.Run();
```

```csharp
public sealed record CreateOrder(Guid CustomerId, decimal Amount) : ICommand<OrderId>;
```

## Packages (112)

| Category | # | Highlights |
|----------|--:|-----------|
| **Core** | 9 | ROP foundations, CQRS, pipeline behaviors, DomainModeling, GuardClauses, AspNetCore, SignalR, gRPC, GraphQL, Refit, CLI |
| **Database Providers** | 13 | ADO.NET, Dapper, EF Core across SQLite / SQL Server / PostgreSQL / MySQL + MongoDB. Same interfaces, swap with one line of DI |
| **Caching** | 8 | Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB — stampede protection, tag invalidation, eager refresh |
| **Messaging & Transports** | 10 | Outbox, Inbox, Saga, Scheduling + RabbitMQ, Kafka, NATS, Azure Service Bus, Amazon SQS, Redis PubSub, MQTT, InMemory |
| **Message Encryption** | 4 | Encryption abstractions + Data Protection API, AWS KMS, Azure Key Vault |
| **Security** | 13 | XACML 3.0 ABAC with EEL, Roslyn analyzers, field-level encryption, PII masking, sanitization, anti-tampering, read audit |
| **Secrets Management** | 5 | Reader/writer abstractions + AWS Secrets Manager, Azure Key Vault, Google Secret Manager, HashiCorp Vault |
| **Compliance — GDPR** | 11 | Core GDPR, consent, lawful basis, DSR, anonymization, data residency, cross-border transfer, DPIA, retention, privacy by design, processor agreements |
| **Compliance — Other** | 4 | Breach notification, attestation, NIS2 Directive, EU AI Act |
| **Change Data Capture** | 6 | CDC abstractions + SQL Server, PostgreSQL, MySQL, MongoDB, Debezium |
| **Database Sharding** | — | Hash, Range, Directory, Geo routing — compound keys, co-location, time-based tiering, shadow sharding, scatter-gather |
| **Infrastructure** | 12 | Distributed locks (InMemory, Redis, SqlServer), ID generation (GUID, Snowflake, Ulid), Hangfire, Quartz, Polly, resilience, OpenTelemetry, multi-tenancy |
| **Validation** | 3 | FluentValidation, DataAnnotations, MiniValidator — all via centralized Orchestrator |
| **Event Sourcing** | 2 | Marten event store with projections, snapshots, GDPR crypto-shredding |
| **Cloud & Serverless** | 3 | AWS Lambda, Azure Functions, .NET Aspire testing |
| **Testing** | 12 | Core fixtures, fakes, Respawn, WireMock, Shouldly, Verify, Bogus, FsCheck, ArchUnitNET, Testcontainers, TUnit, Pact |

## Quality & Coverage

- [Coverage Dashboard](https://dlrivada.github.io/Encina/coverage/) — Per-package weighted coverage metrics
- [Codecov](https://codecov.io/gh/dlrivada/Encina) — Per-module thresholds
- [SonarCloud](https://sonarcloud.io/summary/new_code?id=dlrivada_Encina) — Static analysis

## Resources

- [GitHub Repository](https://github.com/dlrivada/Encina)
- [README](https://github.com/dlrivada/Encina#readme)
- [Changelog](https://github.com/dlrivada/Encina/blob/main/CHANGELOG.md)
- [Contributing Guide](https://github.com/dlrivada/Encina/blob/main/CONTRIBUTING.md)
- [Roadmap](https://github.com/dlrivada/Encina/blob/main/ROADMAP.md)
