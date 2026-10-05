using Encina.Security.ABAC.EEL;

namespace Encina.UnitTests.Security.ABAC.EEL;

/// <summary>
/// Holds the one <see cref="EELCompiler"/> shared by the EEL unit test classes. It is static so it
/// survives repeated runs in the same process (Stryker's reused test server): each compiled
/// expression emits a Roslyn script assembly that is never unloaded, so a compiler built per run
/// would re-emit them every run. It is never disposed; tests that need a cold cache, disposal or a
/// concurrency scenario create their own compiler.
/// </summary>
internal static class SharedEELCompiler
{
    public static readonly EELCompiler Instance = new();
}
