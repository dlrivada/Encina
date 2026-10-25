```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean        | Error     | StdDev     | Median      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|----------:|-----------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **183.44 ns** |  **4.724 ns** |   **6.623 ns** |   **179.12 ns** |  **1.02** |    **0.04** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,062.85 ns | 42.326 ns |  60.703 ns | 4,057.55 ns | 22.68 |    0.36 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    89.90 ns |  2.807 ns |   3.934 ns |    86.92 ns |  0.50 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   179.14 ns |  0.793 ns |   1.138 ns |   179.14 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |           |            |             |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **181.34 ns** |  **5.360 ns** |   **8.023 ns** |   **181.60 ns** |  **1.03** |    **0.05** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,940.82 ns | 88.239 ns | 117.796 ns | 3,974.48 ns | 22.28 |    0.71 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    83.98 ns |  0.397 ns |   0.594 ns |    84.06 ns |  0.47 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   176.94 ns |  1.703 ns |   2.274 ns |   177.95 ns |  1.00 |    0.02 |    2 | 0.0196 |      - |     328 B |        1.00 |
