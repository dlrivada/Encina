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
| **GetOrCreateExistingKey** | **10**           |   **161.7 ns** |  **20.56 ns** |  **1.13 ns** |  **1.21** |    **0.02** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,240.4 ns | 635.65 ns | 34.84 ns | 31.81 |    0.59 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   117.3 ns |  12.05 ns |  0.66 ns |  0.88 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   133.3 ns |  47.79 ns |  2.62 ns |  1.00 |    0.02 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **155.2 ns** |  **19.59 ns** |  **1.07 ns** |  **1.20** |    **0.01** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,146.4 ns | 437.93 ns | 24.00 ns | 31.96 |    0.38 |    2 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   113.1 ns |  16.90 ns |  0.93 ns |  0.87 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   129.8 ns |  29.20 ns |  1.60 ns |  1.00 |    0.02 |    1 | 0.0162 |      - |     272 B |        1.00 |
