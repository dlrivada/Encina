using FluentValidation;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.FluentValidation.ContractTests;

/// <summary>
/// Contract tests for ValidationPipelineBehavior.
/// Verifies that the behavior adheres to expected contracts and guarantees.
/// </summary>
public sealed class ValidationPipelineBehaviorContractTests
{
    private sealed record TestCommand(string Name, string Email) : ICommand<string>;

    private sealed class NameValidator : AbstractValidator<TestCommand>
    {
        public NameValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        }
    }

    private sealed class EmailValidator : AbstractValidator<TestCommand>
    {
        public EmailValidator()
        {
            RuleFor(x => x.Email).EmailAddress().WithMessage("Invalid email format");
        }
    }

    private sealed class SlowValidator : AbstractValidator<TestCommand>
    {
        private readonly int _delayMs;

        public SlowValidator(int delayMs)
        {
            _delayMs = delayMs;
            RuleFor(x => x.Name).MustAsync(async (name, ct) =>
            {
                await Task.Delay(_delayMs, ct);
                return !string.IsNullOrEmpty(name);
            }).WithMessage("Name validation delayed");
        }
    }

    [Fact]
    public async Task Contract_IPipelineBehavior_ShouldImplementInterface()
    {
        // Arrange
        var validators = new[] { new NameValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);

        // Assert
        behavior.ShouldBeAssignableTo<IPipelineBehavior<TestCommand, string>>();
    }

    [Fact]
    public async Task Contract_ValidRequest_MustInvokeNextStep()
    {
        // Arrange
        var validators = new[] { new NameValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John", "john@example.com");
        var context = RequestContext.Create();
        var nextStepInvoked = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepInvoked = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: Valid requests MUST proceed to handler
        nextStepInvoked.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_InvalidRequest_MustNotInvokeNextStep()
    {
        // Arrange
        var validators = new[] { new NameValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", "john@example.com"); // Invalid name
        var context = RequestContext.Create();
        var nextStepInvoked = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepInvoked = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
        };

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: Invalid requests MUST short-circuit
        nextStepInvoked.ShouldBeFalse();
    }

    [Fact]
    public async Task Contract_InvalidRequest_MustReturnLeft()
    {
        // Arrange
        var validators = new[] { new NameValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", "john@example.com");
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: Validation failures MUST return Left
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_MultipleValidators_MustAggregateAllFailures()
    {
        // Arrange
        var validators = new IValidator<TestCommand>[] { new NameValidator(), new EmailValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", "invalid-email"); // Both validators fail
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: ALL validation failures MUST be aggregated
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("2 error(s)");
                error.Exception.ShouldBeOfType<ValidationException>();
                var validationException = (ValidationException)error.Exception!;
                validationException.Errors.Count().ShouldBe(2);
            });
    }

    [Fact]
    public async Task Contract_NoValidators_MustBypassValidation()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", ""); // Would fail validation if validators existed
        var context = RequestContext.Create();
        var nextStepInvoked = false;

        RequestHandlerCallback<string> nextStep = () =>
        {
            nextStepInvoked = true;
            return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
        };

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: No validators MUST skip validation entirely
        nextStepInvoked.ShouldBeTrue();
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_ContextEnrichment_MustPassCorrelationId()
    {
        // Arrange
        var contextData = new Dictionary<string, object>();
        var validator = new InlineValidator<TestCommand>();
        validator.RuleFor(x => x.Name).Custom((name, ctx) =>
        {
            // Capture context data for verification
            if (ctx.RootContextData.TryGetValue("CorrelationId", out var correlationId))
            {
                contextData["CorrelationId"] = correlationId;
            }
        });

        var validators = new[] { validator };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John", "john@example.com");
        var context = RequestContext.Create();
        var expectedCorrelationId = context.CorrelationId;

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: CorrelationId MUST be passed to validation context
        contextData.ShouldContainKey("CorrelationId");
        contextData["CorrelationId"].ShouldBe(expectedCorrelationId);
    }

    [Fact]
    public async Task Contract_ContextEnrichment_MustPassUserId()
    {
        // Arrange
        var contextData = new Dictionary<string, object>();
        var validator = new InlineValidator<TestCommand>();
        validator.RuleFor(x => x.Name).Custom((name, ctx) =>
        {
            if (ctx.RootContextData.TryGetValue("UserId", out var userId))
            {
                contextData["UserId"] = userId;
            }
        });

        var validators = new[] { validator };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John", "john@example.com");
        var context = RequestContext.CreateForTest(userId: "user-123");

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: UserId MUST be passed when present
        contextData.ShouldContainKey("UserId");
        contextData["UserId"].ShouldBe("user-123");
    }

    [Fact]
    public async Task Contract_ContextEnrichment_MustPassTenantId()
    {
        // Arrange
        var contextData = new Dictionary<string, object>();
        var validator = new InlineValidator<TestCommand>();
        validator.RuleFor(x => x.Name).Custom((name, ctx) =>
        {
            if (ctx.RootContextData.TryGetValue("TenantId", out var tenantId))
            {
                contextData["TenantId"] = tenantId;
            }
        });

        var validators = new[] { validator };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John", "john@example.com");
        var context = RequestContext.CreateForTest(tenantId: "tenant-456");

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: TenantId MUST be passed when present
        contextData.ShouldContainKey("TenantId");
        contextData["TenantId"].ShouldBe("tenant-456");
    }

    [Fact]
    public async Task Contract_ParallelExecution_MustRunValidatorsConcurrently()
    {
        // Arrange - Multiple slow validators
        var validators = new IValidator<TestCommand>[]
        {
            new SlowValidator(100),
            new SlowValidator(100),
            new SlowValidator(100)
        };

        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John", "john@example.com");
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await behavior.Handle(request, context, nextStep, CancellationToken.None);
        stopwatch.Stop();

        // Assert - Contract: Validators MUST run in parallel (< 200ms instead of 300ms sequential)
        stopwatch.ElapsedMilliseconds.ShouldBeLessThan(200);
    }

    [Fact]
    public async Task Contract_CancellationToken_MustPropagateToValidators()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var validatorReceivesCancellation = false;

        var validator = new InlineValidator<TestCommand>();
        validator.RuleFor(x => x.Name).MustAsync(async (name, ct) =>
        {
            validatorReceivesCancellation = ct.CanBeCanceled;
            await Task.Delay(1, ct);
            return true;
        });

        var validators = new[] { validator };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John", "john@example.com");
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        await behavior.Handle(request, context, nextStep, cts.Token);

        // Assert - Contract: CancellationToken MUST be propagated to validators
        validatorReceivesCancellation.ShouldBeTrue();
    }

    [Fact]
    public async Task Contract_ValidationException_MustContainAllErrors()
    {
        // Arrange
        var validators = new IValidator<TestCommand>[] { new NameValidator(), new EmailValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", "bad-email");
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Contract: ValidationException MUST contain ALL error details
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Exception.ShouldBeOfType<ValidationException>();
                var validationException = (ValidationException)error.Exception!;

                var errorMessages = validationException.Errors.Select(e => e.ErrorMessage).ToList();
                errorMessages.ShouldContain("Name is required");
                errorMessages.ShouldContain("Invalid email format");
            });
    }
}
