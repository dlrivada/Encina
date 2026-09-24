```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |    **79.33 ns** |    **18.852 ns** |  **1.033 ns** |  **1.12** |    **0.02** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 2,948.53 ns |   252.533 ns | 13.842 ns | 41.44 |    0.56 |    3 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |    57.65 ns |    27.223 ns |  1.492 ns |  0.81 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |    71.16 ns |    19.240 ns |  1.055 ns |  1.00 |    0.02 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |    **77.81 ns** |    **17.134 ns** |  **0.939 ns** |  **1.10** |    **0.02** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 2,924.94 ns | 1,408.613 ns | 77.211 ns | 41.32 |    1.05 |    2 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |    65.19 ns |     5.407 ns |  0.296 ns |  0.92 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    70.80 ns |    16.362 ns |  0.897 ns |  1.00 |    0.02 |    1 | 0.0162 |      - |     272 B |        1.00 |
