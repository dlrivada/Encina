```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **152.3 ns** |   **4.80 ns** |  **0.26 ns** |  **1.20** |    **0.01** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,978.8 ns | 358.86 ns | 19.67 ns | 31.30 |    0.27 |    2 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   112.7 ns |   2.02 ns |  0.11 ns |  0.89 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   127.1 ns |  20.46 ns |  1.12 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **151.8 ns** |   **4.88 ns** |  **0.27 ns** |  **1.19** |    **0.00** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,985.5 ns | 352.20 ns | 19.31 ns | 31.19 |    0.15 |    2 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   109.4 ns |   5.22 ns |  0.29 ns |  0.86 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   127.8 ns |   5.92 ns |  0.32 ns |  1.00 |    0.00 |    1 | 0.0162 |      - |     272 B |        1.00 |
