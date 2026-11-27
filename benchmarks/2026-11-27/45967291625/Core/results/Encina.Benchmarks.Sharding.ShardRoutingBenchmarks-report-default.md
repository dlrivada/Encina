
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          | **2.779 μs** | **25.033 μs** | **1.3721 μs** | **2.083 μs** |  **1.15** |    **0.64** |    **1** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          | 3.289 μs | 14.553 μs | 0.7977 μs | 3.179 μs |  1.36 |    0.55 |    2 |      48 B |        0.86 |
 'Directory routing'              | 3          | 3.197 μs | 31.652 μs | 1.7350 μs | 2.325 μs |  1.32 |    0.79 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 3          | 4.980 μs | 19.335 μs | 1.0598 μs | 5.441 μs |  2.05 |    0.80 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          | 6.248 μs | 22.254 μs | 1.2198 μs | 5.769 μs |  2.58 |    0.98 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 3          | 3.014 μs | 24.596 μs | 1.3482 μs | 2.539 μs |  1.24 |    0.66 |    1 |     104 B |        1.86 |
 GetShardConnectionString         | 3          | 3.086 μs | 26.407 μs | 1.4475 μs | 2.554 μs |  1.27 |    0.69 |    1 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          | 5.061 μs | 41.429 μs | 2.2709 μs | 4.413 μs |  2.09 |    1.10 |    3 |     152 B |        2.71 |
                                  |            |          |           |           |          |       |         |      |           |             |
 **'Hash routing'**                   | **50**         | **4.129 μs** | **11.033 μs** | **0.6048 μs** | **3.956 μs** |  **1.01** |    **0.18** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         | 2.338 μs |  6.578 μs | 0.3606 μs | 2.151 μs |  0.57 |    0.10 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         | 2.861 μs | 12.507 μs | 0.6855 μs | 2.615 μs |  0.70 |    0.17 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 50         | 4.475 μs | 18.688 μs | 1.0243 μs | 4.151 μs |  1.10 |    0.26 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 8.107 μs | 19.367 μs | 1.0616 μs | 7.696 μs |  1.99 |    0.33 |    5 |     416 B |        7.43 |
 GetAllShardIds                   | 50         | 3.672 μs | 17.187 μs | 0.9421 μs | 3.143 μs |  0.90 |    0.23 |    3 |     480 B |        8.57 |
 GetShardConnectionString         | 50         | 4.519 μs | 18.220 μs | 0.9987 μs | 4.283 μs |  1.11 |    0.25 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         | 6.392 μs | 23.276 μs | 1.2758 μs | 5.982 μs |  1.57 |    0.33 |    4 |     152 B |        2.71 |
