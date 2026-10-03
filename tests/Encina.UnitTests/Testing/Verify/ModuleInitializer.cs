using System.Runtime.CompilerServices;
using Encina.Testing.Verify;

namespace Encina.UnitTests.Testing.Verify;

/// <summary>
/// Module initializer to configure Verify settings for all tests.
/// </summary>
public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Verify records every snapshot file prefix it has used in a static, process-wide set and
        // throws "The prefix has already been used" when a prefix comes back. A test host that runs
        // the same tests again in the same process (the Stryker MTP test server between mutants)
        // would make every snapshot test fail on its second run, unrelated to the code under test
        // (#1442). The snapshot comparison itself is unaffected by this switch.
        VerifierSettings.DisableRequireUniquePrefix();

        EncinaVerifySettings.Initialize();
    }
}
