using System.Collections.ObjectModel;
using BenchmarkDotNet.Attributes;
using Encina.Compliance.GDPR;
using Encina.Compliance.GDPR.Export;

namespace Encina.Benchmarks.Compliance.GDPR;

/// <summary>
/// Benchmarks for RoPA (Records of Processing Activities) export operations.
/// Measures throughput, allocations, and serialization cost for JSON and CSV exports
/// with varying activity counts (10, 50, 200).
/// </summary>
/// <remarks>
/// <para>
/// Article 30 GDPR requires controllers to export their processing activity records
/// for supervisory authority audits. While not a hot-path operation (typically quarterly/annual),
/// benchmarking export performance establishes baselines for capacity planning.
/// </para>
/// <para>
/// Run via:
/// <code>
/// cd tests/Encina.BenchmarkTests/Encina.Benchmarks
/// dotnet run -c Release -- --filter "*GDPRExportBenchmarks*"
///
/// # Quick validation:
/// dotnet run -c Release -- --filter "*GDPRExportBenchmarks*" --job short
///
/// # List available benchmarks:
/// dotnet run -c Release -- --list flat --filter "*GDPRExport*"
/// </code>
/// </para>
/// </remarks>
[MemoryDiagnoser]
[RankColumn]
public class GDPRExportBenchmarks
{
    private JsonRoPAExporter _jsonExporter = null!;
    private CsvRoPAExporter _csvExporter = null!;
    private IReadOnlyList<ProcessingActivity> _activities10 = null!;
    private IReadOnlyList<ProcessingActivity> _activities50 = null!;
    private IReadOnlyList<ProcessingActivity> _activities200 = null!;
    private RoPAExportMetadata _metadata = null!;

    private static readonly LawfulBasis[] AllBases =
        [LawfulBasis.Consent, LawfulBasis.Contract, LawfulBasis.LegalObligation,
         LawfulBasis.VitalInterests, LawfulBasis.PublicTask, LawfulBasis.LegitimateInterests];

    private static readonly string[] Purposes =
        ["Order fulfillment", "Newsletter subscription", "Employee payroll",
         "Marketing analytics", "Customer support", "Fraud detection",
         "Compliance audit", "Access control", "Data backup", "Research"];

    [GlobalSetup]
    public void GlobalSetup()
    {
        _jsonExporter = new JsonRoPAExporter();
        _csvExporter = new CsvRoPAExporter();

        _activities10 = GenerateActivities(10);
        _activities50 = GenerateActivities(50);
        _activities200 = GenerateActivities(200);

        _metadata = new RoPAExportMetadata(
            "Benchmark Corp",
            "privacy@benchmark.com",
            DateTimeOffset.UtcNow,
            new DataProtectionOfficer("Jane Smith", "dpo@benchmark.com", "+1-555-0100"));
    }

    // ────────────────────────────────────────────────────────────
    //  JSON Export — Small (10 Activities, Baseline)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Baseline = true, Description = "JSON export: 10 activities")]
    public async Task<int> JsonExport_10Activities()
    {
        var result = await _jsonExporter.ExportAsync(_activities10, _metadata);
        return result.Match(Left: _ => -1, Right: r => r.Content.Length);
    }

    // ────────────────────────────────────────────────────────────
    //  JSON Export — Medium (50 Activities)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "JSON export: 50 activities")]
    public async Task<int> JsonExport_50Activities()
    {
        var result = await _jsonExporter.ExportAsync(_activities50, _metadata);
        return result.Match(Left: _ => -1, Right: r => r.Content.Length);
    }

    // ────────────────────────────────────────────────────────────
    //  JSON Export — Large (200 Activities)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "JSON export: 200 activities")]
    public async Task<int> JsonExport_200Activities()
    {
        var result = await _jsonExporter.ExportAsync(_activities200, _metadata);
        return result.Match(Left: _ => -1, Right: r => r.Content.Length);
    }

    // ────────────────────────────────────────────────────────────
    //  CSV Export — Small (10 Activities)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "CSV export: 10 activities")]
    public async Task<int> CsvExport_10Activities()
    {
        var result = await _csvExporter.ExportAsync(_activities10, _metadata);
        return result.Match(Left: _ => -1, Right: r => r.Content.Length);
    }

    // ────────────────────────────────────────────────────────────
    //  CSV Export — Medium (50 Activities)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "CSV export: 50 activities")]
    public async Task<int> CsvExport_50Activities()
    {
        var result = await _csvExporter.ExportAsync(_activities50, _metadata);
        return result.Match(Left: _ => -1, Right: r => r.Content.Length);
    }

    // ────────────────────────────────────────────────────────────
    //  CSV Export — Large (200 Activities)
    // ────────────────────────────────────────────────────────────

    [Benchmark(Description = "CSV export: 200 activities")]
    public async Task<int> CsvExport_200Activities()
    {
        var result = await _csvExporter.ExportAsync(_activities200, _metadata);
        return result.Match(Left: _ => -1, Right: r => r.Content.Length);
    }

    // ────────────────────────────────────────────────────────────
    //  Infrastructure
    // ────────────────────────────────────────────────────────────

    private static ReadOnlyCollection<ProcessingActivity> GenerateActivities(int count)
    {
        var now = DateTimeOffset.UtcNow;
        return Enumerable.Range(0, count).Select(i => new ProcessingActivity
        {
            Id = Guid.NewGuid(),
            Name = $"Activity_{i:D4}",
            Purpose = Purposes[i % Purposes.Length],
            LawfulBasis = AllBases[i % AllBases.Length],
            CategoriesOfDataSubjects = ["Customers", "Employees", "Partners"],
            CategoriesOfPersonalData = ["Name", "Email", "Address", "Phone", "IP Address"],
            Recipients = ["Cloud Provider", "Analytics Service", "Tax Authority"],
            ThirdCountryTransfers = i % 3 == 0 ? "US (SCC), JP (Adequacy)" : null,
            Safeguards = i % 3 == 0 ? "Standard Contractual Clauses" : null,
            RetentionPeriod = TimeSpan.FromDays(365 * (1 + i % 7)),
            SecurityMeasures = "AES-256 at rest, TLS 1.3 in transit, RBAC, audit logging",
            RequestType = typeof(GDPRExportBenchmarks),
            CreatedAtUtc = now.AddDays(-i),
            LastUpdatedAtUtc = now
        }).ToList().AsReadOnly();
    }
}
