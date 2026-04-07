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
| **GetExistingKey**         | **10**           |   **136.2 ns** |  **3.47 ns** |   **4.86 ns** |  **1.00** |    **0.05** |    **2** | **0.0162** |      **-** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 10           |   157.1 ns |  1.47 ns |   2.15 ns |  1.15 |    0.04 |    3 | 0.0148 |      - |     248 B |        0.91 |
| CreateNewKey           | 10           | 4,277.0 ns | 98.60 ns | 138.23 ns | 31.44 |    1.49 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   113.8 ns |  1.29 ns |   1.90 ns |  0.84 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.41 |
|                        |              |            |          |           |       |         |      |        |        |           |             |
| **GetExistingKey**         | **100**          |   **131.2 ns** |  **2.54 ns** |   **3.72 ns** |  **1.00** |    **0.04** |    **2** | **0.0162** |      **-** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 100          |   166.0 ns |  6.07 ns |   9.08 ns |  1.27 |    0.08 |    3 | 0.0148 |      - |     248 B |        0.91 |
| CreateNewKey           | 100          | 4,306.6 ns | 91.95 ns | 128.90 ns | 32.86 |    1.34 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   113.7 ns |  0.59 ns |   0.88 ns |  0.87 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.41 |
