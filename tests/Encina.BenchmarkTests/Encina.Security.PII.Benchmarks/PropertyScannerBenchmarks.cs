using Encina.Security.PII.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Security.PII.Benchmarks;

/// <summary>
/// Benchmarks for the property scanner and object-level masking pipeline.
/// The internal <c>PIIPropertyScanner</c> is exercised indirectly through
/// <see cref="PIIMasker.MaskObject{T}"/>, which includes property discovery,
/// JSON serialization, and strategy application.
/// </summary>
/// <remarks>
/// First-call benchmarks measure cold-cache performance (reflection + expression compilation).
/// Subsequent-call benchmarks measure warm-cache performance (dictionary lookup only).
/// </remarks>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class PropertyScannerBenchmarks
{
    private PIIMasker _masker = null!;

    private SingleFieldEntity _singleField = null!;
    private MultiFieldEntity _multiField = null!;
    private PlainEntity _plainEntity = null!;
    private MixedAttributeEntity _mixedEntity = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddEncinaPII(options =>
        {
            options.EnableTracing = false;
            options.EnableMetrics = false;
        });
        var provider = services.BuildServiceProvider();
        _masker = (PIIMasker)provider.GetRequiredService<IPIIMasker>();

        _singleField = new SingleFieldEntity { Email = "user@example.com", Name = "John" };
        _multiField = new MultiFieldEntity
        {
            Email = "user@example.com",
            Phone = "555-123-4567",
            CreditCard = "4111-1111-1111-1111",
            SSN = "123-45-6789",
            Name = "John Doe"
        };
        _plainEntity = new PlainEntity { Name = "John", Age = 30 };
        _mixedEntity = new MixedAttributeEntity
        {
            Email = "user@example.com",
            Secret = "top-secret",
            LogField = "debug-info",
            Normal = "normal-text"
        };

        // Warm cache for warm-cache benchmarks
        _masker.MaskObject(_singleField);
        _masker.MaskObject(_multiField);
        _masker.MaskObject(_plainEntity);
        _masker.MaskObject(_mixedEntity);
    }

    [Benchmark(Baseline = true)]
    public SingleFieldEntity MaskObject_SingleField_WarmCache()
        => _masker.MaskObject(_singleField);

    [Benchmark]
    public MultiFieldEntity MaskObject_MultiField_WarmCache()
        => _masker.MaskObject(_multiField);

    [Benchmark]
    public PlainEntity MaskObject_NoAttributes_WarmCache()
        => _masker.MaskObject(_plainEntity);

    [Benchmark]
    public MixedAttributeEntity MaskObject_MixedAttributes_WarmCache()
        => _masker.MaskObject(_mixedEntity);

    [Benchmark]
    public SingleFieldEntity MaskForAudit_SingleField()
        => _masker.MaskForAudit(_singleField);

    [Benchmark]
    public object MaskForAudit_NonGeneric()
        => _masker.MaskForAudit((object)_multiField);

    public sealed class SingleFieldEntity
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    public sealed class MultiFieldEntity
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = string.Empty;

        [PII(PIIType.Phone)]
        public string Phone { get; set; } = string.Empty;

        [PII(PIIType.CreditCard)]
        public string CreditCard { get; set; } = string.Empty;

        [PII(PIIType.SSN)]
        public string SSN { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    public sealed class PlainEntity
    {
        public string Name { get; set; } = string.Empty;
        public int Age { get; set; }
    }

    public sealed class MixedAttributeEntity
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = string.Empty;

        [SensitiveData]
        public string Secret { get; set; } = string.Empty;

        [MaskInLogs]
        public string LogField { get; set; } = string.Empty;

        public string Normal { get; set; } = string.Empty;
    }
}
