
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.531 μs** |  **7.1522 μs** | **0.3920 μs** |  **1.02** |    **0.19** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.499 μs |  1.6457 μs | 0.0902 μs |  0.60 |    0.08 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.699 μs |  9.6542 μs | 0.5292 μs |  1.08 |    0.23 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.345 μs |  9.5183 μs | 0.5217 μs |  1.34 |    0.25 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.360 μs | 12.5026 μs | 0.6853 μs |  2.55 |    0.40 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.634 μs | 15.2518 μs | 0.8360 μs |  1.46 |    0.35 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.951 μs |  8.9019 μs | 0.4879 μs |  1.18 |    0.23 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.504 μs | 12.4664 μs | 0.6833 μs |  1.81 |    0.33 |    3 |     152 B |        2.71 |
                                  |            |          |            |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.103 μs** |  **4.0124 μs** | **0.2199 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.686 μs |  0.5865 μs | 0.0321 μs |  0.55 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.429 μs |  3.9761 μs | 0.2179 μs |  0.79 |    0.08 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.258 μs |  5.1644 μs | 0.2831 μs |  1.05 |    0.10 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 7.583 μs | 41.7602 μs | 2.2890 μs |  2.45 |    0.66 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 2.894 μs |  3.1731 μs | 0.1739 μs |  0.94 |    0.08 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.599 μs |  0.8321 μs | 0.0456 μs |  1.16 |    0.08 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 4.653 μs | 16.9802 μs | 0.9307 μs |  1.50 |    0.28 |    2 |     152 B |        2.71 |
