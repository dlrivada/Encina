
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.48GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.945 μs** | **27.513 μs** | **1.5081 μs** | **2.4540 μs** |  **1.17** |    **0.72** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.154 μs | 17.148 μs | 0.9399 μs | 0.6160 μs |  0.46 |    0.39 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.790 μs | 22.140 μs | 1.2136 μs | 1.2565 μs |  0.71 |    0.53 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 2.282 μs | 37.625 μs | 2.0624 μs | 1.3780 μs |  0.91 |    0.84 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 3.489 μs | 36.532 μs | 2.0024 μs | 2.4140 μs |  1.39 |    0.91 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.006 μs | 22.613 μs | 1.2395 μs | 1.6120 μs |  0.80 |    0.55 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.463 μs | 45.248 μs | 2.4802 μs | 1.4780 μs |  0.98 |    0.99 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.024 μs | 26.374 μs | 1.4457 μs | 3.2300 μs |  1.61 |    0.82 |    5 |     152 B |        2.71 |
                                  |            |          |           |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.373 μs** | **28.434 μs** | **1.5586 μs** | **2.9390 μs** |  **1.14** |    **0.64** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.508 μs | 13.875 μs | 0.7606 μs | 1.1670 μs |  0.51 |    0.30 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.240 μs | 25.012 μs | 1.3710 μs | 1.7615 μs |  0.76 |    0.51 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.171 μs | 53.863 μs | 2.9524 μs | 1.5325 μs |  1.08 |    1.00 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.147 μs | 16.779 μs | 0.9197 μs | 9.3840 μs |  3.10 |    1.17 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.095 μs | 35.392 μs | 1.9399 μs | 2.3940 μs |  1.05 |    0.71 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.411 μs | 64.177 μs | 3.5178 μs | 2.5685 μs |  1.50 |    1.22 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 2.924 μs | 36.364 μs | 1.9933 μs | 1.9430 μs |  0.99 |    0.72 |    3 |     152 B |        2.71 |
