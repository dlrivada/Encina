
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.36GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **4.816 μs** | **15.584 μs** | **0.8542 μs** | **5.178 μs** |  **1.02** |    **0.24** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.233 μs | 10.198 μs | 0.5590 μs | 2.360 μs |  0.47 |    0.13 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 5.566 μs | 26.885 μs | 1.4736 μs | 6.389 μs |  1.18 |    0.34 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 2.636 μs | 34.028 μs | 1.8652 μs | 1.661 μs |  0.56 |    0.36 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.938 μs | 28.871 μs | 1.5825 μs | 5.950 μs |  1.26 |    0.36 |    2 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 4.761 μs | 12.606 μs | 0.6910 μs | 5.032 μs |  1.01 |    0.21 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.701 μs | 43.811 μs | 2.4014 μs | 4.838 μs |  1.00 |    0.48 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.290 μs | 53.754 μs | 2.9464 μs | 4.466 μs |  0.91 |    0.57 |    2 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **5.799 μs** | **17.521 μs** | **0.9604 μs** | **5.382 μs** |  **1.02** |    **0.20** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 3.350 μs |  5.652 μs | 0.3098 μs | 3.216 μs |  0.59 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 4.710 μs | 10.696 μs | 0.5863 μs | 4.404 μs |  0.83 |    0.14 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 6.286 μs |  3.097 μs | 0.1697 μs | 6.188 μs |  1.10 |    0.15 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.651 μs | 12.808 μs | 0.7020 μs | 8.336 μs |  1.52 |    0.23 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 5.547 μs |  3.557 μs | 0.1950 μs | 5.562 μs |  0.97 |    0.13 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 6.514 μs | 19.927 μs | 1.0923 μs | 6.585 μs |  1.14 |    0.23 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 8.790 μs | 23.252 μs | 1.2745 μs | 8.137 μs |  1.54 |    0.28 |    4 |     152 B |        2.71 |
