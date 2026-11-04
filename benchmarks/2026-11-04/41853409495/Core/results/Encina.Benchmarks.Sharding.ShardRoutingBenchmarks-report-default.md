
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.764 μs** |  **5.541 μs** | **0.3037 μs** |  **1.01** |    **0.13** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.479 μs |  2.770 μs | 0.1519 μs |  0.54 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.007 μs |  2.237 μs | 0.1226 μs |  0.73 |    0.08 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.078 μs | 13.255 μs | 0.7266 μs |  1.12 |    0.25 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 3.846 μs |  6.962 μs | 0.3816 μs |  1.40 |    0.17 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.567 μs |  5.484 μs | 0.3006 μs |  0.94 |    0.13 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.071 μs | 14.452 μs | 0.7922 μs |  1.12 |    0.27 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.364 μs |  8.227 μs | 0.4510 μs |  1.59 |    0.20 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **2.981 μs** |  **8.480 μs** | **0.4648 μs** |  **1.02** |    **0.19** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.846 μs |  6.144 μs | 0.3368 μs |  0.63 |    0.13 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 3.065 μs |  9.659 μs | 0.5294 μs |  1.04 |    0.20 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.272 μs |  9.980 μs | 0.5470 μs |  1.11 |    0.21 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 6.877 μs |  6.042 μs | 0.3312 μs |  2.34 |    0.31 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.295 μs | 12.274 μs | 0.6728 μs |  1.12 |    0.24 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.255 μs |  7.449 μs | 0.4083 μs |  1.11 |    0.18 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 4.334 μs |  8.210 μs | 0.4500 μs |  1.48 |    0.23 |    3 |     152 B |        2.71 |
