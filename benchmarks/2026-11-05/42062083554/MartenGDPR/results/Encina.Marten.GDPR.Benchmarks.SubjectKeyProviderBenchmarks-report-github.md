```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.98GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **185.62 ns** |   **125.306 ns** |   **6.868 ns** |  **1.12** |    **0.05** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,698.07 ns | 2,827.931 ns | 155.008 ns | 16.28 |    0.98 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    72.72 ns |     4.072 ns |   0.223 ns |  0.44 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   165.87 ns |   118.495 ns |   6.495 ns |  1.00 |    0.05 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **179.98 ns** |    **19.648 ns** |   **1.077 ns** |  **1.12** |    **0.04** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,637.40 ns | 3,277.093 ns | 179.629 ns | 16.43 |    1.13 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    71.20 ns |    58.728 ns |   3.219 ns |  0.44 |    0.02 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   160.74 ns |   122.154 ns |   6.696 ns |  1.00 |    0.05 |    2 | 0.0038 |      - |     328 B |        1.00 |
