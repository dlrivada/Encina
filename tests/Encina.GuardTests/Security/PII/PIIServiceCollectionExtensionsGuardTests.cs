using Encina.Security.PII;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Security.PII;

/// <summary>
/// Guard clause tests for <see cref="ServiceCollectionExtensions"/> (AddEncinaPII).
/// </summary>
public sealed class PIIServiceCollectionExtensionsGuardTests
{
    [Fact]
    public void AddEncinaPII_NullServices_ThrowsArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act & Assert
        var act = () => services.AddEncinaPII();
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddEncinaPII_NullConfigure_RegistersDefaultsAndReturnsSameCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddEncinaPII(null);

        // Assert
        result.ShouldBeSameAs(services);
        services.Count.ShouldBeGreaterThan(0);
    }
}
