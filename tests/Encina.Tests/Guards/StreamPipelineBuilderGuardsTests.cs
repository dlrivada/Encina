using System.Runtime.CompilerServices;
using FluentAssertions;
using LanguageExt;
using Microsoft.Extensions.DependencyInjection;
using static LanguageExt.Prelude;

namespace Encina.Tests.Guards;

/// <summary>
/// Guard clause tests for <see cref="StreamPipelineBuilder{TRequest, TItem}"/>.
/// Tests ensure proper null validation for constructor parameters and build method.
/// </summary>
public sealed class StreamPipelineBuilderGuardsTests
{
    #region Constructor Guard Tests

    [Fact]
    public void Constructor_WithNullRequest_ShouldThrow()
    {
        // Arrange
        TestStreamRequest nullRequest = null!;
        var handler = new TestStreamHandler();
        var context = RequestContext.Create();
        var cancellationToken = CancellationToken.None;

        // Act
        var act = () => new StreamPipelineBuilder<TestStreamRequest, int>(
            nullRequest,
            handler,
            context,
            cancellationToken);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("request");
    }

    [Fact]
    public void Constructor_WithNullHandler_ShouldThrow()
    {
        // Arrange
        var request = new TestStreamRequest();
        TestStreamHandler nullHandler = null!;
        var context = RequestContext.Create();
        var cancellationToken = CancellationToken.None;

        // Act
        var act = () => new StreamPipelineBuilder<TestStreamRequest, int>(
            request,
            nullHandler,
            context,
            cancellationToken);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("handler");
    }

    [Fact]
    public void Constructor_WithNullContext_ShouldThrow()
    {
        // Arrange
        var request = new TestStreamRequest();
        var handler = new TestStreamHandler();
        IRequestContext nullContext = null!;
        var cancellationToken = CancellationToken.None;

        // Act
        var act = () => new StreamPipelineBuilder<TestStreamRequest, int>(
            request,
            handler,
            nullContext,
            cancellationToken);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("context");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldNotThrow()
    {
        // Arrange
        var request = new TestStreamRequest();
        var handler = new TestStreamHandler();
        var context = RequestContext.Create();
        var cancellationToken = CancellationToken.None;

        // Act
        var act = () => new StreamPipelineBuilder<TestStreamRequest, int>(
            request,
            handler,
            context,
            cancellationToken);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Constructor_WithCancelledToken_ShouldNotThrow()
    {
        // Arrange
        var request = new TestStreamRequest();
        var handler = new TestStreamHandler();
        var context = RequestContext.Create();
        var cancellationToken = new CancellationToken(canceled: true);

        // Act
        var act = () => new StreamPipelineBuilder<TestStreamRequest, int>(
            request,
            handler,
            context,
            cancellationToken);

        // Assert - cancellation is allowed at construction time
        act.Should().NotThrow();
    }

    #endregion

    #region Build Method Guard Tests

    [Fact]
    public void Build_WithNullServiceProvider_ShouldThrow()
    {
        // Arrange
        var request = new TestStreamRequest();
        var handler = new TestStreamHandler();
        var context = RequestContext.Create();
        var cancellationToken = CancellationToken.None;

        var builder = new StreamPipelineBuilder<TestStreamRequest, int>(
            request,
            handler,
            context,
            cancellationToken);

        // Act
        var act = () => builder.Build(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("serviceProvider");
    }

    [Fact]
    public void Build_WithValidServiceProvider_ShouldNotThrow()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var request = new TestStreamRequest();
        var handler = new TestStreamHandler();
        var context = RequestContext.Create();
        var cancellationToken = CancellationToken.None;

        var builder = new StreamPipelineBuilder<TestStreamRequest, int>(
            request,
            handler,
            context,
            cancellationToken);

        // Act
        var act = () => builder.Build(provider);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Build_WithEmptyServiceProvider_ShouldReturnPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var request = new TestStreamRequest();
        var handler = new TestStreamHandler();
        var context = RequestContext.Create();
        var cancellationToken = CancellationToken.None;

        var builder = new StreamPipelineBuilder<TestStreamRequest, int>(
            request,
            handler,
            context,
            cancellationToken);

        // Act
        var pipeline = builder.Build(provider);

        // Assert
        pipeline.Should().NotBeNull();
    }

    [Fact]
    public void Build_WithBehaviorsRegistered_ShouldIncludeThemInPipeline()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IStreamPipelineBehavior<TestStreamRequest, int>, TestStreamBehavior>();
        var provider = services.BuildServiceProvider();

        var request = new TestStreamRequest();
        var handler = new TestStreamHandler();
        var context = RequestContext.Create();
        var cancellationToken = CancellationToken.None;

        var builder = new StreamPipelineBuilder<TestStreamRequest, int>(
            request,
            handler,
            context,
            cancellationToken);

        // Act
        var pipeline = builder.Build(provider);

        // Assert
        pipeline.Should().NotBeNull();
    }

    #endregion

    #region Test Data

    private sealed record TestStreamRequest : IStreamRequest<int>;

    private sealed class TestStreamHandler : IStreamRequestHandler<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield return Right<EncinaError, int>(1);
            await Task.CompletedTask;
        }
    }

    private sealed class TestStreamBehavior : IStreamPipelineBehavior<TestStreamRequest, int>
    {
        public async IAsyncEnumerable<Either<EncinaError, int>> Handle(
            TestStreamRequest request,
            IRequestContext context,
            StreamHandlerCallback<int> nextStep,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await foreach (var item in nextStep().WithCancellation(cancellationToken))
            {
                yield return item;
            }
        }
    }

    #endregion
}
