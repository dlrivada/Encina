using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Hangfire job execution requires catching all exceptions to report failures properly")]

[assembly: SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates",
    Justification = "High-performance logging not critical for background job execution")]
