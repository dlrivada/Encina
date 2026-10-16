```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | SubjectCount | Mean       | Error       | StdDev   | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------- |-----------:|------------:|---------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetOrCreateExistingKey** | **10**           |   **227.2 ns** |    **11.94 ns** |  **0.65 ns** |  **0.86** |    **0.03** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 10           | 3,816.5 ns | 1,066.85 ns | 58.48 ns | 14.44 |    0.46 |    3 | 0.0305 | 0.0229 |     952 B |        2.90 |
| CheckIsForgotten       | 10           |   101.5 ns |     3.07 ns |  0.17 ns |  0.38 |    0.01 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 10           |   264.5 ns |   164.99 ns |  9.04 ns |  1.00 |    0.04 |    2 | 0.0129 |      - |     328 B |        1.00 |
|                        |              |            |             |          |       |         |      |        |        |           |             |
| **GetOrCreateExistingKey** | **100**          |   **227.4 ns** |    **28.44 ns** |  **1.56 ns** |  **1.01** |    **0.01** |    **2** | **0.0134** |      **-** |     **336 B** |        **1.02** |
| CreateNewKey           | 100          | 3,790.2 ns |   345.75 ns | 18.95 ns | 16.79 |    0.07 |    3 | 0.0305 | 0.0229 |     952 B |        2.90 |
| CheckIsForgotten       | 100          |   106.8 ns |     8.14 ns |  0.45 ns |  0.47 |    0.00 |    1 | 0.0044 |      - |     112 B |        0.34 |
| GetExistingKey         | 100          |   225.8 ns |     4.74 ns |  0.26 ns |  1.00 |    0.00 |    2 | 0.0129 |      - |     328 B |        1.00 |
