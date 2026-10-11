```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean        | Error     | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **106.48 ns** |  **1.471 ns** |   **2.062 ns** |  **0.96** |    **0.03** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,196.36 ns | 66.724 ns |  93.538 ns | 28.83 |    0.99 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    56.24 ns |  0.594 ns |   0.852 ns |  0.51 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   110.91 ns |  1.411 ns |   2.112 ns |  1.00 |    0.03 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |           |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **108.12 ns** |  **1.183 ns** |   **1.734 ns** |  **0.99** |    **0.03** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,213.85 ns | 97.166 ns | 136.214 ns | 29.30 |    1.45 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    54.41 ns |  0.978 ns |   1.465 ns |  0.50 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   109.76 ns |  2.169 ns |   3.110 ns |  1.00 |    0.04 |    2 | 0.0196 |      - |     328 B |        1.00 |
