using System.Runtime.CompilerServices;
using Encina.Testing.Verify;

namespace Encina.Testing.Verify.Tests;

/// <summary>
/// Module initializer to configure Verify settings for all tests.
/// </summary>
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        EncinaVerifySettings.Initialize();
    }
}
