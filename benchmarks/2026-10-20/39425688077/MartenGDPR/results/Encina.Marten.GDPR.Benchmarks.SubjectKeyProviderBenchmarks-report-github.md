```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.92GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **216.65 ns** |     **9.855 ns** |   **0.540 ns** |  **1.01** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,734.57 ns | 2,587.125 ns | 141.809 ns | 17.45 |    0.60 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   113.64 ns |     6.895 ns |   0.378 ns |  0.53 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   214.02 ns |    42.626 ns |   2.336 ns |  1.00 |    0.01 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **214.98 ns** |     **5.842 ns** |   **0.320 ns** |  **1.02** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,422.80 ns | 2,426.787 ns | 133.020 ns | 16.27 |    0.57 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    92.04 ns |     6.103 ns |   0.335 ns |  0.44 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   210.46 ns |    47.626 ns |   2.611 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
