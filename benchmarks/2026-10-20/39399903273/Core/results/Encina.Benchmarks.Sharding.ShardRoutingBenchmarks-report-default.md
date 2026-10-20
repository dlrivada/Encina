
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.97GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.120 μs** | **6.4383 μs** | **0.3529 μs** |  **1.01** |    **0.14** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.951 μs | 0.8314 μs | 0.0456 μs |  0.63 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.545 μs | 0.3160 μs | 0.0173 μs |  0.82 |    0.08 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.923 μs | 2.3829 μs | 0.1306 μs |  1.27 |    0.12 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.088 μs | 6.9708 μs | 0.3821 μs |  1.97 |    0.21 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.326 μs | 0.5565 μs | 0.0305 μs |  1.07 |    0.10 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.426 μs | 1.9734 μs | 0.1082 μs |  1.11 |    0.11 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.537 μs | 3.4418 μs | 0.1887 μs |  1.79 |    0.17 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.529 μs** | **3.1086 μs** | **0.1704 μs** |  **1.00** |    **0.06** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.898 μs | 0.4591 μs | 0.0252 μs |  0.82 |    0.03 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.654 μs | 3.4912 μs | 0.1914 μs |  0.75 |    0.06 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.201 μs | 2.9732 μs | 0.1630 μs |  1.19 |    0.06 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.261 μs | 8.0153 μs | 0.4393 μs |  2.63 |    0.15 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 4.114 μs | 3.5539 μs | 0.1948 μs |  1.17 |    0.07 |    1 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.914 μs | 3.3811 μs | 0.1853 μs |  1.11 |    0.06 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.611 μs | 5.2623 μs | 0.2884 μs |  1.59 |    0.10 |    2 |     152 B |        2.71 |
