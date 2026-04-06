```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetExistingKey**         | **10**           |   **138.3 ns** |  **15.49 ns** |  **0.85 ns** |  **1.00** |    **0.01** |    **1** | **0.0162** |      **-** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 10           |   164.1 ns |  16.24 ns |  0.89 ns |  1.19 |    0.01 |    1 | 0.0148 |      - |     248 B |        0.91 |
| CreateNewKey           | 10           | 4,113.1 ns | 995.40 ns | 54.56 ns | 29.74 |    0.38 |    2 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   117.0 ns |   2.76 ns |  0.15 ns |  0.85 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetExistingKey**         | **100**          |   **130.5 ns** |  **38.61 ns** |  **2.12 ns** |  **1.00** |    **0.02** |    **1** | **0.0162** |      **-** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 100          |   160.6 ns |  14.16 ns |  0.78 ns |  1.23 |    0.02 |    2 | 0.0148 |      - |     248 B |        0.91 |
| CreateNewKey           | 100          | 4,101.7 ns | 419.37 ns | 22.99 ns | 31.45 |    0.47 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   113.5 ns |  13.94 ns |  0.76 ns |  0.87 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
