```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.32GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev     | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|-----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |    **83.60 ns** |     **4.105 ns** |   **0.225 ns** |  **1.10** |    **0.04** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,158.70 ns | 1,107.733 ns |  60.719 ns | 41.56 |    1.65 |    3 | 0.0534 | 0.0496 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |    55.13 ns |    23.663 ns |   1.297 ns |  0.73 |    0.03 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |    76.10 ns |    59.086 ns |   3.239 ns |  1.00 |    0.05 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |            |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |    **80.96 ns** |    **11.306 ns** |   **0.620 ns** |  **1.10** |    **0.01** |    **1** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,066.64 ns | 2,594.368 ns | 142.206 ns | 41.63 |    1.72 |    2 | 0.0534 | 0.0496 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |    63.42 ns |    14.052 ns |   0.770 ns |  0.86 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    73.67 ns |    14.471 ns |   0.793 ns |  1.00 |    0.01 |    1 | 0.0162 |      - |     272 B |        1.00 |
