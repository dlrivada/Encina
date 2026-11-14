```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **232.47 ns** |    **27.380 ns** |   **1.501 ns** |  **1.01** |    **0.01** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,529.84 ns | 3,707.338 ns | 203.212 ns | 15.36 |    0.77 |    3 | 0.0343 | 0.0305 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   101.77 ns |     4.357 ns |   0.239 ns |  0.44 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   229.74 ns |    12.531 ns |   0.687 ns |  1.00 |    0.00 |    2 | 0.0129 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **229.72 ns** |     **9.925 ns** |   **0.544 ns** |  **1.01** |    **0.01** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,526.61 ns | 3,317.024 ns | 181.817 ns | 15.52 |    0.70 |    3 | 0.0343 | 0.0305 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    98.77 ns |     6.052 ns |   0.332 ns |  0.43 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   227.30 ns |    33.813 ns |   1.853 ns |  1.00 |    0.01 |    2 | 0.0129 |      - |     328 B |        1.00 |
