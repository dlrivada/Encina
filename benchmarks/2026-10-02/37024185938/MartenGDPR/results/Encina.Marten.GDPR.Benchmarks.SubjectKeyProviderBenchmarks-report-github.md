```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **123.72 ns** |    **14.044 ns** |  **0.770 ns** |  **1.20** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,658.03 ns | 1,097.377 ns | 60.151 ns | 35.52 |    0.52 |    3 | 0.0534 | 0.0458 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |    83.67 ns |     7.587 ns |  0.416 ns |  0.81 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |   102.98 ns |     7.641 ns |  0.419 ns |  1.00 |    0.00 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **125.54 ns** |    **11.949 ns** |  **0.655 ns** |  **1.24** |    **0.01** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,872.27 ns | 1,129.124 ns | 61.891 ns | 38.37 |    0.55 |    3 | 0.0534 | 0.0496 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |    87.04 ns |     3.698 ns |  0.203 ns |  0.86 |    0.00 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |   100.91 ns |     7.047 ns |  0.386 ns |  1.00 |    0.00 |    1 | 0.0162 |      - |     272 B |        1.00 |
