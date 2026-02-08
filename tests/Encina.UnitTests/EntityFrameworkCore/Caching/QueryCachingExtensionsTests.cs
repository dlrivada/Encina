using Encina.Caching;
using Encina.EntityFrameworkCore;
using Encina.EntityFrameworkCore.Caching;
using Microsoft.EntityFrameworkCore;

namespace Encina.UnitTests.EntityFrameworkCore.Caching;

/// <summary>
/// Unit tests for <see cref="QueryCachingExtensions"/>.
/// </summary>
public class QueryCachingExtensionsTests
{
    #region AddQueryCaching Tests

    [Fact]
    public void AddQueryCaching_WithNullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => QueryCachingExtensions.AddQueryCaching(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddQueryCaching_WithConfigure_NullServices_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => QueryCachingExtensions.AddQueryCaching(null!, _ => { });
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("services");
    }

    [Fact]
    public void AddQueryCaching_WithNullConfigure_ThrowsArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        var act = () => services.AddQueryCaching(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("configure");
    }

    [Fact]
    public void AddQueryCaching_RegistersKeyGenerator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICacheProvider>());

        // Act
        services.AddQueryCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var keyGenerator = provider.GetService<IQueryCacheKeyGenerator>();
        keyGenerator.ShouldNotBeNull();
        keyGenerator.ShouldBeOfType<DefaultQueryCacheKeyGenerator>();
    }

    [Fact]
    public void AddQueryCaching_RegistersInterceptor()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICacheProvider>());

        // Act
        services.AddQueryCaching();
        var provider = services.BuildServiceProvider();

        // Assert
        var interceptor = provider.GetService<QueryCacheInterceptor>();
        interceptor.ShouldNotBeNull();
    }

    [Fact]
    public void AddQueryCaching_WithConfigure_AppliesOptions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICacheProvider>());

        // Act
        services.AddQueryCaching(options =>
        {
            options.Enabled = true;
            options.DefaultExpiration = TimeSpan.FromMinutes(30);
            options.KeyPrefix = "custom:qc";
            options.ThrowOnCacheErrors = true;
        });

        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<QueryCacheOptions>>().Value;

        // Assert
        options.Enabled.ShouldBeTrue();
        options.DefaultExpiration.ShouldBe(TimeSpan.FromMinutes(30));
        options.KeyPrefix.ShouldBe("custom:qc");
        options.ThrowOnCacheErrors.ShouldBeTrue();
    }

    [Fact]
    public void AddQueryCaching_ReturnsSameServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddQueryCaching();

        // Assert
        result.ShouldBeSameAs(services);
    }

    [Fact]
    public void AddQueryCaching_TryAdd_DoesNotOverrideExistingRegistration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICacheProvider>());

        var customGenerator = Substitute.For<IQueryCacheKeyGenerator>();
        services.AddSingleton(customGenerator);

        // Act
        services.AddQueryCaching();
        var provider = services.BuildServiceProvider();

        // Assert — should resolve the custom one, not the default
        var resolved = provider.GetRequiredService<IQueryCacheKeyGenerator>();
        resolved.ShouldBeSameAs(customGenerator);
    }

    #endregion

    #region UseQueryCaching Tests

    [Fact]
    public void UseQueryCaching_WithNullOptionsBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        var sp = new ServiceCollection().BuildServiceProvider();

        // Act & Assert
        var act = () => QueryCachingExtensions.UseQueryCaching(null!, sp);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("optionsBuilder");
    }

    [Fact]
    public void UseQueryCaching_WithNullServiceProvider_ThrowsArgumentNullException()
    {
        // Arrange
        var optionsBuilder = new DbContextOptionsBuilder();

        // Act & Assert
        var act = () => optionsBuilder.UseQueryCaching(null!);
        var ex = Should.Throw<ArgumentNullException>(act);
        ex.ParamName.ShouldBe("serviceProvider");
    }

    [Fact]
    public void UseQueryCaching_WithNoCacheProvider_ThrowsInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddQueryCaching();
        // Note: ICacheProvider is NOT registered

        var provider = services.BuildServiceProvider();
        var optionsBuilder = new DbContextOptionsBuilder();

        // Act & Assert
        var ex = Should.Throw<InvalidOperationException>(() =>
            optionsBuilder.UseQueryCaching(provider));
        ex.Message.ShouldContain("ICacheProvider");
    }

    [Fact]
    public void UseQueryCaching_WithCacheProvider_ReturnsOptionsBuilder()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(Substitute.For<ICacheProvider>());
        services.AddQueryCaching();

        var provider = services.BuildServiceProvider();
        var optionsBuilder = new DbContextOptionsBuilder();

        // Act
        var result = optionsBuilder.UseQueryCaching(provider);

        // Assert
        result.ShouldBeSameAs(optionsBuilder);
    }

    #endregion
}
