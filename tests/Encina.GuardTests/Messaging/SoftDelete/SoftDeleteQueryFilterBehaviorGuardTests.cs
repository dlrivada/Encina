using Encina.Messaging;
using Encina.Messaging.SoftDelete;
using IRequestContext = Encina.IRequestContext;

namespace Encina.GuardTests.Messaging.SoftDelete;

/// <summary>
/// Guard tests for <see cref="SoftDeleteQueryFilterBehavior{TRequest, TResponse}"/> to verify null parameter handling.
/// </summary>
public sealed class SoftDeleteQueryFilterBehaviorGuardTests
{
    [Fact]
    public void Constructor_NullFilterContext_ThrowsArgumentNullException()
    {
        // Arrange
        ISoftDeleteFilterContext filterContext = null!;
        var options = new SoftDeleteOptions();

        // Act & Assert
        var act = () => new SoftDeleteQueryFilterBehavior<TestRequest, TestResponse>(
            filterContext,
            options);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("filterContext");
    }

    [Fact]
    public void Constructor_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var filterContext = new SoftDeleteFilterContext();
        SoftDeleteOptions options = null!;

        // Act & Assert
        var act = () => new SoftDeleteQueryFilterBehavior<TestRequest, TestResponse>(
            filterContext,
            options);

        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("options");
    }

    [Fact]
    public async Task Handle_NullRequest_ThrowsArgumentNullException()
    {
        // Arrange
        var filterContext = new SoftDeleteFilterContext();
        var options = new SoftDeleteOptions();
        var behavior = new SoftDeleteQueryFilterBehavior<TestRequest, TestResponse>(
            filterContext,
            options);
        TestRequest request = null!;
        var context = new TestRequestContext();
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        Func<Task> act = () => behavior.Handle(
            request,
            context,
            () => throw new InvalidOperationException("Should not reach here"),
            cancellationToken).AsTask();

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("request");
    }

    [Fact]
    public async Task Handle_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var filterContext = new SoftDeleteFilterContext();
        var options = new SoftDeleteOptions();
        var behavior = new SoftDeleteQueryFilterBehavior<TestRequest, TestResponse>(
            filterContext,
            options);
        var request = new TestRequest();
        IRequestContext context = null!;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        Func<Task> act = () => behavior.Handle(
            request,
            context,
            () => throw new InvalidOperationException("Should not reach here"),
            cancellationToken).AsTask();

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("context");
    }

    [Fact]
    public async Task Handle_NullNextStep_ThrowsArgumentNullException()
    {
        // Arrange
        var filterContext = new SoftDeleteFilterContext();
        var options = new SoftDeleteOptions();
        var behavior = new SoftDeleteQueryFilterBehavior<TestRequest, TestResponse>(
            filterContext,
            options);
        var request = new TestRequest();
        var context = new TestRequestContext();
        RequestHandlerCallback<TestResponse> nextStep = null!;
        var cancellationToken = CancellationToken.None;

        // Act & Assert
        Func<Task> act = () => behavior.Handle(
            request,
            context,
            nextStep,
            cancellationToken).AsTask();

        var ex = await Should.ThrowAsync<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("nextStep");
    }

    private sealed record TestRequest : IRequest<TestResponse>;

    private sealed record TestResponse;

    private sealed class TestRequestContext : IRequestContext
    {
        public string CorrelationId => Guid.NewGuid().ToString();
        public string? UserId => null;
        public string? IdempotencyKey => null;
        public string? TenantId => null;
        public DateTimeOffset Timestamp => DateTimeOffset.UtcNow;
        public IReadOnlyDictionary<string, object?> Metadata => new Dictionary<string, object?>();

        public IRequestContext WithMetadata(string key, object? value) => this;
        public IRequestContext WithUserId(string? userId) => this;
        public IRequestContext WithIdempotencyKey(string? idempotencyKey) => this;
        public IRequestContext WithTenantId(string? tenantId) => this;
    }
}
