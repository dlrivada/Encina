```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **162.8 ns** |    **23.96 ns** |   **1.31 ns** |  **1.24** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,600.2 ns | 1,106.29 ns |  60.64 ns | 34.94 |    0.41 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   111.9 ns |     2.34 ns |   0.13 ns |  0.85 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   131.6 ns |     8.00 ns |   0.44 ns |  1.00 |    0.00 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |             |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **153.1 ns** |    **12.54 ns** |   **0.69 ns** |  **1.10** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,752.7 ns | 3,920.95 ns | 214.92 ns | 34.29 |    1.35 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   106.6 ns |    10.43 ns |   0.57 ns |  0.77 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   138.6 ns |     6.92 ns |   0.38 ns |  1.00 |    0.00 |    2 | 0.0162 |      - |     272 B |        1.00 |
