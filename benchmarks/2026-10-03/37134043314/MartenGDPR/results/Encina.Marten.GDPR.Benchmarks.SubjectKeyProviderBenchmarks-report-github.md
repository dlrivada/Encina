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
| **GetOrCreateExistingKey** | **10**           |    **81.55 ns** |    **19.440 ns** |   **1.066 ns** |  **1.17** |    **0.02** |    **2** | **0.0167** |      **-** |     **280 B** |        **1.03** |
| CreateNewKey           | 10           | 3,258.76 ns | 8,797.497 ns | 482.221 ns | 46.56 |    6.02 |    3 | 0.0534 | 0.0496 |     952 B |        3.50 |
| CheckIsForgotten       | 10           |    56.73 ns |    15.258 ns |   0.836 ns |  0.81 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |    70.01 ns |    24.399 ns |   1.337 ns |  1.00 |    0.02 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |    **89.24 ns** |    **16.964 ns** |   **0.930 ns** |  **1.29** |    **0.04** |    **2** | **0.0167** |      **-** |     **280 B** |        **1.03** |
| CreateNewKey           | 100          | 2,911.22 ns |   795.594 ns |  43.609 ns | 41.96 |    1.42 |    3 | 0.0534 | 0.0496 |     952 B |        3.50 |
| CheckIsForgotten       | 100          |    57.77 ns |     8.982 ns |   0.492 ns |  0.83 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    69.44 ns |    46.528 ns |   2.550 ns |  1.00 |    0.04 |    1 | 0.0162 |      - |     272 B |        1.00 |
