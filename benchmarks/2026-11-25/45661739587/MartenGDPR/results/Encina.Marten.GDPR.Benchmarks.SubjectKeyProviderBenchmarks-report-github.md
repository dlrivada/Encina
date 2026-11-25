```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error      | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **121.28 ns** |  **43.551 ns** |  **2.387 ns** |  **1.00** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,241.06 ns | 209.157 ns | 11.465 ns | 26.67 |    0.27 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    61.82 ns |   3.428 ns |  0.188 ns |  0.51 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   121.52 ns |  25.071 ns |  1.374 ns |  1.00 |    0.01 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |             |            |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **117.70 ns** |  **23.834 ns** |  **1.306 ns** |  **0.87** |    **0.04** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,195.52 ns | 687.791 ns | 37.700 ns | 23.61 |    0.97 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    57.05 ns |  16.911 ns |  0.927 ns |  0.42 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   135.52 ns | 116.013 ns |  6.359 ns |  1.00 |    0.06 |    2 | 0.0196 |      - |     328 B |        1.00 |
