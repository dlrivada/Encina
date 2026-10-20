```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.34GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **114.12 ns** |  **36.89 ns** |  **2.022 ns** |  **0.98** |    **0.04** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,142.02 ns | 229.88 ns | 12.600 ns | 27.02 |    1.05 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    56.77 ns |  15.81 ns |  0.866 ns |  0.49 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   116.45 ns |  97.75 ns |  5.358 ns |  1.00 |    0.06 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |           |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **114.77 ns** |  **45.52 ns** |  **2.495 ns** |  **0.93** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,210.06 ns | 340.52 ns | 18.665 ns | 26.15 |    0.38 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    61.38 ns |  44.64 ns |  2.447 ns |  0.50 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   122.78 ns |  34.99 ns |  1.918 ns |  1.00 |    0.02 |    2 | 0.0196 |      - |     328 B |        1.00 |
