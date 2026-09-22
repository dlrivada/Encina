
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.768 μs** | **11.186 μs** | **0.6132 μs** |  **1.02** |    **0.19** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.881 μs | 17.265 μs | 0.9464 μs |  0.78 |    0.24 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  3.756 μs | 11.307 μs | 0.6198 μs |  1.01 |    0.20 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  4.227 μs | 22.030 μs | 1.2075 μs |  1.14 |    0.32 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  6.561 μs | 30.912 μs | 1.6944 μs |  1.77 |    0.46 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  4.228 μs | 18.054 μs | 0.9896 μs |  1.14 |    0.28 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  4.440 μs | 29.411 μs | 1.6121 μs |  1.20 |    0.41 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  7.267 μs | 25.390 μs | 1.3917 μs |  1.96 |    0.41 |    3 |     152 B |        2.71 |
                                  |            |           |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **6.283 μs** | **23.017 μs** | **1.2616 μs** |  **1.03** |    **0.25** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.684 μs |  1.323 μs | 0.0725 μs |  0.44 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  4.445 μs | 12.181 μs | 0.6677 μs |  0.73 |    0.15 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  6.663 μs | 21.581 μs | 1.1829 μs |  1.09 |    0.24 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 10.780 μs | 22.926 μs | 1.2567 μs |  1.76 |    0.34 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  5.350 μs | 12.089 μs | 0.6627 μs |  0.87 |    0.17 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  6.192 μs | 15.866 μs | 0.8697 μs |  1.01 |    0.21 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 11.559 μs | 29.556 μs | 1.6201 μs |  1.89 |    0.38 |    3 |     152 B |        2.71 |
