using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates",
    Justification = "High-performance logging not critical for MassTransit consumer execution")]
