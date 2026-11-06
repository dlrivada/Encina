
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.81GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

 Method                                   | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
----------------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
 **'Bare HashRouter'**                        | **3**          | **4.041 μs** | **15.7779 μs** | **0.8648 μs** |  **1.03** |    **0.27** |    **1** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 3          | 4.306 μs |  4.8964 μs | 0.2684 μs |  1.10 |    0.21 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 3          | 5.015 μs |  7.4376 μs | 0.4077 μs |  1.28 |    0.25 |    1 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 3          | 3.843 μs |  9.7642 μs | 0.5352 μs |  0.98 |    0.22 |    1 |     104 B |        1.86 |
 'Decorated GetShardConnectionString'     | 3          | 3.651 μs |  5.6160 μs | 0.3078 μs |  0.93 |    0.18 |    1 |      64 B |        1.14 |
                                          |            |          |            |           |       |         |      |           |             |
 **'Bare HashRouter'**                        | **50**         | **5.804 μs** |  **6.5222 μs** | **0.3575 μs** |  **1.00** |    **0.08** |    **2** |      **56 B** |        **1.00** |
 'Decorated GetShardId (production path)' | 50         | 3.891 μs |  9.1457 μs | 0.5013 μs |  0.67 |    0.08 |    1 |      56 B |        1.00 |
 'Decorated CompareAsync'                 | 50         | 5.067 μs |  8.6959 μs | 0.4767 μs |  0.88 |    0.09 |    2 |     320 B |        5.71 |
 'Decorated GetAllShardIds'               | 50         | 6.084 μs | 14.6617 μs | 0.8037 μs |  1.05 |    0.13 |    2 |     480 B |        8.57 |
 'Decorated GetShardConnectionString'     | 50         | 4.668 μs |  0.0105 μs | 0.0006 μs |  0.81 |    0.04 |    2 |      64 B |        1.14 |
