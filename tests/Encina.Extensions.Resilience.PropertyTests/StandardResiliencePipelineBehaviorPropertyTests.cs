using Encina.Extensions.Resilience;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Registry;
using Polly.Timeout;

namespace Encina.Extensions.Resilience.PropertyTests;

/// <summary>
/// Property-based tests for <see cref="StandardResiliencePipelineBehavior{TRequest, TResponse}"/>.
/// Uses FsCheck to verify behavioral invariants across random inputs.
/// </summary>
public class StandardResiliencePipelineBehaviorPropertyTests
{
    [Property]
    public Property Property_HandleSuccess_AlwaysReturnsRight()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 1000).Select(x => new TestResponse { Value = x })),
            async (expectedResponse) =>
            {
                // Arrange
                var registry = new ResiliencePipelineRegistry<string>();
                registry.TryAddBuilder("TestRequest", (builder, _) =>
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(10));
                });

                var logger = new LoggerFactory().CreateLogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>();
                var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);

                var request = new TestRequest();
                var context = RequestContext.Create(Guid.NewGuid().ToString());
                RequestHandlerCallback<TestResponse> nextStep = () =>
                    ValueTask.FromResult<Either<EncinaError, TestResponse>>(expectedResponse);

                // Act
                var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

                // Assert
                return result.IsRight &&
                       result.Match(
                           Right: r => r.Value == expectedResponse.Value,
                           Left: _ => false);
            });
    }

    [Property]
    public Property Property_HandleFailure_AlwaysReturnsLeft()
    {
        return Prop.ForAll(
            Arb.From(Gen.Elements("Error1", "Error2", "Error3", "TestError", "FailureMessage")),
            async (errorMessage) =>
            {
                // Arrange
                var registry = new ResiliencePipelineRegistry<string>();
                registry.TryAddBuilder("TestRequest", (builder, _) =>
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(10));
                });

                var logger = new LoggerFactory().CreateLogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>();
                var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);

                var request = new TestRequest();
                var context = RequestContext.Create(Guid.NewGuid().ToString());
                var error = EncinaError.New(errorMessage);
                RequestHandlerCallback<TestResponse> nextStep = () =>
                    ValueTask.FromResult<Either<EncinaError, TestResponse>>(error);

                // Act
                var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

                // Assert
                return result.IsLeft &&
                       result.Match(
                           Right: _ => false,
                           Left: e => e.Message == errorMessage);
            });
    }

    [Property]
    public Property Property_Pipeline_IdempotentOnSuccess()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 100).Select(x => new TestResponse { Value = x })),
            async (expectedResponse) =>
            {
                // Arrange
                var registry = new ResiliencePipelineRegistry<string>();
                registry.TryAddBuilder("TestRequest", (builder, _) =>
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(10));
                });

                var logger = new LoggerFactory().CreateLogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>();
                var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);

                var request = new TestRequest();
                var context = RequestContext.Create(Guid.NewGuid().ToString());
                RequestHandlerCallback<TestResponse> nextStep = () =>
                    ValueTask.FromResult<Either<EncinaError, TestResponse>>(expectedResponse);

                // Act - Execute twice
                var result1 = await behavior.Handle(request, context, nextStep, CancellationToken.None);
                var result2 = await behavior.Handle(request, context, nextStep, CancellationToken.None);

                // Assert - Both should return the same result
                return result1.IsRight && result2.IsRight &&
                       result1.Match(
                           Right: r1 => result2.Match(
                               Right: r2 => r1.Value == r2.Value,
                               Left: _ => false),
                           Left: _ => false);
            });
    }

    [Property(Arbitrary = new[] { typeof(CorrelationIdGenerator) })]
    public Property Property_DifferentCorrelationIds_DoNotAffectResult(string correlationId)
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 100).Select(x => new TestResponse { Value = x })),
            async (expectedResponse) =>
            {
                // Arrange
                var registry = new ResiliencePipelineRegistry<string>();
                registry.TryAddBuilder("TestRequest", (builder, _) =>
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(10));
                });

                var logger = new LoggerFactory().CreateLogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>();
                var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);

                var request = new TestRequest();
                var context = RequestContext.Create(correlationId);
                RequestHandlerCallback<TestResponse> nextStep = () =>
                    ValueTask.FromResult<Either<EncinaError, TestResponse>>(expectedResponse);

                // Act
                var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);

                // Assert - Result should be Right regardless of correlation ID
                return result.IsRight &&
                       result.Match(
                           Right: r => r.Value == expectedResponse.Value,
                           Left: _ => false);
            });
    }

    [Property(MaxTest = 10)] // Limit tests due to async operations
    public Property Property_MultipleRequests_DoNotInterfere()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(3, 10).SelectMany(count =>
                Gen.ListOf(Gen.Choose(1, 100), count))),
            async (values) =>
            {
                // Arrange
                var registry = new ResiliencePipelineRegistry<string>();
                registry.TryAddBuilder("TestRequest", (builder, _) =>
                {
                    builder.AddTimeout(TimeSpan.FromSeconds(10));
                });

                var logger = new LoggerFactory().CreateLogger<StandardResiliencePipelineBehavior<TestRequest, TestResponse>>();
                var behavior = new StandardResiliencePipelineBehavior<TestRequest, TestResponse>(registry, logger);

                // Act - Execute multiple requests concurrently
                var tasks = values.Select(async value =>
                {
                    var request = new TestRequest();
                    var context = RequestContext.Create(Guid.NewGuid().ToString());
                    var expectedResponse = new TestResponse { Value = value };
                    RequestHandlerCallback<TestResponse> nextStep = () =>
                        ValueTask.FromResult<Either<EncinaError, TestResponse>>(expectedResponse);

                    var result = await behavior.Handle(request, context, nextStep, CancellationToken.None);
                    return result.Match(
                        Right: r => r.Value == value,
                        Left: _ => false);
                }).ToArray();

                var results = await Task.WhenAll(tasks);

                // Assert - All should succeed with correct values
                return results.All(x => x);
            });
    }

    // Test helper classes
    private sealed record TestRequest : IRequest<TestResponse>;
    private sealed record TestResponse
    {
        public int Value { get; init; }
    }
}

/// <summary>
/// Generator for correlation IDs.
/// </summary>
public class CorrelationIdGenerator
{
    public static Arbitrary<string> CorrelationIds() =>
        Arb.From(Gen.Elements(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            "correlation-123",
            "test-correlation",
            string.Empty));
}
