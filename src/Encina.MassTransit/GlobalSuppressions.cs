using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates",
    Justification = "High-performance logging not critical for MassTransit consumer execution")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ámbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.MassTransit")]
