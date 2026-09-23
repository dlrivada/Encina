
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.597 μs** | **12.5295 μs** | **0.6868 μs** | **3.236 μs** |  **1.02** |    **0.23** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.278 μs |  5.3265 μs | 0.2920 μs | 2.305 μs |  0.65 |    0.12 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.615 μs |  4.9191 μs | 0.2696 μs | 2.535 μs |  0.74 |    0.13 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.941 μs |  1.3746 μs | 0.0753 μs | 3.898 μs |  1.12 |    0.17 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.218 μs |  2.4499 μs | 0.1343 μs | 6.162 μs |  1.77 |    0.27 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 5.810 μs | 13.6197 μs | 0.7465 μs | 6.211 μs |  1.65 |    0.31 |    4 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.172 μs |  1.7056 μs | 0.0935 μs | 4.128 μs |  1.19 |    0.18 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.446 μs |  5.2373 μs | 0.2871 μs | 5.295 μs |  1.55 |    0.24 |    4 |     152 B |        2.71 |
                                  |            |          |            |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.895 μs** |  **4.3633 μs** | **0.2392 μs** | **3.821 μs** |  **1.00** |    **0.07** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.154 μs |  0.8281 μs | 0.0454 μs | 2.144 μs |  0.55 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.611 μs |  1.0997 μs | 0.0603 μs | 2.604 μs |  0.67 |    0.04 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.008 μs |  2.1048 μs | 0.1154 μs | 3.997 μs |  1.03 |    0.06 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.419 μs | 25.2140 μs | 1.3821 μs | 7.885 μs |  2.17 |    0.33 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.315 μs | 32.8391 μs | 1.8000 μs | 3.327 μs |  1.11 |    0.41 |    4 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.835 μs |  2.8108 μs | 0.1541 μs | 4.909 μs |  1.24 |    0.07 |    5 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 7.501 μs | 14.1148 μs | 0.7737 μs | 7.434 μs |  1.93 |    0.20 |    6 |     152 B |        2.71 |
