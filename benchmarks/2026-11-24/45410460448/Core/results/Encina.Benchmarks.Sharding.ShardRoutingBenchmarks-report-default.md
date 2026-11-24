
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error       | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|------------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.163 μs** |   **1.0348 μs** |  **0.0567 μs** | **3.147 μs** |  **1.00** |    **0.02** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.616 μs |   0.4862 μs |  0.0267 μs | 2.605 μs |  0.83 |    0.01 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  3.449 μs |   8.6369 μs |  0.4734 μs | 3.266 μs |  1.09 |    0.13 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  3.704 μs |   2.7505 μs |  0.1508 μs | 3.627 μs |  1.17 |    0.05 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 13.079 μs | 217.8185 μs | 11.9394 μs | 6.277 μs |  4.14 |    3.27 |    2 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.093 μs |   3.3853 μs |  0.1856 μs | 3.076 μs |  0.98 |    0.05 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.176 μs |   2.0825 μs |  0.1142 μs | 3.126 μs |  1.00 |    0.03 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  5.488 μs |   3.6866 μs |  0.2021 μs | 5.371 μs |  1.74 |    0.06 |    2 |     152 B |        2.71 |
                                  |            |           |             |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **4.936 μs** |  **23.5179 μs** |  **1.2891 μs** | **4.268 μs** |  **1.04** |    **0.32** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.107 μs |   1.2901 μs |  0.0707 μs | 2.113 μs |  0.44 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.489 μs |   0.6502 μs |  0.0356 μs | 2.485 μs |  0.52 |    0.10 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  3.921 μs |   0.6407 μs |  0.0351 μs | 3.917 μs |  0.83 |    0.16 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         |  7.888 μs |  16.7684 μs |  0.9191 μs | 7.424 μs |  1.66 |    0.37 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  4.191 μs |   6.1167 μs |  0.3353 μs | 4.077 μs |  0.88 |    0.19 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  3.912 μs |   1.7698 μs |  0.0970 μs | 3.953 μs |  0.83 |    0.16 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  5.522 μs |   4.2783 μs |  0.2345 μs | 5.476 μs |  1.16 |    0.23 |    3 |     152 B |        2.71 |
