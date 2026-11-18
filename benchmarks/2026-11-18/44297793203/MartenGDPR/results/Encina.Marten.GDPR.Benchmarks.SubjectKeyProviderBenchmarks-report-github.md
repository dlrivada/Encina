```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.57GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **186.90 ns** |  **18.903 ns** |  **1.036 ns** |  **1.08** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,919.31 ns | 493.021 ns | 27.024 ns | 22.55 |    0.15 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    86.71 ns |  23.397 ns |  1.282 ns |  0.50 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   173.77 ns |  11.698 ns |  0.641 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |            |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **176.44 ns** |  **17.165 ns** |  **0.941 ns** |  **0.97** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,915.89 ns | 682.942 ns | 37.434 ns | 21.58 |    0.19 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    83.84 ns |   7.033 ns |  0.386 ns |  0.46 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   181.49 ns |  13.483 ns |  0.739 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
