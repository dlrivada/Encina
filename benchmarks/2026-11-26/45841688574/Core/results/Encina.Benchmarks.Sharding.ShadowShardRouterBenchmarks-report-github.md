```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **1.985 μs** | **32.213 μs** | **1.7657 μs** | **1.1670 μs** |  **1.58** |    **1.65** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 1.506 μs | 19.454 μs | 1.0663 μs | 0.9510 μs |  1.20 |    1.08 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 1.923 μs | 29.079 μs | 1.5939 μs | 1.2930 μs |  1.54 |    1.52 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.473 μs | 16.174 μs | 0.8865 μs | 2.9240 μs |  1.97 |    1.32 |    3 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.164 μs | 18.776 μs | 1.0292 μs | 2.7540 μs |  2.53 |    1.64 |    4 |      64 B |        1.14 |
|                                          |            |          |           |           |           |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.292 μs** |  **4.494 μs** | **0.2463 μs** | **3.3150 μs** |  **1.00** |    **0.09** |    **4** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 1.931 μs | 16.603 μs | 0.9100 μs | 1.4780 μs |  0.59 |    0.24 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 4.444 μs | 13.649 μs | 0.7481 μs | 4.1765 μs |  1.35 |    0.22 |    5 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 1.637 μs | 19.837 μs | 1.0873 μs | 1.0970 μs |  0.50 |    0.29 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 2.167 μs | 20.648 μs | 1.1318 μs | 1.8025 μs |  0.66 |    0.30 |    3 |      64 B |        1.14 |
