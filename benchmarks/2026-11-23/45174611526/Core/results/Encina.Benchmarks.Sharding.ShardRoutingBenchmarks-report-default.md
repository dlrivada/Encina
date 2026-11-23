
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **4.415 μs** | **34.191 μs** | **1.8741 μs** | **4.604 μs** |  **1.16** |    **0.67** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 5.152 μs | 26.702 μs | 1.4636 μs | 5.988 μs |  1.35 |    0.68 |    4 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.802 μs | 20.619 μs | 1.1302 μs | 1.578 μs |  0.47 |    0.34 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 6.474 μs |  9.882 μs | 0.5417 μs | 6.247 μs |  1.70 |    0.74 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 7.292 μs | 17.822 μs | 0.9769 μs | 7.467 μs |  1.91 |    0.85 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 4.330 μs | 33.145 μs | 1.8168 μs | 5.232 μs |  1.14 |    0.66 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.482 μs | 23.617 μs | 1.2946 μs | 1.829 μs |  0.65 |    0.42 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.328 μs | 37.480 μs | 2.0544 μs | 5.793 μs |  1.66 |    0.87 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.300 μs** | **14.641 μs** | **0.8025 μs** | **2.957 μs** |  **1.04** |    **0.30** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 3.568 μs |  7.399 μs | 0.4056 μs | 3.592 μs |  1.12 |    0.24 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 4.243 μs |  4.440 μs | 0.2434 μs | 4.326 μs |  1.33 |    0.26 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 5.339 μs |  7.154 μs | 0.3921 μs | 5.380 μs |  1.68 |    0.33 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.472 μs | 20.476 μs | 1.1224 μs | 8.175 μs |  2.66 |    0.59 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 5.307 μs |  5.335 μs | 0.2924 μs | 5.263 μs |  1.67 |    0.32 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 6.075 μs | 18.057 μs | 0.9897 μs | 6.261 μs |  1.91 |    0.45 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 8.671 μs |  9.387 μs | 0.5145 μs | 8.666 μs |  2.72 |    0.53 |    4 |     152 B |        2.71 |
