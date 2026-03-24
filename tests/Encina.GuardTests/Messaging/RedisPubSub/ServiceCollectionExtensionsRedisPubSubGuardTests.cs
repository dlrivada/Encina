using Encina.Redis.PubSub;

namespace Encina.GuardTests.Messaging.RedisPubSub;

/// <summary>
/// Guard clause tests for Redis Pub/Sub <see cref="ServiceCollectionExtensions"/>.
/// Verifies that null parameters are properly guarded.
/// </summary>
public sealed class ServiceCollectionExtensionsRedisPubSubGuardTests
{
    #region AddEncinaRedisPubSub Guards

    /// <summary>
    /// Verifies that AddEncinaRedisPubSub throws ArgumentNullException when services is null.
    /// </summary>
    [Fact]
    public void AddEncinaRedisPubSub_NullServices_ShouldThrowArgumentNullException()
    {
        // Arrange
        IServiceCollection services = null!;

        // Act
        var act = () => services.AddEncinaRedisPubSub();

        // Assert
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    #endregion
}
