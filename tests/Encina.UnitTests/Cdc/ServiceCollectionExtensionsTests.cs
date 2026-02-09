using Encina.Cdc;
using Encina.Cdc.Abstractions;
using Encina.Cdc.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Encina.UnitTests.Cdc;

/// <summary>
/// Unit tests for <see cref="ServiceCollectionExtensions"/>.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    #region Test Helpers

    private sealed class TestEntity
    {
        public int Id { get; set; }
    }

    private sealed class TestHandler : IChangeEventHandler<TestEntity>
    {
        public ValueTask<LanguageExt.Either<EncinaError, LanguageExt.Unit>> HandleInsertAsync(TestEntity entity, ChangeContext context)
            => new(LanguageExt.Prelude.Right(LanguageExt.Prelude.unit));

        public ValueTask<LanguageExt.Either<EncinaError, LanguageExt.Unit>> HandleUpdateAsync(TestEntity before, TestEntity after, ChangeContext context)
            => new(LanguageExt.Prelude.Right(LanguageExt.Prelude.unit));

        public ValueTask<LanguageExt.Either<EncinaError, LanguageExt.Unit>> HandleDeleteAsync(TestEntity entity, ChangeContext context)
            => new(LanguageExt.Prelude.Right(LanguageExt.Prelude.unit));
    }

    #endregion

    #region AddEncinaCdc

    [Fact]
    public void AddEncinaCdc_NullServices_ThrowsArgumentNullException()
    {
        IServiceCollection services = null!;

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdc(_ => { }));
    }

    [Fact]
    public void AddEncinaCdc_NullConfigure_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();

        Should.Throw<ArgumentNullException>(() =>
            services.AddEncinaCdc(null!));
    }

    [Fact]
    public void AddEncinaCdc_RegistersConfigurationSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdc(config => config.UseCdc());

        services.ShouldContain(d =>
            d.ServiceType == typeof(CdcConfiguration) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdc_RegistersOptionsSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdc(config => config.UseCdc());

        services.ShouldContain(d =>
            d.ServiceType == typeof(CdcOptions) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdc_RegistersDispatcherSingleton()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdc(config => config.UseCdc());

        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcDispatcher) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdc_RegistersDefaultPositionStore()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdc(config => config.UseCdc());

        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcPositionStore) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdc_WithHandler_RegistersHandlerAsScoped()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .AddHandler<TestEntity, TestHandler>();
        });

        var handlerType = typeof(IChangeEventHandler<TestEntity>);
        services.ShouldContain(d =>
            d.ServiceType == handlerType &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaCdc_WithMessagingBridge_RegistersInterceptor()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .WithMessagingBridge();
        });

        services.ShouldContain(d =>
            d.ServiceType == typeof(ICdcEventInterceptor) &&
            d.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddEncinaCdc_WithMessagingBridge_RegistersMessagingOptions()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdc(config =>
        {
            config.UseCdc()
                  .WithMessagingBridge();
        });

        services.ShouldContain(d =>
            d.ServiceType == typeof(CdcMessagingOptions) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddEncinaCdc_WithoutMessagingBridge_DoesNotRegisterInterceptor()
    {
        var services = new ServiceCollection();

        services.AddEncinaCdc(config => config.UseCdc());

        services.ShouldNotContain(d =>
            d.ServiceType == typeof(ICdcEventInterceptor));
    }

    [Fact]
    public void AddEncinaCdc_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();

        var result = services.AddEncinaCdc(config => config.UseCdc());

        result.ShouldBeSameAs(services);
    }

    #endregion
}
