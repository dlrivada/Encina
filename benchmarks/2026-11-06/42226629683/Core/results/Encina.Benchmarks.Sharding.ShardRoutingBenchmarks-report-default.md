
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.767 μs** |  **2.130 μs** | **0.1168 μs** | **2.744 μs** |  **1.00** |    **0.05** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.475 μs |  3.558 μs | 0.1950 μs | 1.472 μs |  0.53 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.972 μs |  3.014 μs | 0.1652 μs | 1.982 μs |  0.71 |    0.06 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.259 μs |  5.488 μs | 0.3008 μs | 3.085 μs |  1.18 |    0.10 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 7.027 μs | 86.447 μs | 4.7384 μs | 4.327 μs |  2.54 |    1.49 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.602 μs |  9.442 μs | 0.5175 μs | 2.318 μs |  0.94 |    0.17 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.724 μs |  4.482 μs | 0.2457 μs | 2.714 μs |  0.99 |    0.08 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.376 μs |  9.492 μs | 0.5203 μs | 4.095 μs |  1.58 |    0.17 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.412 μs** |  **5.034 μs** | **0.2759 μs** | **3.305 μs** |  **1.00** |    **0.10** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.690 μs |  1.486 μs | 0.0814 μs | 1.653 μs |  0.50 |    0.04 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.093 μs |  3.325 μs | 0.1822 μs | 2.004 μs |  0.62 |    0.06 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.379 μs |  9.212 μs | 0.5049 μs | 3.245 μs |  0.99 |    0.15 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 5.636 μs |  7.752 μs | 0.4249 μs | 5.569 μs |  1.66 |    0.16 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 2.925 μs |  4.190 μs | 0.2297 μs | 2.975 μs |  0.86 |    0.08 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.147 μs |  2.621 μs | 0.1436 μs | 3.090 μs |  0.93 |    0.07 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 4.897 μs | 14.720 μs | 0.8069 μs | 4.556 μs |  1.44 |    0.23 |    4 |     152 B |        2.71 |
