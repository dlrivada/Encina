```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **224.9 ns** |    **18.75 ns** |   **1.03 ns** |  **1.01** |    **0.01** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,860.4 ns | 2,608.85 ns | 143.00 ns | 17.28 |    0.56 |    3 | 0.0343 | 0.0305 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   101.9 ns |     0.56 ns |   0.03 ns |  0.46 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   223.4 ns |    16.23 ns |   0.89 ns |  1.00 |    0.00 |    2 | 0.0129 |      - |     328 B |        1.00 |
|                        |              |            |             |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **217.4 ns** |    **14.39 ns** |   **0.79 ns** |  **0.99** |    **0.00** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,795.7 ns | 3,442.69 ns | 188.71 ns | 17.26 |    0.75 |    3 | 0.0343 | 0.0305 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   116.4 ns |     2.37 ns |   0.13 ns |  0.53 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   219.9 ns |    15.00 ns |   0.82 ns |  1.00 |    0.00 |    2 | 0.0129 |      - |     328 B |        1.00 |
