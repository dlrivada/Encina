```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **114.72 ns** |    **34.20 ns** |  **1.875 ns** |  **0.89** |    **0.06** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,004.66 ns | 1,347.48 ns | 73.860 ns | 23.35 |    1.70 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    73.65 ns |    19.65 ns |  1.077 ns |  0.57 |    0.04 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   129.28 ns |   198.60 ns | 10.886 ns |  1.00 |    0.10 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |             |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **113.15 ns** |    **67.71 ns** |  **3.712 ns** |  **0.95** |    **0.03** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,941.45 ns |   846.99 ns | 46.426 ns | 24.71 |    0.57 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    63.85 ns |    39.60 ns |  2.170 ns |  0.54 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   119.08 ns |    46.84 ns |  2.567 ns |  1.00 |    0.03 |    2 | 0.0196 |      - |     328 B |        1.00 |
