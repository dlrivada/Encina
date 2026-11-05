```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **213.54 ns** |     **7.722 ns** |   **0.423 ns** |  **1.01** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,941.92 ns | 4,836.747 ns | 265.118 ns | 13.94 |    1.10 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    95.89 ns |    13.414 ns |   0.735 ns |  0.45 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   211.07 ns |    48.685 ns |   2.669 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **211.05 ns** |    **16.391 ns** |   **0.898 ns** |  **1.00** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,244.83 ns | 1,573.459 ns |  86.247 ns | 15.32 |    0.39 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    92.09 ns |    10.171 ns |   0.557 ns |  0.43 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   211.84 ns |    49.334 ns |   2.704 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
