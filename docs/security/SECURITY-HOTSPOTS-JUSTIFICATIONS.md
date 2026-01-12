# SonarCloud Security Hotspots Justifications

This document provides justifications for Security Hotspots identified by SonarCloud that have been reviewed and determined to be safe.

## Overview

| File | Rule | Status | Justification Summary |
|------|------|--------|----------------------|
| SqliteRespawner.cs | S2077 (SQL Injection) | Safe | Test infrastructure only, internal table names |
| link-check.yml | S2612 (Permissions) | Safe | Read-only permissions, no secrets |
| sonarcloud.yml (L26-34) | S2612 (Permissions) | Safe | Build artifacts only, no sensitive data |
| sonarcloud.yml (L91-113) | S2631 (Regex DoS) | Safe | Static patterns, no user input |
| PackageManager.cs (L58) | S4036 (PATH Injection) | Safe | Hardcoded "dotnet" command |
| PackageManager.cs (L105) | S4036 (PATH Injection) | Safe | Hardcoded "dotnet" command |
| PackageManager.cs (L150) | S4036 (PATH Injection) | Safe | Hardcoded "dotnet" command |

---

## Detailed Justifications

### 1. SqliteRespawner.cs - S2077 (SQL Injection)

**File:** `src/Encina.Testing.Respawn/Providers/SqliteRespawner.cs`
**Rule:** S2077 - SQL queries should not be constructed from user input
**Line:** ~45

**Context:**
```csharp
await connection.ExecuteAsync($"DELETE FROM {tableName}");
```

**Justification:**
This code is part of test infrastructure (`Encina.Testing.Respawn`) used exclusively for resetting database state between tests. The `tableName` variable comes from querying SQLite's internal `sqlite_master` table, not from user input. The flow is:

1. Query `sqlite_master` for table names (system-controlled)
2. Filter tables based on configuration
3. Execute DELETE on each table

**Why it's safe:**
- Test infrastructure only - never runs in production
- Table names come from database metadata, not user input
- No external input reaches this code path
- Package is explicitly marked as test-only via NuGet metadata

**Resolution:** Mark as Safe - Test Infrastructure

---

### 2. link-check.yml - S2612 (File Permissions)

**File:** `.github/workflows/link-check.yml`
**Rule:** S2612 - Make sure this permission is safe
**Line:** 17

**Context:**
```yaml
permissions:
    contents: read
```

**Justification:**
This workflow only checks markdown links for validity. It requires `contents: read` to access repository files but performs no write operations.

**Why it's safe:**
- Read-only permission (minimal privilege)
- No secrets or tokens used
- No external code execution
- Only validates URLs in markdown files

**Resolution:** Mark as Safe - Minimal Read-Only Permission

---

### 3. sonarcloud.yml (L26-34) - S2612 (File Permissions)

**File:** `.github/workflows/sonarcloud.yml`
**Rule:** S2612 - Make sure this permission is safe
**Lines:** 26-34

**Context:**
```yaml
- name: Free disk space
  run: |
    sudo rm -rf /usr/share/dotnet/sdk/7.* /usr/share/dotnet/sdk/8.* || true
    sudo rm -rf /usr/local/lib/android || true
    sudo rm -rf /opt/ghc || true
```

**Justification:**
This step frees disk space on the GitHub Actions runner by removing unused SDK versions and tools. This is a common pattern for large .NET solutions that require significant disk space for build and analysis.

**Why it's safe:**
- Only removes pre-installed tools from the ephemeral runner
- Uses `|| true` to handle missing directories gracefully
- No sensitive data involved
- Runner is destroyed after workflow completion
- Standard GitHub Actions pattern documented by GitHub

**Resolution:** Mark as Safe - Standard Runner Cleanup

---

### 4. sonarcloud.yml (L91-113) - S2631 (Regex DoS)

**File:** `.github/workflows/sonarcloud.yml`
**Rule:** S2631 - Regular expressions should not be vulnerable to Denial of Service
**Lines:** 91-113

**Context:**
```yaml
/d:sonar.coverage.exclusions="src/Encina.Testing*/**/*.cs,..."
/d:sonar.cpd.exclusions="src/Encina.Dapper.*/**/*.cs,..."
/d:sonar.issue.ignore.multicriteria.e1.resourceKey="src/Encina.Dapper.*/**/*Store*.cs"
```

**Justification:**
These are SonarCloud configuration patterns for file matching, not runtime regular expressions. They use simple glob patterns processed by SonarCloud's scanner.

**Why it's safe:**
- Static patterns defined at build time
- No user input reaches these patterns
- Patterns are simple globs, not complex regex
- Processed by SonarCloud's file matcher, not a regex engine
- Build-time only, no runtime impact

**Resolution:** Mark as Safe - Static Build Configuration

---

### 5. PackageManager.cs (L58) - S4036 (PATH Injection)

**File:** `src/Encina.Cli/Services/PackageManager.cs`
**Rule:** S4036 - Make sure the PATH is not altered
**Line:** 58

**Context:**
```csharp
var startInfo = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"add \"{project}\" package {packageName}",
    ...
};
```

**Justification:**
This is a CLI tool (`Encina.Cli`) that wraps the `dotnet` CLI for package management. The `FileName = "dotnet"` is hardcoded and relies on the system PATH, which is the expected behavior for CLI tools.

**Why it's safe:**
- `FileName` is hardcoded to "dotnet" - not user-controllable
- CLI tool runs in developer's local environment
- Same security model as running `dotnet` directly
- PATH manipulation would require local system compromise (out of scope)
- Standard pattern for .NET CLI wrapper tools

**Resolution:** Mark as Safe - Hardcoded Command, Developer Tool

---

### 6. PackageManager.cs (L105) - S4036 (PATH Injection)

**File:** `src/Encina.Cli/Services/PackageManager.cs`
**Rule:** S4036 - Make sure the PATH is not altered
**Line:** 105

**Context:**
```csharp
var startInfo = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"remove \"{project}\" package {packageName}",
    ...
};
```

**Justification:**
Same as L58 - this is the `RemovePackageAsync` method with identical security characteristics.

**Why it's safe:**
- Same hardcoded "dotnet" command
- Same CLI tool context
- Same security model

**Resolution:** Mark as Safe - Hardcoded Command, Developer Tool

---

### 7. PackageManager.cs (L150) - S4036 (PATH Injection)

**File:** `src/Encina.Cli/Services/PackageManager.cs`
**Rule:** S4036 - Make sure the PATH is not altered
**Line:** 150

**Context:**
```csharp
var startInfo = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"list \"{project}\" package",
    ...
};
```

**Justification:**
Same as L58 and L105 - this is the `ListPackagesAsync` method with identical security characteristics.

**Why it's safe:**
- Same hardcoded "dotnet" command
- Same CLI tool context
- Read-only operation (listing packages)

**Resolution:** Mark as Safe - Hardcoded Command, Developer Tool

---

## How to Mark as Safe in SonarCloud

1. Go to [SonarCloud Security Hotspots](https://sonarcloud.io/project/security_hotspots?id=dlrivada_Encina)
2. Click on each hotspot
3. Select "Safe" from the review dropdown
4. Copy the corresponding justification from this document
5. Submit the review

## Document History

- **2026-01-12**: Initial documentation of 7 Security Hotspots
