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

## Key Capabilities

| Area | Highlights |
|------|-----------|
| **Core** | Railway Oriented Programming, CQRS (`ICommand<T>`, `IQuery<T>`, `INotification`), pipeline behaviors |
| **Database (13 providers)** | ADO.NET, Dapper, EF Core across SQLite/SQL Server/PostgreSQL/MySQL + MongoDB |
| **Caching (8 providers)** | Memory, Hybrid, Redis, Valkey, Dragonfly, Garnet, KeyDB with stampede protection |
| **Messaging (10 transports)** | Outbox, Inbox, Saga, Scheduling across RabbitMQ, Kafka, NATS, Azure Service Bus, SQS, MQTT |
| **Security** | XACML 3.0 ABAC, field-level encryption, PII masking, anti-tampering, read audit |
| **Compliance** | GDPR (Articles 5-49), NIS2, EU AI Act — consent, DSR, breach notification, DPIA, crypto-shredding |
| **Sharding** | Hash, Range, Directory, Geo routing — compound keys, co-location, scatter-gather |
| **CDC** | Real-time change streaming for SQL Server, PostgreSQL, MySQL, MongoDB, Debezium |
| **Event Sourcing** | Marten-based event store with projections, snapshots, GDPR crypto-shredding |
| **Observability** | OpenTelemetry tracing/metrics, structured logging, automatic health checks |

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
