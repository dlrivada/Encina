
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.769 μs** |  **5.809 μs** | **0.3184 μs** |  **1.01** |    **0.14** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.434 μs |  7.625 μs | 0.4179 μs |  0.89 |    0.16 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.861 μs |  7.968 μs | 0.4368 μs |  1.41 |    0.20 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.975 μs | 12.767 μs | 0.6998 μs |  1.08 |    0.25 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.856 μs |  8.749 μs | 0.4795 μs |  1.04 |    0.19 |    1 |      64 B |        1.14 |
                                          |            |          |           |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **3.324 μs** |  **7.786 μs** | **0.4268 μs** |  **1.01** |    **0.16** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.200 μs |  5.215 μs | 0.2858 μs |  0.97 |    0.14 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.870 μs |  9.496 μs | 0.5205 μs |  1.48 |    0.22 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 4.360 μs | 13.829 μs | 0.7580 μs |  1.33 |    0.25 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.378 μs | 10.086 μs | 0.5528 μs |  1.03 |    0.19 |    1 |      64 B |        1.14 |
