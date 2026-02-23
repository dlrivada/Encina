using Encina.Compliance.Consent;

namespace Encina.GuardTests.Compliance.Consent;

/// <summary>
/// Guard tests for <see cref="ConsentRequiredPipelineBehavior{TRequest, TResponse}"/>
/// to verify null parameter handling in constructor and Handle method.
/// </summary>
public class ConsentRequiredPipelineBehaviorGuardTests
{
    private readonly IConsentValidator _validator;
    private readonly IOptions<ConsentOptions> _options;
    private readonly ILogger<ConsentRequiredPipelineBehavior<TestConsentRequest, string>> _logger;

    public ConsentRequiredPipelineBehaviorGuardTests()
    {
        _validator = Substitute.For<IConsentValidator>();
        _options = Options.Create(new ConsentOptions());
        _logger = NullLogger<ConsentRequiredPipelineBehavior<TestConsentRequest, string>>.Instance;
    }

    #region Constructor Guard Tests

    [Fact]
    public void Constructor_NullValidator_ThrowsArgumentNullException()
    {
        var act = () => new ConsentRequiredPipelineBehavior<TestConsentRequest, string>(
            null!,
            _options,
            _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("validator");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ConsentRequiredPipelineBehavior<TestConsentRequest, string>(
            _validator,
            null!,
            _logger);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ConsentRequiredPipelineBehavior<TestConsentRequest, string>(
            _validator,
            _options,
            null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("logger");
    }

    #endregion

    #region Handle Guard Tests

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();
        var context = CreateContext();
        TestConsentRequest request = null!;

        var act = () => behavior.Handle(
            request,
            context,
            () => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("ok")),
            CancellationToken.None).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe(nameof(request));
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();
        var request = new TestConsentRequest("user-1");
        IRequestContext context = null!;

        var act = () => behavior.Handle(
            request,
            context,
            () => ValueTask.FromResult(LanguageExt.Prelude.Right<EncinaError, string>("ok")),
            CancellationToken.None).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe(nameof(context));
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        var behavior = CreateBehavior();
        var request = new TestConsentRequest("user-1");
        var context = CreateContext();

        var act = () => behavior.Handle(
            request,
            context,
            null!,
            CancellationToken.None).AsTask();
        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Helpers

    private ConsentRequiredPipelineBehavior<TestConsentRequest, string> CreateBehavior()
    {
        return new ConsentRequiredPipelineBehavior<TestConsentRequest, string>(
            _validator,
            _options,
            _logger);
    }

    private static IRequestContext CreateContext()
    {
        var context = Substitute.For<IRequestContext>();
        context.UserId.Returns("user-1");
        context.TenantId.Returns("tenant-1");
        context.CorrelationId.Returns("corr-123");
        return context;
    }

    /// <summary>
    /// Test request type for pipeline behavior guard tests.
    /// </summary>
    private sealed record TestConsentRequest(string UserId) : IRequest<string>;

    #endregion
}
