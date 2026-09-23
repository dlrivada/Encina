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
| **GetOrCreateExistingKey** | **10**           |   **164.7 ns** |   **9.61 ns** |  **0.53 ns** |  **1.27** |    **0.00** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,122.6 ns | 536.30 ns | 29.40 ns | 31.68 |    0.20 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   118.6 ns |  10.59 ns |  0.58 ns |  0.91 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   130.1 ns |   2.58 ns |  0.14 ns |  1.00 |    0.00 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **158.2 ns** |   **4.61 ns** |  **0.25 ns** |  **1.27** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,213.3 ns | 657.09 ns | 36.02 ns | 33.83 |    0.30 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   111.1 ns |   4.41 ns |  0.24 ns |  0.89 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   124.5 ns |  12.56 ns |  0.69 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
