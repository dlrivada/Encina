using Encina.Compliance.DataSubjectRights;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="HardDeleteErasureStrategy"/> verifying null parameter handling.
/// </summary>
public class HardDeleteErasureStrategyGuardTests
{
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new HardDeleteErasureStrategy(null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    [Fact]
    public async Task EraseFieldAsync_NullLocation_ThrowsArgumentNullException()
    {
        var sut = new HardDeleteErasureStrategy(
            NullLoggerFactory.Instance.CreateLogger<HardDeleteErasureStrategy>());

        var act = () => sut.EraseFieldAsync(null!).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }
}
