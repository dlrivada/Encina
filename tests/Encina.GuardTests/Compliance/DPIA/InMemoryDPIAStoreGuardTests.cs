using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

namespace Encina.GuardTests.Compliance.DPIA;

/// <summary>
/// Guard tests for <see cref="InMemoryDPIAStore"/> to verify null parameter handling.
/// </summary>
public class InMemoryDPIAStoreGuardTests
{
    private readonly InMemoryDPIAStore _store = new(
        NullLogger<InMemoryDPIAStore>.Instance);

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new InMemoryDPIAStore(null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region SaveAssessmentAsync Guards

    /// <summary>
    /// Verifies that SaveAssessmentAsync throws ArgumentNullException when assessment is null.
    /// </summary>
    [Fact]
    public async Task SaveAssessmentAsync_NullAssessment_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.SaveAssessmentAsync(null!));
        ex.ParamName.ShouldBe("assessment");
    }

    #endregion

    #region GetAssessmentAsync Guards

    /// <summary>
    /// Verifies that GetAssessmentAsync throws ArgumentNullException when requestTypeName is null.
    /// </summary>
    [Fact]
    public async Task GetAssessmentAsync_NullRequestTypeName_ThrowsArgumentNullException()
    {
        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await _store.GetAssessmentAsync(null!));
        ex.ParamName.ShouldBe("requestTypeName");
    }

    #endregion
}
