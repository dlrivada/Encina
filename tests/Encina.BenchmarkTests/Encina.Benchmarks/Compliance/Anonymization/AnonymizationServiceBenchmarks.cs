using BenchmarkDotNet.Attributes;
using Encina.Compliance.Anonymization;
using Encina.Compliance.Anonymization.Model;
using Microsoft.Extensions.DependencyInjection;

namespace Encina.Benchmarks.Compliance.Anonymization;

/// <summary>
/// Benchmarks for the core anonymization, pseudonymization, and tokenization operations.
/// Measures throughput and allocations for each operation type:
/// - Tokenization (UUID, prefixed formats)
/// - Pseudonymization (AES-256-GCM reversible, HMAC-SHA256 deterministic)
/// - Anonymization with data masking profile
/// - Risk assessment computation on datasets
/// </summary>
/// <remarks>
/// <para>
/// Anonymization operations execute on every data export/deletion request where anonymization
/// is the chosen strategy over erasure. These are cryptographically intensive operations whose
/// allocation profile and throughput are essential for capacity planning.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*AnonymizationServiceBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*AnonymizationServiceBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*AnonymizationService*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class AnonymizationServiceBenchmarks
{
    private ServiceProvider _provider = null!;
    private ITokenizer _tokenizer = null!;
    private IPseudonymizer _pseudonymizer = null!;
    private IAnonymizer _anonymizer = null!;
    private IRiskAssessor _riskAssessor = null!;
    private string _keyId = null!;

    private TokenizationOptions _uuidOptions = null!;
    private TokenizationOptions _prefixedOptions = null!;
    private AnonymizationProfile _maskingProfile = null!;
    private IReadOnlyList<BenchPerson> _riskDataset = null!;

    private static readonly IReadOnlyList<string> QuasiIdentifiers = new[] { "Age" }.AsReadOnly();

    private int _tokenCounter;

    [GlobalSetup]
    public async Task GlobalSetup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddEncinaAnonymization(options =>
        {
            options.EnforcementMode = AnonymizationEnforcementMode.Block;
            options.TrackAuditTrail = false;
        });

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = false,
            ValidateOnBuild = false
        });

        _tokenizer = _provider.GetRequiredService<ITokenizer>();
        _pseudonymizer = _provider.GetRequiredService<IPseudonymizer>();
        _anonymizer = _provider.GetRequiredService<IAnonymizer>();
        _riskAssessor = _provider.GetRequiredService<IRiskAssessor>();

        var keyProvider = _provider.GetRequiredService<IKeyProvider>();
        var keyResult = await keyProvider.GetActiveKeyIdAsync();
        _keyId = (string)keyResult;

        _uuidOptions = new TokenizationOptions { Format = TokenFormat.Uuid };
        _prefixedOptions = new TokenizationOptions { Format = TokenFormat.Prefixed, Prefix = "tok" };

        _maskingProfile = AnonymizationProfile.Create(
            name: "bench-masking",
            fieldRules:
            [
                new FieldAnonymizationRule
                {
                    FieldName = "Name",
                    Technique = AnonymizationTechnique.DataMasking,
                    Parameters = new Dictionary<string, object>
                    {
                        ["PreserveStart"] = 1,
                        ["PreserveEnd"] = 0
                    }
                },
                new FieldAnonymizationRule
                {
                    FieldName = "Email",
                    Technique = AnonymizationTechnique.DataMasking,
                    Parameters = new Dictionary<string, object>
                    {
                        ["PreserveDomain"] = true,
                        ["PreserveStart"] = 1
                    }
                }
            ]);

        _riskDataset = Enumerable.Range(0, 100).Select(i => new BenchPerson
        {
            Name = $"Person-{i % 10}",
            Email = $"user{i % 10}@example.com",
            Age = 20 + (i % 60)
        }).ToList().AsReadOnly();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _provider.Dispose();
    }

    // ────────────────────────────────────────────────────────────
    //  Tokenization — UUID (Baseline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "Tokenize: UUID format")]
    public async Task<string> Tokenize_Uuid()
    {
        var value = $"bench-uuid-{Interlocked.Increment(ref _tokenCounter)}";
        var result = await _tokenizer.TokenizeAsync(value, _uuidOptions);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Tokenization — Prefixed
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Tokenize: prefixed format")]
    public async Task<string> Tokenize_Prefixed()
    {
        var value = $"bench-pfx-{Interlocked.Increment(ref _tokenCounter)}";
        var result = await _tokenizer.TokenizeAsync(value, _prefixedOptions);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pseudonymization — AES-256-GCM (Reversible)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pseudonymize: AES-256-GCM")]
    public async Task<string> Pseudonymize_Aes256Gcm()
    {
        var result = await _pseudonymizer.PseudonymizeValueAsync(
            "john.doe@example.com", _keyId, PseudonymizationAlgorithm.Aes256Gcm);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pseudonymization — HMAC-SHA256 (Deterministic)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pseudonymize: HMAC-SHA256")]
    public async Task<string> Pseudonymize_HmacSha256()
    {
        var result = await _pseudonymizer.PseudonymizeValueAsync(
            "john.doe@example.com", _keyId, PseudonymizationAlgorithm.HmacSha256);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Pseudonymize + Depseudonymize Roundtrip
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Pseudonymize + Depseudonymize roundtrip")]
    public async Task<string> Pseudonymize_Depseudonymize_Roundtrip()
    {
        var pseudoResult = await _pseudonymizer.PseudonymizeValueAsync(
            "sensitive-roundtrip@example.com", _keyId, PseudonymizationAlgorithm.Aes256Gcm);
        var pseudonym = pseudoResult.Match(Left: _ => null!, Right: v => v);

        var depseudoResult = await _pseudonymizer.DepseudonymizeValueAsync(pseudonym, _keyId);
        return depseudoResult.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Anonymization — Data Masking (2 fields)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Anonymize: data masking (2 fields)")]
    public async Task<BenchPerson> Anonymize_DataMasking()
    {
        var data = new BenchPerson
        {
            Name = "John Smith",
            Email = "john@example.com",
            Age = 35
        };

        var result = await _anonymizer.AnonymizeAsync(data, _maskingProfile);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Risk Assessment — 100-record dataset
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "Risk assessment: 100-record dataset")]
    public async Task<RiskAssessmentResult> RiskAssessment_100Records()
    {
        var result = await _riskAssessor.AssessAsync(_riskDataset, QuasiIdentifiers);
        return result.Match(Left: _ => null!, Right: v => v);
    }

    // ────────────────────────────────────────────────────────────
    //  Test Data
    // ────────────────────────────────────────────────────────────

    public sealed class BenchPerson
    {
        public string? Name { get; set; }
        public string? Email { get; set; }
        public int Age { get; set; }
    }
}
