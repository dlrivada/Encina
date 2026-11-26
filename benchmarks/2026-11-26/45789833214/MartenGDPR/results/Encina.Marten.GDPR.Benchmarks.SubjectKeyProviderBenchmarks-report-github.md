```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **113.15 ns** |    **16.017 ns** |  **0.878 ns** |  **1.02** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,814.07 ns | 1,204.858 ns | 66.042 ns | 25.45 |    0.54 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    52.90 ns |     7.566 ns |  0.415 ns |  0.48 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   110.60 ns |    13.074 ns |  0.717 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |              |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **104.89 ns** |    **24.950 ns** |  **1.368 ns** |  **0.84** |    **0.01** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,833.03 ns |   929.037 ns | 50.924 ns | 22.70 |    0.41 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    55.25 ns |    21.462 ns |  1.176 ns |  0.44 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   124.80 ns |    24.058 ns |  1.319 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
