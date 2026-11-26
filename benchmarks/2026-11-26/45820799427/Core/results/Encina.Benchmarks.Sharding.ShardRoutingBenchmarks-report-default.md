
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.898 μs** |  **6.355 μs** | **0.3483 μs** |  **1.01** |    **0.15** |    **2** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 1.536 μs |  2.976 μs | 0.1631 μs |  0.54 |    0.08 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 2.273 μs |  5.855 μs | 0.3209 μs |  0.79 |    0.13 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 3.168 μs |  8.684 μs | 0.4760 μs |  1.10 |    0.19 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 3.925 μs |  8.301 μs | 0.4550 μs |  1.37 |    0.20 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 2.598 μs |  5.304 μs | 0.2907 μs |  0.91 |    0.13 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.082 μs | 10.738 μs | 0.5886 μs |  1.07 |    0.21 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.534 μs | 12.748 μs | 0.6988 μs |  1.58 |    0.27 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **2.810 μs** |  **5.436 μs** | **0.2980 μs** |  **1.01** |    **0.13** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.775 μs |  1.037 μs | 0.0569 μs |  0.64 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.223 μs |  5.211 μs | 0.2856 μs |  0.80 |    0.11 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 3.268 μs |  7.734 μs | 0.4239 μs |  1.17 |    0.17 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 5.654 μs |  8.147 μs | 0.4466 μs |  2.03 |    0.22 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.174 μs |  7.784 μs | 0.4267 μs |  1.14 |    0.17 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 3.767 μs |  8.210 μs | 0.4500 μs |  1.35 |    0.18 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 4.865 μs |  7.777 μs | 0.4263 μs |  1.74 |    0.20 |    4 |     152 B |        2.71 |
