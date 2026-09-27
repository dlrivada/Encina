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
| **GetOrCreateExistingKey** | **10**           |   **156.4 ns** |  **12.46 ns** |  **0.68 ns** |  **1.18** |    **0.03** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,078.9 ns | 552.50 ns | 30.28 ns | 30.67 |    0.82 |    2 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |   116.8 ns |  11.53 ns |  0.63 ns |  0.88 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   133.1 ns |  72.10 ns |  3.95 ns |  1.00 |    0.04 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **156.8 ns** |  **11.74 ns** |  **0.64 ns** |  **1.19** |    **0.01** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,148.2 ns | 371.63 ns | 20.37 ns | 31.43 |    0.34 |    2 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |   111.1 ns |   5.19 ns |  0.28 ns |  0.84 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   132.0 ns |  27.54 ns |  1.51 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
