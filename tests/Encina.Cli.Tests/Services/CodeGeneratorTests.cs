using Encina.Cli.Services;
using FluentAssertions;
using Xunit;

namespace Encina.Cli.Tests.Services;

public class CodeGeneratorTests : IDisposable
{
    private readonly string _tempDir;

    public CodeGeneratorTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"encina-cli-tests-{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GenerateCommandHandlerAsync_WithValidOptions_GeneratesFiles()
    {
        // Arrange
        var options = new HandlerOptions
        {
            Name = "CreateOrder",
            ResponseType = "Unit",
            OutputDirectory = _tempDir,
            Namespace = "MyApp.Commands"
        };

        // Act
        var result = await CodeGenerator.GenerateCommandHandlerAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.GeneratedFiles.Should().HaveCount(2);

        var commandFile = Path.Combine(_tempDir, "CreateOrder.cs");
        var handlerFile = Path.Combine(_tempDir, "CreateOrderHandler.cs");

        File.Exists(commandFile).Should().BeTrue();
        File.Exists(handlerFile).Should().BeTrue();

        var commandContent = await File.ReadAllTextAsync(commandFile);
        commandContent.Should().Contain("namespace MyApp.Commands;");
        commandContent.Should().Contain("public sealed record CreateOrder : ICommand");

        var handlerContent = await File.ReadAllTextAsync(handlerFile);
        handlerContent.Should().Contain("public sealed class CreateOrderHandler : ICommandHandler<CreateOrder>");
    }

    [Fact]
    public async Task GenerateCommandHandlerAsync_WithResponseType_GeneratesTypedHandler()
    {
        // Arrange
        var options = new HandlerOptions
        {
            Name = "CreateOrder",
            ResponseType = "OrderId",
            OutputDirectory = _tempDir,
            Namespace = "MyApp.Commands"
        };

        // Act
        var result = await CodeGenerator.GenerateCommandHandlerAsync(options);

        // Assert
        result.Success.Should().BeTrue();

        var commandFile = Path.Combine(_tempDir, "CreateOrder.cs");
        var handlerFile = Path.Combine(_tempDir, "CreateOrderHandler.cs");

        var commandContent = await File.ReadAllTextAsync(commandFile);
        commandContent.Should().Contain("ICommand<OrderId>");

        var handlerContent = await File.ReadAllTextAsync(handlerFile);
        handlerContent.Should().Contain("ICommandHandler<CreateOrder, OrderId>");
        handlerContent.Should().Contain("Either<EncinaError, OrderId>");
    }

    [Fact]
    public async Task GenerateQueryHandlerAsync_WithValidOptions_GeneratesFiles()
    {
        // Arrange
        var options = new QueryOptions
        {
            Name = "GetOrderById",
            ResponseType = "OrderDto",
            OutputDirectory = _tempDir,
            Namespace = "MyApp.Queries"
        };

        // Act
        var result = await CodeGenerator.GenerateQueryHandlerAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.GeneratedFiles.Should().HaveCount(2);

        var queryFile = Path.Combine(_tempDir, "GetOrderById.cs");
        var handlerFile = Path.Combine(_tempDir, "GetOrderByIdHandler.cs");

        File.Exists(queryFile).Should().BeTrue();
        File.Exists(handlerFile).Should().BeTrue();

        var queryContent = await File.ReadAllTextAsync(queryFile);
        queryContent.Should().Contain("IQuery<OrderDto>");

        var handlerContent = await File.ReadAllTextAsync(handlerFile);
        handlerContent.Should().Contain("IQueryHandler<GetOrderById, OrderDto>");
    }

    [Fact]
    public async Task GenerateSagaAsync_WithValidOptions_GeneratesFiles()
    {
        // Arrange
        var options = new SagaOptions
        {
            Name = "OrderProcessing",
            Steps = ["ValidateOrder", "ProcessPayment", "ShipOrder"],
            OutputDirectory = _tempDir,
            Namespace = "MyApp.Sagas"
        };

        // Act
        var result = await CodeGenerator.GenerateSagaAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.GeneratedFiles.Should().HaveCount(2);

        var dataFile = Path.Combine(_tempDir, "OrderProcessingData.cs");
        var sagaFile = Path.Combine(_tempDir, "OrderProcessingSaga.cs");

        File.Exists(dataFile).Should().BeTrue();
        File.Exists(sagaFile).Should().BeTrue();

        var sagaContent = await File.ReadAllTextAsync(sagaFile);
        sagaContent.Should().Contain(".Step(\"ValidateOrder\")");
        sagaContent.Should().Contain(".Step(\"ProcessPayment\")");
        sagaContent.Should().Contain(".Step(\"ShipOrder\")");
    }

    [Fact]
    public async Task GenerateSagaAsync_WithEmptySteps_ReturnsError()
    {
        // Arrange
        var options = new SagaOptions
        {
            Name = "OrderProcessing",
            Steps = [],
            OutputDirectory = _tempDir,
            Namespace = "MyApp.Sagas"
        };

        // Act
        var result = await CodeGenerator.GenerateSagaAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("At least one step is required");
    }

    [Fact]
    public async Task GenerateNotificationAsync_WithValidOptions_GeneratesFiles()
    {
        // Arrange
        var options = new NotificationOptions
        {
            Name = "OrderCreated",
            OutputDirectory = _tempDir,
            Namespace = "MyApp.Events"
        };

        // Act
        var result = await CodeGenerator.GenerateNotificationAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.GeneratedFiles.Should().HaveCount(2);

        var notificationFile = Path.Combine(_tempDir, "OrderCreated.cs");
        var handlerFile = Path.Combine(_tempDir, "OrderCreatedHandler.cs");

        File.Exists(notificationFile).Should().BeTrue();
        File.Exists(handlerFile).Should().BeTrue();

        var notificationContent = await File.ReadAllTextAsync(notificationFile);
        notificationContent.Should().Contain("INotification");

        var handlerContent = await File.ReadAllTextAsync(handlerFile);
        handlerContent.Should().Contain("INotificationHandler<OrderCreated>");
    }

    [Fact]
    public void GenerateCommandHandlerAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await CodeGenerator.GenerateCommandHandlerAsync(null!);
        act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GenerateCommandHandlerAsync_WithInvalidName_ThrowsArgumentException(string name)
    {
        // Arrange
        var options = new HandlerOptions
        {
            Name = name,
            OutputDirectory = _tempDir
        };

        // Act & Assert
        var act = async () => await CodeGenerator.GenerateCommandHandlerAsync(options);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
