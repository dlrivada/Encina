
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **5.580 μs** | **78.199 μs** | **4.2864 μs** | **3.361 μs** |  **1.38** |    **1.20** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.283 μs |  4.620 μs | 0.2532 μs | 3.189 μs |  0.81 |    0.39 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.162 μs | 11.213 μs | 0.6146 μs | 4.466 μs |  1.03 |    0.51 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.327 μs |  8.334 μs | 0.4568 μs | 3.170 μs |  0.82 |    0.40 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.516 μs |  7.872 μs | 0.4315 μs | 3.686 μs |  0.87 |    0.42 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.574 μs** |  **3.965 μs** | **0.2173 μs** | **3.490 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 5.117 μs | 27.077 μs | 1.4842 μs | 4.305 μs |  1.44 |    0.37 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.728 μs |  6.260 μs | 0.3431 μs | 4.588 μs |  1.33 |    0.11 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.621 μs |  4.899 μs | 0.2685 μs | 3.571 μs |  1.02 |    0.08 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.121 μs |  4.117 μs | 0.2257 μs | 4.131 μs |  1.16 |    0.08 |    1 |      64 B |        1.14 |
