```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **246.5 ns** |    **49.02 ns** |   **2.69 ns** |  **1.04** |    **0.01** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,843.6 ns | 3,032.33 ns | 166.21 ns | 16.21 |    0.62 |    3 | 0.0305 | 0.0229 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   108.9 ns |    17.19 ns |   0.94 ns |  0.46 |    0.01 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   237.2 ns |    41.53 ns |   2.28 ns |  1.00 |    0.01 |    2 | 0.0129 |      - |     328 B |        1.00 |
|                        |              |            |             |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **239.4 ns** |   **121.87 ns** |   **6.68 ns** |  **1.00** |    **0.03** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,911.3 ns | 1,733.27 ns |  95.01 ns | 16.40 |    0.38 |    3 | 0.0305 | 0.0229 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   109.4 ns |     9.70 ns |   0.53 ns |  0.46 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   238.6 ns |    49.36 ns |   2.71 ns |  1.00 |    0.01 |    2 | 0.0129 |      - |     328 B |        1.00 |
