```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          |  **4.487 μs** |  **14.488 μs** |  **0.7941 μs** | **4.348 μs** |  **1.02** |    **0.22** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |  3.644 μs |  10.185 μs |  0.5583 μs | 3.560 μs |  0.83 |    0.17 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |  4.100 μs |  17.051 μs |  0.9346 μs | 4.207 μs |  0.93 |    0.23 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |  6.237 μs |  12.181 μs |  0.6677 μs | 5.986 μs |  1.42 |    0.25 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          |  6.947 μs |  14.337 μs |  0.7859 μs | 6.606 μs |  1.58 |    0.28 |    2 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |  4.423 μs |  15.827 μs |  0.8675 μs | 4.250 μs |  1.01 |    0.23 |    1 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |  4.878 μs |   9.304 μs |  0.5100 μs | 4.904 μs |  1.11 |    0.19 |    1 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          |  7.183 μs |  24.330 μs |  1.3336 μs | 7.096 μs |  1.63 |    0.36 |    2 |     152 B |        2.71 |
|                                  |            |           |            |            |          |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |  **5.223 μs** |  **13.166 μs** |  **0.7217 μs** | **5.365 μs** |  **1.01** |    **0.18** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |  3.852 μs |  11.828 μs |  0.6483 μs | 3.978 μs |  0.75 |    0.14 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         |  3.988 μs |   2.491 μs |  0.1365 μs | 4.044 μs |  0.77 |    0.10 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         |  7.452 μs |   9.168 μs |  0.5025 μs | 7.256 μs |  1.45 |    0.20 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         |  8.938 μs |  19.463 μs |  1.0668 μs | 8.717 μs |  1.73 |    0.28 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |  5.392 μs |   6.897 μs |  0.3780 μs | 5.337 μs |  1.05 |    0.15 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 16.757 μs | 281.606 μs | 15.4357 μs | 8.349 μs |  3.25 |    2.64 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         |  8.261 μs |  15.615 μs |  0.8559 μs | 8.473 μs |  1.60 |    0.25 |    3 |     152 B |        2.71 |
