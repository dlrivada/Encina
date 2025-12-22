using FluentValidation;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.FluentValidation.PropertyTests;

/// <summary>
/// Property-based tests for ValidationPipelineBehavior.
/// Verifies invariants hold across different scenarios.
/// </summary>
public sealed class ValidationPipelineBehaviorPropertyTests
{
    private sealed record TestCommand(string Name, string Email, int Age) : ICommand<string>;

    private sealed class TestValidator : AbstractValidator<TestCommand>
    {
        public TestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Email).EmailAddress();
            RuleFor(x => x.Age).GreaterThan(0);
        }
    }

    [Fact]
    public async Task Property_ValidRequest_AlwaysInvokesNextStep()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var context = RequestContext.Create();

        var testCases = new[]
        {
            new TestCommand("John", "john@test.com", 25),
            new TestCommand("Alice", "alice@example.org", 30),
            new TestCommand("Bob Smith", "bob@company.net", 45),
            new TestCommand("X", "x@a.io", 1),
            new TestCommand("Very Long Name Here", "test@test.test", 100)
        };

        foreach (var request in testCases)
        {
            var nextStepInvoked = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepInvoked = true;
                return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));
            };

            // Act
            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: Valid requests ALWAYS invoke next step
            nextStepInvoked.ShouldBeTrue();
            result.IsRight.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_InvalidRequest_NeverInvokesNextStep()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var context = RequestContext.Create();

        var invalidTestCases = new[]
        {
            new TestCommand("", "test@test.com", 25),  // Empty name
            new TestCommand("John", "invalid-email", 25),  // Invalid email
            new TestCommand("John", "john@test.com", -1),  // Negative age
            new TestCommand("", "", 0),  // All invalid
        };

        foreach (var request in invalidTestCases)
        {
            var nextStepInvoked = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepInvoked = true;
                return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));
            };

            // Act
            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: Invalid requests NEVER invoke next step
            nextStepInvoked.ShouldBeFalse();
            result.IsLeft.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_NoValidators_AlwaysBypassesValidation()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var context = RequestContext.Create();

        var testCases = new[]
        {
            new TestCommand("Valid", "valid@test.com", 25),
            new TestCommand("", "invalid", -1),  // Would fail validation if validators existed
            new TestCommand("", "", 0),  // Would fail validation if validators existed
        };

        foreach (var request in testCases)
        {
            var nextStepInvoked = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepInvoked = true;
                return new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));
            };

            // Act
            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: No validators ALWAYS bypasses validation
            nextStepInvoked.ShouldBeTrue();
            result.IsRight.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_Idempotency_SameRequestAlwaysSameResult()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("John", "john@example.com", 25);
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act - Call multiple times
        var result1 = await behavior.Handle(request, context, nextStep, CancellationToken.None);
        var result2 = await behavior.Handle(request, context, nextStep, CancellationToken.None);
        var result3 = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Property: Same request ALWAYS produces same result
        result1.IsRight.ShouldBe(result2.IsRight);
        result2.IsRight.ShouldBe(result3.IsRight);
    }

    [Fact]
    public async Task Property_ContextEnrichment_AlwaysPassesCorrelationId()
    {
        // Arrange
        var capturedCorrelationIds = new List<string>();
        var validator = new InlineValidator<TestCommand>();
        validator.RuleFor(x => x.Name).Custom((name, ctx) =>
        {
            if (ctx.RootContextData.TryGetValue("CorrelationId", out var correlationId))
            {
                capturedCorrelationIds.Add(correlationId.ToString()!);
            }
        });

        var validators = new[] { validator };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);

        var testCases = new[]
        {
            new TestCommand("Test1", "test1@test.com", 25),
            new TestCommand("Test2", "test2@test.com", 30),
            new TestCommand("Test3", "test3@test.com", 35),
        };

        foreach (var request in testCases)
        {
            var context = RequestContext.Create();
            var expectedCorrelationId = context.CorrelationId;

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

            // Act
            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: CorrelationId ALWAYS passed
            capturedCorrelationIds.ShouldContain(expectedCorrelationId);
        }

        // Assert - All CorrelationIds captured
        capturedCorrelationIds.Count.ShouldBe(testCases.Length);
    }

    [Fact]
    public async Task Property_UserId_PassedWhenPresent()
    {
        // Arrange
        var capturedUserIds = new List<string>();
        var validator = new InlineValidator<TestCommand>();
        validator.RuleFor(x => x.Name).Custom((name, ctx) =>
        {
            if (ctx.RootContextData.TryGetValue("UserId", out var userId))
            {
                capturedUserIds.Add(userId.ToString()!);
            }
        });

        var validators = new[] { validator };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);

        var userIds = new[] { "user-1", "user-2", "user-3" };

        foreach (var userId in userIds)
        {
            var request = new TestCommand("Test", "test@test.com", 25);
            var context = RequestContext.CreateForTest(userId: userId);

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

            // Act
            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: UserId ALWAYS passed when present
            capturedUserIds.ShouldContain(userId);
        }

        // Assert - All UserIds captured
        capturedUserIds.Count.ShouldBe(userIds.Length);
    }

    [Fact]
    public async Task Property_TenantId_PassedWhenPresent()
    {
        // Arrange
        var capturedTenantIds = new List<string>();
        var validator = new InlineValidator<TestCommand>();
        validator.RuleFor(x => x.Name).Custom((name, ctx) =>
        {
            if (ctx.RootContextData.TryGetValue("TenantId", out var tenantId))
            {
                capturedTenantIds.Add(tenantId.ToString()!);
            }
        });

        var validators = new[] { validator };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);

        var tenantIds = new[] { "tenant-1", "tenant-2", "tenant-3" };

        foreach (var tenantId in tenantIds)
        {
            var request = new TestCommand("Test", "test@test.com", 25);
            var context = RequestContext.CreateForTest(tenantId: tenantId);

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

            // Act
            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: TenantId ALWAYS passed when present
            capturedTenantIds.ShouldContain(tenantId);
        }

        // Assert - All TenantIds captured
        capturedTenantIds.Count.ShouldBe(tenantIds.Length);
    }

    [Fact]
    public async Task Property_MultipleValidators_AlwaysAggregatesAllErrors()
    {
        // Arrange
        var validators = new IValidator<TestCommand>[]
        {
            new TestValidator(),
            new TestValidator()
        };

        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var context = RequestContext.Create();

        var invalidTestCases = new[]
        {
            new TestCommand("", "invalid", -1),
            new TestCommand("", "", 0),
        };

        foreach (var request in invalidTestCases)
        {
            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

            // Act
            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: Multiple validators ALWAYS aggregate errors
            result.IsLeft.ShouldBeTrue();
            result.Match(
                Right: _ => false.ShouldBeTrue("Expected Left"),
                Left: error =>
                {
                    error.Exception.ShouldBeOfType<ValidationException>();
                    var validationException = (ValidationException)error.Exception!;
                    validationException.Errors.Count().ShouldBeGreaterThan(0);
                });
        }
    }

    [Fact]
    public async Task Property_Cancellation_AlwaysReturnsLeft()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);

        var testCases = new[]
        {
            new TestCommand("Valid", "valid@test.com", 25),
            new TestCommand("Another", "another@test.com", 30),
        };

        foreach (var request in testCases)
        {
            var context = RequestContext.Create();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

            // Act
            var result = await behavior.Handle(request, context, nextStep, cts.Token);

            // Assert - Property: Cancelled requests ALWAYS return Left
            result.IsLeft.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_ValidatorOrder_DoesNotAffectOutcome()
    {
        // Arrange
        var validator1 = new TestValidator();
        var validator2 = new TestValidator();

        var order1 = new[] { validator1, validator2 };
        var order2 = new[] { validator2, validator1 };

        var behavior1 = new ValidationPipelineBehavior<TestCommand, string>(order1);
        var behavior2 = new ValidationPipelineBehavior<TestCommand, string>(order2);

        var testCases = new[]
        {
            new TestCommand("Valid", "valid@test.com", 25),
            new TestCommand("", "invalid", -1),
        };

        foreach (var request in testCases)
        {
            var context = RequestContext.Create();

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

            // Act
            var result1 = await behavior1.Handle(request, context, nextStep, CancellationToken.None);
            var result2 = await behavior2.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: Validator order NEVER affects outcome
            result1.IsRight.ShouldBe(result2.IsRight);
        }
    }

    [Fact]
    public async Task Property_ConcurrentExecution_ThreadSafe()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("Valid", "valid@test.com", 25);
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act - Execute concurrently
        var tasks = Enumerable.Range(0, 20)
            .Select(_ => Task.Run(async () => await behavior.Handle(request, context, nextStep, CancellationToken.None)))
            .ToArray();

        await Task.WhenAll(tasks);

        // Assert - Property: All concurrent calls succeed
        tasks.All(t => t.Result.IsRight).ShouldBeTrue();
    }

    [Fact]
    public async Task Property_ErrorDetails_AlwaysContainValidationFailures()
    {
        // Arrange
        var validators = new[] { new TestValidator() };
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);
        var request = new TestCommand("", "", -1); // All fields invalid
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Property: Errors ALWAYS contain validation failure details
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Exception.ShouldBeOfType<ValidationException>();
                var validationException = (ValidationException)error.Exception!;
                validationException.Errors.Count().ShouldBeGreaterThan(0);
            });
    }

    [Fact]
    public async Task Property_EmptyValidators_AlwaysSucceeds()
    {
        // Arrange
        var validators = Enumerable.Empty<IValidator<TestCommand>>();
        var behavior = new ValidationPipelineBehavior<TestCommand, string>(validators);

        var testCases = new[]
        {
            new TestCommand("Valid", "valid@test.com", 25),
            new TestCommand("", "invalid", -1),
            new TestCommand("", "", 0),
        };

        foreach (var request in testCases)
        {
            var context = RequestContext.Create();

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<EncinaError, string>>(Right<EncinaError, string>("Success"));

            // Act
            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: Empty validators ALWAYS succeed
            result.IsRight.ShouldBeTrue();
        }
    }
}
