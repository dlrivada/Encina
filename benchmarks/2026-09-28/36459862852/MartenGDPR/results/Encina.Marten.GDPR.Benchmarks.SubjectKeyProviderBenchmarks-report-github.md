```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **154.7 ns** |    **13.81 ns** |  **0.76 ns** |  **1.12** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,597.2 ns | 1,780.99 ns | 97.62 ns | 33.13 |    0.61 |    3 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |   114.5 ns |     5.48 ns |  0.30 ns |  0.83 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   138.8 ns |     5.84 ns |  0.32 ns |  1.00 |    0.00 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |             |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **153.6 ns** |    **17.11 ns** |  **0.94 ns** |  **1.17** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,489.6 ns |   739.58 ns | 40.54 ns | 34.27 |    0.40 |    3 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |   105.7 ns |     4.16 ns |  0.23 ns |  0.81 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   131.0 ns |    24.06 ns |  1.32 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
