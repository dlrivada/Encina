```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|---------:|----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **167.9 ns** |  **1.33 ns** |   **2.00 ns** |   **167.5 ns** |  **1.08** |    **0.13** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,499.2 ns | 94.52 ns | 135.56 ns | 4,479.3 ns | 28.85 |    3.51 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   118.0 ns |  0.29 ns |   0.43 ns |   118.0 ns |  0.76 |    0.09 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   158.1 ns | 12.83 ns |  18.81 ns |   173.3 ns |  1.01 |    0.17 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |          |           |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **165.6 ns** |  **2.35 ns** |   **3.52 ns** |   **165.8 ns** |  **1.23** |    **0.03** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,406.5 ns | 60.24 ns |  80.42 ns | 4,424.3 ns | 32.68 |    0.69 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   114.2 ns |  0.29 ns |   0.43 ns |   114.3 ns |  0.85 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   134.9 ns |  1.04 ns |   1.56 ns |   135.0 ns |  1.00 |    0.02 |    2 | 0.0162 |      - |     272 B |        1.00 |
