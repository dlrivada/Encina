```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **190.61 ns** |    **22.469 ns** |   **1.232 ns** |  **1.05** |    **0.01** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,116.36 ns | 2,584.915 ns | 141.688 ns | 17.22 |    0.71 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |    82.80 ns |     4.995 ns |   0.274 ns |  0.46 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   180.97 ns |    43.482 ns |   2.383 ns |  1.00 |    0.02 |    2 | 0.0038 |      - |     328 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **187.70 ns** |    **92.215 ns** |   **5.055 ns** |  **0.99** |    **0.04** |    **2** | **0.0038** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,143.92 ns | 1,924.816 ns | 105.506 ns | 16.58 |    0.69 |    3 | 0.0076 | 0.0038 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |    83.53 ns |    47.358 ns |   2.596 ns |  0.44 |    0.02 |    1 | 0.0013 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   189.77 ns |   115.910 ns |   6.353 ns |  1.00 |    0.04 |    2 | 0.0038 |      - |     328 B |        1.00 |
