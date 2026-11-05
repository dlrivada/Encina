
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 3.89GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **1.796 μs** | **24.120 μs** | **1.3221 μs** | **1.2420 μs** |  **1.38** |    **1.19** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.095 μs | 15.690 μs | 0.8600 μs | 0.8410 μs |  0.84 |    0.76 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.379 μs | 18.266 μs | 1.0012 μs | 1.0020 μs |  1.06 |    0.91 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 2.404 μs | 29.860 μs | 1.6367 μs | 1.6130 μs |  1.84 |    1.52 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 2.990 μs | 29.010 μs | 1.5901 μs | 2.5190 μs |  2.29 |    1.64 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 1.818 μs | 33.236 μs | 1.8218 μs | 0.7960 μs |  1.39 |    1.51 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 1.758 μs | 26.870 μs | 1.4728 μs | 1.0570 μs |  1.35 |    1.28 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 3.287 μs | 36.665 μs | 2.0097 μs | 2.3480 μs |  2.52 |    1.94 |    4 |     152 B |        2.71 |
                                  |            |          |           |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.249 μs** |  **3.329 μs** | **0.1825 μs** | **3.1595 μs** |  **1.00** |    **0.07** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.336 μs | 13.405 μs | 0.7348 μs | 0.9220 μs |  0.41 |    0.20 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 1.468 μs | 15.667 μs | 0.8588 μs | 1.2670 μs |  0.45 |    0.23 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 7.339 μs | 87.823 μs | 4.8139 μs | 5.9535 μs |  2.26 |    1.29 |    5 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 6.300 μs | 28.178 μs | 1.5445 μs | 5.9090 μs |  1.94 |    0.42 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 2.365 μs | 24.798 μs | 1.3593 μs | 1.7680 μs |  0.73 |    0.36 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.060 μs | 42.899 μs | 2.3515 μs | 1.9180 μs |  0.94 |    0.63 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 4.333 μs | 46.802 μs | 2.5654 μs | 3.6160 μs |  1.34 |    0.69 |    4 |     152 B |        2.71 |
