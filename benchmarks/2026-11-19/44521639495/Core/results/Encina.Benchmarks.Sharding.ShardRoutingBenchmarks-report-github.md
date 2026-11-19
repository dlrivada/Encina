```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.644 μs** |  **2.641 μs** | **0.1448 μs** |  **1.00** |    **0.07** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.667 μs |  1.120 μs | 0.0614 μs |  0.63 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.310 μs |  3.650 μs | 0.2001 μs |  0.88 |    0.08 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.193 μs | 11.923 μs | 0.6536 μs |  1.59 |    0.23 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.805 μs | 16.401 μs | 0.8990 μs |  1.82 |    0.31 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.098 μs |  9.439 μs | 0.5174 μs |  1.17 |    0.18 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.004 μs |  2.631 μs | 0.1442 μs |  1.14 |    0.07 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.397 μs |  6.970 μs | 0.3820 μs |  1.67 |    0.15 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **2.791 μs** |  **7.984 μs** | **0.4376 μs** |  **1.02** |    **0.19** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.232 μs |  3.911 μs | 0.2144 μs |  0.81 |    0.12 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.467 μs |  8.785 μs | 0.4815 μs |  0.90 |    0.19 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.896 μs |  8.357 μs | 0.4581 μs |  1.42 |    0.23 |    1 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.903 μs |  5.488 μs | 0.3008 μs |  2.15 |    0.28 |    2 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.376 μs | 16.116 μs | 0.8834 μs |  1.23 |    0.32 |    1 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.594 μs | 14.683 μs | 0.8048 μs |  1.31 |    0.30 |    1 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 5.334 μs |  4.811 μs | 0.2637 μs |  1.94 |    0.26 |    2 |     152 B |        2.71 |
