```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **225.7 ns** |   **104.14 ns** |   **5.71 ns** |  **1.00** |    **0.02** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,451.0 ns | 3,976.85 ns | 217.98 ns | 15.36 |    0.84 |    3 | 0.0343 | 0.0305 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   100.8 ns |     6.05 ns |   0.33 ns |  0.45 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   224.6 ns |     6.59 ns |   0.36 ns |  1.00 |    0.00 |    2 | 0.0129 |      - |     328 B |        1.00 |
|                        |              |            |             |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **221.3 ns** |     **7.18 ns** |   **0.39 ns** |  **0.96** |    **0.01** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,517.5 ns | 4,300.99 ns | 235.75 ns | 15.20 |    0.90 |    3 | 0.0343 | 0.0305 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   100.1 ns |     1.60 ns |   0.09 ns |  0.43 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   231.5 ns |    55.87 ns |   3.06 ns |  1.00 |    0.02 |    2 | 0.0129 |      - |     328 B |        1.00 |
