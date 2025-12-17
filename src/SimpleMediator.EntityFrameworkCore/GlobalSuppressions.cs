// This file is used by Code Analysis to maintain SuppressMessage attributes that are applied to this project.

using System.Diagnostics.CodeAnalysis;

// Suppress CA1848 for now - LoggerMessage delegates will be implemented in a future iteration
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Will be implemented in future iteration for performance optimization", Scope = "module")]

// Suppress CA2263 for now - Generic overload preferred but Type parameter needed for dynamic serialization
[assembly: SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Type parameter needed for dynamic serialization scenarios", Scope = "module")]
