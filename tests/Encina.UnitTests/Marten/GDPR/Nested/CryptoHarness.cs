using System.Text;

using Encina.Marten.GDPR;
using Encina.Marten.GDPR.Abstractions;

using Marten;
using Marten.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Time.Testing;

using NSubstitute;

namespace Encina.UnitTests.Marten.GDPR.Nested;

/// <summary>
/// A real <see cref="CryptoShredderSerializer"/> over Marten's real System.Text.Json serializer, with a key
/// provider (the real in-memory one by default) and a substitute forgotten-subject handler resolved through DI.
/// </summary>
internal sealed class CryptoHarness : IDisposable
{
    internal const string Placeholder = "[REDACTED]";

    private readonly ServiceProvider _provider;

    internal CryptoHarness(ISubjectKeyProvider? keys = null)
    {
        Keys = keys ?? new InMemorySubjectKeyProvider(new FakeTimeProvider(), NullLogger<InMemorySubjectKeyProvider>.Instance);
        var services = new ServiceCollection();
        services.AddSingleton(Keys);
        services.AddSingleton(Handler);
        _provider = services.BuildServiceProvider();
        Inner = (SystemTextJsonSerializer)new StoreOptions().Serializer();
        Serializer = new CryptoShredderSerializer(Inner, ScopeFactory, Logger, Placeholder);
    }

    internal ISubjectKeyProvider Keys { get; }

    internal IForgottenSubjectHandler Handler { get; } = Substitute.For<IForgottenSubjectHandler>();

    internal FakeLogger<CryptoShredderSerializer> Logger { get; } = new();

    internal SystemTextJsonSerializer Inner { get; }

    internal CryptoShredderSerializer Serializer { get; }

    internal IServiceScopeFactory ScopeFactory => _provider.GetRequiredService<IServiceScopeFactory>();

    internal T RoundTrip<T>(T value) => FromJson<T>(Serializer.ToJson(value));

    internal T FromJson<T>(string json)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return Serializer.FromJson<T>(stream);
    }

    internal async Task<T> FromJsonAsync<T>(string json, CancellationToken cancellationToken = default)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return await Serializer.FromJsonAsync<T>(stream, cancellationToken);
    }

    public void Dispose() => _provider.Dispose();
}
