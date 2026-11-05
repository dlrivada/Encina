```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.95GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean      | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 24.369 ns | 17.2004 ns | 0.9428 ns |  0.67 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  1.382 ns |  0.0455 ns | 0.0025 ns |  0.04 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression | 13.326 ns |  7.4882 ns | 0.4105 ns |  0.36 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 36.552 ns |  0.5872 ns | 0.0322 ns |  1.00 |    0.00 | 0.0033 |      56 B |        1.00 |
