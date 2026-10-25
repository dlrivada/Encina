
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error       | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|------------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.727 μs** |   **7.4844 μs** |  **0.4102 μs** | **3.627 μs** |  **1.01** |    **0.13** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.110 μs |   0.5903 μs |  0.0324 μs | 2.124 μs |  0.57 |    0.05 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.588 μs |   1.4198 μs |  0.0778 μs | 2.564 μs |  0.70 |    0.07 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 13.672 μs | 307.1415 μs | 16.8355 μs | 4.038 μs |  3.70 |    3.97 |    5 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.198 μs |   4.4551 μs |  0.2442 μs | 6.121 μs |  1.68 |    0.16 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.152 μs |   2.4331 μs |  0.1334 μs | 3.075 μs |  0.85 |    0.08 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.419 μs |   1.7343 μs |  0.0951 μs | 3.415 μs |  0.92 |    0.09 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  5.464 μs |   2.0411 μs |  0.1119 μs | 5.421 μs |  1.48 |    0.14 |    4 |     152 B |        2.71 |
                                  |            |           |             |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **3.560 μs** |   **1.8989 μs** |  **0.1041 μs** | **3.527 μs** |  **1.00** |    **0.04** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.191 μs |   1.1007 μs |  0.0603 μs | 2.184 μs |  0.62 |    0.02 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.679 μs |   1.5405 μs |  0.0844 μs | 2.635 μs |  0.75 |    0.03 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  4.117 μs |   0.3160 μs |  0.0173 μs | 4.127 μs |  1.16 |    0.03 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         |  7.601 μs |   5.2850 μs |  0.2897 μs | 7.455 μs |  2.14 |    0.09 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  3.357 μs |   1.4144 μs |  0.0775 μs | 3.317 μs |  0.94 |    0.03 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  3.817 μs |   2.0616 μs |  0.1130 μs | 3.757 μs |  1.07 |    0.04 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  5.630 μs |   2.0159 μs |  0.1105 μs | 5.630 μs |  1.58 |    0.05 |    4 |     152 B |        2.71 |
