
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.220 μs** |  **4.7988 μs** | **0.2630 μs** |  **1.00** |    **0.10** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.000 μs |  1.8450 μs | 0.1011 μs |  0.62 |    0.05 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.749 μs |  3.9718 μs | 0.2177 μs |  0.86 |    0.09 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.318 μs |  8.6013 μs | 0.4715 μs |  1.35 |    0.16 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.230 μs |  6.1647 μs | 0.3379 μs |  1.94 |    0.17 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.355 μs |  5.8790 μs | 0.3222 μs |  1.05 |    0.12 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.485 μs |  2.1380 μs | 0.1172 μs |  1.09 |    0.09 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.736 μs |  3.1752 μs | 0.1740 μs |  1.79 |    0.14 |    3 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **5.276 μs** | **22.1145 μs** | **1.2122 μs** |  **1.03** |    **0.28** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.090 μs |  0.3725 μs | 0.0204 μs |  0.41 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.500 μs |  2.0825 μs | 0.1142 μs |  0.49 |    0.09 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.834 μs |  3.6815 μs | 0.2018 μs |  0.75 |    0.14 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.808 μs |  6.2157 μs | 0.3407 μs |  1.53 |    0.28 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.239 μs |  3.2898 μs | 0.1803 μs |  0.63 |    0.12 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.028 μs |  9.6539 μs | 0.5292 μs |  0.79 |    0.17 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.733 μs |  7.5947 μs | 0.4163 μs |  1.32 |    0.25 |    4 |     152 B |        2.71 |
