#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.DPIA;
using Encina.Compliance.DPIA.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.DPIA;

/// <summary>
/// Unit tests for <see cref="DPIARequiredPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class DPIARequiredPipelineBehaviorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IDPIAStore _store = Substitute.For<IDPIAStore>();
    private readonly FakeTimeProvider _timeProvider = new(FixedNow);
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();

    private DPIARequiredPipelineBehavior<TestCommandWithDPIA, string> CreateSut(
        DPIAOptions? options = null)
    {
        var opts = Options.Create(options ?? new DPIAOptions());
        var logger = NullLogger<DPIARequiredPipelineBehavior<TestCommandWithDPIA, string>>.Instance;
        return new DPIARequiredPipelineBehavior<TestCommandWithDPIA, string>(
            _store, opts, _timeProvider, logger);
    }

    private DPIARequiredPipelineBehavior<TestCommandWithoutDPIA, string> CreateSutWithoutAttribute(
        DPIAOptions? options = null)
    {
        var opts = Options.Create(options ?? new DPIAOptions());
        var logger = NullLogger<DPIARequiredPipelineBehavior<TestCommandWithoutDPIA, string>>.Instance;
        return new DPIARequiredPipelineBehavior<TestCommandWithoutDPIA, string>(
            _store, opts, _timeProvider, logger);
    }

    private static RequestHandlerCallback<string> SuccessNext()
        => () => ValueTask.FromResult<Either<EncinaError, string>>("handler-result");

    private static RequestHandlerCallback<string> FailNext()
        => () => throw new InvalidOperationException("nextStep should not be called");

    private static DPIAAssessment CreateApprovedAssessment(
        DateTimeOffset? nextReviewAtUtc = null) => new()
        {
            Id = Guid.NewGuid(),
            RequestTypeName = typeof(TestCommandWithDPIA).FullName!,
            Status = DPIAAssessmentStatus.Approved,
            CreatedAtUtc = FixedNow.AddDays(-30),
            ApprovedAtUtc = FixedNow.AddDays(-5),
            NextReviewAtUtc = nextReviewAtUtc,
        };

    #region Disabled Enforcement Mode Tests

    [Fact]
    public async Task Handle_DisabledMode_SkipsAllChecksAndCallsNext()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Disabled });

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
        ((string)result).Should().Be("handler-result");
        await _store.DidNotReceive().GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region No Attribute Tests

    [Fact]
    public async Task Handle_NoAttribute_SkipsChecksAndCallsNext()
    {
        var sut = CreateSutWithoutAttribute();

        var result = await sut.Handle(
            new TestCommandWithoutDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
        ((string)result).Should().Be("handler-result");
        await _store.DidNotReceive().GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Block Mode — No Assessment Tests

    [Fact]
    public async Task Handle_BlockMode_NoAssessment_ReturnsError()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(None));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().Should().Be(DPIAErrors.AssessmentRequiredCode);
    }

    #endregion

    #region Block Mode — Not Approved Tests

    [Theory]
    [InlineData(DPIAAssessmentStatus.Draft)]
    [InlineData(DPIAAssessmentStatus.InReview)]
    [InlineData(DPIAAssessmentStatus.RequiresRevision)]
    [InlineData(DPIAAssessmentStatus.Expired)]
    public async Task Handle_BlockMode_NotApproved_ReturnsError(DPIAAssessmentStatus status)
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var assessment = CreateApprovedAssessment() with { Status = status };
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BlockMode_Rejected_ReturnsRejectedError()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var assessment = CreateApprovedAssessment() with { Status = DPIAAssessmentStatus.Rejected };
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().Should().Be(DPIAErrors.AssessmentRejectedCode);
    }

    #endregion

    #region Block Mode — Expired Assessment Tests

    [Fact]
    public async Task Handle_BlockMode_ExpiredReview_ReturnsError()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var assessment = CreateApprovedAssessment(nextReviewAtUtc: FixedNow.AddDays(-1));
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().Should().Be(DPIAErrors.AssessmentExpiredCode);
    }

    [Fact]
    public async Task Handle_BlockMode_ExactReviewDate_ReturnsError()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var assessment = CreateApprovedAssessment(nextReviewAtUtc: FixedNow);
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().Should().Be(DPIAErrors.AssessmentExpiredCode);
    }

    #endregion

    #region Block Mode — Valid Assessment Tests

    [Fact]
    public async Task Handle_BlockMode_ValidAssessment_CallsNext()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var assessment = CreateApprovedAssessment(nextReviewAtUtc: FixedNow.AddDays(30));
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
        ((string)result).Should().Be("handler-result");
    }

    [Fact]
    public async Task Handle_BlockMode_NoReviewDate_CallsNext()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var assessment = CreateApprovedAssessment(nextReviewAtUtc: null);
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Warn Mode Tests

    [Fact]
    public async Task Handle_WarnMode_NoAssessment_WarnsAndCallsNext()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Warn });
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(None));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
        ((string)result).Should().Be("handler-result");
    }

    [Fact]
    public async Task Handle_WarnMode_NotApproved_WarnsAndCallsNext()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Warn });
        var assessment = CreateApprovedAssessment() with { Status = DPIAAssessmentStatus.Draft };
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WarnMode_Expired_WarnsAndCallsNext()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Warn });
        var assessment = CreateApprovedAssessment(nextReviewAtUtc: FixedNow.AddDays(-1));
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Store Error Tests

    [Fact]
    public async Task Handle_BlockMode_StoreError_ReturnsError()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var storeError = EncinaErrors.Create("DPIA_STORE_ERROR", "Store failed");
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Option<DPIAAssessment>>(storeError));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WarnMode_StoreError_WarnsAndCallsNext()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Warn });
        var storeError = EncinaErrors.Create("DPIA_STORE_ERROR", "Store failed");
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, Option<DPIAAssessment>>(storeError));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task Handle_BlockMode_StoreThrows_ReturnsStoreError()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database down"));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().Should().Be(DPIAErrors.StoreErrorCode);
    }

    [Fact]
    public async Task Handle_WarnMode_StoreThrows_WarnsAndCallsNext()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Warn });
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Database down"));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Tenant Context Propagation Tests

    [Fact]
    public async Task Handle_WithTenantId_PropagatesContextToStore()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        _context.TenantId.Returns("tenant-123");
        var assessment = CreateApprovedAssessment(nextReviewAtUtc: FixedNow.AddDays(30));
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region Default Enforcement Mode Tests

    [Fact]
    public async Task Handle_DefaultOptions_UsesWarnMode()
    {
        // Default DPIAOptions has EnforcementMode = Warn
        var sut = CreateSut();
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(None));

        var result = await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        // Warn mode lets the request through even without assessment
        result.IsRight.Should().BeTrue();
    }

    #endregion

    #region RequestType Full Name Tests

    [Fact]
    public async Task Handle_UsesFullTypeName_ForStoreQuery()
    {
        var sut = CreateSut(new DPIAOptions { EnforcementMode = DPIAEnforcementMode.Block });
        var assessment = CreateApprovedAssessment(nextReviewAtUtc: FixedNow.AddDays(30));
        _store.GetAssessmentAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Option<DPIAAssessment>>(Some(assessment)));

        await sut.Handle(
            new TestCommandWithDPIA(), _context, SuccessNext(), CancellationToken.None);

        await _store.Received(1).GetAssessmentAsync(
            typeof(TestCommandWithDPIA).FullName!, Arg.Any<CancellationToken>());
    }

    #endregion

    #region Test Helper Types

    [RequiresDPIA(ProcessingType = "AutomatedDecisionMaking", Reason = "Test")]
    public sealed record TestCommandWithDPIA : IRequest<string>;

    public sealed record TestCommandWithoutDPIA : IRequest<string>;

    #endregion
}
