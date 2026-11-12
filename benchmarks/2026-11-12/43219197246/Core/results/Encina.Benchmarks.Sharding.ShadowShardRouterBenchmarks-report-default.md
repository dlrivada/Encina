
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error    | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|---------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.494 μs** | **4.458 μs** | **0.2443 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.350 μs | 4.399 μs | 0.2411 μs |  0.95 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.526 μs | 8.132 μs | 0.4457 μs |  1.42 |    0.19 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.717 μs | 7.652 μs | 0.4194 μs |  1.10 |    0.17 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.445 μs | 8.597 μs | 0.4712 μs |  0.99 |    0.18 |    1 |      64 B |        1.14 |
                                          |            |          |          |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.295 μs** | **2.377 μs** | **0.1303 μs** |  **1.00** |    **0.05** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.273 μs | 4.225 μs | 0.2316 μs |  0.99 |    0.07 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.126 μs | 1.766 μs | 0.0968 μs |  1.25 |    0.05 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 3.131 μs | 8.669 μs | 0.4752 μs |  0.95 |    0.13 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.268 μs | 8.096 μs | 0.4438 μs |  0.99 |    0.12 |    1 |      64 B |        1.14 |
