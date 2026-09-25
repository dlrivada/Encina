```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **162.2 ns** |    **12.61 ns** |  **0.69 ns** |  **1.15** |    **0.01** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,251.5 ns | 1,221.60 ns | 66.96 ns | 30.24 |    0.42 |    2 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   117.7 ns |     1.99 ns |  0.11 ns |  0.84 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   140.6 ns |     7.54 ns |  0.41 ns |  1.00 |    0.00 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |             |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **159.7 ns** |    **12.30 ns** |  **0.67 ns** |  **1.21** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,322.8 ns |   197.72 ns | 10.84 ns | 32.81 |    0.19 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   116.5 ns |     5.18 ns |  0.28 ns |  0.88 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   131.8 ns |    14.83 ns |  0.81 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
