```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **224.1 ns** |     **5.83 ns** |   **0.32 ns** |  **0.94** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,107.0 ns |   378.20 ns |  20.73 ns | 17.19 |    0.12 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   113.9 ns |     3.89 ns |   0.21 ns |  0.48 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   239.0 ns |    26.37 ns |   1.45 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |             |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **230.2 ns** |     **8.06 ns** |   **0.44 ns** |  **0.94** |    **0.00** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 4,105.6 ns | 2,095.80 ns | 114.88 ns | 16.72 |    0.41 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   111.3 ns |     2.47 ns |   0.14 ns |  0.45 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   245.5 ns |     8.05 ns |   0.44 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
