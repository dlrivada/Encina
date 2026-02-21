using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Security.PII.Benchmarks;

/// <summary>
/// Benchmarks for the JSON serialization overhead in PII masking.
/// The <see cref="PIIMasker.MaskObject{T}"/> method uses JSON round-trip
/// to create a deep copy before masking. These benchmarks measure the
/// serialization cost relative to the overall masking operation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class SerializationBenchmarks
{
    private PIIMasker _masker = null!;
    private JsonSerializerOptions _jsonOptions = null!;

    private SmallEntity _small = null!;
    private MediumEntity _medium = null!;
    private LargeEntity _large = null!;

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

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        _small = new SmallEntity
        {
            Email = "user@example.com",
            Name = "John"
        };

        _medium = new MediumEntity
        {
            Email = "user@example.com",
            Phone = "555-123-4567",
            CreditCard = "4111-1111-1111-1111",
            SSN = "123-45-6789",
            Name = "John Doe",
            Address = "123 Main St",
            City = "Springfield",
            State = "IL"
        };

        _large = new LargeEntity
        {
            Email = "user@example.com",
            Phone = "555-123-4567",
            CreditCard = "4111-1111-1111-1111",
            SSN = "123-45-6789",
            Name = "John Doe",
            Address = "123 Main St, Springfield, IL 62701",
            DateOfBirth = "01/15/1990",
            IPAddress = "192.168.1.100",
            Description = new string('x', 1024),
            Notes = new string('y', 512),
            Field1 = "value1",
            Field2 = "value2",
            Field3 = "value3",
            Field4 = "value4",
            Field5 = "value5"
        };
    }

    [Benchmark(Baseline = true)]
    public string Serialize_Small()
        => JsonSerializer.Serialize(_small, _jsonOptions);

    [Benchmark]
    public string Serialize_Medium()
        => JsonSerializer.Serialize(_medium, _jsonOptions);

    [Benchmark]
    public string Serialize_Large()
        => JsonSerializer.Serialize(_large, _jsonOptions);

    [Benchmark]
    public SmallEntity? SerializeRoundtrip_Small()
    {
        var json = JsonSerializer.Serialize(_small, _jsonOptions);
        return JsonSerializer.Deserialize<SmallEntity>(json, _jsonOptions);
    }

    [Benchmark]
    public LargeEntity? SerializeRoundtrip_Large()
    {
        var json = JsonSerializer.Serialize(_large, _jsonOptions);
        return JsonSerializer.Deserialize<LargeEntity>(json, _jsonOptions);
    }

    [Benchmark]
    public JsonNode? ParseAndModify_Small()
    {
        var json = JsonSerializer.Serialize(_small, _jsonOptions);
        var node = JsonNode.Parse(json);
        if (node is JsonObject obj)
        {
            obj["email"] = "m***@example.com";
        }

        return node;
    }

    [Benchmark]
    public SmallEntity MaskObject_Small()
        => _masker.MaskObject(_small);

    [Benchmark]
    public MediumEntity MaskObject_Medium()
        => _masker.MaskObject(_medium);

    [Benchmark]
    public LargeEntity MaskObject_Large()
        => _masker.MaskObject(_large);

    public sealed class SmallEntity
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
    }

    public sealed class MediumEntity
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
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
    }

    public sealed class LargeEntity
    {
        [PII(PIIType.Email)]
        public string Email { get; set; } = string.Empty;

        [PII(PIIType.Phone)]
        public string Phone { get; set; } = string.Empty;

        [PII(PIIType.CreditCard)]
        public string CreditCard { get; set; } = string.Empty;

        [PII(PIIType.SSN)]
        public string SSN { get; set; } = string.Empty;

        [PII(PIIType.Name)]
        public string Name { get; set; } = string.Empty;

        [PII(PIIType.Address)]
        public string Address { get; set; } = string.Empty;

        [PII(PIIType.DateOfBirth)]
        public string DateOfBirth { get; set; } = string.Empty;

        [PII(PIIType.IPAddress)]
        public string IPAddress { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string Field1 { get; set; } = string.Empty;
        public string Field2 { get; set; } = string.Empty;
        public string Field3 { get; set; } = string.Empty;
        public string Field4 { get; set; } = string.Empty;
        public string Field5 { get; set; } = string.Empty;
    }
}
