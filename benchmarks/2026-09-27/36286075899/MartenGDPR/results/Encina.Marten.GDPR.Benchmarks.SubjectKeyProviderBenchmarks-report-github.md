```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean        | Error      | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |    **85.43 ns** |   **1.045 ns** |   **1.430 ns** |  **1.17** |    **0.03** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,297.15 ns |  85.353 ns | 119.653 ns | 45.18 |    1.83 |    4 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |    58.00 ns |   0.936 ns |   1.343 ns |  0.79 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |    73.01 ns |   1.018 ns |   1.460 ns |  1.00 |    0.03 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |            |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |    **87.23 ns** |   **3.888 ns** |   **5.699 ns** |  **1.24** |    **0.08** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,424.20 ns | 125.819 ns | 180.446 ns | 48.73 |    2.75 |    4 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |    62.68 ns |   3.096 ns |   4.635 ns |  0.89 |    0.07 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    70.30 ns |   1.169 ns |   1.600 ns |  1.00 |    0.03 |    2 | 0.0162 |      - |     272 B |        1.00 |
