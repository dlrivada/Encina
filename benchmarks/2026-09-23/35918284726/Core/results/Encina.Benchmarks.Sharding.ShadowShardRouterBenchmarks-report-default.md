
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.873 μs** | **27.599 μs** | **1.5128 μs** | **2.055 μs** |  **1.17** |    **0.69** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.044 μs | 24.224 μs | 1.3278 μs | 2.654 μs |  1.24 |    0.66 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.854 μs | 42.236 μs | 2.3151 μs | 2.736 μs |  1.56 |    1.02 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.906 μs | 29.027 μs | 1.5911 μs | 2.026 μs |  1.18 |    0.72 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.044 μs | 32.800 μs | 1.7979 μs | 2.399 μs |  1.24 |    0.79 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |          |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.837 μs** | **23.053 μs** | **1.2636 μs** | **3.123 μs** |  **1.06** |    **0.40** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 6.080 μs |  7.127 μs | 0.3907 μs | 6.232 μs |  1.69 |    0.42 |    2 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.360 μs | 24.515 μs | 1.3437 μs | 4.756 μs |  1.49 |    0.49 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.120 μs | 19.024 μs | 1.0428 μs | 3.816 μs |  1.14 |    0.38 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.756 μs | 25.217 μs | 1.3822 μs | 4.237 μs |  1.32 |    0.46 |    1 |      64 B |        1.14 |
