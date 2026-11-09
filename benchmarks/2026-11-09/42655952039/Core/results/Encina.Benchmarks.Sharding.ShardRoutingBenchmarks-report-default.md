
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.454 μs** |  **9.414 μs** | **0.5160 μs** |  **1.01** |    **0.18** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.065 μs |  4.132 μs | 0.2265 μs |  0.61 |    0.10 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.472 μs |  3.065 μs | 0.1680 μs |  0.73 |    0.10 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.506 μs | 18.707 μs | 1.0254 μs |  1.32 |    0.31 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 7.003 μs |  9.960 μs | 0.5460 μs |  2.06 |    0.29 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.319 μs |  3.802 μs | 0.2084 μs |  0.98 |    0.13 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.767 μs | 10.756 μs | 0.5896 μs |  1.11 |    0.21 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.929 μs |  2.207 μs | 0.1210 μs |  1.74 |    0.22 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.067 μs** |  **5.852 μs** | **0.3208 μs** |  **1.00** |    **0.10** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.478 μs |  4.969 μs | 0.2723 μs |  0.61 |    0.07 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 4.530 μs | 15.276 μs | 0.8373 μs |  1.12 |    0.20 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.822 μs |  9.616 μs | 0.5271 μs |  1.19 |    0.14 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.820 μs | 10.454 μs | 0.5730 μs |  2.18 |    0.20 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.914 μs |  7.915 μs | 0.4339 μs |  0.97 |    0.12 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.395 μs |  3.531 μs | 0.1936 μs |  1.33 |    0.10 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.740 μs |  5.707 μs | 0.3128 μs |  1.42 |    0.12 |    2 |     152 B |        2.71 |
