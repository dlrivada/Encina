```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **189.71 ns** |    **28.102 ns** |   **1.540 ns** |  **1.08** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,915.28 ns | 2,790.467 ns | 152.955 ns | 22.37 |    0.81 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    85.50 ns |     7.443 ns |   0.408 ns |  0.49 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   175.02 ns |    47.044 ns |   2.579 ns |  1.00 |    0.02 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **173.89 ns** |    **20.218 ns** |   **1.108 ns** |  **0.97** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,854.64 ns |   367.394 ns |  20.138 ns | 21.51 |    0.11 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    86.60 ns |     3.117 ns |   0.171 ns |  0.48 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   179.18 ns |     8.637 ns |   0.473 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
