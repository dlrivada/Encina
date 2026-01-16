using Encina.Testing.Pact;
namespace Encina.UnitTests.Testing.Pact;

public sealed class EncinaPactConsumerBuilderTests : IDisposable
{
    private readonly string _testPactDir;
    private readonly EncinaPactConsumerBuilder _sut;

    public EncinaPactConsumerBuilderTests()
    {
        _testPactDir = Path.Combine(Path.GetTempPath(), $"pact-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testPactDir);
        _sut = new EncinaPactConsumerBuilder("TestConsumer", "TestProvider", _testPactDir);
    }

    public void Dispose()
    {
        _sut.Dispose();

        // Cleanup test directory - suppress exceptions to avoid masking test failures
        try
        {
            if (Directory.Exists(_testPactDir))
            {
                Directory.Delete(_testPactDir, true);
            }
        }
        catch (IOException)
        {
            // Directory may be locked by another process; ignore cleanup failure
        }
        catch (UnauthorizedAccessException)
        {
            // Insufficient permissions; ignore cleanup failure
        }
    }

    [Fact]
    public void Constructor_WithValidNames_SetsProperties()
    {
        // Assert
        _sut.ConsumerName.ShouldBe("TestConsumer");
        _sut.ProviderName.ShouldBe("TestProvider");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidConsumerName_Throws(string? invalidName)
    {
        // Arrange
        const string consumerName = "consumerName";

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder(invalidName!, "Provider"));
        ex.ParamName.ShouldBe(consumerName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidProviderName_Throws(string? invalidName)
    {
        // Arrange
        const string providerName = "providerName";

        // Act & Assert
        var ex = Should.Throw<ArgumentException>(() =>
            new EncinaPactConsumerBuilder("Consumer", invalidName!));
        ex.ParamName.ShouldBe(providerName);
    }

    [Fact]
    public void WithCommandExpectation_NullCommand_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _sut.WithCommandExpectation<TestCreateOrderCommand, TestOrderDto>(
                null!,
                Either<EncinaError, TestOrderDto>.Right(new TestOrderDto())));
    }

    [Fact]
    public void WithCommandExpectation_ValidCommand_ReturnsBuilder()
    {
        // Arrange
        var command = new TestCreateOrderCommand(Guid.NewGuid(), "Test Customer");
        var response = Either<EncinaError, TestOrderDto>.Right(new TestOrderDto { Id = command.OrderId });

        // Act
        var result = _sut.WithCommandExpectation(command, response);

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void WithCommandExpectation_WithDescription_ReturnsBuilder()
    {
        // Arrange
        var command = new TestCreateOrderCommand(Guid.NewGuid(), "Test Customer");
        var response = Either<EncinaError, TestOrderDto>.Right(new TestOrderDto { Id = command.OrderId });

        // Act
        var result = _sut.WithCommandExpectation(
            command,
            response,
            description: "Create a new order");

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void WithCommandExpectation_WithProviderState_ReturnsBuilder()
    {
        // Arrange
        var command = new TestCreateOrderCommand(Guid.NewGuid(), "Test Customer");
        var response = Either<EncinaError, TestOrderDto>.Right(new TestOrderDto { Id = command.OrderId });

        // Act
        var result = _sut.WithCommandExpectation(
            command,
            response,
            providerState: "user is authenticated");

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void WithQueryExpectation_NullQuery_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _sut.WithQueryExpectation<TestGetOrderByIdQuery, TestOrderDto>(
                null!,
                Either<EncinaError, TestOrderDto>.Right(new TestOrderDto())));
    }

    [Fact]
    public void WithQueryExpectation_ValidQuery_ReturnsBuilder()
    {
        // Arrange
        var query = new TestGetOrderByIdQuery(Guid.NewGuid());
        var response = Either<EncinaError, TestOrderDto>.Right(new TestOrderDto { Id = query.OrderId });

        // Act
        var result = _sut.WithQueryExpectation(query, response);

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void WithNotificationExpectation_NullNotification_Throws()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _sut.WithNotificationExpectation<TestOrderCreatedNotification>(null!));
    }

    [Fact]
    public void WithNotificationExpectation_ValidNotification_ReturnsBuilder()
    {
        // Arrange
        var notification = new TestOrderCreatedNotification(Guid.NewGuid(), "Test");

        // Act
        var result = _sut.WithNotificationExpectation(notification);

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void WithCommandFailureExpectation_NullCommand_Throws()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _sut.WithCommandFailureExpectation<TestCreateOrderCommand, TestOrderDto>(null!, error));
    }

    [Fact]
    public void WithCommandFailureExpectation_Valid_ReturnsBuilder()
    {
        // Arrange
        var command = new TestCreateOrderCommand(Guid.NewGuid(), "Test");
        var error = EncinaErrors.Create("test.error", "Test error");

        // Act
        var result = _sut.WithCommandFailureExpectation<TestCreateOrderCommand, TestOrderDto>(command, error);

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void WithQueryFailureExpectation_NullQuery_Throws()
    {
        // Arrange
        var error = EncinaErrors.Create("test.error", "Test error");

        // Act & Assert
        Should.Throw<ArgumentNullException>(() =>
            _sut.WithQueryFailureExpectation<TestGetOrderByIdQuery, TestOrderDto>(null!, error));
    }

    [Fact]
    public void WithQueryFailureExpectation_Valid_ReturnsBuilder()
    {
        // Arrange
        var query = new TestGetOrderByIdQuery(Guid.NewGuid());
        var error = EncinaErrors.Create("test.error", "Test error");

        // Act
        var result = _sut.WithQueryFailureExpectation<TestGetOrderByIdQuery, TestOrderDto>(query, error);

        // Assert
        result.ShouldBeSameAs(_sut);
    }

    [Fact]
    public void GetMockServerUri_BeforeVerify_ThrowsInvalidOperationException()
    {
        // Act & Assert
        var exception = Should.Throw<InvalidOperationException>(() => _sut.GetMockServerUri());
        exception.Message.ShouldContain("Mock server has not been started");
    }

    [Fact]
    public void Verify_WithNullAction_ThrowsArgumentNullException()
    {
        // Act & Assert
        Should.Throw<ArgumentNullException>(() => _sut.Verify(null!));
    }

    [Fact]
    public async Task VerifyAsync_WithNullAction_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () => await _sut.VerifyAsync(null!));
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert - should not throw
        var exception = Record.Exception(() =>
        {
            _sut.Dispose();
            _sut.Dispose();
        });
        Assert.Null(exception);
    }

    [Fact]
    public void Verify_AfterDispose_Throws()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => _sut.Verify(_ => { }));
    }

    [Fact]
    public async Task VerifyAsync_AfterDispose_Throws()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        await Should.ThrowAsync<ObjectDisposedException>(async () =>
            await _sut.VerifyAsync(_ => Task.CompletedTask));
    }

    [Fact]
    public void GetMockServerUri_AfterDispose_Throws()
    {
        // Arrange
        _sut.Dispose();

        // Act & Assert
        Should.Throw<ObjectDisposedException>(() => _sut.GetMockServerUri());
    }

    [Fact]
    public void FluentChaining_Works()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var command = new TestCreateOrderCommand(orderId, "Test");
        var query = new TestGetOrderByIdQuery(orderId);
        var notification = new TestOrderCreatedNotification(orderId, "Test");
        var response = Either<EncinaError, TestOrderDto>.Right(new TestOrderDto { Id = orderId });

        // Act
        var result = _sut
            .WithCommandExpectation(command, response, "Create order")
            .WithQueryExpectation(query, response, "Get order")
            .WithNotificationExpectation(notification, "Order created event");

        // Assert
        result.ShouldBeSameAs(_sut);
    }
}
