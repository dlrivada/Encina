using Encina.Compliance.Retention;

using Microsoft.Extensions.DependencyInjection;

namespace Encina.GuardTests.Compliance.Retention;

/// <summary>
/// Guard tests for <see cref="RetentionMartenExtensions"/> null parameter handling.
/// </summary>
public sealed class RetentionMartenExtensionsGuardTests
{
    [Fact]
    public void AddRetentionAggregates_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        var act = () => services.AddRetentionAggregates();

        Should.Throw<ArgumentNullException>(act).ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddRetentionAggregates_ValidServices_DoesNotThrow()
    {
        var services = new ServiceCollection();

        var act = () => services.AddRetentionAggregates();

        Should.NotThrow(act);
    }
}
