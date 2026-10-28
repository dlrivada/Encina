# Encina.Security.ABAC.Analyzers

[![NuGet](https://img.shields.io/nuget/v/Encina.Security.ABAC.Analyzers.svg)](https://www.nuget.org/packages/Encina.Security.ABAC.Analyzers/)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](https://github.com/dlrivada/Encina/blob/main/LICENSE)

Roslyn analyzer and code fix that check EEL (Encina Expression Language) expressions written in `[RequireCondition]` attributes while you type, so mistakes show up as compiler diagnostics instead of runtime failures. This package is pre-1.0.

The analyzer inspects the first argument of `Encina.Security.ABAC.RequireConditionAttribute` when it is a constant string. The checks are lightweight, syntax-level heuristics; they do not compile the expression.

## Installation

```bash
dotnet add package Encina.Security.ABAC.Analyzers
```

This is a development dependency: it ships analyzer assemblies only, with no runtime assembly, so it adds nothing to your application's output. Use it together with [`Encina.Security.ABAC`](https://github.com/dlrivada/Encina/tree/main/src/Encina.Security.ABAC), which defines the attribute.

## Diagnostics

| Id | Severity | Title | Message |
| --- | --- | --- | --- |
| EEL001 | Error | Expression is not valid C# syntax | EEL expression contains syntax errors: {0} |
| EEL002 | Warning | Unknown top-level identifier in EEL expression | Unknown identifier '{0}'. EEL expressions can only access: user, resource, environment, action |
| EEL003 | Warning | Expression may not return a boolean value | EEL expression may not evaluate to a boolean. Consider adding a comparison operator |
| EEL004 | Error | Expression is empty or whitespace | EEL expression cannot be empty or whitespace |
| EEL005 | Warning | Expensive operation detected in EEL expression | EEL expression contains potentially expensive operation: {0}. Consider simplifying |
| EEL006 | Error | Statement syntax detected in EEL expression | EEL expressions must be single expressions, not statements. Found: {0} |

All six use the category `Encina.ABAC` and are enabled by default. Their severity can be changed with the usual `.editorconfig` entry, for example `dotnet_diagnostic.EEL005.severity = suggestion`.

What each check looks at:

- **EEL001**: the text is parsed as a C# expression and any parse error is reported.
- **EEL002**: the root of a member-access chain (the `x` in `x.y.z`) must be one of `user`, `resource`, `environment` or `action`.
- **EEL003**: a heuristic that flags top-level expressions that are not comparisons, logical operators, boolean literals, invocations or plain member accesses.
- **EEL004**: the expression is empty or only whitespace. It stops further checks on that attribute.
- **EEL005**: LINQ-style method names (`Select`, `Where`, `Any`, `Count` and similar), lambdas and `new` object creation. Expressions run on every request.
- **EEL006**: a semicolon or a statement keyword (`if`, `for`, `while`, `return`, `var` and similar) in the expression. It stops further checks on that attribute.

### Example

```csharp
[RequireCondition("users.department == \"Finance\"")]   // EEL002: Unknown identifier 'users'
public sealed record ApproveInvoice(Guid InvoiceId) : ICommand<Unit>;

[RequireCondition("user.roles.Any(r => r == \"admin\")")]   // EEL005: expensive operation: Any
public sealed record DeleteInvoice(Guid InvoiceId) : ICommand<Unit>;

[RequireCondition("")]   // EEL004: EEL expression cannot be empty or whitespace
public sealed record ExportInvoices() : IQuery<Unit>;
```

The corrected first expression is `user.department == "Finance"`.

## Code fix

`RequireConditionCodeFixProvider` fixes EEL004 only. For an empty or whitespace expression it offers two actions, `Replace with "true"` and `Replace with "false"`, and supports Fix All in document, project or solution. The other diagnostics have no automatic fix.

## Related documentation

- [Encina.Security.ABAC package README](https://github.com/dlrivada/Encina/tree/main/src/Encina.Security.ABAC) for EEL, the attribute and the policy engine
- [ADR-015: XACML 3.0 ABAC foundation](https://github.com/dlrivada/Encina/blob/main/docs/architecture/adr/015-xacml-3.0-abac-foundation.md)

## License

MIT. See the [LICENSE](https://github.com/dlrivada/Encina/blob/main/LICENSE) file.
