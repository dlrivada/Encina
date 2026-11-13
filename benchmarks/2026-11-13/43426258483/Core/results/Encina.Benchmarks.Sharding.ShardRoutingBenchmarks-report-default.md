
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.027 μs** |  **6.442 μs** | **0.3531 μs** |  **1.01** |    **0.14** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.636 μs |  4.201 μs | 0.2303 μs |  0.55 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.953 μs |  4.606 μs | 0.2525 μs |  0.65 |    0.10 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.542 μs | 12.623 μs | 0.6919 μs |  1.18 |    0.23 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 4.113 μs |  7.976 μs | 0.4372 μs |  1.37 |    0.19 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.837 μs |  9.329 μs | 0.5114 μs |  0.95 |    0.18 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.451 μs |  6.300 μs | 0.3453 μs |  1.15 |    0.15 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.935 μs |  2.501 μs | 0.1371 μs |  1.64 |    0.17 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.187 μs** |  **8.650 μs** | **0.4741 μs** |  **1.01** |    **0.18** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.251 μs |  3.110 μs | 0.1705 μs |  0.72 |    0.10 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.281 μs |  5.145 μs | 0.2820 μs |  0.73 |    0.12 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.733 μs |  7.099 μs | 0.3891 μs |  1.19 |    0.18 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 6.369 μs |  4.280 μs | 0.2346 μs |  2.03 |    0.25 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.053 μs |  7.393 μs | 0.4052 μs |  0.97 |    0.16 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.239 μs | 13.720 μs | 0.7521 μs |  1.35 |    0.26 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.402 μs |  5.435 μs | 0.2979 μs |  1.72 |    0.22 |    3 |     152 B |        2.71 |
