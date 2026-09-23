```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |    **89.09 ns** |    **73.482 ns** |   **4.028 ns** |  **1.07** |    **0.05** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,296.07 ns |   797.534 ns |  43.716 ns | 39.41 |    0.83 |    3 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |    66.38 ns |    55.798 ns |   3.058 ns |  0.79 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |    83.67 ns |    31.494 ns |   1.726 ns |  1.00 |    0.03 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |    **87.84 ns** |     **6.570 ns** |   **0.360 ns** |  **1.07** |    **0.14** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,423.89 ns | 2,388.808 ns | 130.939 ns | 41.57 |    5.45 |    2 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |    68.51 ns |    61.808 ns |   3.388 ns |  0.83 |    0.11 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    83.67 ns |   243.181 ns |  13.330 ns |  1.02 |    0.19 |    1 | 0.0162 |      - |     272 B |        1.00 |
