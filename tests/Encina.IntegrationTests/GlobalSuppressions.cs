// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Suppress CA1852 (sealed) for test types as they're not meant to be subclassed
[assembly: SuppressMessage("Performance", "CA1852:Seal internal types", Justification = "Test types don't need to be sealed")]

// Suppress CA1805 (default value initialization) for test code
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "Test code clarity")]
