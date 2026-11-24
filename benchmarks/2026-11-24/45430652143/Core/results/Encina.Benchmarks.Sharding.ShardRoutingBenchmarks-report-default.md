
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **5.026 μs** | **30.163 μs** | **1.6533 μs** |  **1.07** |    **0.43** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 3.075 μs | 15.368 μs | 0.8424 μs |  0.66 |    0.24 |    3 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.922 μs | 23.819 μs | 1.3056 μs |  0.41 |    0.27 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 5.510 μs | 36.878 μs | 2.0214 μs |  1.18 |    0.50 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 8.001 μs | 17.054 μs | 0.9348 μs |  1.71 |    0.50 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 6.336 μs | 29.179 μs | 1.5994 μs |  1.35 |    0.48 |    4 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.531 μs | 26.278 μs | 1.4404 μs |  0.54 |    0.31 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.394 μs | 41.571 μs | 2.2786 μs |  1.36 |    0.57 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **5.165 μs** | **12.547 μs** | **0.6877 μs** |  **1.01** |    **0.17** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 3.556 μs |  2.799 μs | 0.1534 μs |  0.70 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 4.415 μs | 33.875 μs | 1.8568 μs |  0.87 |    0.33 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 6.724 μs | 12.323 μs | 0.6754 μs |  1.32 |    0.20 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.285 μs | 34.547 μs | 1.8936 μs |  1.82 |    0.39 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 5.065 μs | 17.134 μs | 0.9391 μs |  0.99 |    0.20 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.349 μs | 17.546 μs | 0.9618 μs |  1.05 |    0.21 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 7.717 μs | 10.942 μs | 0.5997 μs |  1.51 |    0.21 |    3 |     152 B |        2.71 |
