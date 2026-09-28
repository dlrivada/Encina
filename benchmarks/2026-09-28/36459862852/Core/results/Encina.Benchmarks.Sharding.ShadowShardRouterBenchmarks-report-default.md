
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.401 μs** |   **7.118 μs** | **0.3902 μs** | **3.483 μs** |  **1.01** |    **0.15** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 4.470 μs |  31.948 μs | 1.7512 μs | 3.626 μs |  1.33 |    0.47 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.803 μs |  35.251 μs | 1.9322 μs | 4.092 μs |  1.43 |    0.52 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 4.277 μs |  16.102 μs | 0.8826 μs | 4.136 μs |  1.27 |    0.26 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 7.540 μs | 112.263 μs | 6.1535 μs | 4.718 μs |  2.24 |    1.61 |    1 |      64 B |        1.14 |
                                          |            |          |            |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **4.256 μs** |   **6.224 μs** | **0.3412 μs** | **4.378 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.076 μs |  12.314 μs | 0.6750 μs | 3.991 μs |  0.96 |    0.15 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 7.429 μs |   2.202 μs | 0.1207 μs | 7.377 μs |  1.75 |    0.13 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.025 μs |   7.448 μs | 0.4082 μs | 3.817 μs |  0.95 |    0.11 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.543 μs |  30.693 μs | 1.6824 μs | 4.702 μs |  1.31 |    0.36 |    1 |      64 B |        1.14 |
