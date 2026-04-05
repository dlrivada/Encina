
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                           | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |------------:|------:|------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **17,792.6 μs** |    **NA** |  **1.00** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 17,862.2 μs |    NA |  1.00 |    4 |      48 B |        0.86 |
 'Directory routing'              | 3          | 17,497.6 μs |    NA |  0.98 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 20,670.0 μs |    NA |  1.16 |    8 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 17,865.6 μs |    NA |  1.00 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |    578.4 μs |    NA |  0.03 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 18,828.5 μs |    NA |  1.06 |    6 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 19,828.5 μs |    NA |  1.11 |    7 |     152 B |        2.71 |
                                  |            |             |       |       |      |           |             |
 **'Hash routing'**                   | **10**         | **17,851.0 μs** |    **NA** |  **1.00** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 10         | 18,114.1 μs |    NA |  1.01 |    6 |      48 B |        0.86 |
 'Directory routing'              | 10         | 17,354.2 μs |    NA |  0.97 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 10         | 19,919.8 μs |    NA |  1.12 |    8 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 10         | 17,935.1 μs |    NA |  1.00 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 10         |    612.4 μs |    NA |  0.03 |    1 |     160 B |        2.86 |
 GetShardConnectionString         | 10         | 18,237.1 μs |    NA |  1.02 |    7 |      64 B |        1.14 |
 'Directory add + lookup'         | 10         | 17,843.0 μs |    NA |  1.00 |    3 |     152 B |        2.71 |
                                  |            |             |       |       |      |           |             |
 **'Hash routing'**                   | **50**         | **17,668.3 μs** |    **NA** |  **1.00** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 18,082.4 μs |    NA |  1.02 |    6 |      48 B |        0.86 |
 'Directory routing'              | 50         | 17,416.1 μs |    NA |  0.99 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 19,994.2 μs |    NA |  1.13 |    8 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 18,056.2 μs |    NA |  1.02 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |    639.6 μs |    NA |  0.04 |    1 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 18,328.6 μs |    NA |  1.04 |    7 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 17,878.3 μs |    NA |  1.01 |    4 |     152 B |        2.71 |
