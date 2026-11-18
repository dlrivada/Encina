
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.125 μs** | **16.324 μs** | **0.8948 μs** |  **1.07** |    **0.41** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.344 μs |  3.976 μs | 0.2179 μs |  0.80 |    0.23 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.655 μs |  4.261 μs | 0.2336 μs |  1.25 |    0.36 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.469 μs |  6.243 μs | 0.3422 μs |  0.84 |    0.26 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.591 μs |  8.605 μs | 0.4717 μs |  0.88 |    0.29 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.968 μs** |  **5.106 μs** | **0.2799 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.125 μs | 11.246 μs | 0.6164 μs |  1.06 |    0.20 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.123 μs |  3.732 μs | 0.2046 μs |  1.40 |    0.13 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.885 μs |  6.322 μs | 0.3466 μs |  0.98 |    0.13 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.175 μs |  9.230 μs | 0.5059 μs |  1.08 |    0.17 |    1 |      64 B |        1.14 |
