```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **195.58 ns** |    **67.716 ns** |   **3.712 ns** |  **1.01** |    **0.02** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,406.91 ns | 3,030.941 ns | 166.136 ns | 17.63 |    0.75 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    86.02 ns |     9.021 ns |   0.494 ns |  0.45 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   193.28 ns |    19.524 ns |   1.070 ns |  1.00 |    0.01 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **194.84 ns** |    **20.074 ns** |   **1.100 ns** |  **0.99** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,015.82 ns | 5,358.341 ns | 293.709 ns | 15.32 |    1.30 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    84.20 ns |     2.627 ns |   0.144 ns |  0.43 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   196.92 ns |    29.147 ns |   1.598 ns |  1.00 |    0.01 |    2 | 0.0038 |      - |     328 B |        1.00 |
