```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                     | Mean       | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |-----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| CreateChangeEvent          | 12.5622 ns | 7.0208 ns | 0.3848 ns |  0.52 |    0.02 | 0.0081 |     136 B |        2.43 |
| ChangeEvent_Equals         |  0.4577 ns | 0.0385 ns | 0.0021 ns |  0.02 |    0.00 |      - |         - |        0.00 |
| ChangeEvent_WithExpression |  7.3238 ns | 5.5199 ns | 0.3026 ns |  0.30 |    0.01 | 0.0033 |      56 B |        1.00 |
| CreateChangeMetadata       | 24.0796 ns | 8.8628 ns | 0.4858 ns |  1.00 |    0.02 | 0.0033 |      56 B |        1.00 |
