```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                                   | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|----------------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Bare HashRouter&#39;**                        | **3**          | **3.533 μs** | **17.215 μs** | **0.9436 μs** | **3.640 μs** |  **1.05** |    **0.36** |    **2** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 3          | 4.039 μs | 12.743 μs | 0.6985 μs | 3.860 μs |  1.20 |    0.35 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 3          | 4.659 μs | 29.144 μs | 1.5975 μs | 4.614 μs |  1.39 |    0.55 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 3          | 3.074 μs | 29.100 μs | 1.5950 μs | 2.417 μs |  0.92 |    0.48 |    1 |     104 B |        1.86 |
| &#39;Decorated GetShardConnectionString&#39;     | 3          | 3.188 μs | 31.516 μs | 1.7275 μs | 2.505 μs |  0.95 |    0.52 |    1 |      64 B |        1.14 |
|                                          |            |          |           |           |          |       |         |      |           |             |
| **&#39;Bare HashRouter&#39;**                        | **50**         | **3.822 μs** | **15.690 μs** | **0.8600 μs** | **3.480 μs** |  **1.03** |    **0.27** |    **1** |      **56 B** |        **1.00** |
| &#39;Decorated GetShardId (production path)&#39; | 50         | 5.375 μs | 17.899 μs | 0.9811 μs | 5.176 μs |  1.45 |    0.35 |    2 |      56 B |        1.00 |
| &#39;Decorated CompareAsync&#39;                 | 50         | 5.708 μs | 11.332 μs | 0.6212 μs | 5.457 μs |  1.54 |    0.31 |    2 |     320 B |        5.71 |
| &#39;Decorated GetAllShardIds&#39;               | 50         | 3.604 μs | 17.400 μs | 0.9537 μs | 3.305 μs |  0.97 |    0.28 |    1 |     480 B |        8.57 |
| &#39;Decorated GetShardConnectionString&#39;     | 50         | 4.789 μs | 13.436 μs | 0.7365 μs | 4.505 μs |  1.29 |    0.29 |    2 |      64 B |        1.14 |
