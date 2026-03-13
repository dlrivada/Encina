#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ProcessorValidationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class ProcessorValidationPipelineBehaviorTests
{
    private static readonly DateTimeOffset FixedNow = new(2026, 3, 1, 12, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _timeProvider = new(FixedNow);
    private readonly IDPAValidator _validator = Substitute.For<IDPAValidator>();
    private readonly IProcessorAuditStore _auditStore = Substitute.For<IProcessorAuditStore>();
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();

    [RequiresProcessor(ProcessorId = "test-processor")]
    private sealed record TestCommandWithProcessor : ICommand<string>;

    private sealed record TestCommandWithoutProcessor : ICommand<string>;

    #region Helpers

    private ProcessorValidationPipelineBehavior<TRequest, TResponse> CreateSut<TRequest, TResponse>(
        ProcessorAgreementOptions? options = null)
        where TRequest : IRequest<TResponse>
    {
        var opts = Options.Create(options ?? new ProcessorAgreementOptions());
        return new ProcessorValidationPipelineBehavior<TRequest, TResponse>(
            _validator, _auditStore, opts, _timeProvider,
            NullLogger<ProcessorValidationPipelineBehavior<TRequest, TResponse>>.Instance);
    }

    private static RequestHandlerCallback<string> SuccessNext() =>
        () => ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("handler-result"));

    private void SetupHasValidDPA(bool isValid) =>
        _validator.HasValidDPAAsync("test-processor", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(isValid)));

    private void SetupHasValidDPAError(EncinaError error) =>
        _validator.HasValidDPAAsync("test-processor", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, bool>(error)));

    private void SetupValidateAsync() =>
        _validator.ValidateAsync("test-processor", Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPAValidationResult>(new DPAValidationResult
            {
                ProcessorId = "test-processor",
                IsValid = false,
                MissingTerms = [],
                Warnings = ["No active DPA"],
                ValidatedAtUtc = FixedNow
            })));

    private void SetupAuditStoreSuccess() =>
        _auditStore.RecordAsync(Arg.Any<ProcessorAgreementAuditEntry>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, Unit>(unit)));

    #endregion

    [Fact]
    public async Task Handle_DisabledMode_SkipsValidation()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Disabled
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("handler-result"));
        };

        // Act
        var result = await sut.Handle(new TestCommandWithProcessor(), _context, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
        ((string)result).Should().Be("handler-result");
        await _validator.DidNotReceive().HasValidDPAAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoAttribute_SkipsValidation()
    {
        // Arrange
        var sut = CreateSut<TestCommandWithoutProcessor, string>();
        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("handler-result"));
        };

        // Act
        var result = await sut.Handle(new TestCommandWithoutProcessor(), _context, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
        await _validator.DidNotReceive().HasValidDPAAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BlockMode_ValidDPA_CallsNextStep()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        SetupHasValidDPA(true);

        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("handler-result"));
        };

        // Act
        var result = await sut.Handle(new TestCommandWithProcessor(), _context, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
        ((string)result).Should().Be("handler-result");
    }

    [Fact]
    public async Task Handle_BlockMode_InvalidDPA_ReturnsError()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block,
            TrackAuditTrail = false
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        SetupHasValidDPA(false);
        SetupValidateAsync();

        // Act
        var result = await sut.Handle(new TestCommandWithProcessor(), _context, SuccessNext(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WarnMode_InvalidDPA_CallsNextStep()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Warn
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        SetupHasValidDPA(false);

        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("handler-result"));
        };

        // Act
        var result = await sut.Handle(new TestCommandWithProcessor(), _context, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BlockMode_ValidatorError_ReturnsError()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        var validatorError = EncinaError.New("Validator infrastructure error");
        SetupHasValidDPAError(validatorError);

        // Act
        var result = await sut.Handle(new TestCommandWithProcessor(), _context, SuccessNext(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Be("Validator infrastructure error");
    }

    [Fact]
    public async Task Handle_WarnMode_ValidatorError_CallsNextStep()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Warn
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        var validatorError = EncinaError.New("Validator infrastructure error");
        SetupHasValidDPAError(validatorError);

        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("handler-result"));
        };

        // Act
        var result = await sut.Handle(new TestCommandWithProcessor(), _context, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_BlockMode_Exception_ReturnsError()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        _validator.HasValidDPAAsync("test-processor", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Connection failed"));

        // Act
        var result = await sut.Handle(new TestCommandWithProcessor(), _context, SuccessNext(), CancellationToken.None);

        // Assert
        result.IsLeft.Should().BeTrue();
        var error = (EncinaError)result;
        error.Message.Should().Contain("Connection failed");
    }

    [Fact]
    public async Task Handle_WarnMode_Exception_CallsNextStep()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Warn
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        _validator.HasValidDPAAsync("test-processor", Arg.Any<CancellationToken>())
            .Throws(new InvalidOperationException("Connection failed"));

        var nextCalled = false;
        RequestHandlerCallback<string> next = () =>
        {
            nextCalled = true;
            return ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("handler-result"));
        };

        // Act
        var result = await sut.Handle(new TestCommandWithProcessor(), _context, next, CancellationToken.None);

        // Assert
        nextCalled.Should().BeTrue();
        result.IsRight.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_AuditTrailEnabled_RecordsBlockAction()
    {
        // Arrange
        var options = new ProcessorAgreementOptions
        {
            EnforcementMode = ProcessorAgreementEnforcementMode.Block,
            TrackAuditTrail = true
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        SetupHasValidDPA(false);
        SetupValidateAsync();
        SetupAuditStoreSuccess();

        // Act
        await sut.Handle(new TestCommandWithProcessor(), _context, SuccessNext(), CancellationToken.None);

        // Assert
        await _auditStore.Received(1).RecordAsync(
            Arg.Is<ProcessorAgreementAuditEntry>(e =>
                e.ProcessorId == "test-processor" &&
                e.Action == "Blocked"),
            Arg.Any<CancellationToken>());
    }
}
