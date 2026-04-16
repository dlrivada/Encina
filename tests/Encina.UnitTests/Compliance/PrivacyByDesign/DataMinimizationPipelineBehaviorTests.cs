#pragma warning disable CA2012 // Use ValueTasks correctly

using Encina.Compliance.PrivacyByDesign;
using Encina.Compliance.PrivacyByDesign.Model;
using Encina.Modules.Isolation;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.PrivacyByDesign;

/// <summary>
/// Unit tests for <see cref="DataMinimizationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class DataMinimizationPipelineBehaviorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly IPrivacyByDesignValidator _validator = Substitute.For<IPrivacyByDesignValidator>();
    private readonly FakeTimeProvider _timeProvider = new(FixedNow);
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();
    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    private DataMinimizationPipelineBehavior<TestPbDRequest, string> CreateSut(
        PrivacyByDesignOptions? options = null)
    {
        var opts = Options.Create(options ?? new PrivacyByDesignOptions());
        var logger = NullLogger<DataMinimizationPipelineBehavior<TestPbDRequest, string>>.Instance;
        return new DataMinimizationPipelineBehavior<TestPbDRequest, string>(
            _validator, opts, _timeProvider, logger, _serviceProvider);
    }

    private DataMinimizationPipelineBehavior<TestNoPbDRequest, string> CreateSutWithoutAttribute(
        PrivacyByDesignOptions? options = null)
    {
        var opts = Options.Create(options ?? new PrivacyByDesignOptions());
        var logger = NullLogger<DataMinimizationPipelineBehavior<TestNoPbDRequest, string>>.Instance;
        return new DataMinimizationPipelineBehavior<TestNoPbDRequest, string>(
            _validator, opts, _timeProvider, logger, _serviceProvider);
    }

    private static RequestHandlerCallback<string> SuccessNext()
        => () => ValueTask.FromResult<Either<EncinaError, string>>("handler-result");

    private static RequestHandlerCallback<string> FailNext()
        => () => throw new InvalidOperationException("nextStep should not be called");

    private void SetupCompliantValidation()
    {
        _validator.ValidateAsync(Arg.Any<TestPbDRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, PrivacyValidationResult>(new PrivacyValidationResult
            {
                RequestTypeName = nameof(TestPbDRequest),
                Violations = [],
                MinimizationReport = new MinimizationReport
                {
                    RequestTypeName = nameof(TestPbDRequest),
                    NecessaryFields = [new PrivacyFieldInfo("ProductId", null, true)],
                    UnnecessaryFields = [],
                    MinimizationScore = 1.0,
                    Recommendations = [],
                    AnalyzedAtUtc = FixedNow
                },
                ValidatedAtUtc = FixedNow
            }));
    }

    private void SetupNonCompliantValidation(double minimizationScore = 0.5)
    {
        _validator.ValidateAsync(Arg.Any<TestPbDRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, PrivacyValidationResult>(new PrivacyValidationResult
            {
                RequestTypeName = nameof(TestPbDRequest),
                Violations =
                [
                    new PrivacyViolation(
                        "ReferralSource",
                        PrivacyViolationType.DataMinimization,
                        "Not necessary for processing",
                        MinimizationSeverity.Warning)
                ],
                MinimizationReport = new MinimizationReport
                {
                    RequestTypeName = nameof(TestPbDRequest),
                    NecessaryFields = [new PrivacyFieldInfo("ProductId", null, true)],
                    UnnecessaryFields = [new UnnecessaryFieldInfo("ReferralSource", "Analytics", true, MinimizationSeverity.Warning)],
                    MinimizationScore = minimizationScore,
                    Recommendations = ["Remove ReferralSource"],
                    AnalyzedAtUtc = FixedNow
                },
                ValidatedAtUtc = FixedNow
            }));
    }

    private void SetupValidatorReturnsLeft()
    {
        var error = EncinaErrors.Create("pbd.validator_error", "Validator infrastructure failure");
        _validator.ValidateAsync(Arg.Any<TestPbDRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Left<EncinaError, PrivacyValidationResult>(error));
    }

    #region Enforcement Mode Tests

    [Fact]
    public async Task Handle_WhenEnforcementDisabled_SkipsValidation()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Disabled
        });

        var result = await sut.Handle(
            new TestPbDRequest(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("handler-result");
        await _validator.DidNotReceive()
            .ValidateAsync(Arg.Any<TestPbDRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoAttribute_SkipsValidation()
    {
        var sut = CreateSutWithoutAttribute();

        var result = await sut.Handle(
            new TestNoPbDRequest(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("handler-result");
        await _validator.DidNotReceive()
            .ValidateAsync(Arg.Any<TestNoPbDRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>());
    }

    #endregion

    #region Block Mode Tests

    [Fact]
    public async Task Handle_BlockMode_WhenCompliant_CallsNextStep()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Block
        });
        SetupCompliantValidation();

        var result = await sut.Handle(
            new TestPbDRequest(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("handler-result");
    }

    [Fact]
    public async Task Handle_BlockMode_WhenViolations_ReturnsError()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Block
        });
        SetupNonCompliantValidation();

        var result = await sut.Handle(
            new TestPbDRequest(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.DataMinimizationViolationCode);
    }

    [Fact]
    public async Task Handle_BlockMode_WhenScoreBelowThreshold_ReturnsError()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Block,
            MinimizationScoreThreshold = 0.7
        });
        // Score 0.5 is below threshold 0.7 but the request is otherwise compliant (no violations list)
        _validator.ValidateAsync(Arg.Any<TestPbDRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, PrivacyValidationResult>(new PrivacyValidationResult
            {
                RequestTypeName = nameof(TestPbDRequest),
                Violations = [],
                MinimizationReport = new MinimizationReport
                {
                    RequestTypeName = nameof(TestPbDRequest),
                    NecessaryFields = [new PrivacyFieldInfo("ProductId", null, true)],
                    UnnecessaryFields = [new UnnecessaryFieldInfo("ReferralSource", "Analytics", true, MinimizationSeverity.Warning)],
                    MinimizationScore = 0.5,
                    Recommendations = ["Remove ReferralSource"],
                    AnalyzedAtUtc = FixedNow
                },
                ValidatedAtUtc = FixedNow
            }));

        var result = await sut.Handle(
            new TestPbDRequest(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.MinimizationScoreBelowThresholdCode);
    }

    #endregion

    #region Warn Mode Tests

    [Fact]
    public async Task Handle_WarnMode_WhenViolations_CallsNextStep()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Warn
        });
        SetupNonCompliantValidation();

        var result = await sut.Handle(
            new TestPbDRequest(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("handler-result");
    }

    [Fact]
    public async Task Handle_WarnMode_WhenValidatorReturnsLeft_CallsNextStep()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Warn
        });
        SetupValidatorReturnsLeft();

        var result = await sut.Handle(
            new TestPbDRequest(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("handler-result");
    }

    #endregion

    #region Module Context Tests

    [Fact]
    public async Task Handle_WithModuleContext_PassesModuleIdToValidator()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Block
        });
        SetupCompliantValidation();

        var moduleContext = Substitute.For<IModuleExecutionContext>();
        moduleContext.CurrentModule.Returns("orders-module");
        _serviceProvider.GetService(typeof(IModuleExecutionContext)).Returns(moduleContext);

        await sut.Handle(
            new TestPbDRequest(), _context, SuccessNext(), CancellationToken.None);

        await _validator.Received(1).ValidateAsync(
            Arg.Any<TestPbDRequest>(), "orders-module", Arg.Any<CancellationToken>());
    }

    #endregion

    #region Notification Tests

    [Fact]
    public async Task Handle_WhenViolations_PublishesNotification()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Warn
        });
        SetupNonCompliantValidation();

        var encina = Substitute.For<IEncina>();
        encina.Publish(Arg.Any<DataMinimizationViolationDetected>(), Arg.Any<CancellationToken>())
            .Returns(Right<EncinaError, Unit>(Unit.Default));
        _serviceProvider.GetService(typeof(IEncina)).Returns(encina);

        await sut.Handle(
            new TestPbDRequest(), _context, SuccessNext(), CancellationToken.None);

        await encina.Received(1).Publish(
            Arg.Is<DataMinimizationViolationDetected>(n =>
                n.RequestTypeName.Contains(nameof(TestPbDRequest)) &&
                n.Violations.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenEncinaNotRegistered_SkipsNotification()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Warn
        });
        SetupNonCompliantValidation();

        // IEncina not registered — returns null
        _serviceProvider.GetService(typeof(IEncina)).Returns(null as object);

        // Should not throw — notification is silently skipped
        var result = await sut.Handle(
            new TestPbDRequest(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Exception Handling Tests

    [Fact]
    public async Task Handle_WhenValidatorThrows_BlockMode_ReturnsError()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Block
        });
        _validator.ValidateAsync(Arg.Any<TestPbDRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Validator exploded"));

        var result = await sut.Handle(
            new TestPbDRequest(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
        var error = (EncinaError)result;
        error.GetEncinaCode().ShouldBe(PrivacyByDesignErrors.StoreErrorCode);
    }

    [Fact]
    public async Task Handle_WhenValidatorThrows_WarnMode_CallsNextStep()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Warn
        });
        _validator.ValidateAsync(Arg.Any<TestPbDRequest>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Validator exploded"));

        var result = await sut.Handle(
            new TestPbDRequest(), _context, SuccessNext(), CancellationToken.None);

        result.IsRight.ShouldBeTrue();
        ((string)result).ShouldBe("handler-result");
    }

    #endregion

    #region Guard Clause Tests

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        Func<Task> act = async () => await sut.Handle(null!, _context, SuccessNext(), CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        Func<Task> act = async () => await sut.Handle(new TestPbDRequest(), null!, SuccessNext(), CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        var sut = CreateSut();

        Func<Task> act = async () => await sut.Handle(new TestPbDRequest(), _context, null!, CancellationToken.None);

        (await Should.ThrowAsync<ArgumentNullException>(act)).ParamName.ShouldBe("nextStep");
    }

    #endregion

    #region Validator Returns Left in Block Mode

    [Fact]
    public async Task Handle_BlockMode_WhenValidatorReturnsLeft_ReturnsError()
    {
        var sut = CreateSut(new PrivacyByDesignOptions
        {
            EnforcementMode = PrivacyByDesignEnforcementMode.Block
        });
        SetupValidatorReturnsLeft();

        var result = await sut.Handle(
            new TestPbDRequest(), _context, FailNext(), CancellationToken.None);

        result.IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Test Helper Types

    [EnforceDataMinimization(Purpose = "Order Processing")]
    public sealed class TestPbDRequest : IRequest<string>
    {
        public string ProductId { get; set; } = "";

        [NotStrictlyNecessary(Reason = "Analytics")]
        public string? ReferralSource { get; set; }
    }

    public sealed class TestNoPbDRequest : IRequest<string>
    {
        public string Name { get; set; } = "";
    }

    #endregion
}
