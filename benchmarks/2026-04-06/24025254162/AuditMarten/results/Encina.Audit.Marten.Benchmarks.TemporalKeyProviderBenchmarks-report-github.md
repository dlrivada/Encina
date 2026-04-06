```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                 | PeriodCount | Mean        | Error       | StdDev   | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|----------------------- |------------ |------------:|------------:|---------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **GetExistingKey**         | **12**          |    **148.2 ns** |    **31.85 ns** |  **1.75 ns** |   **1.00** |    **0.01** |    **2** | **0.0210** |      **-** |     **352 B** |        **1.00** |
| GetOrCreateExistingKey | 12          |    173.0 ns |    36.58 ns |  2.01 ns |   1.17 |    0.02 |    2 | 0.0196 |      - |     328 B |        0.93 |
| CreateNewKey           | 12          |  3,912.2 ns |   889.35 ns | 48.75 ns |  26.39 |    0.39 |    4 | 0.0458 | 0.0381 |     848 B |        2.41 |
| IsKeyDestroyed         | 12          |    114.9 ns |     7.89 ns |  0.43 ns |   0.78 |    0.01 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 12          |  2,825.4 ns |   162.39 ns |  8.90 ns |  19.06 |    0.20 |    3 | 0.1450 |      - |    2464 B |        7.00 |
|                        |             |             |             |          |        |         |      |        |        |           |             |
| **GetExistingKey**         | **84**          |    **140.7 ns** |    **61.58 ns** |  **3.38 ns** |   **1.00** |    **0.03** |    **2** | **0.0210** |      **-** |     **352 B** |        **1.00** |
| GetOrCreateExistingKey | 84          |    167.8 ns |    21.56 ns |  1.18 ns |   1.19 |    0.03 |    2 | 0.0196 |      - |     328 B |        0.93 |
| CreateNewKey           | 84          |  4,121.8 ns | 1,119.90 ns | 61.39 ns |  29.30 |    0.72 |    3 | 0.0496 | 0.0458 |     848 B |        2.41 |
| IsKeyDestroyed         | 84          |    114.2 ns |     4.00 ns |  0.22 ns |   0.81 |    0.02 |    1 | 0.0067 |      - |     112 B |        0.32 |
| GetActiveKeysCount     | 84          | 20,330.2 ns |   100.05 ns |  5.48 ns | 144.52 |    3.00 |    4 | 0.8850 |      - |   15272 B |       43.39 |
