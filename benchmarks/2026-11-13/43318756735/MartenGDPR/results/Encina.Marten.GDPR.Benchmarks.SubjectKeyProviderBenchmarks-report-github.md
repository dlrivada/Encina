```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **221.8 ns** |    **19.44 ns** |  **1.07 ns** |  **0.97** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,544.6 ns | 1,214.03 ns | 66.55 ns | 19.88 |    0.26 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   111.0 ns |     1.60 ns |  0.09 ns |  0.49 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   228.6 ns |    15.80 ns |  0.87 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |             |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **216.1 ns** |     **2.04 ns** |  **0.11 ns** |  **0.92** |    **0.00** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 4,538.6 ns |   949.48 ns | 52.04 ns | 19.30 |    0.20 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   100.9 ns |    17.39 ns |  0.95 ns |  0.43 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   235.1 ns |    16.56 ns |  0.91 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
