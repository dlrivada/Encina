<!--
title: [DEBT] docs/messaging/transports.md GraphQL sample chains AddQueryType on IServiceCollection and does not compile
labels: technical-debt
milestone:
kind: docs
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [ ] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [x] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

The GraphQL sample in `docs/messaging/transports.md` chains `AddQueryType` and `AddMutationType` onto the result of `AddEncinaGraphQL()`, which is an `IServiceCollection`. The sample does not compile.

## Location

- **File(s)**: `docs/messaging/transports.md:462-464`; `src/Encina.GraphQL/ServiceCollectionExtensions.cs:17`
- **Package(s)**: Encina.GraphQL

## Current Behavior

Lines 462-464 document `services.AddEncinaGraphQL().AddQueryType<QueryRoot>().AddMutationType<MutationRoot>();`. `AddEncinaGraphQL` returns `IServiceCollection` (`src/Encina.GraphQL/ServiceCollectionExtensions.cs:17`), which has no `AddQueryType` or `AddMutationType` members. Those belong to HotChocolate's `IRequestExecutorBuilder`, obtained from `services.AddGraphQLServer()`, a call the page never shows.

## Expected Behavior

The sample registers the Encina bridge with `AddEncinaGraphQL()` and configures the HotChocolate server through `services.AddGraphQLServer()` before adding the query and mutation types.

## Root Cause

The sample treated `AddEncinaGraphQL` as if it returned the HotChocolate builder.

## Proposed Fix

Rewrite lines 462-464 as two calls: `services.AddEncinaGraphQL();` and `services.AddGraphQLServer().AddQueryType<QueryRoot>().AddMutationType<MutationRoot>();`.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [x] **Medium** - Should be fixed before 1.0 release
- [ ] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #18 (This issue)
