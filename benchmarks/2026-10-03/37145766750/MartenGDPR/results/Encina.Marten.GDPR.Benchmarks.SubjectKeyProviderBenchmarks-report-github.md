```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **160.5 ns** |  **11.07 ns** |  **0.61 ns** |  **1.23** |    **0.01** |    **2** | **0.0167** |      **-** |     **280 B** |        **1.03** |
| CreateNewKey           | 10           | 4,189.7 ns | 292.39 ns | 16.03 ns | 32.19 |    0.25 |    3 | 0.0534 | 0.0458 |     952 B |        3.50 |
| CheckIsForgotten       | 10           |   116.4 ns |   3.39 ns |  0.19 ns |  0.89 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   130.1 ns |  19.23 ns |  1.05 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **158.7 ns** |  **11.17 ns** |  **0.61 ns** |  **1.24** |    **0.01** |    **2** | **0.0167** |      **-** |     **280 B** |        **1.03** |
| CreateNewKey           | 100          | 4,151.3 ns | 757.43 ns | 41.52 ns | 32.53 |    0.42 |    3 | 0.0534 | 0.0458 |     952 B |        3.50 |
| CheckIsForgotten       | 100          |   110.9 ns |   6.65 ns |  0.36 ns |  0.87 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   127.6 ns |  26.13 ns |  1.43 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
