---
uid: api-home
title: API Reference
---

# Encina API Reference

Complete API reference for all 112 Encina packages, auto-generated from XML documentation comments.

## Navigating the reference

- **Namespaces** — use the left sidebar to browse by namespace (all prefixed with `Encina.*`)
- **Types** — each class, interface, struct, enum and delegate has its own page
- **Members** — methods, properties and fields each have dedicated pages (separate-pages layout)
- **Search** — the top-right search box indexes every public symbol

## Key entry points

Start here if you're new to Encina:

| Namespace | What's inside |
|---|---|
| `Encina` | Core mediator interface (`IEncina`), error types, pipeline abstractions |
| `Encina.Messaging` | Outbox, Inbox, Saga, scheduling abstractions |
| `Encina.DomainModeling` | Repository, Unit of Work, Specification |
| `Encina.Caching` | `ICacheProvider`, hybrid/distributed patterns |
| `Encina.Validation` | `IValidationProvider`, orchestrator pattern |
| `Encina.Testing` | Test fixtures, fakes, fluent assertions |

## Looking for guides instead?

This is the **API reference** (code-level documentation). For narrative content — getting started, tutorials, architectural overviews, ADRs — see the **[main documentation site](https://dlrivada.github.io/Encina/)**.

## Source

All pages are generated from XML `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<example>` comments in the source `.cs` files. If you find a missing or incorrect description, please [open an issue](https://github.com/dlrivada/Encina/issues/new/choose) or edit the source directly.
