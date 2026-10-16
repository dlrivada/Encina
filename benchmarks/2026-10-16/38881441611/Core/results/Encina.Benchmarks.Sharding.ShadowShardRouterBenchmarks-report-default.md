
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.427 μs** | **29.820 μs** | **1.6345 μs** | **2.803 μs** |  **1.14** |    **0.64** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.256 μs | 36.769 μs | 2.0155 μs | 2.305 μs |  1.09 |    0.72 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 6.245 μs | 38.067 μs | 2.0866 μs | 5.141 μs |  2.08 |    0.97 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.339 μs | 31.284 μs | 1.7148 μs | 2.655 μs |  1.11 |    0.65 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.359 μs | 33.125 μs | 1.8157 μs | 2.594 μs |  1.12 |    0.68 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.890 μs** | **16.671 μs** | **0.9138 μs** | **3.481 μs** |  **1.03** |    **0.28** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.375 μs | 12.728 μs | 0.6976 μs | 4.079 μs |  1.16 |    0.27 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.667 μs | 23.210 μs | 1.2722 μs | 5.186 μs |  1.51 |    0.40 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.373 μs | 26.876 μs | 1.4732 μs | 3.801 μs |  1.16 |    0.40 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 5.242 μs | 20.556 μs | 1.1267 μs | 4.821 μs |  1.39 |    0.36 |    1 |      64 B |        1.14 |
