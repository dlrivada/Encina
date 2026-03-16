#pragma warning disable CA2012

using Encina.Compliance.ProcessorAgreements;
using Encina.Compliance.ProcessorAgreements.Abstractions;
using Encina.Compliance.ProcessorAgreements.Model;

using FluentAssertions;

using LanguageExt;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;
using NSubstitute.ExceptionExtensions;

using static LanguageExt.Prelude;

namespace Encina.UnitTests.Compliance.ProcessorAgreements;

/// <summary>
/// Unit tests for <see cref="ProcessorValidationPipelineBehavior{TRequest, TResponse}"/>.
/// </summary>
public class ProcessorValidationPipelineBehaviorTests
{
    private static readonly Guid TestProcessorGuid = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private readonly IDPAService _dpaService = Substitute.For<IDPAService>();
    private readonly IRequestContext _context = Substitute.For<IRequestContext>();

    [RequiresProcessor(ProcessorId = "00000000-0000-0000-0000-000000000001")]
    private sealed record TestCommandWithProcessor : ICommand<string>;

    private sealed record TestCommandWithoutProcessor : ICommand<string>;

    #region Helpers

    private ProcessorValidationPipelineBehavior<TRequest, TResponse> CreateSut<TRequest, TResponse>(
        ProcessorAgreementOptions? options = null)
        where TRequest : IRequest<TResponse>
    {
        var opts = Options.Create(options ?? new ProcessorAgreementOptions());
        return new ProcessorValidationPipelineBehavior<TRequest, TResponse>(
            _dpaService, opts,
            NullLogger<ProcessorValidationPipelineBehavior<TRequest, TResponse>>.Instance);
    }

    private static RequestHandlerCallback<string> SuccessNext() =>
        () => ValueTask.FromResult<Either<EncinaError, string>>(Right<EncinaError, string>("handler-result"));

    private void SetupHasValidDPA(bool isValid) =>
        _dpaService.HasValidDPAAsync(TestProcessorGuid, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, bool>(isValid)));

    private void SetupHasValidDPAError(EncinaError error) =>
        _dpaService.HasValidDPAAsync(TestProcessorGuid, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Left<EncinaError, bool>(error)));

    private void SetupValidateDPAAsync() =>
        _dpaService.ValidateDPAAsync(TestProcessorGuid, Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(Right<EncinaError, DPAValidationResult>(new DPAValidationResult
            {
                ProcessorId = TestProcessorGuid.ToString(),
                IsValid = false,
                MissingTerms = [],
                Warnings = ["No active DPA"],
                ValidatedAtUtc = DateTimeOffset.UtcNow
            })));

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
        await _dpaService.DidNotReceive().HasValidDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
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
        await _dpaService.DidNotReceive().HasValidDPAAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
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
            EnforcementMode = ProcessorAgreementEnforcementMode.Block
        };
        var sut = CreateSut<TestCommandWithProcessor, string>(options);
        SetupHasValidDPA(false);
        SetupValidateDPAAsync();

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
        var serviceError = EncinaError.New("Validator infrastructure error");
        SetupHasValidDPAError(serviceError);

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
        var serviceError = EncinaError.New("Validator infrastructure error");
        SetupHasValidDPAError(serviceError);

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
        _dpaService.HasValidDPAAsync(TestProcessorGuid, Arg.Any<CancellationToken>())
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
        _dpaService.HasValidDPAAsync(TestProcessorGuid, Arg.Any<CancellationToken>())
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
}
