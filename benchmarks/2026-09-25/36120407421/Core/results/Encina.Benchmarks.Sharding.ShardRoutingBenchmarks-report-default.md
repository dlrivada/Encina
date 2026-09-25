
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.817 μs** | **28.465 μs** | **1.5602 μs** | **2.182 μs** |  **1.20** |    **0.77** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.715 μs | 15.263 μs | 0.8366 μs | 1.399 μs |  0.73 |    0.43 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.292 μs | 20.829 μs | 1.1417 μs | 1.751 μs |  0.97 |    0.59 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 5.615 μs | 32.247 μs | 1.7676 μs | 4.598 μs |  2.38 |    1.16 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 7.440 μs | 22.771 μs | 1.2481 μs | 7.955 μs |  3.16 |    1.33 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 4.444 μs | 27.021 μs | 1.4811 μs | 3.922 μs |  1.89 |    0.94 |    4 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.508 μs | 27.395 μs | 1.5016 μs | 3.857 μs |  1.91 |    0.95 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.168 μs | 53.884 μs | 2.9536 μs | 4.147 μs |  2.19 |    1.44 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.872 μs** | **17.904 μs** | **0.9814 μs** | **3.368 μs** |  **1.04** |    **0.31** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.039 μs | 12.096 μs | 0.6630 μs | 1.748 μs |  0.55 |    0.19 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.063 μs |  8.120 μs | 0.4451 μs | 2.881 μs |  0.82 |    0.19 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.224 μs | 30.603 μs | 1.6775 μs | 3.721 μs |  1.13 |    0.45 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.080 μs | 20.835 μs | 1.1420 μs | 8.734 μs |  2.44 |    0.54 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.717 μs | 20.933 μs | 1.1474 μs | 3.236 μs |  1.00 |    0.33 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.410 μs | 19.754 μs | 1.0828 μs | 4.197 μs |  1.18 |    0.34 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.493 μs | 24.474 μs | 1.3415 μs | 5.809 μs |  1.74 |    0.46 |    3 |     152 B |        2.71 |
