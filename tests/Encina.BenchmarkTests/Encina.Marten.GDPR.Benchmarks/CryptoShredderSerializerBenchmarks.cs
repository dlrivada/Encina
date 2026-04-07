using Encina.Compliance.DataSubjectRights;

using Marten;

using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Marten.GDPR.Benchmarks;

/// <summary>
/// Benchmarks measuring the serializer overhead of crypto-shredding.
/// Compares plain serialization vs crypto-shredded serialization.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class CryptoShredderSerializerBenchmarks
{
    private ISerializer _innerSerializer = null!;
    private CryptoShredderSerializer _cryptoSerializer = null!;
    private InMemorySubjectKeyProvider _keyProvider = null!;
    private NonPiiBenchmarkEvent _nonPiiEvent = null!;
    private PiiBenchmarkEvent _piiEvent = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        // Get the default serializer from Marten's StoreOptions
        var opts = new StoreOptions();
        _innerSerializer = opts.Serializer();

        _keyProvider = new InMemorySubjectKeyProvider(
            TimeProvider.System,
            NullLogger<InMemorySubjectKeyProvider>.Instance);

        _cryptoSerializer = new CryptoShredderSerializer(
            _innerSerializer,
            _keyProvider,
            new DefaultForgottenSubjectHandler(
                NullLogger<DefaultForgottenSubjectHandler>.Instance),
            NullLogger<CryptoShredderSerializer>.Instance);

        _nonPiiEvent = new NonPiiBenchmarkEvent
        {
            EventName = "OrderShipped",
            OrderId = Guid.NewGuid().ToString(),
            Timestamp = DateTimeOffset.UtcNow
        };

        _piiEvent = new PiiBenchmarkEvent
        {
            UserId = "benchmark-user-1",
            Email = "benchmark@example.com",
            OrderId = Guid.NewGuid().ToString()
        };

        // Pre-create the key so benchmarks don't include key creation time
        _keyProvider.GetOrCreateSubjectKeyAsync("benchmark-user-1")
            .AsTask().GetAwaiter().GetResult();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _keyProvider.Clear();
        CryptoShreddedPropertyCache.ClearCache();
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

    #endregion
}
