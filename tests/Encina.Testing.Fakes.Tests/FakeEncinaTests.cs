using Encina.Testing.Fakes;
using LanguageExt;

namespace Encina.Testing.Fakes.Tests;

public sealed class FakeEncinaTests
{
    private readonly FakeEncina _sut = new();

    #region Test DTOs

    public sealed record GetUserQuery(int UserId) : IRequest<UserDto>;
    public sealed record UserDto(int Id, string Name);

    public sealed record CreateOrderCommand(string Product) : IRequest<OrderResult>;
    public sealed record OrderResult(Guid OrderId);

    public sealed record OrderCreatedNotification(Guid OrderId) : INotification;

    public sealed record StreamProductsQuery(int PageSize = 10) : IStreamRequest<ProductDto>;
    public sealed record ProductDto(int Id, string Name);

    #endregion

    #region Send Tests

    [Fact]
    public async Task Send_WithConfiguredResponse_ReturnsSuccess()
    {
        // Arrange
        var expectedResponse = new UserDto(1, "Test User");
        _sut.SetupResponse<GetUserQuery, UserDto>(expectedResponse);

        // Act
        var result = await _sut.Send(new GetUserQuery(1));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r => r.ShouldBe(expectedResponse));
    }

    [Fact]
    public async Task Send_WithConfiguredResponseFactory_UsesRequestToGenerateResponse()
    {
        // Arrange
        _sut.SetupResponse<GetUserQuery, UserDto>(q => new UserDto(q.UserId, $"User {q.UserId}"));

        // Act
        var result = await _sut.Send(new GetUserQuery(42));

        // Assert
        result.IsRight.ShouldBeTrue();
        result.IfRight(r =>
        {
            r.Id.ShouldBe(42);
            r.Name.ShouldBe("User 42");
        });
    }

    [Fact]
    public async Task Send_WithConfiguredError_ReturnsError()
    {
        // Arrange
        var error = EncinaErrors.Create("test.not_found", "User not found");
        _sut.SetupError<GetUserQuery, UserDto>(error);

        // Act
        var result = await _sut.Send(new GetUserQuery(999));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.ShouldBe(error));
    }

    [Fact]
    public async Task Send_WithoutConfiguration_ReturnsHandlerMissingError()
    {
        // Act
        var result = await _sut.Send(new GetUserQuery(1));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.GetCode().IfSome(c => c.ShouldBe(EncinaErrorCodes.HandlerMissing)));
    }

    [Fact]
    public async Task Send_RecordsRequest()
    {
        // Arrange
        _sut.SetupResponse<GetUserQuery, UserDto>(new UserDto(1, "Test"));

        // Act
        await _sut.Send(new GetUserQuery(1));
        await _sut.Send(new GetUserQuery(2));

        // Assert
        _sut.SentRequests.Count.ShouldBe(2);
        _sut.WasSent<GetUserQuery>().ShouldBeTrue();
        _sut.GetSentCount<GetUserQuery>().ShouldBe(2);
    }

    [Fact]
    public async Task WasSent_WithPredicate_MatchesCorrectly()
    {
        // Arrange
        _sut.SetupResponse<GetUserQuery, UserDto>(new UserDto(1, "Test"));

        // Act
        await _sut.Send(new GetUserQuery(5));
        await _sut.Send(new GetUserQuery(10));

        // Assert
        _sut.WasSent<GetUserQuery>(q => q.UserId == 5).ShouldBeTrue();
        _sut.WasSent<GetUserQuery>(q => q.UserId == 10).ShouldBeTrue();
        _sut.WasSent<GetUserQuery>(q => q.UserId == 99).ShouldBeFalse();
    }

    [Fact]
    public async Task GetSentRequests_ReturnsFilteredRequests()
    {
        // Arrange
        _sut.SetupResponse<GetUserQuery, UserDto>(new UserDto(1, "Test"));
        _sut.SetupResponse<CreateOrderCommand, OrderResult>(new OrderResult(Guid.NewGuid()));

        // Act
        await _sut.Send(new GetUserQuery(1));
        await _sut.Send(new CreateOrderCommand("Widget"));
        await _sut.Send(new GetUserQuery(2));

        // Assert
        var userQueries = _sut.GetSentRequests<GetUserQuery>();
        userQueries.Count.ShouldBe(2);
        userQueries[0].UserId.ShouldBe(1);
        userQueries[1].UserId.ShouldBe(2);
    }

    #endregion

    #region Publish Tests

    [Fact]
    public async Task Publish_RecordsNotification()
    {
        // Act
        var notification = new OrderCreatedNotification(Guid.NewGuid());
        await _sut.Publish(notification);

        // Assert
        _sut.PublishedNotifications.Count.ShouldBe(1);
        _sut.WasPublished<OrderCreatedNotification>().ShouldBeTrue();
    }

    [Fact]
    public async Task Publish_WithConfiguredError_ReturnsError()
    {
        // Arrange
        var error = EncinaErrors.Create("test.publish_failed", "Failed to publish");
        _sut.SetupPublishError<OrderCreatedNotification>(error);

        // Act
        var result = await _sut.Publish(new OrderCreatedNotification(Guid.NewGuid()));

        // Assert
        result.IsLeft.ShouldBeTrue();
        result.IfLeft(e => e.ShouldBe(error));
    }

    [Fact]
    public async Task Publish_WithoutConfiguredError_ReturnsSuccess()
    {
        // Act
        var result = await _sut.Publish(new OrderCreatedNotification(Guid.NewGuid()));

        // Assert
        result.IsRight.ShouldBeTrue();
    }

    [Fact]
    public async Task WasPublished_WithPredicate_MatchesCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();

        // Act
        await _sut.Publish(new OrderCreatedNotification(orderId));
        await _sut.Publish(new OrderCreatedNotification(Guid.NewGuid()));

        // Assert
        _sut.WasPublished<OrderCreatedNotification>(n => n.OrderId == orderId).ShouldBeTrue();
        _sut.WasPublished<OrderCreatedNotification>(n => n.OrderId == Guid.Empty).ShouldBeFalse();
    }

    [Fact]
    public async Task GetPublishedNotifications_ReturnsFilteredNotifications()
    {
        // Arrange
        var orderId1 = Guid.NewGuid();
        var orderId2 = Guid.NewGuid();

        // Act
        await _sut.Publish(new OrderCreatedNotification(orderId1));
        await _sut.Publish(new OrderCreatedNotification(orderId2));

        // Assert
        var notifications = _sut.GetPublishedNotifications<OrderCreatedNotification>();
        notifications.Count.ShouldBe(2);
        var orderIds = notifications.Select(n => n.OrderId).ToList();
        orderIds.ShouldContain(orderId1);
        orderIds.ShouldContain(orderId2);
    }

    #endregion

    #region Stream Tests

    [Fact]
    public async Task Stream_WithConfiguredItems_YieldsItems()
    {
        // Arrange
        var products = new[]
        {
            new ProductDto(1, "Widget"),
            new ProductDto(2, "Gadget"),
            new ProductDto(3, "Gizmo")
        };
        _sut.SetupStream<StreamProductsQuery, ProductDto>(products);

        // Act
        var results = new List<ProductDto>();
        await foreach (var result in _sut.Stream(new StreamProductsQuery()))
        {
            result.IfRight(p => results.Add(p));
        }

        // Assert
        results.Count.ShouldBe(3);
        results.ShouldBe(products);
        _sut.WasStreamed<StreamProductsQuery>().ShouldBeTrue();
    }

    [Fact]
    public async Task Stream_WithConfiguredEitherResults_YieldsErrorsAndItems()
    {
        // Arrange
        var error = EncinaErrors.Create("test.validation_failed", "Invalid product");
        var results = new List<Either<EncinaError, ProductDto>>
        {
            new ProductDto(1, "Widget"),
            error,
            new ProductDto(2, "Gadget")
        };
        _sut.SetupStream<StreamProductsQuery, ProductDto>(results);

        // Act
        var successCount = 0;
        var errorCount = 0;
        await foreach (var result in _sut.Stream(new StreamProductsQuery()))
        {
            _ = result.Match(
                Left: _ => errorCount++,
                Right: _ => successCount++);
        }

        // Assert
        successCount.ShouldBe(2);
        errorCount.ShouldBe(1);
    }

    [Fact]
    public async Task Stream_WithoutConfiguration_YieldsEmpty()
    {
        // Act
        var count = 0;
        await foreach (var _ in _sut.Stream(new StreamProductsQuery()))
        {
            count++;
        }

        // Assert
        count.ShouldBe(0);
        _sut.WasStreamed<StreamProductsQuery>().ShouldBeTrue();
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task Clear_ResetsEverything()
    {
        // Arrange
        _sut.SetupResponse<GetUserQuery, UserDto>(new UserDto(1, "Test"));
        await _sut.Send(new GetUserQuery(1));
        await _sut.Publish(new OrderCreatedNotification(Guid.NewGuid()));

        // Act
        _sut.Clear();

        // Assert
        _sut.SentRequests.ShouldBeEmpty();
        _sut.PublishedNotifications.ShouldBeEmpty();

        // Setup should also be cleared
        var result = await _sut.Send(new GetUserQuery(1));
        result.IsLeft.ShouldBeTrue();
    }

    [Fact]
    public async Task ClearRecordedCalls_KeepsSetup()
    {
        // Arrange
        _sut.SetupResponse<GetUserQuery, UserDto>(new UserDto(1, "Test"));
        await _sut.Send(new GetUserQuery(1));
        await _sut.Publish(new OrderCreatedNotification(Guid.NewGuid()));

        // Act
        _sut.ClearRecordedCalls();

        // Assert
        _sut.SentRequests.ShouldBeEmpty();
        _sut.PublishedNotifications.ShouldBeEmpty();

        // Setup should still work
        var result = await _sut.Send(new GetUserQuery(1));
        result.IsRight.ShouldBeTrue();
    }

    #endregion

    #region Fluent API Tests

    [Fact]
    public async Task SetupResponse_SupportsFluentChaining_ReturnsConfiguredResponse()
    {
        // Arrange
        var expectedUser = new UserDto(1, "Test");

        // Act
        var chain = _sut.SetupResponse<GetUserQuery, UserDto>(expectedUser);
        var userResult = await chain.Send(new GetUserQuery(1));

        // Assert
        userResult.IsRight.ShouldBeTrue();
        userResult.IfRight(r => r.ShouldBe(expectedUser));
    }

    [Fact]
    public async Task SetupError_SupportsFluentChaining_ReturnsConfiguredError()
    {
        // Arrange
        var expectedValidationError = EncinaErrors.Create("test.validation_failed", "Invalid");

        // Act
        var chain = _sut.SetupError<CreateOrderCommand, OrderResult>(expectedValidationError);
        var orderResult = await chain.Send(new CreateOrderCommand("Widget"));

        // Assert
        orderResult.IsLeft.ShouldBeTrue();
        orderResult.IfLeft(e => e.GetCode().IfSome(c => c.ShouldBe("test.validation_failed")));
    }

    [Fact]
    public async Task SetupPublishError_SupportsFluentChaining_ReturnsConfiguredError()
    {
        // Arrange
        var expectedPublishError = EncinaErrors.Create("test.internal_error", "Failed");

        // Act
        var chain = _sut.SetupPublishError<OrderCreatedNotification>(expectedPublishError);
        var publishResult = await chain.Publish(new OrderCreatedNotification(Guid.NewGuid()));

        // Assert
        publishResult.IsLeft.ShouldBeTrue();
        publishResult.IfLeft(e => e.GetCode().IfSome(c => c.ShouldBe("test.internal_error")));
    }

    #endregion
}
