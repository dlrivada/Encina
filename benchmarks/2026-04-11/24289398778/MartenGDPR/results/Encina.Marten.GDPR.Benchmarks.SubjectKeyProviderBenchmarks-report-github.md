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
| **GetOrCreateExistingKey** | **10**           |   **162.8 ns** |  **0.99 ns** |   **1.45 ns** |   **163.1 ns** |  **1.26** |    **0.02** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,316.5 ns | 78.14 ns | 106.96 ns | 4,313.2 ns | 33.41 |    0.97 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   119.1 ns |  1.01 ns |   1.46 ns |   119.5 ns |  0.92 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   129.2 ns |  1.41 ns |   2.12 ns |   128.8 ns |  1.00 |    0.02 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |          |           |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **160.8 ns** |  **1.88 ns** |   **2.81 ns** |   **160.8 ns** |  **1.25** |    **0.03** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,290.0 ns | 94.93 ns | 136.15 ns | 4,284.3 ns | 33.31 |    1.20 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   116.6 ns |  3.45 ns |   5.05 ns |   120.9 ns |  0.91 |    0.04 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   128.9 ns |  1.62 ns |   2.37 ns |   128.8 ns |  1.00 |    0.03 |    2 | 0.0162 |      - |     272 B |        1.00 |
