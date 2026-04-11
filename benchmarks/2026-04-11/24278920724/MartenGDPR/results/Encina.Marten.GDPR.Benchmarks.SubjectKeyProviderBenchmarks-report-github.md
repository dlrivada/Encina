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
| **GetOrCreateExistingKey** | **10**           |   **166.4 ns** |  **1.45 ns** |   **2.07 ns** |   **166.3 ns** |  **1.21** |    **0.02** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,383.0 ns | 77.44 ns | 111.06 ns | 4,361.0 ns | 31.96 |    0.80 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   118.7 ns |  0.95 ns |   1.36 ns |   117.7 ns |  0.87 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   137.1 ns |  0.31 ns |   0.45 ns |   137.1 ns |  1.00 |    0.00 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |          |           |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **161.2 ns** |  **0.24 ns** |   **0.36 ns** |   **161.2 ns** |  **1.14** |    **0.01** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,384.7 ns | 62.14 ns |  87.11 ns | 4,368.5 ns | 31.07 |    0.62 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   112.8 ns |  0.10 ns |   0.15 ns |   112.7 ns |  0.80 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   141.1 ns |  0.43 ns |   0.62 ns |   141.0 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
