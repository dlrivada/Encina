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
| **GetExistingKey**         | **10**           |   **143.3 ns** |  **2.76 ns** |   **4.13 ns** |  **1.00** |    **0.04** |    **2** | **0.0162** |      **-** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 10           |   162.9 ns |  0.86 ns |   1.23 ns |  1.14 |    0.03 |    3 | 0.0148 |      - |     248 B |        0.91 |
| CreateNewKey           | 10           | 4,351.1 ns | 75.76 ns | 106.20 ns | 30.38 |    1.12 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   116.1 ns |  0.79 ns |   1.18 ns |  0.81 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
|                        |              |            |          |           |       |         |      |        |        |           |             |
| **GetExistingKey**         | **100**          |   **130.0 ns** |  **1.22 ns** |   **1.79 ns** |  **1.00** |    **0.02** |    **2** | **0.0162** |      **-** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 100          |   161.2 ns |  2.79 ns |   4.00 ns |  1.24 |    0.03 |    3 | 0.0148 |      - |     248 B |        0.91 |
| CreateNewKey           | 100          | 4,298.2 ns | 86.15 ns | 120.77 ns | 33.06 |    1.02 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   112.6 ns |  0.33 ns |   0.49 ns |  0.87 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
