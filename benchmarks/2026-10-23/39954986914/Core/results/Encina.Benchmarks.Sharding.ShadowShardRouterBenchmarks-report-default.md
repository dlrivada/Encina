
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.87GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean      | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |----------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          |  **3.135 μs** |   **8.203 μs** | **0.4496 μs** | **3.065 μs** |  **1.01** |    **0.18** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          |  3.401 μs |  11.096 μs | 0.6082 μs | 3.264 μs |  1.10 |    0.22 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          |  4.283 μs |  14.755 μs | 0.8088 μs | 4.205 μs |  1.38 |    0.28 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          |  3.861 μs |  13.603 μs | 0.7456 μs | 3.431 μs |  1.25 |    0.26 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          |  3.335 μs |  15.522 μs | 0.8508 μs | 2.885 μs |  1.08 |    0.27 |    1 |      64 B |        1.14 |
                                          |            |           |            |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         |  **4.771 μs** |  **12.747 μs** | **0.6987 μs** | **4.566 μs** |  **1.01** |    **0.18** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         |  4.691 μs |  18.848 μs | 1.0331 μs | 4.758 μs |  1.00 |    0.23 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 12.909 μs | 135.949 μs | 7.4518 μs | 8.753 μs |  2.74 |    1.42 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         |  3.706 μs |   7.550 μs | 0.4138 μs | 3.586 μs |  0.79 |    0.12 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         |  5.209 μs |  21.095 μs | 1.1563 μs | 4.618 μs |  1.11 |    0.25 |    2 |      64 B |        1.14 |
