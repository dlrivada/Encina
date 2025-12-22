using System.ComponentModel.DataAnnotations;
using LanguageExt;
using Shouldly;
using static LanguageExt.Prelude;

namespace Encina.MiniValidator.PropertyTests;

/// <summary>
/// Property-based tests for MiniValidationBehavior.
/// Verifies invariants hold across different scenarios.
/// </summary>
public sealed class MiniValidationBehaviorPropertyTests
{
    private sealed record TestCommand : ICommand<string>
    {
        [Required]
        [MinLength(3)]
        public string Name { get; init; } = string.Empty;

        [EmailAddress]
        public string Email { get; init; } = string.Empty;

        [Range(1, 120)]
        public int Age { get; init; }
    }

    [Fact]
    public async Task Property_ValidRequest_AlwaysInvokesNextStep()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var context = RequestContext.Create();

        var validTestCases = new[]
        {
            new TestCommand { Name = "John", Email = "john@test.com", Age = 25 },
            new TestCommand { Name = "Alice", Email = "alice@example.org", Age = 30 },
            new TestCommand { Name = "Bob Smith", Email = "bob@company.net", Age = 45 },
            new TestCommand { Name = "XXX", Email = "x@a.io", Age = 1 },
            new TestCommand { Name = "Very Long Name Here", Email = "test@test.test", Age = 120 }
        };

        foreach (var request in validTestCases)
        {
            var nextStepInvoked = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepInvoked = true;
                return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
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
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var context = RequestContext.Create();

        var invalidTestCases = new[]
        {
            new TestCommand { Name = "", Email = "test@test.com", Age = 25 },  // Empty name
            new TestCommand { Name = "John", Email = "invalid-email", Age = 25 },  // Invalid email
            new TestCommand { Name = "John", Email = "john@test.com", Age = -1 },  // Negative age
            new TestCommand { Name = "", Email = "", Age = 0 },  // All invalid
        };

        foreach (var request in invalidTestCases)
        {
            var nextStepInvoked = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepInvoked = true;
                return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
            };

            // Act
            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: Invalid requests NEVER invoke next step
            nextStepInvoked.ShouldBeFalse();
            result.IsLeft.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_NoValidationAttributes_AlwaysBypassesValidation()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<NoValidationCommand, string>();
        var context = RequestContext.Create();

        var testCases = new[]
        {
            new NoValidationCommand { Value = "Valid" },
            new NoValidationCommand { Value = "" },  // Would fail if validation existed
            new NoValidationCommand { Value = "   " },  // Would fail if validation existed
        };

        foreach (var request in testCases)
        {
            var nextStepInvoked = false;

            RequestHandlerCallback<string> nextStep = () =>
            {
                nextStepInvoked = true;
                return new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));
            };

            // Act
            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: No validation attributes ALWAYS bypasses validation
            nextStepInvoked.ShouldBeTrue();
            result.IsRight.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_Idempotency_SameRequestAlwaysSameResult()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "John", Email = "john@example.com", Age = 25 };
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act - Call multiple times
        var result1 = await behavior.Handle(request, context, nextStep, CancellationToken.None);
        var result2 = await behavior.Handle(request, context, nextStep, CancellationToken.None);
        var result3 = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Property: Same request ALWAYS produces same result
        result1.IsRight.ShouldBe(result2.IsRight);
        result2.IsRight.ShouldBe(result3.IsRight);
    }

    [Fact(Skip = "MiniValidation uses MiniValidator.TryValidate which does not pass ValidationContext")]
    public async Task Property_ContextEnrichment_AlwaysPassesCorrelationId()
    {
        // Arrange
        var capturedCorrelationIds = new List<string>();
        var behavior = new MiniValidationBehavior<ContextAwareCommand, string>();

        var testCases = new[]
        {
            new ContextAwareCommand { Value = "Test1" },
            new ContextAwareCommand { Value = "Test2" },
            new ContextAwareCommand { Value = "Test3" },
        };

        foreach (var request in testCases)
        {
            var context = RequestContext.Create();
            var expectedCorrelationId = context.CorrelationId;

            ContextAwareCommand.OnValidation = (validationContext) =>
            {
                if (validationContext.Items.TryGetValue("CorrelationId", out var correlationId))
                {
                    capturedCorrelationIds.Add(correlationId?.ToString() ?? string.Empty);
                }
            };

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

            // Act
            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: CorrelationId ALWAYS passed
            capturedCorrelationIds.ShouldContain(expectedCorrelationId);
        }

        // Assert - All CorrelationIds captured
        capturedCorrelationIds.Count.ShouldBe(testCases.Length);
    }

    [Fact(Skip = "MiniValidation uses MiniValidator.TryValidate which does not pass ValidationContext")]
    public async Task Property_UserId_PassedWhenPresent()
    {
        // Arrange
        var capturedUserIds = new List<string>();
        var behavior = new MiniValidationBehavior<ContextAwareCommand, string>();

        var userIds = new[] { "user-1", "user-2", "user-3" };

        foreach (var userId in userIds)
        {
            var request = new ContextAwareCommand { Value = "Test" };
            var context = RequestContext.CreateForTest(userId: userId);

            ContextAwareCommand.OnValidation = (validationContext) =>
            {
                if (validationContext.Items.TryGetValue("UserId", out var uid))
                {
                    capturedUserIds.Add(uid?.ToString() ?? string.Empty);
                }
            };

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

            // Act
            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: UserId ALWAYS passed when present
            capturedUserIds.ShouldContain(userId);
        }

        // Assert - All UserIds captured
        capturedUserIds.Count.ShouldBe(userIds.Length);
    }

    [Fact(Skip = "MiniValidation uses MiniValidator.TryValidate which does not pass ValidationContext")]
    public async Task Property_TenantId_PassedWhenPresent()
    {
        // Arrange
        var capturedTenantIds = new List<string>();
        var behavior = new MiniValidationBehavior<ContextAwareCommand, string>();

        var tenantIds = new[] { "tenant-1", "tenant-2", "tenant-3" };

        foreach (var tenantId in tenantIds)
        {
            var request = new ContextAwareCommand { Value = "Test" };
            var context = RequestContext.CreateForTest(tenantId: tenantId);

            ContextAwareCommand.OnValidation = (validationContext) =>
            {
                if (validationContext.Items.TryGetValue("TenantId", out var tid))
                {
                    capturedTenantIds.Add(tid?.ToString() ?? string.Empty);
                }
            };

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

            // Act
            await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: TenantId ALWAYS passed when present
            capturedTenantIds.ShouldContain(tenantId);
        }

        // Assert - All TenantIds captured
        capturedTenantIds.Count.ShouldBe(tenantIds.Length);
    }

    [Fact]
    public async Task Property_MultipleErrors_AlwaysAggregatesAll()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var context = RequestContext.Create();

        var invalidTestCases = new[]
        {
            new TestCommand { Name = "", Email = "invalid", Age = -1 },
            new TestCommand { Name = "", Email = "", Age = 0 },
        };

        foreach (var request in invalidTestCases)
        {
            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

            // Act
            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: Multiple errors ALWAYS aggregated in message
            // Note: MiniValidation returns errors in the message string, not as ValidationException
            result.IsLeft.ShouldBeTrue();
            _ = result.Match(
                Right: _ => throw new InvalidOperationException("Expected Left"),
                Left: error =>
                {
                    error.Message.ShouldContain("Validation failed");
                    error.Message.ShouldContain("error(s)");
                    return true;
                });
        }
    }

    [Fact]
    public async Task Property_Cancellation_AlwaysReturnsLeft()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();

        var testCases = new[]
        {
            new TestCommand { Name = "Valid", Email = "valid@test.com", Age = 25 },
            new TestCommand { Name = "Another", Email = "another@test.com", Age = 30 },
        };

        foreach (var request in testCases)
        {
            var context = RequestContext.Create();
            var cts = new CancellationTokenSource();
            cts.Cancel();

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

            // Act
            var result = await behavior.Handle(request, context, nextStep, cts.Token);

            // Assert - Property: Cancelled requests ALWAYS return Left
            result.IsLeft.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Property_ConcurrentExecution_ThreadSafe()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "Valid", Email = "valid@test.com", Age = 25 };
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

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
        var behavior = new MiniValidationBehavior<TestCommand, string>();
        var request = new TestCommand { Name = "", Email = "", Age = -1 }; // All fields invalid
        var context = RequestContext.Create();

        RequestHandlerCallback<string> nextStep = () =>
            new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

        // Act
        var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

        // Assert - Property: Errors ALWAYS contain validation failure details in message
        // Note: MiniValidation returns errors in the message string, not as ValidationException
        _ = result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error =>
            {
                error.Message.ShouldContain("Validation failed");
                error.Message.ShouldContain("TestCommand");
                return true;
            });
    }

    [Fact]
    public async Task Property_EmptyValidation_AlwaysSucceeds()
    {
        // Arrange
        var behavior = new MiniValidationBehavior<NoValidationCommand, string>();

        var testCases = new[]
        {
            new NoValidationCommand { Value = "Valid" },
            new NoValidationCommand { Value = "" },
            new NoValidationCommand { Value = "   " },
        };

        foreach (var request in testCases)
        {
            var context = RequestContext.Create();

            RequestHandlerCallback<string> nextStep = () =>
                new ValueTask<Either<MediatorError, string>>(Right<MediatorError, string>("Success"));

            // Act
            var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

            // Assert - Property: Empty validation ALWAYS succeeds
            result.IsRight.ShouldBeTrue();
        }
    }

    // Helper types
    private sealed record NoValidationCommand : ICommand<string>
    {
        public string Value { get; init; } = string.Empty;
    }

    private sealed record ContextAwareCommand : ICommand<string>
    {
        [CustomValidation(typeof(ContextAwareCommand), nameof(ValidateWithContext))]
        public string Value { get; init; } = string.Empty;

        public static Action<ValidationContext>? OnValidation { get; set; }

        public static ValidationResult ValidateWithContext(object value, ValidationContext context)
        {
            OnValidation?.Invoke(context);
            return ValidationResult.Success!;
        }
    }
}
