```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **228.1 ns** |    **22.70 ns** |   **1.24 ns** |  **0.98** |    **0.00** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,703.8 ns |   879.92 ns |  48.23 ns | 20.14 |    0.18 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   128.2 ns |    74.32 ns |   4.07 ns |  0.55 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   233.6 ns |     8.18 ns |   0.45 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |             |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **227.7 ns** |    **17.92 ns** |   **0.98 ns** |  **0.95** |    **0.00** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 4,774.5 ns | 2,049.54 ns | 112.34 ns | 19.99 |    0.41 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   111.5 ns |     3.52 ns |   0.19 ns |  0.47 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   238.8 ns |    17.29 ns |   0.95 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
