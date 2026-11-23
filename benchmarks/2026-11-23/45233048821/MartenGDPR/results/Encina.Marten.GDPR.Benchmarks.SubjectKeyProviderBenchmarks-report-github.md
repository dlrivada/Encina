```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.03GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **186.38 ns** |    **10.641 ns** |   **0.583 ns** |  **1.04** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,554.86 ns | 4,009.439 ns | 219.771 ns | 14.30 |    1.07 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    77.96 ns |     7.873 ns |   0.432 ns |  0.44 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   178.67 ns |    24.479 ns |   1.342 ns |  1.00 |    0.01 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **181.89 ns** |     **6.405 ns** |   **0.351 ns** |  **1.09** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 2,326.19 ns | 3,940.218 ns | 215.977 ns | 13.94 |    1.13 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    76.82 ns |     1.856 ns |   0.102 ns |  0.46 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   166.82 ns |    30.786 ns |   1.687 ns |  1.00 |    0.01 |    2 | 0.0038 |      - |     328 B |        1.00 |
