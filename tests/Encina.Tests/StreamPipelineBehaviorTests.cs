using System.Runtime.CompilerServices;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Tests;

/// <summary>
/// Tests for streaming pipeline behaviors (<see cref="IStreamPipelineBehavior{TRequest, TItem}"/>).
/// </summary>
public sealed class StreamPipelineBehaviorTests
{
    #region Test Data

    public sealed record StreamNumbersQuery(int Count) : IStreamRequest<int>;

    public sealed class StreamNumbersHandler : IStreamRequestHandler<StreamNumbersQuery, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamNumbersQuery request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            for (var i = 1; i <= request.Count; i++)
            {
                await Task.Delay(1, cancellationToken);
                yield return Right<EncinaError, int>(i);
            }
        }
    }

    private sealed class StreamLoggingBehavior : IStreamPipelineBehavior<StreamNumbersQuery, int>
    {
        public List<string> Logs { get; } = new();

        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamNumbersQuery request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            Logs.Add("Stream started");
            var count = 0;

            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                count++;
                Logs.Add($"Item {count}");
                yield return item;
            }

            Logs.Add($"Stream completed: {count} items");
        }
    }

    private sealed class StreamTransformBehavior : IStreamPipelineBehavior<StreamNumbersQuery, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamNumbersQuery request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                // Multiply each successful item by 10
                yield return item.Map(value => value * 10);
            }
        }
    }

    private sealed class StreamFilterBehavior : IStreamPipelineBehavior<StreamNumbersQuery, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamNumbersQuery request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                // Only yield even numbers
                var shouldYield = item.Match(
                    Left: _ => true, // Always yield errors
                    Right: value => value % 2 == 0);

                if (shouldYield)
                {
                    yield return item;
                }
            }
        }
    }

    #endregion

    [Fact]
    public async Task StreamBehavior_ShouldWrapHandlerExecution()
    {
        // Arrange
        var loggingBehavior = new StreamLoggingBehavior();
        var services = new ServiceCollection();
        services.AddEncina(); // Register Encina without scanning assemblies
        services.AddTransient<IStreamRequestHandler<StreamNumbersQuery, int>, StreamNumbersHandler>();
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>>(_ => loggingBehavior);

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamNumbersQuery(3);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(query))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        loggingBehavior.Logs.Should().Contain("Stream started");
        loggingBehavior.Logs.Should().Contain("Item 1");
        loggingBehavior.Logs.Should().Contain("Item 2");
        loggingBehavior.Logs.Should().Contain("Item 3");
        loggingBehavior.Logs.Should().Contain("Stream completed: 3 items");
    }

    [Fact]
    public async Task StreamBehavior_Transform_ShouldModifyItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(); // Register Encina without scanning assemblies
        services.AddTransient<IStreamRequestHandler<StreamNumbersQuery, int>, StreamNumbersHandler>();
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>, StreamTransformBehavior>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamNumbersQuery(3);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(query))
        {
            results.Add(item);
        }

        // Assert
        results.Should().HaveCount(3);
        results.Should().AllSatisfy(r => r.ShouldBeSuccess());

        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(10, 20, 30); // Original: 1, 2, 3 → Transformed: 10, 20, 30
    }

    [Fact]
    public async Task StreamBehavior_Filter_ShouldOnlyYieldMatchingItems()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddEncina(); // Register Encina without scanning assemblies
        services.AddTransient<IStreamRequestHandler<StreamNumbersQuery, int>, StreamNumbersHandler>();
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>, StreamFilterBehavior>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamNumbersQuery(10);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(query))
        {
            results.Add(item);
        }

        // Assert - only even numbers (2, 4, 6, 8, 10)
        results.Should().HaveCount(5);
        results.Should().AllSatisfy(r => r.ShouldBeSuccess());

        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(2, 4, 6, 8, 10);
    }

    [Fact]
    public async Task StreamBehavior_Multiple_ShouldChainInOrder()
    {
        // Arrange
        var loggingBehavior = new StreamLoggingBehavior();
        var services = new ServiceCollection();
        services.AddEncina(); // Register Encina without scanning assemblies
        services.AddTransient<IStreamRequestHandler<StreamNumbersQuery, int>, StreamNumbersHandler>();

        // Register behaviors in order: Logging, Transform, Filter
        // Pipeline wrapping order (inside-out): Filter wraps Handler, Transform wraps Filter, Logging wraps Transform
        // Data flow from handler: Handler → Filter → Transform → Logging → User
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>>(_ => loggingBehavior);
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>, StreamTransformBehavior>();
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>, StreamFilterBehavior>();

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamNumbersQuery(6);

        // Act
        var results = new List<Either<EncinaError, int>>();
        await foreach (var item in Encina.Stream(query))
        {
            results.Add(item);
        }

        // Assert
        // Original from Handler: 1, 2, 3, 4, 5, 6
        // After Filter (even only, closest to handler): 2, 4, 6
        // After Transform (x10): 20, 40, 60
        // After Logging (pass-through): 20, 40, 60
        results.Should().HaveCount(3);

        var values = results.Select(r => r.Match(Left: _ => 0, Right: v => v)).ToList();
        values.Should().Equal(20, 40, 60);

        // Verify logging behavior executed (counts filtered items)
        loggingBehavior.Logs.Should().Contain("Stream started");
        loggingBehavior.Logs.Should().Contain("Stream completed: 3 items");
    }

    [Fact]
    public async Task StreamBehavior_WithContextPropagation_ShouldAccessContext()
    {
        // Arrange
        var contextCapture = new List<string>();

        var services = new ServiceCollection();
        services.AddEncina(); // Register Encina without scanning assemblies
        services.AddTransient<IStreamRequestHandler<StreamNumbersQuery, int>, StreamNumbersHandler>();
        services.AddTransient<IStreamPipelineBehavior<StreamNumbersQuery, int>>(sp =>
            new ContextCapturingBehavior(contextCapture));

        var provider = services.BuildServiceProvider();
        var Encina = provider.GetRequiredService<IEncina>();

        var query = new StreamNumbersQuery(2);

        // Act
        await foreach (var _ in Encina.Stream(query))
        {
            // Just consume the stream
        }

        // Assert
        contextCapture.Should().Contain(c => c.StartsWith("CorrelationId:", StringComparison.Ordinal));
    }

    private sealed class ContextCapturingBehavior : IStreamPipelineBehavior<StreamNumbersQuery, int>
    {
        private readonly List<string> _capture;

        public ContextCapturingBehavior(List<string> capture)
        {
            _capture = capture;
        }

        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamNumbersQuery request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            _capture.Add($"CorrelationId: {context.CorrelationId}");

            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }

    [Fact]
    public void AssemblyScanning_ShouldNotDuplicateBehaviors_WhenManuallyRegisteredFirst()
    {
        // Arrange - Register behavior manually BEFORE assembly scanning
        // The scanner should detect the existing registration and NOT add a duplicate
        var services = new ServiceCollection();

        // First, manually register the behavior
        services.AddScoped<IStreamPipelineBehavior<StreamNumbersQuery, int>, PublicStreamTransformBehavior>();

        // Count registrations before scanning
        var countBefore = services.Count(s =>
            s.ServiceType == typeof(IStreamPipelineBehavior<StreamNumbersQuery, int>) &&
            GetImplementationType(s) == typeof(PublicStreamTransformBehavior));

        // Act - Now scan the assembly (which would normally find and register PublicStreamTransformBehavior)
        services.AddEncina(typeof(StreamPipelineBehaviorTests).Assembly);

        // Count registrations after scanning
        var countAfter = services.Count(s =>
            s.ServiceType == typeof(IStreamPipelineBehavior<StreamNumbersQuery, int>) &&
            GetImplementationType(s) == typeof(PublicStreamTransformBehavior));

        // Assert - Count should remain 1 (no duplicate added by scanner)
        countBefore.Should().Be(1);
        countAfter.Should().Be(1, "scanner should not add duplicate when implementation type already registered");
    }

    [Fact]
    public void AssemblyScanning_CannotDeduplicateFactoryRegistrations()
    {
        // Arrange - Register behavior via factory BEFORE assembly scanning
        // The scanner cannot detect factory-based registrations because the implementation
        // type is not known until the factory is invoked at runtime.
        var services = new ServiceCollection();

        // Register via factory
        services.AddScoped<IStreamPipelineBehavior<StreamNumbersQuery, int>>(_ => new PublicStreamTransformBehavior());

        // Count PublicStreamTransformBehavior registrations before scanning
        var countBefore = services.Count(s =>
            s.ServiceType == typeof(IStreamPipelineBehavior<StreamNumbersQuery, int>) &&
            GetImplementationType(s) == typeof(PublicStreamTransformBehavior));

        // Act - Now scan the assembly
        services.AddEncina(typeof(StreamPipelineBehaviorTests).Assembly);

        // Count PublicStreamTransformBehavior registrations after scanning
        var countAfter = services.Count(s =>
            s.ServiceType == typeof(IStreamPipelineBehavior<StreamNumbersQuery, int>) &&
            GetImplementationType(s) == typeof(PublicStreamTransformBehavior));

        // Assert - Scanner adds another registration since it can't detect factory types
        // Factory registration has null implementation type (it's a Func delegate)
        countBefore.Should().Be(0, "factory registrations have null implementation type");
        // Scanner finds the public class and adds it
        countAfter.Should().Be(1, "scanner adds the type-based registration");

        // Total for this service type includes the factory + scanned registration
        var totalRegistrations = services.Count(s =>
            s.ServiceType == typeof(IStreamPipelineBehavior<StreamNumbersQuery, int>));
        totalRegistrations.Should().BeGreaterThan(1, "factory registration is not deduplicated");
    }

    private static Type? GetImplementationType(ServiceDescriptor descriptor)
    {
        if (descriptor.ImplementationType is not null)
        {
            return descriptor.ImplementationType;
        }

        if (descriptor.ImplementationInstance is not null)
        {
            return descriptor.ImplementationInstance.GetType();
        }

        return null;
    }

    /// <summary>
    /// Public behavior that will be discovered by assembly scanning.
    /// Used to test that manual + scanned registration doesn't cause duplicates.
    /// </summary>
    public sealed class PublicStreamTransformBehavior : IStreamPipelineBehavior<StreamNumbersQuery, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            StreamNumbersQuery request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item.Map(value => value * 10);
            }
        }
    }
}
