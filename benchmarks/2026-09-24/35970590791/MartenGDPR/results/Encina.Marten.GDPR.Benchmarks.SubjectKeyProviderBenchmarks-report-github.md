```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |    **85.94 ns** |     **5.683 ns** |   **0.312 ns** |  **1.17** |    **0.02** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,238.73 ns | 2,041.004 ns | 111.874 ns | 43.92 |    1.57 |    3 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 10           |    60.58 ns |    10.907 ns |   0.598 ns |  0.82 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |    73.76 ns |    30.972 ns |   1.698 ns |  1.00 |    0.03 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |    **86.13 ns** |    **18.786 ns** |   **1.030 ns** |  **1.18** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,148.10 ns | 1,173.116 ns |  64.302 ns | 43.24 |    0.80 |    3 | 0.0534 | 0.0496 |     928 B |        3.41 |
| CheckIsForgotten       | 100          |    59.21 ns |     4.472 ns |   0.245 ns |  0.81 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    72.81 ns |     8.587 ns |   0.471 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
