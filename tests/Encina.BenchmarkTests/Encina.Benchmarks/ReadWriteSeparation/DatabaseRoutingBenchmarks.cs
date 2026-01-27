using System.Diagnostics.CodeAnalysis;
using BenchmarkDotNet.Attributes;
using Encina.Messaging.ReadWriteSeparation;

namespace Encina.Benchmarks.ReadWriteSeparation;

/// <summary>
/// Benchmarks for routing context operations measuring AsyncLocal read/write performance
/// and scope lifecycle overhead.
/// </summary>
/// <remarks>
/// <para>
/// Expected performance targets:
/// <list type="bullet">
/// <item>Routing context get: &lt;10ns (AsyncLocal read)</item>
/// <item>Routing scope create/dispose: &lt;100ns (AsyncLocal write + dispose)</item>
/// </list>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "BenchmarkDotNet requires instance methods for benchmarks")]
public class DatabaseRoutingBenchmarks
{
    /// <summary>
    /// Ensures a clean state before each iteration.
    /// </summary>
    [IterationSetup]
    public void IterationSetup()
    {
        DatabaseRoutingContext.Clear();
    }

    /// <summary>
    /// Benchmarks reading the CurrentIntent static property (AsyncLocal read).
    /// </summary>
    [Benchmark(Description = "Read CurrentIntent (AsyncLocal)")]
    public DatabaseIntent? Read_CurrentIntent() => DatabaseRoutingContext.CurrentIntent;

    /// <summary>
    /// Benchmarks reading the EffectiveIntent property (null-coalescing behavior).
    /// </summary>
    [Benchmark(Description = "Read EffectiveIntent (null-coalesce)")]
    public DatabaseIntent Read_EffectiveIntent() => DatabaseRoutingContext.EffectiveIntent;

    /// <summary>
    /// Benchmarks reading HasIntent property.
    /// </summary>
    [Benchmark(Description = "Read HasIntent")]
    public bool Read_HasIntent() => DatabaseRoutingContext.HasIntent;

    /// <summary>
    /// Benchmarks reading IsReadIntent property.
    /// </summary>
    [Benchmark(Description = "Read IsReadIntent")]
    public bool Read_IsReadIntent() => DatabaseRoutingContext.IsReadIntent;

    /// <summary>
    /// Benchmarks reading IsWriteIntent property.
    /// </summary>
    [Benchmark(Description = "Read IsWriteIntent")]
    public bool Read_IsWriteIntent() => DatabaseRoutingContext.IsWriteIntent;

    /// <summary>
    /// Benchmarks ForRead scope creation and disposal.
    /// </summary>
    [Benchmark(Description = "DatabaseRoutingScope.ForRead()")]
    public void Scope_ForRead()
    {
        using var scope = DatabaseRoutingScope.ForRead();
    }

    /// <summary>
    /// Benchmarks ForWrite scope creation and disposal.
    /// </summary>
    [Benchmark(Description = "DatabaseRoutingScope.ForWrite()")]
    public void Scope_ForWrite()
    {
        using var scope = DatabaseRoutingScope.ForWrite();
    }

    /// <summary>
    /// Benchmarks ForForceWrite scope creation and disposal.
    /// </summary>
    [Benchmark(Description = "DatabaseRoutingScope.ForForceWrite()")]
    public void Scope_ForForceWrite()
    {
        using var scope = DatabaseRoutingScope.ForForceWrite();
    }

    /// <summary>
    /// Benchmarks the Clear operation.
    /// </summary>
    [Benchmark(Description = "DatabaseRoutingContext.Clear()")]
    public void Context_Clear()
    {
        DatabaseRoutingContext.Clear();
    }

    /// <summary>
    /// Benchmarks nested scopes to measure restore overhead.
    /// </summary>
    [Benchmark(Description = "Nested scopes (Read → ForceWrite)")]
    public void Scope_Nested()
    {
        using var outerScope = DatabaseRoutingScope.ForRead();
        using var innerScope = DatabaseRoutingScope.ForForceWrite();
    }
}
