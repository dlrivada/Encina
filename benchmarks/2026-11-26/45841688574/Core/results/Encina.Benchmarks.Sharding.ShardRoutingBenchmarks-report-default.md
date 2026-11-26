
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **5.008 μs** | **13.863 μs** | **0.7599 μs** | **4.684 μs** |  **1.01** |    **0.18** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.725 μs |  6.062 μs | 0.3323 μs | 2.604 μs |  0.55 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 4.408 μs | 10.473 μs | 0.5741 μs | 4.329 μs |  0.89 |    0.15 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.199 μs | 41.879 μs | 2.2955 μs | 4.452 μs |  0.85 |    0.42 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 5.896 μs | 21.684 μs | 1.1886 μs | 5.226 μs |  1.19 |    0.26 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 4.809 μs | 47.320 μs | 2.5938 μs | 5.407 μs |  0.97 |    0.47 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 5.751 μs | 25.449 μs | 1.3949 μs | 6.107 μs |  1.17 |    0.28 |    5 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.888 μs | 74.366 μs | 4.0763 μs | 2.614 μs |  0.99 |    0.73 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **5.005 μs** |  **4.556 μs** | **0.2498 μs** | **4.992 μs** |  **1.00** |    **0.06** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 3.841 μs | 33.081 μs | 1.8133 μs | 3.479 μs |  0.77 |    0.32 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.110 μs |  8.608 μs | 0.4719 μs | 3.031 μs |  0.62 |    0.09 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 5.429 μs | 26.721 μs | 1.4647 μs | 5.691 μs |  1.09 |    0.26 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.072 μs | 36.844 μs | 2.0196 μs | 8.620 μs |  1.62 |    0.36 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.566 μs |  4.655 μs | 0.2551 μs | 4.588 μs |  0.91 |    0.06 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 6.200 μs | 16.182 μs | 0.8870 μs | 5.742 μs |  1.24 |    0.16 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 9.338 μs | 12.069 μs | 0.6616 μs | 9.649 μs |  1.87 |    0.14 |    3 |     152 B |        2.71 |
