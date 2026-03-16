using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="ProcessorValidationPipelineBehavior{TRequest, TResponse}"/>
/// to verify null parameter handling.
/// </summary>
public class ProcessorValidationPipelineBehaviorGuardTests
{
    private readonly IDPAService _dpaService = Substitute.For<IDPAService>();
    private readonly IOptions<ProcessorAgreementOptions> _options = Options.Create(new ProcessorAgreementOptions());
    private readonly ILogger<ProcessorValidationPipelineBehavior<TestCommand, string>> _logger =
        NullLogger<ProcessorValidationPipelineBehavior<TestCommand, string>>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullDPAService_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<TestCommand, string>(
            null!, _options, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("dpaService");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<TestCommand, string>(
            _dpaService, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<TestCommand, string>(
            _dpaService, _options, null!);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region Handle Guards

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var context = Substitute.For<IRequestContext>();
        var nextStep = Substitute.For<RequestHandlerCallback<string>>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(null!, context, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var request = new TestCommand();
        var nextStep = Substitute.For<RequestHandlerCallback<string>>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, null!, nextStep, CancellationToken.None));
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        var sut = CreateSut();
        var request = new TestCommand();
        var context = Substitute.For<IRequestContext>();

        var ex = await Should.ThrowAsync<ArgumentNullException>(
            async () => await sut.Handle(request, context, null!, CancellationToken.None));
        ex.ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Helpers

    private ProcessorValidationPipelineBehavior<TestCommand, string> CreateSut() =>
        new(_dpaService, _options, _logger);

    public sealed record TestCommand : ICommand<string>;

    #endregion
}
