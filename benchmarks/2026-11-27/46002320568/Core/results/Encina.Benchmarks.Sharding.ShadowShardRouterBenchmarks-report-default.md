
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **2.711 μs** |  **4.3444 μs** | **0.2381 μs** |  **1.00** |    **0.11** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 2.539 μs | 16.2203 μs | 0.8891 μs |  0.94 |    0.29 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 3.447 μs | 10.0165 μs | 0.5490 μs |  1.28 |    0.20 |    3 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 2.724 μs |  5.4101 μs | 0.2965 μs |  1.01 |    0.12 |    2 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 2.727 μs |  0.7567 μs | 0.0415 μs |  1.01 |    0.07 |    2 |      64 B |        1.14 |
                                          |            |          |            |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **2.915 μs** |  **3.1914 μs** | **0.1749 μs** |  **1.00** |    **0.07** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 2.855 μs |  5.0466 μs | 0.2766 μs |  0.98 |    0.10 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 4.110 μs |  3.7774 μs | 0.2071 μs |  1.41 |    0.10 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 2.756 μs |  8.5129 μs | 0.4666 μs |  0.95 |    0.15 |    1 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 3.395 μs |  1.2047 μs | 0.0660 μs |  1.17 |    0.07 |    1 |      64 B |        1.14 |
