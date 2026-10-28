
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                           | ShardCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
--------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Hash routing'**                   | **3**          |  **3.585 μs** |  **4.800 μs** | **0.2631 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 3          |  2.059 μs |  3.187 μs | 0.1747 μs |  0.58 |    0.06 |    1 |      48 B |        0.86 |
 'Directory routing'              | 3          |  2.857 μs |  6.014 μs | 0.3297 μs |  0.80 |    0.10 |    2 |      24 B |        0.43 |
 'Geo routing'                    | 3          |  5.300 μs |  3.891 μs | 0.2133 μs |  1.48 |    0.11 |    4 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 3          |  8.665 μs | 15.399 μs | 0.8440 μs |  2.43 |    0.26 |    6 |     416 B |        7.43 |
 GetAllShardIds                   | 3          |  3.927 μs |  3.501 μs | 0.1919 μs |  1.10 |    0.09 |    3 |     104 B |        1.86 |
 GetShardConnectionString         | 3          |  3.803 μs | 17.728 μs | 0.9717 μs |  1.06 |    0.25 |    3 |      64 B |        1.14 |
 'Directory add + lookup'         | 3          |  6.216 μs | 10.742 μs | 0.5888 μs |  1.74 |    0.18 |    5 |     152 B |        2.71 |
                                  |            |           |           |           |       |         |      |           |             |
 **'Hash routing'**                   | **50**         |  **6.452 μs** | **20.258 μs** | **1.1104 μs** |  **1.02** |    **0.23** |    **3** |      **56 B** |        **1.00** |
 'Range routing'                  | 50         |  2.865 μs |  4.275 μs | 0.2343 μs |  0.45 |    0.08 |    1 |      48 B |        0.86 |
 'Directory routing'              | 50         |  2.784 μs |  5.367 μs | 0.2942 μs |  0.44 |    0.08 |    1 |      24 B |        0.43 |
 'Geo routing'                    | 50         |  6.268 μs | 49.269 μs | 2.7006 μs |  0.99 |    0.41 |    3 |      96 B |        1.71 |
 'Hash routing (miss → re-route)' | 50         | 10.226 μs | 18.967 μs | 1.0397 μs |  1.62 |    0.30 |    4 |     416 B |        7.43 |
 GetAllShardIds                   | 50         |  4.136 μs | 10.555 μs | 0.5786 μs |  0.66 |    0.13 |    2 |     480 B |        8.57 |
 GetShardConnectionString         | 50         |  4.332 μs | 12.485 μs | 0.6844 μs |  0.69 |    0.15 |    2 |      64 B |        1.14 |
 'Directory add + lookup'         | 50         |  8.691 μs | 30.998 μs | 1.6991 μs |  1.38 |    0.33 |    4 |     152 B |        2.71 |
