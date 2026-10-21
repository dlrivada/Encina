```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **265.6 ns** |    **33.28 ns** |  **1.82 ns** |  **1.11** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,335.8 ns | 1,481.15 ns | 81.19 ns | 18.08 |    0.31 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   116.9 ns |    10.23 ns |  0.56 ns |  0.49 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   239.9 ns |    28.52 ns |  1.56 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |             |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **247.2 ns** |    **28.93 ns** |  **1.59 ns** |  **1.06** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 4,306.2 ns |   914.45 ns | 50.12 ns | 18.45 |    0.20 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   112.1 ns |     6.03 ns |  0.33 ns |  0.48 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   233.4 ns |    21.53 ns |  1.18 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
