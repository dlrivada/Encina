
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **3.872 μs** | **37.666 μs** | **2.0646 μs** | **3.681 μs** |  **1.24** |    **0.88** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 3.496 μs | 38.133 μs | 2.0902 μs | 3.231 μs |  1.12 |    0.85 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          | 5.354 μs | 27.590 μs | 1.5123 μs | 5.822 μs |  1.72 |    0.97 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 5.226 μs | 20.313 μs | 1.1134 μs | 5.447 μs |  1.68 |    0.90 |    2 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.787 μs | 38.861 μs | 2.1301 μs | 7.360 μs |  2.18 |    1.26 |    3 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 5.739 μs | 16.285 μs | 0.8926 μs | 6.118 μs |  1.84 |    0.95 |    2 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 4.960 μs | 11.038 μs | 0.6050 μs | 5.176 μs |  1.59 |    0.81 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 4.417 μs | 59.129 μs | 3.2411 μs | 2.781 μs |  1.42 |    1.22 |    1 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **5.223 μs** | **11.639 μs** | **0.6380 μs** | **5.173 μs** |  **1.01** |    **0.15** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 1.726 μs |  9.227 μs | 0.5058 μs | 1.480 μs |  0.33 |    0.09 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.781 μs | 16.049 μs | 0.8797 μs | 2.326 μs |  0.54 |    0.16 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 6.056 μs | 12.867 μs | 0.7053 μs | 6.292 μs |  1.17 |    0.17 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.386 μs |  8.066 μs | 0.4421 μs | 8.270 μs |  1.62 |    0.19 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 6.112 μs | 19.212 μs | 1.0531 μs | 6.292 μs |  1.18 |    0.22 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 5.396 μs | 21.487 μs | 1.1778 μs | 5.200 μs |  1.04 |    0.23 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 5.620 μs | 21.645 μs | 1.1865 μs | 5.358 μs |  1.09 |    0.23 |    3 |     152 B |        2.71 |
