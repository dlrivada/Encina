```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean       | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **156.8 ns** |  **1.11 ns** |   **1.63 ns** |  **1.16** |    **0.03** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,277.8 ns | 72.62 ns | 101.80 ns | 31.60 |    1.09 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   114.6 ns |  0.50 ns |   0.75 ns |  0.85 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   135.5 ns |  2.32 ns |   3.47 ns |  1.00 |    0.04 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |          |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **151.5 ns** |  **0.91 ns** |   **1.30 ns** |  **1.20** |    **0.01** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,299.2 ns | 85.28 ns | 122.31 ns | 34.15 |    0.98 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   113.3 ns |  1.29 ns |   1.84 ns |  0.90 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   125.9 ns |  0.55 ns |   0.81 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
