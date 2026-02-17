using Encina.IdGeneration;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.IdGeneration;

/// <summary>
/// Guard tests for <see cref="Encina.IdGeneration.ServiceCollectionExtensions"/> to verify null parameter handling.
/// </summary>
public sealed class ServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaIdGeneration_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddEncinaIdGeneration(_ => { });
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaIdGeneration_NullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddEncinaIdGeneration(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }
}
