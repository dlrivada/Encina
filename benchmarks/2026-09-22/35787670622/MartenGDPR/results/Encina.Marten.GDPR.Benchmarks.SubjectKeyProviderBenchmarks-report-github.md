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
| **GetOrCreateExistingKey** | **10**           |   **162.3 ns** |  **24.90 ns** |  **1.36 ns** |  **1.22** |    **0.02** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,139.2 ns |  52.01 ns |  2.85 ns | 31.07 |    0.37 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   116.9 ns |   4.95 ns |  0.27 ns |  0.88 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   133.3 ns |  33.97 ns |  1.86 ns |  1.00 |    0.02 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **160.0 ns** |  **31.45 ns** |  **1.72 ns** |  **1.21** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,156.8 ns | 817.30 ns | 44.80 ns | 31.49 |    0.36 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   112.7 ns |  10.46 ns |  0.57 ns |  0.85 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   132.0 ns |  18.64 ns |  1.02 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
