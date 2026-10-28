```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.78GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **232.4 ns** |     **6.00 ns** |  **0.33 ns** |  **0.99** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,326.1 ns | 1,094.39 ns | 59.99 ns | 18.37 |    0.27 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   119.8 ns |     1.46 ns |  0.08 ns |  0.51 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   235.5 ns |    40.32 ns |  2.21 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |             |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **226.3 ns** |    **18.72 ns** |  **1.03 ns** |  **0.98** |    **0.00** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 4,342.2 ns | 1,348.98 ns | 73.94 ns | 18.74 |    0.28 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   110.5 ns |     1.64 ns |  0.09 ns |  0.48 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   231.7 ns |    15.04 ns |  0.82 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
