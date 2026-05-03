
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **4.590 μs** | **0.1997 μs** | **0.2800 μs** |  **1.00** |    **0.09** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.806 μs | 0.1564 μs | 0.2141 μs |  0.39 |    0.05 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.502 μs | 0.0834 μs | 0.1196 μs |  0.55 |    0.04 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.857 μs | 0.3149 μs | 0.4517 μs |  0.84 |    0.11 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.724 μs | 0.9050 μs | 1.2387 μs |  1.25 |    0.28 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.722 μs | 0.2788 μs | 0.3999 μs |  0.60 |    0.09 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.946 μs | 0.2184 μs | 0.2989 μs |  0.64 |    0.08 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.278 μs | 0.4610 μs | 0.6311 μs |  1.15 |    0.15 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.744 μs** | **0.2972 μs** | **0.4356 μs** |  **1.01** |    **0.17** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.051 μs | 0.1290 μs | 0.1808 μs |  0.56 |    0.08 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.471 μs | 0.6672 μs | 0.9780 μs |  0.94 |    0.29 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.252 μs | 0.1196 μs | 0.1715 μs |  1.15 |    0.15 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.489 μs | 0.3743 μs | 0.5368 μs |  2.03 |    0.29 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.595 μs | 0.2799 μs | 0.4014 μs |  0.97 |    0.16 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.514 μs | 0.9489 μs | 1.3909 μs |  1.49 |    0.42 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.398 μs | 0.8880 μs | 1.2449 μs |  1.73 |    0.40 |    4 |     152 B |        2.71 |
