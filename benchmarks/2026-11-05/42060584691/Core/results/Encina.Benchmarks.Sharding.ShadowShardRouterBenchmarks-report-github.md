```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.53GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **4.362 μs** | **12.016 μs** | **0.6586 μs** |  **1.02** |    **0.19** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 3.267 μs | 16.731 μs | 0.9171 μs |  0.76 |    0.21 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.340 μs | 14.907 μs | 0.8171 μs |  1.01 |    0.21 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.230 μs |  9.148 μs | 0.5014 μs |  0.75 |    0.14 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.803 μs | 10.520 μs | 0.5766 μs |  0.89 |    0.16 |    2 |      64 B |        1.14 |
|                                          |            |          |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.815 μs** |  **5.278 μs** | **0.2893 μs** |  **1.00** |    **0.09** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.895 μs |  4.251 μs | 0.2330 μs |  1.02 |    0.08 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 7.799 μs | 16.844 μs | 0.9232 μs |  2.05 |    0.25 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.861 μs |  5.891 μs | 0.3229 μs |  1.02 |    0.10 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.516 μs |  4.989 μs | 0.2735 μs |  1.19 |    0.10 |    1 |      64 B |        1.14 |
