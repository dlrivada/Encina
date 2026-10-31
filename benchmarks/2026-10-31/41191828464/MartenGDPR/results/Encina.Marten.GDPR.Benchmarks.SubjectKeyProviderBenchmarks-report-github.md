```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.36GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error       | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **114.38 ns** |   **140.17 ns** |   **7.683 ns** |  **0.96** |    **0.06** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,363.86 ns | 3,318.64 ns | 181.906 ns | 28.29 |    1.43 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    56.76 ns |    11.60 ns |   0.636 ns |  0.48 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   118.95 ns |    47.59 ns |   2.608 ns |  1.00 |    0.03 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |             |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **119.38 ns** |   **112.43 ns** |   **6.163 ns** |  **0.98** |    **0.07** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,267.16 ns | 1,221.15 ns |  66.935 ns | 26.89 |    1.41 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    56.37 ns |    39.22 ns |   2.150 ns |  0.46 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   121.78 ns |   130.33 ns |   7.144 ns |  1.00 |    0.07 |    2 | 0.0196 |      - |     328 B |        1.00 |
