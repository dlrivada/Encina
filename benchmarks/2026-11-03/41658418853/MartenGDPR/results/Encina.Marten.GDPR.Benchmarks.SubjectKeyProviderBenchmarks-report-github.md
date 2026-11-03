```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **225.3 ns** |   **5.01 ns** |  **0.27 ns** |  **0.99** |    **0.00** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,939.3 ns | 549.86 ns | 30.14 ns | 17.35 |    0.12 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   113.3 ns |   5.80 ns |  0.32 ns |  0.50 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   227.0 ns |   3.32 ns |  0.18 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **247.7 ns** |  **73.95 ns** |  **4.05 ns** |  **1.09** |    **0.02** |    **2** | **0.0200** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,925.4 ns | 588.20 ns | 32.24 ns | 17.28 |    0.12 |    3 | 0.0534 | 0.0496 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   108.7 ns |   6.02 ns |  0.33 ns |  0.48 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   227.2 ns |   3.68 ns |  0.20 ns |  1.00 |    0.00 |    2 | 0.0196 |      - |     328 B |        1.00 |
