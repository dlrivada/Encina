using Encina.AzureFunctions;
using Microsoft.Azure.Functions.Worker;
using EncinaBuilderExtensions = Encina.AzureFunctions.FunctionsWorkerApplicationBuilderExtensions;

namespace Encina.UnitTests.AzureFunctions;

public class FunctionsWorkerApplicationBuilderExtensionsTests
{
    [Fact]
    public void UseEncinaMiddleware_WithNullBuilder_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            EncinaBuilderExtensions.UseEncinaMiddleware(null!));
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
