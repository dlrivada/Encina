using System.Diagnostics.CodeAnalysis;

// CA1000: Do not declare static members on generic types
// Justification: Static factory methods on generic types are idiomatic for Strongly-Typed IDs (New(), From(), TryParse())
// This pattern is used by StronglyTypedId, Vogen, and other DDD libraries.
[assembly: SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Static factory methods on strongly-typed IDs are idiomatic DDD pattern", Scope = "type", Target = "~T:Encina.DomainModeling.GuidStronglyTypedId`1")]
[assembly: SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Static factory methods on strongly-typed IDs are idiomatic DDD pattern", Scope = "type", Target = "~T:Encina.DomainModeling.IntStronglyTypedId`1")]
[assembly: SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Static factory methods on strongly-typed IDs are idiomatic DDD pattern", Scope = "type", Target = "~T:Encina.DomainModeling.LongStronglyTypedId`1")]
[assembly: SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Static factory methods on strongly-typed IDs are idiomatic DDD pattern", Scope = "type", Target = "~T:Encina.DomainModeling.StringStronglyTypedId`1")]

// CA1036: Override methods on comparable types
// Justification: Comparison operators are not needed for value objects and strongly-typed IDs in typical DDD usage.
// CompareTo is provided for sorting scenarios, but < > operators are not commonly used with domain types.
[assembly: SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Comparison operators are not needed for DDD value objects and strongly-typed IDs", Scope = "type", Target = "~T:Encina.DomainModeling.StronglyTypedId`1")]
[assembly: SuppressMessage("Design", "CA1036:Override methods on comparable types", Justification = "Comparison operators are not needed for DDD value objects and strongly-typed IDs", Scope = "type", Target = "~T:Encina.DomainModeling.SingleValueObject`1")]

// CA1000: Do not declare static members on generic types
// Justification: Static factory method Empty() on PagedResult<T> is a common pattern for creating empty collections.
[assembly: SuppressMessage("Design", "CA1000:Do not declare static members on generic types", Justification = "Static factory method Empty() is idiomatic for creating empty paged results", Scope = "type", Target = "~T:Encina.DomainModeling.PagedResult`1")]

// CA1848: Use LoggerMessage delegates for performance
// Justification: AdapterBase is a convenience base class. High-performance logging optimization is future work.
// Users can override Execute/ExecuteAsync methods if they need optimized logging.
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "AdapterBase is a convenience class; high-performance logging is future work", Scope = "type", Target = "~T:Encina.DomainModeling.AdapterBase`1")]
