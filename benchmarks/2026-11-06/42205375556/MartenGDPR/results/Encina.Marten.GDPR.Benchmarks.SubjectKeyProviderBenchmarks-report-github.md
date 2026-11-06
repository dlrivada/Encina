```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **115.69 ns** |  **34.077 ns** |  **1.868 ns** |  **0.97** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,983.09 ns | 242.593 ns | 13.297 ns | 24.91 |    0.32 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    62.35 ns |   5.744 ns |  0.315 ns |  0.52 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   119.78 ns |  31.430 ns |  1.723 ns |  1.00 |    0.02 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |            |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **116.63 ns** |  **21.167 ns** |  **1.160 ns** |  **0.91** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,880.51 ns | 467.585 ns | 25.630 ns | 22.58 |    0.42 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    55.37 ns |  14.351 ns |  0.787 ns |  0.43 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   127.60 ns |  46.064 ns |  2.525 ns |  1.00 |    0.02 |    2 | 0.0196 |      - |     328 B |        1.00 |
