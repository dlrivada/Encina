using System.Reflection;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR;

using Marten;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

using NSubstitute;

namespace Encina.UnitTests.Marten.GDPR.Nested;

/// <summary>
/// Builds a service provider with <c>AddEncinaMartenGdpr</c> over a substitute document store whose options are
/// real Marten <see cref="StoreOptions"/> configured by the crypto-shredding configurator.
/// </summary>
internal sealed class StartupValidationHost : IDisposable
{
    internal StartupValidationHost(
        Assembly scanned,
        Action<IServiceCollection>? before = null,
        Action<IServiceCollection>? after = null,
        Action<StoreOptions>? configureStore = null,
        bool validateOnStartup = true,
        bool configureMarten = true)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerProvider>(LoggerProvider);
        services.AddLogging(b => b.SetMinimumLevel(LogLevel.Trace));
        services.AddScoped(_ => Substitute.For<IDocumentSession>());
        before?.Invoke(services);
        services.AddEncinaMartenGdpr(o =>
        {
            o.AssembliesToScan.Add(scanned);
            o.ValidateOnStartup = validateOnStartup;
        });
        after?.Invoke(services);

        var store = Substitute.For<IDocumentStore>();
        services.AddSingleton(store);
        Provider = services.BuildServiceProvider();

        StoreOptions = new StoreOptions();
        if (configureMarten)
        {
            foreach (var configure in Provider.GetServices<IConfigureOptions<StoreOptions>>().OfType<ConfigureMartenCryptoShredding>())
            {
                configure.Configure(StoreOptions);
            }
        }

        configureStore?.Invoke(StoreOptions);
        store.Options.Returns(StoreOptions);
    }

    internal FakeLoggerProvider LoggerProvider { get; } = new();

    internal ServiceProvider Provider { get; }

    internal StoreOptions StoreOptions { get; }

    internal IReadOnlyList<FakeLogRecord> Logs => LoggerProvider.Collector.GetSnapshot();

    internal Task StartAsync() =>
        Provider.GetServices<IHostedService>().OfType<CryptoShreddingStartupValidationHostedService>().Single().StartAsync(CancellationToken.None);

    public void Dispose() => Provider.Dispose();
}

/// <summary>A locator registered by the application.</summary>
internal sealed class ApplicationLocator : IPersonalDataLocator
{
    public ValueTask<LanguageExt.Either<EncinaError, IReadOnlyList<PersonalDataLocation>>> LocateAllDataAsync(
        string subjectId, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult<LanguageExt.Either<EncinaError, IReadOnlyList<PersonalDataLocation>>>(Array.Empty<PersonalDataLocation>());
}

/// <summary>An erasure strategy registered by the application.</summary>
internal sealed class ApplicationErasureStrategy : IDataErasureStrategy
{
    internal List<PersonalDataLocation> Received { get; } = [];

    public ValueTask<LanguageExt.Either<EncinaError, LanguageExt.Unit>> EraseFieldAsync(
        PersonalDataLocation location, CancellationToken cancellationToken = default)
    {
        Received.Add(location);
        return ValueTask.FromResult<LanguageExt.Either<EncinaError, LanguageExt.Unit>>(LanguageExt.Unit.Default);
    }
}
