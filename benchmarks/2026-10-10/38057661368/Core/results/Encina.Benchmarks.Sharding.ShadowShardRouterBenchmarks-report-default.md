
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **3.215 μs** |  **4.449 μs** | **0.2439 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 3.267 μs |  5.718 μs | 0.3134 μs |  1.02 |    0.11 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 4.368 μs |  2.217 μs | 0.1215 μs |  1.36 |    0.09 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.726 μs |  2.860 μs | 0.1568 μs |  1.16 |    0.08 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.687 μs | 10.323 μs | 0.5659 μs |  1.15 |    0.17 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.886 μs** |  **5.959 μs** | **0.3266 μs** |  **1.00** |    **0.10** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 4.271 μs |  2.356 μs | 0.1292 μs |  1.10 |    0.09 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 6.609 μs | 29.842 μs | 1.6357 μs |  1.71 |    0.39 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 5.540 μs |  8.737 μs | 0.4789 μs |  1.43 |    0.15 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.061 μs |  3.946 μs | 0.2163 μs |  1.05 |    0.09 |    1 |      64 B |        1.14 |
