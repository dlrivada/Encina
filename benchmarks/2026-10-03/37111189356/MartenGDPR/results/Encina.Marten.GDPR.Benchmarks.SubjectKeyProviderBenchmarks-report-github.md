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
| **GetOrCreateExistingKey** | **10**           |   **157.0 ns** |     **6.09 ns** |  **0.33 ns** |  **1.19** |    **0.01** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,289.1 ns | 1,294.85 ns | 70.97 ns | 32.55 |    0.50 |    2 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |   116.6 ns |     5.47 ns |  0.30 ns |  0.88 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   131.8 ns |    15.57 ns |  0.85 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |             |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **160.1 ns** |    **10.83 ns** |  **0.59 ns** |  **1.18** |    **0.01** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,245.3 ns |   483.08 ns | 26.48 ns | 31.23 |    0.38 |    2 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |   113.8 ns |     9.07 ns |  0.50 ns |  0.84 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   136.0 ns |    31.27 ns |  1.71 ns |  1.00 |    0.02 |    1 | 0.0162 |      - |     272 B |        1.00 |
