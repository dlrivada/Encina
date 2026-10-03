using VerifyXunit;

namespace Encina.UnitTests.Testing.Verify;

/// <summary>
/// Regression tests for #1442: snapshot tests must pass when a test host runs them again in the
/// same process, as the Stryker MTP test server does between mutants.
/// </summary>
public class VerifyRerunTests
{
    [Fact]
    public async Task Verify_SameSnapshotPrefixTwiceInOneProcess_PassesBothTimes()
    {
        // Arrange - a second run of a test in a reused process verifies the same file prefix again
        const string snapshot = "Snapshot verified twice in the same process";

        // Act - first run, then the rerun with the same prefix
        await Verifier.Verify(snapshot);
        var rerun = await Record.ExceptionAsync(() => Verifier.Verify(snapshot).ToTask());

        // Assert - the rerun compares against the verified file instead of rejecting the prefix
        rerun.ShouldBeNull();
    }
}
