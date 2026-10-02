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
| **GetOrCreateExistingKey** | **10**           |   **156.1 ns** |   **3.21 ns** |  **0.18 ns** |  **1.18** |    **0.01** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,090.0 ns | 392.44 ns | 21.51 ns | 30.95 |    0.26 |    2 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |   118.3 ns |   8.61 ns |  0.47 ns |  0.90 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   132.1 ns |  19.85 ns |  1.09 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **150.7 ns** |  **83.99 ns** |  **4.60 ns** |  **1.16** |    **0.03** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,129.3 ns |  71.78 ns |  3.93 ns | 31.78 |    0.24 |    2 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |   110.2 ns |   8.88 ns |  0.49 ns |  0.85 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   129.9 ns |  20.68 ns |  1.13 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
