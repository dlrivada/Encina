```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error     | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|----------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **160.3 ns** |  **10.21 ns** |  **0.56 ns** |  **1.14** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,209.7 ns | 488.28 ns | 26.76 ns | 30.05 |    0.30 |    3 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |   116.4 ns |  11.34 ns |  0.62 ns |  0.83 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   140.1 ns |  25.12 ns |  1.38 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **150.9 ns** |  **11.07 ns** |  **0.61 ns** |  **1.13** |    **0.01** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,211.6 ns | 423.00 ns | 23.19 ns | 31.52 |    0.22 |    2 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |   115.8 ns |  25.12 ns |  1.38 ns |  0.87 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   133.6 ns |  13.78 ns |  0.76 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
