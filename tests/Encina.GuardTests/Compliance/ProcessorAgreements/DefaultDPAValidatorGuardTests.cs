using Encina.Compliance.ProcessorAgreements;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="DefaultDPAValidator"/> to verify null parameter handling.
/// </summary>
public class DefaultDPAValidatorGuardTests
{
    private readonly IProcessorRegistry _registry = Substitute.For<IProcessorRegistry>();
    private readonly IDPAStore _dpaStore = Substitute.For<IDPAStore>();
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<DefaultDPAValidator> _logger =
        NullLogger<DefaultDPAValidator>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullRegistry_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAValidator(null!, _dpaStore, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("registry");
    }

    [Fact]
    public void Constructor_NullDpaStore_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAValidator(_registry, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dpaStore");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAValidator(_registry, _dpaStore, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new DefaultDPAValidator(_registry, _dpaStore, _timeProvider, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region ValidateAsync Guards

    [Fact]
    public async Task ValidateAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.ValidateAsync(null!));
        ex.ParamName.ShouldBe("processorId");
    }

    #endregion

    #region HasValidDPAAsync Guards

    [Fact]
    public async Task HasValidDPAAsync_NullProcessorId_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.HasValidDPAAsync(null!));
        ex.ParamName.ShouldBe("processorId");
    }

    #endregion

    #region Helpers

    private DefaultDPAValidator CreateSut() =>
        new(_registry, _dpaStore, _timeProvider, _logger);

    #endregion
}
