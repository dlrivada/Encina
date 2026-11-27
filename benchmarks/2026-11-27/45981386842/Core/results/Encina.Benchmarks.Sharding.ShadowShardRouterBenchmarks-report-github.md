```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.12GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **4.735 μs** | **29.115 μs** | **1.5959 μs** | **4.855 μs** |  **1.09** |    **0.48** |    **4** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 2.838 μs | 23.340 μs | 1.2794 μs | 2.203 μs |  0.65 |    0.34 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 3.368 μs | 30.333 μs | 1.6626 μs | 2.936 μs |  0.77 |    0.43 |    3 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 2.510 μs | 33.803 μs | 1.8528 μs | 1.826 μs |  0.58 |    0.43 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 4.321 μs | 18.248 μs | 1.0002 μs | 4.093 μs |  0.99 |    0.38 |    4 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **5.372 μs** | **31.738 μs** | **1.7397 μs** | **4.681 μs** |  **1.06** |    **0.40** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 3.695 μs | 15.909 μs | 0.8720 μs | 3.229 μs |  0.73 |    0.24 |    1 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 6.338 μs | 16.003 μs | 0.8772 μs | 6.763 μs |  1.26 |    0.34 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 5.480 μs | 11.764 μs | 0.6448 μs | 5.139 μs |  1.09 |    0.29 |    2 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 6.022 μs |  2.642 μs | 0.1448 μs | 6.005 μs |  1.19 |    0.29 |    2 |      64 B |        1.14 |
