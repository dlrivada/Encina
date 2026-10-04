```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean       | Error    | StdDev    | Median     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|---------:|----------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **257.3 ns** |  **6.23 ns** |   **9.33 ns** |   **258.1 ns** |  **1.03** |    **0.05** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 4,426.9 ns | 81.51 ns | 111.57 ns | 4,400.3 ns | 17.78 |    0.77 |    3 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   119.7 ns |  3.81 ns |   5.46 ns |   116.6 ns |  0.48 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   249.3 ns |  6.18 ns |   9.06 ns |   247.1 ns |  1.00 |    0.05 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |          |           |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **223.8 ns** |  **1.46 ns** |   **2.05 ns** |   **225.4 ns** |  **0.93** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 4,405.5 ns | 91.41 ns | 131.10 ns | 4,414.7 ns | 18.24 |    0.69 |    4 | 0.0534 | 0.0458 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   110.5 ns |  0.83 ns |   1.24 ns |   110.6 ns |  0.46 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   241.6 ns |  3.89 ns |   5.82 ns |   240.0 ns |  1.00 |    0.03 |    3 | 0.0196 |      - |     328 B |        1.00 |
