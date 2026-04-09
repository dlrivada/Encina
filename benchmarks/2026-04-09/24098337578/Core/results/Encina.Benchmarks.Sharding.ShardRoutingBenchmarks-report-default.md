
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.575 μs** | **0.1658 μs** | **0.2430 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.134 μs | 0.1562 μs | 0.2190 μs |  0.60 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.823 μs | 0.1983 μs | 0.2844 μs |  0.79 |    0.09 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.187 μs | 0.2904 μs | 0.4257 μs |  1.18 |    0.14 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.969 μs | 0.3091 μs | 0.4531 μs |  1.68 |    0.16 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.372 μs | 0.4011 μs | 0.5623 μs |  0.95 |    0.17 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.547 μs | 0.2437 μs | 0.3496 μs |  1.00 |    0.12 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.990 μs | 0.4822 μs | 0.6759 μs |  1.68 |    0.22 |    5 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.637 μs** | **0.3162 μs** | **0.4432 μs** |  **1.01** |    **0.16** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.303 μs | 0.1726 μs | 0.2529 μs |  0.64 |    0.10 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.832 μs | 0.2735 μs | 0.3834 μs |  0.79 |    0.13 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.202 μs | 0.3474 μs | 0.4756 μs |  1.17 |    0.18 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.403 μs | 0.2130 μs | 0.2843 μs |  2.06 |    0.23 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.169 μs | 0.0692 μs | 0.0993 μs |  0.88 |    0.10 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.279 μs | 0.2586 μs | 0.3708 μs |  1.19 |    0.16 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.497 μs | 0.1670 μs | 0.2395 μs |  1.53 |    0.17 |    5 |     152 B |        2.71 |
