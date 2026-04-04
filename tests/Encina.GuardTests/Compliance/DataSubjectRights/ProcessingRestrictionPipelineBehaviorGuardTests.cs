using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.GuardTests.Compliance.DataSubjectRights;

/// <summary>
/// Guard tests for <see cref="ProcessingRestrictionPipelineBehavior{TRequest, TResponse}"/>
/// verifying null parameter handling in constructor and Handle method.
/// </summary>
public class ProcessingRestrictionPipelineBehaviorGuardTests
{
    private sealed record TestCommand(string SubjectId) : IRequest<Unit>;

    private readonly IDSRService _dsrService = Substitute.For<IDSRService>();
    private readonly IDataSubjectIdExtractor _extractor = Substitute.For<IDataSubjectIdExtractor>();
    private readonly IOptions<DataSubjectRightsOptions> _options = Options.Create(new DataSubjectRightsOptions());

    #region Constructor Guards

    [Fact]
    public void Constructor_NullDsrService_ThrowsArgumentNullException()
    {
        var act = () => new ProcessingRestrictionPipelineBehavior<TestCommand, Unit>(
            null!,
            _extractor,
            _options,
            NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<TestCommand, Unit>>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("dsrService");
    }

    [Fact]
    public void Constructor_NullExtractor_ThrowsArgumentNullException()
    {
        var act = () => new ProcessingRestrictionPipelineBehavior<TestCommand, Unit>(
            _dsrService,
            null!,
            _options,
            NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<TestCommand, Unit>>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("subjectIdExtractor");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        var act = () => new ProcessingRestrictionPipelineBehavior<TestCommand, Unit>(
            _dsrService,
            _extractor,
            null!,
            NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<TestCommand, Unit>>());

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("options");
    }

    [Fact]
    public void Constructor_NullLogger_ThrowsArgumentNullException()
    {
        var act = () => new ProcessingRestrictionPipelineBehavior<TestCommand, Unit>(
            _dsrService,
            _extractor,
            _options,
            null!);

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("logger");
    }

    #endregion

    #region Handle Guards

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var behavior = new ProcessingRestrictionPipelineBehavior<TestCommand, Unit>(
            _dsrService,
            _extractor,
            _options,
            NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<TestCommand, Unit>>());

        var act = () => behavior.Handle(
            null!,
            Substitute.For<IRequestContext>(),
            () => ValueTask.FromResult<Either<EncinaError, Unit>>(Unit.Default),
            CancellationToken.None).AsTask();

        await Should.ThrowAsync<ArgumentNullException>(act);
    }

    #endregion
}
