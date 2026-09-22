```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **128.76 ns** |     **4.674 ns** |   **0.256 ns** |  **1.08** |    **0.01** |    **2** | **0.0029** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,195.17 ns | 2,609.812 ns | 143.053 ns | 26.83 |    1.06 |    3 | 0.0076 | 0.0038 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |    81.49 ns |    18.627 ns |   1.021 ns |  0.68 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   119.08 ns |    18.405 ns |   1.009 ns |  1.00 |    0.01 |    2 | 0.0031 |      - |     272 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **124.87 ns** |    **14.509 ns** |   **0.795 ns** |  **1.05** |    **0.01** |    **2** | **0.0029** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,060.79 ns | 2,421.109 ns | 132.709 ns | 25.82 |    0.99 |    3 | 0.0076 | 0.0038 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |    87.10 ns |     3.325 ns |   0.182 ns |  0.73 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   118.55 ns |    18.991 ns |   1.041 ns |  1.00 |    0.01 |    2 | 0.0031 |      - |     272 B |        1.00 |
