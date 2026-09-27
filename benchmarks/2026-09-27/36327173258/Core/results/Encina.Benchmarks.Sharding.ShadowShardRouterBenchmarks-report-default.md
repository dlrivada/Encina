
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.344 μs** |  **4.966 μs** | **0.2722 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.511 μs |  2.745 μs | 0.1504 μs |  1.08 |    0.12 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.462 μs | 10.562 μs | 0.5790 μs |  1.49 |    0.26 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.562 μs |  4.514 μs | 0.2474 μs |  1.10 |    0.14 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.864 μs |  8.561 μs | 0.4693 μs |  1.23 |    0.21 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.098 μs** |  **5.944 μs** | **0.3258 μs** |  **1.01** |    **0.13** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.954 μs | 11.183 μs | 0.6130 μs |  0.96 |    0.19 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 3.749 μs |  4.948 μs | 0.2712 μs |  1.22 |    0.14 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.891 μs |  4.105 μs | 0.2250 μs |  0.94 |    0.11 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.396 μs |  4.035 μs | 0.2212 μs |  1.10 |    0.12 |    1 |      64 B |        1.14 |
