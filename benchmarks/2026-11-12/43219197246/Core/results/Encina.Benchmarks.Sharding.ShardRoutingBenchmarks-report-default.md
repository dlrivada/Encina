
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.771 μs** |  **5.553 μs** | **0.3044 μs** |  **1.01** |    **0.13** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.877 μs |  5.528 μs | 0.3030 μs |  0.68 |    0.11 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.399 μs | 14.928 μs | 0.8182 μs |  0.87 |    0.27 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.141 μs |  8.100 μs | 0.4440 μs |  1.14 |    0.17 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 4.129 μs | 13.141 μs | 0.7203 μs |  1.50 |    0.27 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.759 μs |  8.039 μs | 0.4406 μs |  1.00 |    0.17 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 2.758 μs | 13.093 μs | 0.7177 μs |  1.00 |    0.24 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.649 μs | 11.617 μs | 0.6368 μs |  1.69 |    0.25 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **3.350 μs** | **10.654 μs** | **0.5840 μs** |  **1.02** |    **0.21** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.721 μs |  1.552 μs | 0.0850 μs |  0.52 |    0.08 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.146 μs |  7.586 μs | 0.4158 μs |  0.65 |    0.14 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 5.044 μs | 12.473 μs | 0.6837 μs |  1.53 |    0.28 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 5.511 μs | 14.095 μs | 0.7726 μs |  1.68 |    0.31 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.081 μs |  7.089 μs | 0.3886 μs |  0.94 |    0.17 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.879 μs |  1.846 μs | 0.1012 μs |  1.18 |    0.16 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 7.082 μs | 36.238 μs | 1.9863 μs |  2.15 |    0.61 |    4 |     152 B |        2.71 |
