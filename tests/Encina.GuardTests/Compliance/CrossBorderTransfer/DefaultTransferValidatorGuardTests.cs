#pragma warning disable CA2012

using Encina.Compliance.CrossBorderTransfer.Abstractions;
using Encina.Compliance.CrossBorderTransfer.Model;
using Encina.Compliance.CrossBorderTransfer.Services;
using Encina.Compliance.DataResidency;

namespace Encina.GuardTests.Compliance.CrossBorderTransfer;

/// <summary>
/// Guard tests for <see cref="DefaultTransferValidator"/> to verify null parameter handling.
/// </summary>
public class DefaultTransferValidatorGuardTests
{
    private readonly IAdequacyDecisionProvider _adequacyProvider = Substitute.For<IAdequacyDecisionProvider>();
    private readonly IApprovedTransferService _transferService = Substitute.For<IApprovedTransferService>();
    private readonly ISCCService _sccService = Substitute.For<ISCCService>();
    private readonly ITIAService _tiaService = Substitute.For<ITIAService>();
    private readonly ILogger<DefaultTransferValidator> _logger = NullLogger<DefaultTransferValidator>.Instance;

    #region Constructor Guards

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when adequacyProvider is null.
    /// </summary>
    [Fact]
    public void Constructor_NullAdequacyProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTransferValidator(
            null!, _transferService, _sccService, _tiaService, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("adequacyProvider");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when transferService is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTransferService_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTransferValidator(
            _adequacyProvider, null!, _sccService, _tiaService, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("transferService");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when sccService is null.
    /// </summary>
    [Fact]
    public void Constructor_NullSccService_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTransferValidator(
            _adequacyProvider, _transferService, null!, _tiaService, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("sccService");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when tiaService is null.
    /// </summary>
    [Fact]
    public void Constructor_NullTiaService_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTransferValidator(
            _adequacyProvider, _transferService, _sccService, null!, _logger);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("tiaService");
    }

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultTransferValidator(
            _adequacyProvider, _transferService, _sccService, _tiaService, null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region ValidateAsync Guards

    /// <summary>
    /// Verifies that ValidateAsync throws ArgumentNullException when request is null.
    /// </summary>
    [Fact]
    public async Task ValidateAsync_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ValidateAsync(null!));
        ex.ParamName.ShouldBe("request");
    }

    #endregion

    #region Helpers

    private DefaultTransferValidator CreateSut() =>
        new(_adequacyProvider, _transferService, _sccService, _tiaService, _logger);

    #endregion
}
