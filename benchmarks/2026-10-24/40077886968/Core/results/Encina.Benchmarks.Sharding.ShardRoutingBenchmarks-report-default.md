
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.549 μs** | **33.310 μs** | **1.8259 μs** | **3.1600 μs** |  **2.19** |    **2.90** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 2.336 μs | 10.878 μs | 0.5963 μs | 2.5690 μs |  2.01 |    2.12 |    3 |      48 B |        0.86 |
 'Directory routing'              | 3          | 1.259 μs | 14.389 μs | 0.7887 μs | 1.0010 μs |  1.08 |    1.36 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 1.946 μs | 27.081 μs | 1.4844 μs | 1.2015 μs |  1.68 |    2.27 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 2.865 μs | 22.831 μs | 1.2515 μs | 2.2875 μs |  2.47 |    2.80 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 1.799 μs | 23.241 μs | 1.2739 μs | 1.3120 μs |  1.55 |    2.04 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 1.494 μs | 26.664 μs | 1.4616 μs | 0.7160 μs |  1.29 |    1.98 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.457 μs | 28.177 μs | 1.5445 μs | 4.0960 μs |  3.84 |    4.18 |    5 |     152 B |        2.71 |
                                  |            |          |           |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **2.571 μs** | **31.676 μs** | **1.7363 μs** | **1.8130 μs** |  **1.30** |    **1.02** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.760 μs | 10.357 μs | 0.5677 μs | 2.7090 μs |  1.39 |    0.69 |    3 |      48 B |        0.86 |
 'Directory routing'              | 50         | 1.930 μs | 21.897 μs | 1.2002 μs | 1.2720 μs |  0.98 |    0.72 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 2.068 μs | 27.606 μs | 1.5132 μs | 1.3070 μs |  1.05 |    0.86 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 4.164 μs | 19.361 μs | 1.0613 μs | 3.9600 μs |  2.10 |    1.08 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 2.886 μs | 40.907 μs | 2.2423 μs | 1.8180 μs |  1.46 |    1.26 |    4 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.406 μs | 39.449 μs | 2.1624 μs | 2.2740 μs |  1.72 |    1.29 |    5 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 3.217 μs | 27.804 μs | 1.5240 μs | 2.5390 μs |  1.63 |    1.04 |    5 |     152 B |        2.71 |
