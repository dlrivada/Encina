using Encina.Compliance.DPIA;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="DefaultDPIATemplateProvider"/> to verify null parameter handling.
/// </summary>
public class DefaultDPIATemplateProviderGuardTests
{
    #region GetTemplateAsync Guards

    /// <summary>
    /// Verifies that GetTemplateAsync throws ArgumentNullException when processingType is null.
    /// </summary>
    [Fact]
    public async Task GetTemplateAsync_NullProcessingType_ThrowsArgumentNullException()
    {
        var sut = new DefaultDPIATemplateProvider();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.GetTemplateAsync(null!));
        ex.ParamName.ShouldBe("processingType");
    }

    #endregion
}
