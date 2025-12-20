// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// CA2263: Contract tests need to verify exact Type values, not generic overloads
[assembly: SuppressMessage("Performance", "CA2263:Prefer generic overload when type is known", Justification = "Contract tests intentionally verify Type values", Scope = "namespaceanddescendants", Target = "~N:SimpleMediator.Refit.ContractTests")]
