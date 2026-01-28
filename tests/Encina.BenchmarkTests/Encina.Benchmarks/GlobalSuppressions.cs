// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el método", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.Benchmarks.Infrastructure.DapperTypeHandlers.DateTimeTypeHandler.Parse(System.Object)~System.DateTime")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el método", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.Benchmarks.Infrastructure.DapperTypeHandlers.GuidTypeHandler.Parse(System.Object)~System.Guid")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el método", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.Benchmarks.ValidationBenchmarks.DataAnnotationsCommandHandler.Handle(Encina.Benchmarks.ValidationBenchmarks.DataAnnotationsCommand,System.Threading.CancellationToken)~System.Threading.Tasks.Task{LanguageExt.Either{Encina.EncinaError,System.Guid}}")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el método", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.Benchmarks.ValidationBenchmarks.FluentCommandHandler.Handle(Encina.Benchmarks.ValidationBenchmarks.FluentCommand,System.Threading.CancellationToken)~System.Threading.Tasks.Task{LanguageExt.Either{Encina.EncinaError,System.Guid}}")]
[assembly: SuppressMessage("Style", "IDE0022:Usar cuerpo del bloque para el método", Justification = "<pendiente>", Scope = "member", Target = "~M:Encina.Benchmarks.ValidationBenchmarks.MiniValidatorCommandHandler.Handle(Encina.Benchmarks.ValidationBenchmarks.MiniValidatorCommand,System.Threading.CancellationToken)~System.Threading.Tasks.Task{LanguageExt.Either{Encina.EncinaError,System.Guid}}")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ámbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.Benchmarks")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ámbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.Benchmarks.Inbox")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ámbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.Benchmarks.Infrastructure")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ámbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.Benchmarks.Outbox")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ámbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.Benchmarks.ProviderComparison")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ámbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.Benchmarks.EntityFrameworkCore")]
[assembly: SuppressMessage("Style", "IDE0160:Convertir en namespace con ámbito de bloque", Justification = "<pendiente>", Scope = "namespace", Target = "~N:Encina.Benchmarks.EntityFrameworkCore.Infrastructure")]

// BenchmarkDotNet CA1001 Suppression Pattern:
// Benchmark classes with IDisposable fields (DbContext, SqliteConnection, etc.) are managed
// by BenchmarkDotNet's lifecycle methods ([GlobalSetup], [GlobalCleanup]). Implementing IDisposable
// would interfere with BenchmarkDotNet's own resource management. Instead, cleanup is handled
// explicitly in [GlobalCleanup] methods. This is the standard pattern used throughout all
// BenchmarkDotNet benchmark suites.
