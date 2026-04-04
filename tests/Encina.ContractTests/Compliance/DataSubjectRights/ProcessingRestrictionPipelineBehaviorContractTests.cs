#pragma warning disable CA2012

using Encina.Compliance.DataSubjectRights;
using Encina.Compliance.DataSubjectRights.Abstractions;
using Encina.Compliance.GDPR;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using static LanguageExt.Prelude;

namespace Encina.ContractTests.Compliance.DataSubjectRights;

/// <summary>
/// Behavioral contract tests for <see cref="ProcessingRestrictionPipelineBehavior{TRequest, TResponse}"/>.
/// Validates that the behavior follows its documented contract: disabled mode skips,
/// unrestricted subjects pass through, restricted subjects are blocked.
/// </summary>
public class ProcessingRestrictionPipelineBehaviorContractTests
{
    // Test request types for contract verification
    [RestrictProcessing(SubjectIdProperty = nameof(CustomerId))]
    private sealed record RestrictedCommand(string CustomerId) : IRequest<Unit>;

    private sealed record UnmarkedCommand(string SubjectId) : IRequest<Unit>;

    private static IDSRService CreateDsrServiceWithRestriction(string subjectId, bool isRestricted)
    {
        var dsrService = Substitute.For<IDSRService>();
        dsrService.HasActiveRestrictionAsync(subjectId, Arg.Any<CancellationToken>())
            .Returns(callInfo =>
                new ValueTask<Either<EncinaError, bool>>(Right<EncinaError, bool>(isRestricted)));
        return dsrService;
    }

    #region Contract: Disabled mode skips all checks

    [Fact]
    public async Task Contract_DisabledMode_ShouldSkipRestrictionCheck()
    {
        // Arrange
        var dsrService = Substitute.For<IDSRService>();
        var extractor = Substitute.For<IDataSubjectIdExtractor>();
        var options = Options.Create(new DataSubjectRightsOptions
        {
            RestrictionEnforcementMode = DSREnforcementMode.Disabled
        });
        var logger = NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>>();

        var behavior = new ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>(
            dsrService, extractor, options, logger);

        var request = new RestrictedCommand("subject-1");
        var context = Substitute.For<IRequestContext>();
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            request,
            context,
            () =>
            {
                nextCalled = true;
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Disabled mode should allow processing");
        nextCalled.ShouldBeTrue("Disabled mode should call next step");
        await dsrService.DidNotReceive().HasActiveRestrictionAsync(
            Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Contract: Unmarked requests pass through without restriction check

    [Fact]
    public async Task Contract_UnmarkedRequest_ShouldPassThrough()
    {
        // Arrange
        var dsrService = Substitute.For<IDSRService>();
        var extractor = Substitute.For<IDataSubjectIdExtractor>();
        var options = Options.Create(new DataSubjectRightsOptions
        {
            RestrictionEnforcementMode = DSREnforcementMode.Block
        });
        var logger = NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<UnmarkedCommand, Unit>>();

        var behavior = new ProcessingRestrictionPipelineBehavior<UnmarkedCommand, Unit>(
            dsrService, extractor, options, logger);

        var request = new UnmarkedCommand("subject-1");
        var context = Substitute.For<IRequestContext>();
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            request,
            context,
            () =>
            {
                nextCalled = true;
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Unmarked request should pass through");
        nextCalled.ShouldBeTrue("Unmarked request should call next step");
    }

    #endregion

    #region Contract: Restricted subject in Block mode returns error

    [Fact]
    public async Task Contract_RestrictedSubject_BlockMode_ShouldReturnError()
    {
        // Arrange
        var dsrService = CreateDsrServiceWithRestriction("subject-1", true);
        var extractor = Substitute.For<IDataSubjectIdExtractor>();
        var options = Options.Create(new DataSubjectRightsOptions
        {
            RestrictionEnforcementMode = DSREnforcementMode.Block
        });
        var logger = NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>>();

        var behavior = new ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>(
            dsrService, extractor, options, logger);

        var request = new RestrictedCommand("subject-1");
        var context = Substitute.For<IRequestContext>();
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            request,
            context,
            () =>
            {
                nextCalled = true;
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
            },
            CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue("Block mode should return error for restricted subject");
        nextCalled.ShouldBeFalse("Block mode should NOT call next step for restricted subject");
    }

    #endregion

    #region Contract: Unrestricted subject passes through

    [Fact]
    public async Task Contract_UnrestrictedSubject_ShouldPassThrough()
    {
        // Arrange
        var dsrService = CreateDsrServiceWithRestriction("subject-1", false);
        var extractor = Substitute.For<IDataSubjectIdExtractor>();
        var options = Options.Create(new DataSubjectRightsOptions
        {
            RestrictionEnforcementMode = DSREnforcementMode.Block
        });
        var logger = NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>>();

        var behavior = new ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>(
            dsrService, extractor, options, logger);

        var request = new RestrictedCommand("subject-1");
        var context = Substitute.For<IRequestContext>();
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            request,
            context,
            () =>
            {
                nextCalled = true;
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Unrestricted subject should pass through");
        nextCalled.ShouldBeTrue("Unrestricted subject should call next step");
    }

    #endregion

    #region Contract: Warn mode logs but allows processing

    [Fact]
    public async Task Contract_RestrictedSubject_WarnMode_ShouldAllowProcessing()
    {
        // Arrange
        var dsrService = CreateDsrServiceWithRestriction("subject-1", true);
        var extractor = Substitute.For<IDataSubjectIdExtractor>();
        var options = Options.Create(new DataSubjectRightsOptions
        {
            RestrictionEnforcementMode = DSREnforcementMode.Warn
        });
        var logger = NullLoggerFactory.Instance.CreateLogger<ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>>();

        var behavior = new ProcessingRestrictionPipelineBehavior<RestrictedCommand, Unit>(
            dsrService, extractor, options, logger);

        var request = new RestrictedCommand("subject-1");
        var context = Substitute.For<IRequestContext>();
        var nextCalled = false;

        // Act
        var result = await behavior.Handle(
            request,
            context,
            () =>
            {
                nextCalled = true;
                return new ValueTask<Either<EncinaError, Unit>>(Right<EncinaError, Unit>(unit));
            },
            CancellationToken.None);

        // Assert
        result.IsRight.ShouldBeTrue("Warn mode should allow processing despite restriction");
        nextCalled.ShouldBeTrue("Warn mode should call next step");
    }

    #endregion
}
