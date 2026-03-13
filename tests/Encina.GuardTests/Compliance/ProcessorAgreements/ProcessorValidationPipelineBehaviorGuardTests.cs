using Encina.Compliance.ProcessorAgreements;

namespace Encina.GuardTests.Compliance.ProcessorAgreements;

/// <summary>
/// Guard tests for <see cref="ProcessorValidationPipelineBehavior{TRequest, TResponse}"/>
/// to verify null parameter handling.
/// </summary>
public class ProcessorValidationPipelineBehaviorGuardTests
{
    private readonly IDPAValidator _validator = Substitute.For<IDPAValidator>();
    private readonly IProcessorAuditStore _auditStore = Substitute.For<IProcessorAuditStore>();
    private readonly IOptions<ProcessorAgreementOptions> _options = Options.Create(new ProcessorAgreementOptions());
    private readonly TimeProvider _timeProvider = TimeProvider.System;
    private readonly ILogger<ProcessorValidationPipelineBehavior<TestCommand, string>> _logger =
        NullLogger<ProcessorValidationPipelineBehavior<TestCommand, string>>.Instance;

    #region Constructor Guards

    [Fact]
    public void Constructor_NullValidator_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<TestCommand, string>(
            null!, _auditStore, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("validator");
    }

    [Fact]
    public void Constructor_NullAuditStore_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<TestCommand, string>(
            _validator, null!, _options, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("auditStore");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<TestCommand, string>(
            _validator, _auditStore, null!, _timeProvider, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullTimeProvider_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<TestCommand, string>(
            _validator, _auditStore, _options, null!, _logger);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("timeProvider");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ProcessorValidationPipelineBehavior<TestCommand, string>(
            _validator, _auditStore, _options, _timeProvider, null!);

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
        new(_validator, _auditStore, _options, _timeProvider, _logger);

    public sealed record TestCommand : ICommand<string>;

    #endregion
}
