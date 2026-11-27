```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.39GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **179.71 ns** |    **85.662 ns** |   **4.695 ns** |  **1.07** |    **0.03** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,583.26 ns | 2,853.796 ns | 156.426 ns | 15.35 |    0.83 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    88.94 ns |     4.024 ns |   0.221 ns |  0.53 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   168.29 ns |    50.579 ns |   2.772 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **180.32 ns** |    **13.539 ns** |   **0.742 ns** |  **0.98** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,505.40 ns | 3,269.376 ns | 179.206 ns | 13.59 |    0.85 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    71.03 ns |     3.298 ns |   0.181 ns |  0.39 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   184.40 ns |    40.538 ns |   2.222 ns |  1.00 |    0.01 |    2 | 0.0038 |      - |     328 B |        1.00 |
