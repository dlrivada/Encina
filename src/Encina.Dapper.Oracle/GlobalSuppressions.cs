using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "ConfigureAwait(false) is not necessary in console/background services")]
[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates", Justification = "Performance optimization can be done later")]
[assembly: SuppressMessage("Usage", "CA2263:Prefer generic overload when type is known", Justification = "Dynamic type resolution required for serialization")]
[assembly: SuppressMessage("Design", "CA1040:Avoid empty interfaces", Justification = "Marker interfaces are used for compile-time type checking (IIdempotentRequest, IHasNotifications)")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Parameters are validated by the mediator pipeline")]
[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "General exceptions are caught in background services to prevent service crashes")]
[assembly: SuppressMessage("Naming", "CA1725:Parameter names should match base declaration", Justification = "Parameter names are consistent within the implementation and provide better clarity")]
