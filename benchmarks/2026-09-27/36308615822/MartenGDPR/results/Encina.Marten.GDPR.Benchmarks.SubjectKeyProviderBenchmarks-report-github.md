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
| **GetOrCreateExistingKey** | **10**           |   **143.06 ns** |     **3.570 ns** |   **0.196 ns** |  **1.11** |    **0.01** |    **2** | **0.0029** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,345.14 ns | 2,020.395 ns | 110.745 ns | 26.00 |    0.77 |    3 | 0.0076 | 0.0038 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |    93.39 ns |    10.640 ns |   0.583 ns |  0.73 |    0.01 |    1 | 0.0013 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   128.68 ns |    19.167 ns |   1.051 ns |  1.00 |    0.01 |    2 | 0.0031 |      - |     272 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **143.50 ns** |     **3.384 ns** |   **0.185 ns** |  **1.11** |    **0.00** |    **2** | **0.0029** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,183.25 ns | 6,280.976 ns | 344.282 ns | 24.73 |    2.32 |    3 | 0.0076 | 0.0038 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |    88.11 ns |     6.091 ns |   0.334 ns |  0.68 |    0.00 |    1 | 0.0013 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   128.71 ns |    11.559 ns |   0.634 ns |  1.00 |    0.01 |    2 | 0.0031 |      - |     272 B |        1.00 |
