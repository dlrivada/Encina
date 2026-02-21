using Microsoft.Extensions.DependencyInjection;

namespace Encina.Security.PII.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="PIIMasker"/> end-to-end masking operations.
/// Measures throughput of Mask, MaskObject, and MaskForAudit methods
/// including JSON serialization and strategy resolution overhead.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class PIIMaskerBenchmarks
{
    private PIIMasker _masker = null!;

    private SingleFieldEntity _singleField = null!;
    private MultiFieldEntity _multiField = null!;
    private PlainEntity _plainEntity = null!;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddEncinaPII(options =>
        {
            options.EnableTracing = false;
            options.EnableMetrics = false;
            options.MaskInAuditTrails = true;
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

        // Warm the property scanner cache by calling MaskObject once
        _masker.MaskObject(_singleField);
        _masker.MaskObject(_multiField);
        _masker.MaskObject(_plainEntity);
    }

    [Benchmark(Baseline = true)]
    public string Mask_Email()
        => _masker.Mask("user@example.com", PIIType.Email);

    [Benchmark]
    public string Mask_Phone()
        => _masker.Mask("555-123-4567", PIIType.Phone);

    [Benchmark]
    public string Mask_CreditCard()
        => _masker.Mask("4111-1111-1111-1111", PIIType.CreditCard);

    [Benchmark]
    public string Mask_SSN()
        => _masker.Mask("123-45-6789", PIIType.SSN);

    [Benchmark]
    public string Mask_WithRegexPattern()
        => _masker.Mask("My ID is AB-12345", @"\b[A-Z]{2}-\d{5}\b");

    [Benchmark]
    public SingleFieldEntity MaskObject_SingleField()
        => _masker.MaskObject(_singleField);

    [Benchmark]
    public MultiFieldEntity MaskObject_MultiField()
        => _masker.MaskObject(_multiField);

    [Benchmark]
    public PlainEntity MaskObject_NoAttributes()
        => _masker.MaskObject(_plainEntity);

    [Benchmark]
    public SingleFieldEntity MaskForAudit_SingleField()
        => _masker.MaskForAudit(_singleField);

    [Benchmark]
    public object MaskForAudit_NonGeneric()
        => _masker.MaskForAudit((object)_singleField);

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
}
