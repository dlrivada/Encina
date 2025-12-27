using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Performance",
    "CA1848:Use the LoggerMessage delegates",
    Justification = "LoggerMessage source generator is used via Log.cs partial class")]
