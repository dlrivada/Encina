using Microsoft.Extensions.DependencyInjection;

namespace Encina.Security.PII.Benchmarks;

/// <summary>
/// Benchmarks for masking operations per <see cref="PIIType"/>.
/// Uses the public <see cref="PIIMasker.Mask(string, PIIType)"/> API which
/// delegates to the internal strategy implementations.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MaskingStrategyBenchmarks
{
    private PIIMasker _masker = null!;

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
    }

    [Benchmark(Baseline = true)]
    public string Email_Partial()
        => _masker.Mask("user@example.com", PIIType.Email);

    [Benchmark]
    public string Phone_Partial()
        => _masker.Mask("555-123-4567", PIIType.Phone);

    [Benchmark]
    public string CreditCard_Partial()
        => _masker.Mask("4111-1111-1111-1111", PIIType.CreditCard);

    [Benchmark]
    public string SSN_Partial()
        => _masker.Mask("123-45-6789", PIIType.SSN);

    [Benchmark]
    public string Name_Partial()
        => _masker.Mask("John Doe", PIIType.Name);

    [Benchmark]
    public string Address_Partial()
        => _masker.Mask("123 Main St, Springfield, IL", PIIType.Address);

    [Benchmark]
    public string DateOfBirth_Partial()
        => _masker.Mask("01/15/1990", PIIType.DateOfBirth);

    [Benchmark]
    public string IPAddress_Partial()
        => _masker.Mask("192.168.1.100", PIIType.IPAddress);

    [Benchmark]
    public string Custom_FullMasking()
        => _masker.Mask("SensitiveData12345", PIIType.Custom);

    [Benchmark]
    public string Email_Short()
        => _masker.Mask("a@b.co", PIIType.Email);

    [Benchmark]
    public string Email_Long()
        => _masker.Mask("very.long.email.address.with.dots@subdomain.example.com", PIIType.Email);

    [Benchmark]
    public string RegexPattern()
        => _masker.Mask("My ID is AB-12345", @"\b[A-Z]{2}-\d{5}\b");
}
