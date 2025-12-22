using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates",
    Justification = "High-performance logging not critical for event sourcing operations")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el método", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.Marten.AggregateBase.ClearUncommittedEvents")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ámbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.Marten")]
