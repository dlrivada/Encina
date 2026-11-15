```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 2.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **219.88 ns** |    **10.367 ns** |   **0.568 ns** |  **1.05** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 2,933.21 ns | 7,628.819 ns | 418.161 ns | 14.05 |    1.74 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    93.81 ns |     3.321 ns |   0.182 ns |  0.45 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   208.73 ns |    36.320 ns |   1.991 ns |  1.00 |    0.01 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **211.59 ns** |     **5.506 ns** |   **0.302 ns** |  **1.00** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,189.19 ns | 2,119.284 ns | 116.165 ns | 15.09 |    0.51 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    91.46 ns |    12.782 ns |   0.701 ns |  0.43 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   211.37 ns |    57.652 ns |   3.160 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
