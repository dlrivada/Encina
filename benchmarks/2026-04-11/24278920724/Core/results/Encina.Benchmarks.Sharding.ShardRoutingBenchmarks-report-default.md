
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  InvocationCount=1  IterationCount=15  
LaunchCount=2  UnrollFactor=1  WarmupCount=10  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.843 μs** | **0.1127 μs** | **0.1579 μs** |  **1.00** |    **0.08** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.767 μs | 0.0906 μs | 0.1328 μs |  0.62 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.459 μs | 0.0795 μs | 0.1114 μs |  0.87 |    0.06 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.763 μs | 0.1753 μs | 0.2514 μs |  1.33 |    0.11 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 4.674 μs | 0.1636 μs | 0.2346 μs |  1.65 |    0.12 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.189 μs | 0.2330 μs | 0.3415 μs |  1.12 |    0.13 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.348 μs | 0.1826 μs | 0.2500 μs |  1.18 |    0.11 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.605 μs | 0.2093 μs | 0.3069 μs |  1.98 |    0.15 |    5 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.141 μs** | **0.1706 μs** | **0.2392 μs** |  **1.00** |    **0.08** |    **4** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.237 μs | 0.1352 μs | 0.1982 μs |  0.54 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.684 μs | 0.1292 μs | 0.1894 μs |  0.65 |    0.06 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.070 μs | 0.1191 μs | 0.1709 μs |  0.99 |    0.07 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 6.756 μs | 0.1812 μs | 0.2540 μs |  1.64 |    0.12 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.252 μs | 0.1190 μs | 0.1629 μs |  0.79 |    0.06 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.998 μs | 0.2227 μs | 0.3265 μs |  0.97 |    0.10 |    4 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.256 μs | 0.3421 μs | 0.4796 μs |  1.52 |    0.15 |    5 |     152 B |        2.71 |
