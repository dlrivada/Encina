using Microsoft.Extensions.Logging.Abstractions;

namespace Encina.Marten.GDPR.Benchmarks;

/// <summary>
/// Benchmarks for <see cref="InMemorySubjectKeyProvider"/> key lookup and creation throughput.
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class SubjectKeyProviderBenchmarks
{
    private InMemorySubjectKeyProvider _keyProvider = null!;
    private string[] _existingSubjectIds = null!;

    [Params(10, 100)]
    public int SubjectCount { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        _keyProvider = new InMemorySubjectKeyProvider(
            TimeProvider.System,
            NullLogger<InMemorySubjectKeyProvider>.Instance);

        // Pre-create subjects
        _existingSubjectIds = new string[SubjectCount];
        for (var i = 0; i < SubjectCount; i++)
        {
            var subjectId = $"bench-subject-{i}";
            _existingSubjectIds[i] = subjectId;
            _keyProvider.GetOrCreateSubjectKeyAsync(subjectId)
                .AsTask().GetAwaiter().GetResult();
        }
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _keyProvider.Clear();
    }

    [Benchmark(Baseline = true)]
    public async Task<byte[]?> GetExistingKey()
    {
        var subjectId = _existingSubjectIds[0];
        var result = await _keyProvider.GetSubjectKeyAsync(subjectId);
        byte[]? key = null;
        result.IfRight(k => key = k);
        return key;
    }

    [Benchmark]
    public async Task<byte[]?> GetOrCreateExistingKey()
    {
        var subjectId = _existingSubjectIds[0];
        var result = await _keyProvider.GetOrCreateSubjectKeyAsync(subjectId);
        byte[]? key = null;
        result.IfRight(k => key = k);
        return key;
    }

    [Benchmark]
    public async Task<byte[]?> CreateNewKey()
    {
        var subjectId = $"new-subject-{Guid.NewGuid():N}";
        var result = await _keyProvider.GetOrCreateSubjectKeyAsync(subjectId);
        byte[]? key = null;
        result.IfRight(k => key = k);
        return key;
    }

    [Benchmark]
    public async Task<bool> CheckIsForgotten()
    {
        var subjectId = _existingSubjectIds[0];
        var result = await _keyProvider.IsSubjectForgottenAsync(subjectId);
        bool forgotten = false;
        result.IfRight(f => forgotten = f);
        return forgotten;
    }
}
