using FluentValidation;
using FluentValidation.Results;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.FluentValidation.Tests;

public sealed class ValidationPipelineBehaviorTests
{
    private sealed record TestCommand(string Name, string Email) : ICommand<string>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("Valid email is required");
        }
    }

    private sealed class AlwaysFailValidator : AbstractValidator<TestCommand>
    {
        public AlwaysFailValidator()
        {
            RuleFor(x => x.Name).Must(_ => false).WithMessage("Always fails");
        }
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldInvokeNextStep()
    {
        // Arrange
        var validators = new[] { new TestCommandValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John Doe", "john@example.com");
        var context = RequestContext.Create();
        var nextCalled = false;
        var expectedResponse = "Success";

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextCalled = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>(expectedResponse));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
        result.Match(
            Right: value => value.ShouldBe(expectedResponse),
            Left: _ => throw new InvalidOperationException("Expected Right but got Left"));
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldReturnValidationError()
    {
        // Arrange
        var validators = new[] { new TestCommandValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", "invalid-email"); // Invalid name and email
        var context = RequestContext.Create();
        var nextCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextCalled = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeFalse();
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left but got Right"),
            Left: error =>
            {
                error.Message.ShouldContain("Validation failed");
                error.Message.ShouldContain("TestCommand");
                error.Exception.IsSome.ShouldBeTrue();
                error.Exception.IfSome(ex =>
                {
                    ex.ShouldBeOfType<ValidationException>();
                    var validationEx = (ValidationException)ex;
                    validationEx.Errors.Count().ShouldBeGreaterThanOrEqualTo(2); // Name and Email errors
                });
            });
    }

    [Fact]
    public async Task Handle_WithMultipleValidators_ShouldAggregateAllFailures()
    {
        // Arrange
        var validators = new IValidator<TestCommand>[]
        {
            new TestCommandValidator(),
            new AlwaysFailValidator()
        };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", ""); // Will fail TestCommandValidator
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left but got Right"),
            Left: error =>
            {
                error.Exception.IsSome.ShouldBeTrue();
                error.Exception.IfSome(ex =>
                {
                    var validationEx = (ValidationException)ex;
                    validationEx.Errors.Count().ShouldBeGreaterThanOrEqualTo(3); // Name, Email, and AlwaysFails
                });
            });
    }

    [Fact]
    public async Task Handle_WithNoValidators_ShouldInvokeNextStep()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", ""); // Invalid but no validators
        var context = RequestContext.Create();
        var nextCalled = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextCalled = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        nextCalled.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldReturnCancellationError()
    {
        // Arrange
        var validator = new SlowValidator();
        var validators = new[] { validator };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John", "john@example.com");
        var context = RequestContext.Create();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left but got Right"),
            Left: error =>
            {
                error.Message.ShouldContain("cancelled");
            });
    }

    [Fact]
    public async Task Handle_WithRequestContextMetadata_ShouldEnrichValidationContext()
    {
        // Arrange
        var validator = new ContextAwareValidator();
        var validators = new[] { validator };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John", "john@example.com");
        var correlationId = Guid.NewGuid().ToString();
        var userId = "user-123";
        var tenantId = "tenant-456";
        var context = RequestContext.CreateForTest(userId, tenantId, null, correlationId);

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        validator.CapturedCorrelationId.ShouldBe(correlationId);
        validator.CapturedUserId.ShouldBe(userId);
        validator.CapturedTenantId.ShouldBe(tenantId);
    }

    [Fact]
    public async Task Handle_WithOnlyNameInvalid_ShouldReturnSingleError()
    {
        // Arrange
        var validators = new[] { new TestCommandValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", "valid@example.com"); // Only name is invalid
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left but got Right"),
            Left: error =>
            {
                error.Exception.IsSome.ShouldBeTrue();
                error.Exception.IfSome(ex =>
                {
                    var validationEx = (ValidationException)ex;
                    validationEx.Errors.Count().ShouldBe(1);
                    validationEx.Errors.First().ErrorMessage.ShouldBe("Name is required");
                });
            });
    }

    private sealed class SlowValidator : AbstractValidator<TestCommand>
    {
        public SlowValidator()
        {
            RuleFor(x => x.Name).MustAsync(async (name, ct) =>
            {
                await Task.Delay(5000, ct); // Simulate slow validation
                return true;
            });
        }
    }

    private sealed class ContextAwareValidator : AbstractValidator<TestCommand>
    {
        public string? CapturedCorrelationId { get; private set; }
        public string? CapturedUserId { get; private set; }
        public string? CapturedTenantId { get; private set; }

        public ContextAwareValidator()
        {
            RuleFor(x => x.Name).Custom((value, context) =>
            {
                // Capture context data for assertion
                if (context.RootContextData.TryGetValue("CorrelationId", out var correlationId))
                {
                    CapturedCorrelationId = correlationId as string;
                }
                if (context.RootContextData.TryGetValue("UserId", out var userId))
                {
                    CapturedUserId = userId as string;
                }
                if (context.RootContextData.TryGetValue("TenantId", out var tenantId))
                {
                    CapturedTenantId = tenantId as string;
                }
            });
        }
    }
}
