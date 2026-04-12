```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean       | Error    | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|---------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **158.4 ns** |  **1.13 ns** |   **1.65 ns** |  **1.22** |    **0.02** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,241.8 ns | 97.44 ns | 136.60 ns | 32.76 |    1.09 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   117.8 ns |  0.18 ns |   0.26 ns |  0.91 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   129.5 ns |  0.91 ns |   1.36 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |          |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **153.7 ns** |  **1.11 ns** |   **1.62 ns** |  **1.20** |    **0.01** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,225.4 ns | 98.40 ns | 137.95 ns | 32.94 |    1.08 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   113.8 ns |  1.79 ns |   2.67 ns |  0.89 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   128.3 ns |  0.56 ns |   0.81 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
