```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **182.16 ns** |     **7.085 ns** |  **0.388 ns** |  **1.02** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,724.43 ns | 1,239.152 ns | 67.922 ns | 20.76 |    0.35 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    88.48 ns |    60.651 ns |  3.324 ns |  0.49 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   179.37 ns |    21.443 ns |  1.175 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |              |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **176.01 ns** |    **19.887 ns** |  **1.090 ns** |  **1.02** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,648.04 ns |   150.134 ns |  8.229 ns | 21.13 |    0.17 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    84.20 ns |     6.144 ns |  0.337 ns |  0.49 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   172.62 ns |    28.981 ns |  1.589 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
