
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **4.024 μs** |  **6.388 μs** | **0.3502 μs** |  **1.01** |    **0.11** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.139 μs |  4.222 μs | 0.2314 μs |  0.78 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.505 μs | 10.456 μs | 0.5731 μs |  1.13 |    0.15 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.822 μs | 12.745 μs | 0.6986 μs |  0.95 |    0.17 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.324 μs |  3.117 μs | 0.1709 μs |  0.83 |    0.07 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.780 μs** |  **5.713 μs** | **0.3132 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.791 μs |  5.187 μs | 0.2843 μs |  1.01 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.165 μs |  9.227 μs | 0.5057 μs |  1.37 |    0.15 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.726 μs |  6.758 μs | 0.3704 μs |  0.99 |    0.11 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.749 μs | 18.388 μs | 1.0079 μs |  1.26 |    0.25 |    2 |      64 B |        1.14 |
