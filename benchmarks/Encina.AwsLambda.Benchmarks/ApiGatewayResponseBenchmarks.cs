using BenchmarkDotNet.Attributes;
using LanguageExt;

namespace Encina.AwsLambda.Benchmarks;

/// <summary>
/// Benchmarks for API Gateway response creation performance.
/// </summary>
[MemoryDiagnoser]
[SimpleJob]
public class ApiGatewayResponseBenchmarks
{
    private Either<EncinaError, TestResult> _successResult;
    private Either<EncinaError, TestResult> _errorResult;
    private Either<EncinaError, Unit> _unitResult;

    [GlobalSetup]
    public void Setup()
    {
        _successResult = Either<EncinaError, TestResult>.Right(new TestResult { Id = 1, Name = "Test" });
        _errorResult = Either<EncinaError, TestResult>.Left(EncinaErrors.Create("validation.test", "Test error"));
        _unitResult = Either<EncinaError, Unit>.Right(Unit.Default);
    }

    [Benchmark(Baseline = true)]
    public object ToApiGatewayResponse_Success()
    {
        return _successResult.ToApiGatewayResponse();
    }

    [Benchmark]
    public object ToApiGatewayResponse_Error()
    {
        return _errorResult.ToApiGatewayResponse();
    }

    [Benchmark]
    public object ToCreatedResponse_Success()
    {
        return _successResult.ToCreatedResponse(r => $"/api/resources/{r.Id}");
    }

    [Benchmark]
    public object ToNoContentResponse_Success()
    {
        return _unitResult.ToNoContentResponse();
    }

    [Benchmark]
    public object ToHttpApiResponse_Success()
    {
        return _successResult.ToHttpApiResponse();
    }

    [Benchmark]
    public object ToHttpApiResponse_Error()
    {
        return _errorResult.ToHttpApiResponse();
    }

    public sealed class TestResult
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}
