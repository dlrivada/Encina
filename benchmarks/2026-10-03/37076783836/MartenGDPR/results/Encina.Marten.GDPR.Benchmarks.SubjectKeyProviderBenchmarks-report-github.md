```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean        | Error        | StdDev    | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |------------:|-------------:|----------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |    **89.02 ns** |    **32.336 ns** |  **1.772 ns** |  **1.29** |    **0.03** |    **3** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 10           | 3,249.93 ns | 1,029.704 ns | 56.442 ns | 46.92 |    0.86 |    4 | 0.0534 | 0.0496 |     920 B |        3.38 |
| CheckIsForgotten       | 10           |    56.66 ns |     8.891 ns |  0.487 ns |  0.82 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 10           |    69.27 ns |    15.325 ns |  0.840 ns |  1.00 |    0.01 |    2 | 0.0162 |      - |     272 B |        1.00 |
|                        |              |             |              |           |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |    **81.33 ns** |     **7.302 ns** |  **0.400 ns** |  **1.15** |    **0.02** |    **2** | **0.0148** |      **-** |     **248 B** |        **0.91** |
| CreateNewKey           | 100          | 3,138.43 ns |   398.897 ns | 21.865 ns | 44.23 |    0.73 |    3 | 0.0534 | 0.0496 |     920 B |        3.38 |
| CheckIsForgotten       | 100          |    55.62 ns |    21.462 ns |  1.176 ns |  0.78 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.41 |
| GetExistingKey         | 100          |    70.98 ns |    22.766 ns |  1.248 ns |  1.00 |    0.02 |    2 | 0.0162 |      - |     272 B |        1.00 |
