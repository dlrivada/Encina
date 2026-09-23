using LanguageExt;
using static LanguageExt.Prelude;

namespace Encina.GuardTests.Core;

/// <summary>
/// Guard tests for <see cref="Encina"/> to verify constructor null guards,
/// Send with null request, and Publish with null notification.
/// </summary>
public class EncinaGuardTests
{
    #region Constructor

    /// <summary>
    /// Verifies that the constructor throws ArgumentNullException when scopeFactory is null.
    /// </summary>
    [Fact]
    public void Constructor_NullScopeFactory_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceScopeFactory scopeFactory = null!;

        // Act & Assert
        var act = () => new Encina(scopeFactory);
        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("scopeFactory");
    }

    /// <summary>
    /// Verifies that the constructor succeeds with a valid scopeFactory and null optional parameters.
    /// </summary>
    [Fact]
    public void Constructor_ValidScopeFactory_NullOptionals_DoesNotThrow()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        // Act & Assert
        var act = () => new Encina(scopeFactory, logger: null, notificationOptions: null);
        Should.NotThrow(act);
    }

    /// <summary>
    /// Verifies that the constructor stores the scopeFactory.
    /// </summary>
    [Fact]
    public void Constructor_ValidScopeFactory_StoresScopeFactory()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        // Act
        var sut = new Encina(scopeFactory);

        // Assert
        sut._scopeFactory.ShouldBeSameAs(scopeFactory);
    }

    /// <summary>
    /// Verifies that the constructor assigns a NullLogger when logger is null.
    /// </summary>
    [Fact]
    public void Constructor_NullLogger_AssignsNullLogger()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        // Act
        var sut = new Encina(scopeFactory, logger: null);

        // Assert
        sut._logger.ShouldNotBeNull();
    }

    /// <summary>
    /// Verifies that the constructor uses default notification options when null.
    /// </summary>
    [Fact]
    public void Constructor_NullNotificationOptions_UsesDefaults()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        // Act
        var sut = new Encina(scopeFactory, notificationOptions: null);

        // Assert
        sut._notificationOptions.ShouldNotBeNull();
        sut._notificationOptions.Strategy.ShouldBe(NotificationDispatchStrategy.Sequential);
    }

    /// <summary>
    /// Verifies that the constructor falls back to the default ambient accessor when none is given.
    /// </summary>
    [Fact]
    public void Constructor_NullRequestContextAccessor_UsesDefaultAccessor()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();

        // Act
        var sut = new Encina(scopeFactory, requestContextAccessor: null);

        // Assert
        sut._requestContextAccessor.ShouldBeOfType<RequestContextAccessor>();
    }

    /// <summary>
    /// Verifies that the constructor keeps the accessor it is given.
    /// </summary>
    [Fact]
    public void Constructor_WithRequestContextAccessor_StoresIt()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var accessor = Substitute.For<IRequestContextAccessor>();

        // Act
        var sut = new Encina(scopeFactory, requestContextAccessor: accessor);

        // Assert
        sut._requestContextAccessor.ShouldBeSameAs(accessor);
    }

    #endregion

    #region Explicit-context overloads

    /// <summary>
    /// Verifies that Send with an explicit context rejects a null context.
    /// </summary>
    [Fact]
    public async Task Send_ExplicitContext_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new Encina(Substitute.For<IServiceScopeFactory>());

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await sut.Send(new TestRequest(), null!));
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Send with an explicit context still returns a Left error for a null request.
    /// </summary>
    [Fact]
    public async Task Send_ExplicitContext_NullRequest_ReturnsLeftError()
    {
        // Arrange
        var sut = new Encina(Substitute.For<IServiceScopeFactory>());
        IRequest<string> request = null!;

        // Act
        var result = await sut.Send(request, RequestContext.CreateForTest());

        // Assert
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestNull));
    }

    /// <summary>
    /// Verifies that Publish with an explicit context rejects a null context.
    /// </summary>
    [Fact]
    public async Task Publish_ExplicitContext_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new Encina(Substitute.For<IServiceScopeFactory>());

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentNullException>(async () => await sut.Publish(new TestNotification(), null!));
        ex.ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Publish with an explicit context still returns a Left error for a null notification.
    /// </summary>
    [Fact]
    public async Task Publish_ExplicitContext_NullNotification_ReturnsLeftError()
    {
        // Arrange
        var sut = new Encina(Substitute.For<IServiceScopeFactory>());
        TestNotification notification = null!;

        // Act
        var result = await sut.Publish(notification, RequestContext.CreateForTest());

        // Assert
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationNull));
    }

    /// <summary>
    /// Verifies that Stream with an explicit context rejects a null context before enumeration.
    /// </summary>
    [Fact]
    public void Stream_ExplicitContext_NullContext_ThrowsArgumentNullException()
    {
        // Arrange
        var sut = new Encina(Substitute.For<IServiceScopeFactory>());

        // Act & Assert
        Should.Throw<ArgumentNullException>(() => sut.Stream(new TestStreamRequest(), null!))
            .ParamName.ShouldBe("context");
    }

    /// <summary>
    /// Verifies that Stream with an explicit context still yields a Left error for a null request.
    /// </summary>
    [Fact]
    public async Task Stream_ExplicitContext_NullRequest_YieldsLeftError()
    {
        // Arrange
        var sut = new Encina(Substitute.For<IServiceScopeFactory>());
        IStreamRequest<int> request = null!;

        // Act
        var items = new List<Either<EncinaError, int>>();
        await foreach (var item in sut.Stream(request, RequestContext.CreateForTest()))
        {
            items.Add(item);
        }

        // Assert
        items.ShouldHaveSingleItem().IsLeft.ShouldBeTrue();
    }

    #endregion

    #region Send

    /// <summary>
    /// Verifies that Send returns a Left error when request is null.
    /// </summary>
    [Fact]
    public async Task Send_NullRequest_ReturnsLeftError()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var sut = new Encina(scopeFactory);
        IRequest<string> request = null!;

        // Act
        var result = await sut.Send<string>(request);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.RequestNull));
    }

    /// <summary>
    /// Verifies that Send returns a Left error with the correct message when request is null.
    /// </summary>
    [Fact]
    public async Task Send_NullRequest_ErrorMessageContainsNull()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var sut = new Encina(scopeFactory);
        IRequest<string> request = null!;

        // Act
        var result = await sut.Send<string>(request);

        // Assert
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("null"));
    }

    #endregion

    #region Publish

    /// <summary>
    /// Verifies that Publish returns a Left error when notification is null.
    /// </summary>
    [Fact]
    public async Task Publish_NullNotification_ReturnsLeftError()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var sut = new Encina(scopeFactory);
        TestNotification notification = null!;

        // Act
        var result = await sut.Publish(notification);

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.GetEncinaCode().ShouldBe(EncinaErrorCodes.NotificationNull));
    }

    /// <summary>
    /// Verifies that Publish returns a Left error with the correct message when notification is null.
    /// </summary>
    [Fact]
    public async Task Publish_NullNotification_ErrorMessageContainsNull()
    {
        // Arrange
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var sut = new Encina(scopeFactory);
        TestNotification notification = null!;

        // Act
        var result = await sut.Publish(notification);

        // Assert
        result.Match(
            Right: _ => throw new InvalidOperationException("Expected Left"),
            Left: error => error.Message.ShouldContain("null"));
    }

    #endregion

    #region IsCancellationCode (internal)

    /// <summary>
    /// Verifies that IsCancellationCode returns false for null or whitespace input.
    /// </summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsCancellationCode_NullOrWhitespace_ReturnsFalse(string? code)
    {
        // Act
        var result = Encina.IsCancellationCode(code!);

        // Assert
        result.ShouldBeFalse();
    }

    /// <summary>
    /// Verifies that IsCancellationCode returns true for codes containing "cancelled".
    /// </summary>
    [Theory]
    [InlineData("handler.cancelled")]
    [InlineData("CANCELLED")]
    [InlineData("request_Cancelled")]
    public void IsCancellationCode_ContainsCancelled_ReturnsTrue(string code)
    {
        // Act
        var result = Encina.IsCancellationCode(code);

        // Assert
        result.ShouldBeTrue();
    }

    /// <summary>
    /// Verifies that IsCancellationCode returns false for non-cancellation codes.
    /// </summary>
    [Theory]
    [InlineData("handler.failed")]
    [InlineData("validation.error")]
    [InlineData("unknown")]
    public void IsCancellationCode_NonCancellationCode_ReturnsFalse(string code)
    {
        // Act
        var result = Encina.IsCancellationCode(code);

        // Assert
        result.ShouldBeFalse();
    }

    #endregion

    #region Test Stubs

    private sealed class TestNotification : INotification { }

    private sealed class TestRequest : IRequest<string> { }

    private sealed class TestStreamRequest : IStreamRequest<int> { }

    #endregion
}
