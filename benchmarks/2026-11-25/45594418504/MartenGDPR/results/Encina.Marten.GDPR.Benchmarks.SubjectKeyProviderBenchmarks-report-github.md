```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **178.84 ns** |    **11.141 ns** |   **0.611 ns** |  **1.01** |    **0.02** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,678.57 ns | 1,406.886 ns |  77.116 ns | 15.18 |    0.49 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    83.38 ns |     6.484 ns |   0.355 ns |  0.47 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   176.49 ns |    77.504 ns |   4.248 ns |  1.00 |    0.03 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **170.02 ns** |    **16.760 ns** |   **0.919 ns** |  **0.98** |    **0.02** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,576.63 ns | 2,546.636 ns | 139.590 ns | 14.84 |    0.79 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    78.63 ns |    52.783 ns |   2.893 ns |  0.45 |    0.02 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   173.75 ns |    90.329 ns |   4.951 ns |  1.00 |    0.03 |    2 | 0.0038 |      - |     328 B |        1.00 |
