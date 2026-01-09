using Microsoft.Azure.Functions.Worker;

namespace Encina.AzureFunctions.Tests;

public class FunctionsWorkerApplicationBuilderExtensionsTests
{
    [Fact]
    public void UseEncinaMiddleware_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            FunctionsWorkerApplicationBuilderExtensions.UseEncinaMiddleware(null!));
    }

    [Fact]
    public void UseEncinaMiddleware_ReturnsBuilder()
    {
        // Arrange
        var builder = Substitute.For<IFunctionsWorkerApplicationBuilder>();

        // Act
        var result = builder.UseEncinaMiddleware();

        // Assert
        result.ShouldBe(builder);
    }
}
