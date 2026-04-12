```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean       | Error     | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|----------:|----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **154.7 ns** |   **1.58 ns** |   **2.27 ns** |   **154.7 ns** |  **1.20** |    **0.03** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,661.1 ns |  54.90 ns |  75.15 ns | 4,642.0 ns | 36.14 |    0.92 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   112.4 ns |   1.54 ns |   2.30 ns |   112.3 ns |  0.87 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   129.0 ns |   1.78 ns |   2.61 ns |   127.2 ns |  1.00 |    0.03 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |           |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **152.4 ns** |   **1.32 ns** |   **1.98 ns** |   **152.4 ns** |  **1.13** |    **0.02** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,768.7 ns | 114.34 ns | 163.99 ns | 4,709.3 ns | 35.39 |    1.30 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   113.7 ns |   5.33 ns |   7.64 ns |   113.6 ns |  0.84 |    0.06 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   134.8 ns |   1.38 ns |   1.94 ns |   134.6 ns |  1.00 |    0.02 |    2 | 0.0162 |      - |     272 B |        1.00 |
