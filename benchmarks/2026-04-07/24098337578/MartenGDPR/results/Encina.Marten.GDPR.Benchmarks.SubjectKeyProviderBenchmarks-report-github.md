```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                 | SubjectCount | Mean       | Error     | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|----------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **157.7 ns** |   **1.46 ns** |   **2.19 ns** |  **1.16** |    **0.02** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 4,351.6 ns | 105.65 ns | 148.11 ns | 32.11 |    1.12 |    3 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |   124.1 ns |   5.90 ns |   8.46 ns |  0.92 |    0.06 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   135.6 ns |   0.92 ns |   1.31 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |            |           |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **159.4 ns** |   **1.58 ns** |   **2.26 ns** |  **1.19** |    **0.02** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 4,285.8 ns |  78.87 ns | 110.56 ns | 31.93 |    0.91 |    4 | 0.0534 | 0.0458 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |   113.3 ns |   0.29 ns |   0.43 ns |  0.84 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   134.2 ns |   1.22 ns |   1.78 ns |  1.00 |    0.02 |    2 | 0.0162 |      - |     272 B |        1.00 |
