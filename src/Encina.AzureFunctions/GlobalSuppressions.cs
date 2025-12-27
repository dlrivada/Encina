// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// CA1848: For improved performance, use the LoggerMessage delegates instead of calling LoggerMessage.Define
// Suppressed because we already use source-generated LoggerMessage in Log.cs
[assembly: SuppressMessage(
    "Performance",
    "CA1848:Use the LoggerMessage delegates",
    Justification = "Using source-generated LoggerMessage delegates",
    Scope = "namespaceanddescendants",
    Target = "~N:Encina.AzureFunctions")]

// CA1000: Do not declare static members on generic types
// Suppressed for ActivityResult<T> factory methods - commonly used pattern for Result types
[assembly: SuppressMessage(
    "Design",
    "CA1000:Do not declare static members on generic types",
    Justification = "Factory pattern for ActivityResult<T> is idiomatic and improves usability",
    Scope = "type",
    Target = "~T:Encina.AzureFunctions.Durable.ActivityResult`1")]
