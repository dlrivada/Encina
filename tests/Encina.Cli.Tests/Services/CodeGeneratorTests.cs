using Encina.Cli.Services;
using Shouldly;
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
        result.Success.ShouldBeTrue();
        result.GeneratedFiles.Count.ShouldBe(2);

        var commandFile = Path.Combine(_tempDir, "CreateOrder.cs");
        var handlerFile = Path.Combine(_tempDir, "CreateOrderHandler.cs");

        File.Exists(commandFile).ShouldBeTrue();
        File.Exists(handlerFile).ShouldBeTrue();

        var commandContent = await File.ReadAllTextAsync(commandFile);
        commandContent.ShouldContain("namespace MyApp.Commands;");
        commandContent.ShouldContain("public sealed record CreateOrder : ICommand");

        var handlerContent = await File.ReadAllTextAsync(handlerFile);
        handlerContent.ShouldContain("public sealed class CreateOrderHandler : ICommandHandler<CreateOrder>");
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
        result.Success.ShouldBeTrue();

        var commandFile = Path.Combine(_tempDir, "CreateOrder.cs");
        var handlerFile = Path.Combine(_tempDir, "CreateOrderHandler.cs");

        var commandContent = await File.ReadAllTextAsync(commandFile);
        commandContent.ShouldContain("ICommand<OrderId>");

        var handlerContent = await File.ReadAllTextAsync(handlerFile);
        handlerContent.ShouldContain("ICommandHandler<CreateOrder, OrderId>");
        handlerContent.ShouldContain("Either<EncinaError, OrderId>");
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
        result.Success.ShouldBeTrue();
        result.GeneratedFiles.Count.ShouldBe(2);

        var queryFile = Path.Combine(_tempDir, "GetOrderById.cs");
        var handlerFile = Path.Combine(_tempDir, "GetOrderByIdHandler.cs");

        File.Exists(queryFile).ShouldBeTrue();
        File.Exists(handlerFile).ShouldBeTrue();

        var queryContent = await File.ReadAllTextAsync(queryFile);
        queryContent.ShouldContain("IQuery<OrderDto>");

        var handlerContent = await File.ReadAllTextAsync(handlerFile);
        handlerContent.ShouldContain("IQueryHandler<GetOrderById, OrderDto>");
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
        result.Success.ShouldBeTrue();
        result.GeneratedFiles.Count.ShouldBe(2);

        var dataFile = Path.Combine(_tempDir, "OrderProcessingData.cs");
        var sagaFile = Path.Combine(_tempDir, "OrderProcessingSaga.cs");

        File.Exists(dataFile).ShouldBeTrue();
        File.Exists(sagaFile).ShouldBeTrue();

        var sagaContent = await File.ReadAllTextAsync(sagaFile);
        sagaContent.ShouldContain(".Step(\"ValidateOrder\")");
        sagaContent.ShouldContain(".Step(\"ProcessPayment\")");
        sagaContent.ShouldContain(".Step(\"ShipOrder\")");
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
        result.Success.ShouldBeFalse();
        result.ErrorMessage!.ShouldContain("At least one step is required");
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
        result.Success.ShouldBeTrue();
        result.GeneratedFiles.Count.ShouldBe(2);

        var notificationFile = Path.Combine(_tempDir, "OrderCreated.cs");
        var handlerFile = Path.Combine(_tempDir, "OrderCreatedHandler.cs");

        File.Exists(notificationFile).ShouldBeTrue();
        File.Exists(handlerFile).ShouldBeTrue();

        var notificationContent = await File.ReadAllTextAsync(notificationFile);
        notificationContent.ShouldContain("INotification");

        var handlerContent = await File.ReadAllTextAsync(handlerFile);
        handlerContent.ShouldContain("INotificationHandler<OrderCreated>");
    }

    [Fact]
    public async Task GenerateCommandHandlerAsync_WithNullOptions_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(() => CodeGenerator.GenerateCommandHandlerAsync(null!));
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
        await Should.ThrowAsync<ArgumentException>(() => CodeGenerator.GenerateCommandHandlerAsync(options));
    }
}
