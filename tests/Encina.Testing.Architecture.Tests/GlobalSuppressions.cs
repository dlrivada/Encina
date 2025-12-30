// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Test types are intentionally designed as instance methods for architecture testing purposes.
// They don't use instance data because they're just type stubs for rule validation.
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Test types are designed as instance methods for architecture testing", Scope = "namespaceanddescendants", Target = "~N:Encina.Testing.Architecture.Tests.TestTypes")]
