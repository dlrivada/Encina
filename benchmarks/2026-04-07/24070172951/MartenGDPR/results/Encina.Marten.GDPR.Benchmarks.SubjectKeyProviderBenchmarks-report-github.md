```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetExistingKey**         | **10**           |   **132.2 ns** |    **11.24 ns** |  **0.62 ns** |  **1.00** |    **0.01** |    **1** | **0.0162** |      **-** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 10           |   159.6 ns |     8.93 ns |  0.49 ns |  1.21 |    0.01 |    2 | 0.0148 |      - |     248 B |        0.91 |
| CreateNewKey           | 10           | 4,114.2 ns | 1,076.15 ns | 58.99 ns | 31.12 |    0.41 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   116.4 ns |    13.39 ns |  0.73 ns |  0.88 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
|                        |              |            |             |          |       |         |      |        |        |           |             |
| **GetExistingKey**         | **100**          |   **126.8 ns** |    **12.93 ns** |  **0.71 ns** |  **1.00** |    **0.01** |    **1** | **0.0162** |      **-** |     **272 B** |        **1.00** |
| GetOrCreateExistingKey | 100          |   166.5 ns |    33.94 ns |  1.86 ns |  1.31 |    0.01 |    2 | 0.0148 |      - |     248 B |        0.91 |
| CreateNewKey           | 100          | 4,066.8 ns |   434.44 ns | 23.81 ns | 32.07 |    0.23 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   112.4 ns |     9.75 ns |  0.53 ns |  0.89 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
