```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **173.98 ns** |     **3.889 ns** |   **0.213 ns** |  **1.04** |    **0.00** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,677.43 ns |   499.310 ns |  27.369 ns | 21.90 |    0.16 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    85.98 ns |     5.997 ns |   0.329 ns |  0.51 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   167.89 ns |    13.584 ns |   0.745 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **183.03 ns** |     **4.615 ns** |   **0.253 ns** |  **1.06** |    **0.00** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,693.66 ns | 2,118.365 ns | 116.115 ns | 21.40 |    0.58 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    82.25 ns |     2.900 ns |   0.159 ns |  0.48 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   172.64 ns |     6.704 ns |   0.367 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
