using Encina.Security.ABAC.EEL;

namespace Encina.UnitTests.Security.ABAC.EEL;

/// <summary>
/// Owns one <see cref="EELCompiler"/> shared by every test of the <see cref="Name"/> collection.
/// Each compiled expression emits a Roslyn script assembly that is never unloaded, so building a
/// compiler per test multiplies that cost; the tests that need a cold cache, disposal or a
/// concurrency scenario create their own compiler instead.
/// </summary>
public sealed class EELCompilerFixture : IDisposable
{
    /// <summary>The xUnit collection name that shares this fixture.</summary>
    public const string Name = "EELCompiler";

    /// <summary>The shared compiler. Tests must not dispose it.</summary>
    public EELCompiler Compiler { get; } = new();

    /// <inheritdoc />
    public void Dispose() => Compiler.Dispose();
}

/// <summary>
/// Collection definition that makes the EEL test classes share one <see cref="EELCompilerFixture"/>.
/// </summary>
[CollectionDefinition(EELCompilerFixture.Name)]
public sealed class EELCompilerSharedDefinition : ICollectionFixture<EELCompilerFixture>;
