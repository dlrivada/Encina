
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error      | StdDev     | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|-----------:|-----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.374 μs** |   **7.653 μs** |  **0.4195 μs** | **3.137 μs** |  **1.01** |    **0.15** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  1.927 μs |   3.119 μs |  0.1710 μs | 1.833 μs |  0.58 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.815 μs |   3.116 μs |  0.1708 μs | 2.796 μs |  0.84 |    0.10 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 12.608 μs | 258.984 μs | 14.1958 μs | 4.934 μs |  3.77 |    3.72 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.065 μs |   9.257 μs |  0.5074 μs | 6.021 μs |  1.82 |    0.23 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.143 μs |   5.442 μs |  0.2983 μs | 2.986 μs |  0.94 |    0.12 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.342 μs |   3.380 μs |  0.1853 μs | 3.245 μs |  1.00 |    0.11 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  5.894 μs |  14.065 μs |  0.7710 μs | 5.500 μs |  1.76 |    0.27 |    3 |     152 B |        2.71 |
                                  |            |           |            |            |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **4.475 μs** |   **4.171 μs** |  **0.2286 μs** | **4.519 μs** |  **1.00** |    **0.06** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.386 μs |   7.158 μs |  0.3923 μs | 2.239 μs |  0.53 |    0.08 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.882 μs |   7.181 μs |  0.3936 μs | 2.665 μs |  0.65 |    0.08 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  3.884 μs |   3.076 μs |  0.1686 μs | 3.797 μs |  0.87 |    0.05 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 10.059 μs |  20.113 μs |  1.1024 μs | 9.678 μs |  2.25 |    0.24 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  3.212 μs |   4.974 μs |  0.2727 μs | 3.075 μs |  0.72 |    0.06 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  4.585 μs |   7.587 μs |  0.4159 μs | 4.458 μs |  1.03 |    0.09 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  5.646 μs |   3.079 μs |  0.1688 μs | 5.585 μs |  1.26 |    0.07 |    4 |     152 B |        2.71 |
