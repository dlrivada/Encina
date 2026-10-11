
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **4.155 μs** | **0.5850 μs** | **0.8200 μs** |  **1.06** |    **0.38** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 3.067 μs | 0.5566 μs | 0.8330 μs |  0.78 |    0.32 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 3.074 μs | 0.5438 μs | 0.7799 μs |  0.78 |    0.31 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.734 μs | 0.4458 μs | 0.6394 μs |  1.21 |    0.39 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 7.135 μs | 0.5217 μs | 0.7142 μs |  1.82 |    0.56 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 4.473 μs | 0.4457 μs | 0.6533 μs |  1.14 |    0.37 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.335 μs | 0.4810 μs | 0.6899 μs |  1.10 |    0.37 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 6.289 μs | 0.7321 μs | 1.0021 μs |  1.60 |    0.54 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **5.526 μs** | **0.4497 μs** | **0.6449 μs** |  **1.02** |    **0.19** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 4.173 μs | 0.4068 μs | 0.5963 μs |  0.77 |    0.15 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 4.784 μs | 0.5537 μs | 0.8287 μs |  0.88 |    0.20 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 6.805 μs | 0.5504 μs | 0.7716 μs |  1.25 |    0.23 |    1 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 9.670 μs | 0.5467 μs | 0.8183 μs |  1.78 |    0.29 |    2 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 6.376 μs | 0.5982 μs | 0.8580 μs |  1.17 |    0.23 |    1 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.878 μs | 0.8103 μs | 1.1621 μs |  1.08 |    0.26 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 9.004 μs | 0.7663 μs | 1.0990 μs |  1.66 |    0.31 |    2 |     152 B |        2.71 |
