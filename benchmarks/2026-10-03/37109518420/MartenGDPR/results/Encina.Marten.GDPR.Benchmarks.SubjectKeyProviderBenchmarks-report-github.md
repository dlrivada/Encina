```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |    **86.49 ns** |    **10.339 ns** |   **0.567 ns** |  **1.20** |    **0.01** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 2,943.51 ns |   997.914 ns |  54.699 ns | 40.91 |    0.78 |    4 | 0.0534 | 0.0496 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |    56.06 ns |     8.878 ns |   0.487 ns |  0.78 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |    71.96 ns |    15.684 ns |   0.860 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |    **75.91 ns** |     **5.959 ns** |   **0.327 ns** |  **1.07** |    **0.02** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 2,998.71 ns | 1,872.906 ns | 102.660 ns | 42.17 |    1.44 |    3 | 0.0534 | 0.0496 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |    53.34 ns |     3.386 ns |   0.186 ns |  0.75 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    71.13 ns |    25.099 ns |   1.376 ns |  1.00 |    0.02 |    2 | 0.0162 |      - |     272 B |        1.00 |
