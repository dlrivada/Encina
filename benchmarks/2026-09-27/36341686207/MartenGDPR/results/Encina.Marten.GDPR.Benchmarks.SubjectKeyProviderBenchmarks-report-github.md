```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.38GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **133.35 ns** |    **91.03 ns** |   **4.990 ns** |  **1.11** |    **0.04** |    **2** | **0.0029** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 2,885.20 ns | 3,059.44 ns | 167.698 ns | 24.03 |    1.25 |    3 | 0.0076 | 0.0038 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |    76.61 ns |    23.80 ns |   1.304 ns |  0.64 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   120.09 ns |    31.67 ns |   1.736 ns |  1.00 |    0.02 |    2 | 0.0031 |      - |     272 B |        1.00 |
|                        |              |             |             |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **134.55 ns** |    **33.66 ns** |   **1.845 ns** |  **1.10** |    **0.05** |    **2** | **0.0029** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 2,718.82 ns | 5,667.90 ns | 310.677 ns | 22.23 |    2.38 |    3 | 0.0076 | 0.0038 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |    75.93 ns |    14.61 ns |   0.801 ns |  0.62 |    0.03 |    1 | 0.0013 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   122.47 ns |   106.55 ns |   5.840 ns |  1.00 |    0.06 |    2 | 0.0031 |      - |     272 B |        1.00 |
