```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 30.305 ns | 18.9951 ns | 1.0412 ns |  0.83 |    0.02 | 0.0016 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.640 ns |  0.3831 ns | 0.0210 ns |  0.05 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 16.121 ns | 11.5389 ns | 0.6325 ns |  0.44 |    0.02 | 0.0007 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.362 ns |  0.7319 ns | 0.0401 ns |  1.00 |    0.00 | 0.0007 |      56 B |        1.00 |
