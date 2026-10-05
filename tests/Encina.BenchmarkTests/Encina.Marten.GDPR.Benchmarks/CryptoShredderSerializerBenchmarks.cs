using System.Text;

using Encina.Compliance.DataSubjectRights;
using Encina.Marten.GDPR.Abstractions;

using Marten;
using Marten.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Marten.GDPR.Benchmarks;

/// <summary>
/// Benchmarks measuring the serializer overhead of crypto-shredding through the System.Text.Json contract:
/// plain serialization against crypto-shredded serialization of flat and nested documents.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class CryptoShredderSerializerBenchmarks
{
    private ISerializer _innerSerializer = null!;
    private CryptoShredderSerializer _cryptoSerializer = null!;
    private InMemorySubjectKeyProvider _keyProvider = null!;
    private ServiceProvider _services = null!;
    private NonPiiBenchmarkEvent _nonPiiEvent = null!;
    private PiiBenchmarkEvent _piiEvent = null!;
    private NestedPiiBenchmarkEvent _nestedPiiEvent = null!;
    private NonPiiNestedBenchmarkEvent _nonPiiNestedEvent = null!;
    private byte[] _nestedPiiJson = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // The baseline uses its own Marten serializer, so the contract modifier never touches it.
        _innerSerializer = new StoreOptions().Serializer();

        _keyProvider = new InMemorySubjectKeyProvider(
            TimeProvider.System,
            NullLogger<InMemorySubjectKeyProvider>.Instance);

        var services = new ServiceCollection();
        services.AddSingleton<ISubjectKeyProvider>(_keyProvider);
        services.AddSingleton<IForgottenSubjectHandler>(new DefaultForgottenSubjectHandler(NullLogger<DefaultForgottenSubjectHandler>.Instance));
        _services = services.BuildServiceProvider();

        _cryptoSerializer = new CryptoShredderSerializer(
            (SystemTextJsonSerializer)new StoreOptions().Serializer(),
            _services.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CryptoShredderSerializer>.Instance);

        _nonPiiEvent = new NonPiiBenchmarkEvent
        {
            EventName = "OrderShipped",
            OrderId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UnixEpoch
        };

        _piiEvent = new PiiBenchmarkEvent
        {
            UserId = "benchmark-user-1",
            Email = "benchmark@example.com",
            OrderId = Guid.NewGuid().ToString()
        };

        _nestedPiiEvent = new NestedPiiBenchmarkEvent
        {
            Contact = new BenchmarkContact { SubjectId = "benchmark-user-1", Email = "benchmark@example.com" },
            Items = [.. Enumerable.Range(0, 10).Select(i => new BenchmarkContact { SubjectId = "benchmark-user-2", Email = $"item-{i}@example.com" })]
        };

        _nonPiiNestedEvent = new NonPiiNestedBenchmarkEvent
        {
            Inner = _nonPiiEvent,
            Items = [.. Enumerable.Range(0, 10).Select(_ => _nonPiiEvent)]
        };

        // Pre-create the keys so benchmarks don't include key creation time
        _keyProvider.GetOrCreateSubjectKeyAsync("benchmark-user-1").AsTask().GetAwaiter().GetResult();
        _keyProvider.GetOrCreateSubjectKeyAsync("benchmark-user-2").AsTask().GetAwaiter().GetResult();
        _nestedPiiJson = Encoding.UTF8.GetBytes(_cryptoSerializer.ToJson(_nestedPiiEvent));
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _keyProvider.Clear();
        _services.Dispose();
    }

    [Benchmark(Baseline = true)]
    public string InnerSerializer_NonPii()
    {
        return _innerSerializer.ToJson(_nonPiiEvent);
    }

    [Benchmark]
    public string CryptoSerializer_NonPii()
    {
        return _cryptoSerializer.ToJson(_nonPiiEvent);
    }

    [BenchmarkCategory("DocRef:bench:gdpr/crypto-serialize-pii")]
    [Benchmark]
    public string CryptoSerializer_PiiEvent()
    {
        return _cryptoSerializer.ToJson(_piiEvent);
    }

    [BenchmarkCategory("DocRef:bench:gdpr/crypto-serialize-nested-pii")]
    [Benchmark]
    public string CryptoSerializer_NestedPiiEvent()
    {
        return _cryptoSerializer.ToJson(_nestedPiiEvent);
    }

    [BenchmarkCategory("DocRef:bench:gdpr/crypto-serialize-nonpii-nested")]
    [Benchmark]
    public string CryptoSerializer_NonPiiNested()
    {
        return _cryptoSerializer.ToJson(_nonPiiNestedEvent);
    }

    [BenchmarkCategory("DocRef:bench:gdpr/crypto-deserialize-nested-pii")]
    [Benchmark]
    public NestedPiiBenchmarkEvent CryptoDeserializer_NestedPiiEvent()
    {
        using var stream = new MemoryStream(_nestedPiiJson);
        return _cryptoSerializer.FromJson<NestedPiiBenchmarkEvent>(stream);
    }

    #region Benchmark Events

    public class NonPiiBenchmarkEvent
    {
        public string EventName { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public DateTimeOffset Timestamp { get; set; }
    }

    public class PiiBenchmarkEvent
    {
        public string UserId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(UserId))]
        public string Email { get; set; } = string.Empty;

        public string OrderId { get; set; } = string.Empty;
    }

    public class BenchmarkContact
    {
        public string SubjectId { get; set; } = string.Empty;

        [PersonalData(Category = PersonalDataCategory.Contact, Erasable = true)]
        [CryptoShredded(SubjectIdProperty = nameof(SubjectId))]
        public string Email { get; set; } = string.Empty;
    }

    public class NestedPiiBenchmarkEvent
    {
        public BenchmarkContact Contact { get; set; } = new();

        public List<BenchmarkContact> Items { get; set; } = [];
    }

    public class NonPiiNestedBenchmarkEvent
    {
        public NonPiiBenchmarkEvent Inner { get; set; } = new();

        public List<NonPiiBenchmarkEvent> Items { get; set; } = [];
    }

    #endregion
}
