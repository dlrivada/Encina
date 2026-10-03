<!--
title: [DEBT] Remove dead EventStore.Client.Grpc.Streams pin from Directory.Packages.props
labels: technical-debt
milestone: 
-->

## Type

- [ ] Failing tests
- [ ] Missing tests
- [x] Code quality (warnings, analyzers)
- [ ] Performance optimization
- [ ] Refactoring needed
- [ ] Documentation gap
- [ ] Incorrect implementation
- [ ] Other

## Description

The centralized package management file still contains a pin for `EventStore.Client.Grpc.Streams` version 23.3.9, even though commit `87d92a39` deleted every project that referenced it. This results in dead configuration that does not serve a current purpose and may trigger unnecessary flags in dependency vulnerability scans.

## Location

- **File(s)**: `Directory.Packages.props`
- **Package(s)**: N/A

## Current Behavior

`Directory.Packages.props:124-125` pins `EventStore.Client.Grpc.Streams` (`<!-- EventStoreDB --> <PackageVersion Include="EventStore.Client.Grpc.Streams" Version="23.3.9" />`). No project in the repository currently references this package, as `git grep -l "EventStore.Client" -- '*.csproj'` returns no matches.

## Expected Behavior

The `EventStore.Client.Grpc.Streams` entry should be removed from `Directory.Packages.props` because it is dead configuration for a package that is no longer used by any project in the repository.

## Root Cause

The package pin was not removed in the same commit that deleted the projects referencing it (commit `87d92a39`), leaving behind orphaned centralized-package-management configuration.

## Proposed Fix

Remove the `<!-- EventStoreDB --> <PackageVersion Include="EventStore.Client.Grpc.Streams" Version="23.3.9" />` lines from `Directory.Packages.props`.

## Priority

- [ ] **High** - Blocks functionality or causes failures in production
- [ ] **Medium** - Should be fixed before 1.0 release
- [x] **Low** - Nice to have, can be deferred

## Effort Estimate

- [x] Small (< 1 hour)
- [ ] Medium (1-4 hours)
- [ ] Large (> 4 hours)

## Related Issues

- #17