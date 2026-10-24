```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **243.0 ns** |    **18.94 ns** |  **1.04 ns** |  **1.03** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,207.9 ns | 1,745.50 ns | 95.68 ns | 17.77 |    0.37 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   116.9 ns |    10.83 ns |  0.59 ns |  0.49 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   236.8 ns |    36.07 ns |  1.98 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |             |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **237.2 ns** |     **9.60 ns** |  **0.53 ns** |  **0.97** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 4,135.8 ns |   605.24 ns | 33.18 ns | 16.92 |    0.20 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   112.9 ns |     8.91 ns |  0.49 ns |  0.46 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   244.5 ns |    47.77 ns |  2.62 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
