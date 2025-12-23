using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031:Do not catch general exception types",
    Justification = "Quartz job execution requires catching all exceptions to report failures properly")]

[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ambito de bloque", Justification = "File-scoped namespaces preferred", Scope = "namespace", Target = "~N:Encina.Quartz")]
