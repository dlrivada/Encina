```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean      | Error      | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|-----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          |  **2.912 μs** |   **4.003 μs** | **0.2194 μs** | **2.785 μs** |  **1.00** |    **0.09** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |  1.546 μs |   2.738 μs | 0.1501 μs | 1.552 μs |  0.53 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |  2.019 μs |   3.065 μs | 0.1680 μs | 1.932 μs |  0.70 |    0.07 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |  2.900 μs |   8.047 μs | 0.4411 μs | 2.714 μs |  1.00 |    0.15 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          |  4.798 μs |   3.373 μs | 0.1849 μs | 4.752 μs |  1.65 |    0.12 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |  2.934 μs |   4.727 μs | 0.2591 μs | 2.884 μs |  1.01 |    0.10 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |  2.761 μs |  11.774 μs | 0.6454 μs | 2.413 μs |  0.95 |    0.20 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          |  4.450 μs |   8.441 μs | 0.4627 μs | 4.306 μs |  1.53 |    0.17 |    4 |     152 B |        2.71 |
|                                  |            |           |            |           |          |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |  **2.844 μs** |   **1.751 μs** | **0.0960 μs** | **2.794 μs** |  **1.00** |    **0.04** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |  1.784 μs |   2.161 μs | 0.1185 μs | 1.847 μs |  0.63 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         |  2.408 μs |   4.148 μs | 0.2274 μs | 2.324 μs |  0.85 |    0.07 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         |  3.842 μs |   8.345 μs | 0.4574 μs | 4.005 μs |  1.35 |    0.14 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 11.301 μs | 173.509 μs | 9.5106 μs | 6.190 μs |  3.98 |    2.90 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |  3.485 μs |   9.953 μs | 0.5456 μs | 3.264 μs |  1.23 |    0.17 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         |  3.616 μs |   4.583 μs | 0.2512 μs | 3.756 μs |  1.27 |    0.08 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         |  4.704 μs |   7.768 μs | 0.4258 μs | 4.777 μs |  1.65 |    0.14 |    4 |     152 B |        2.71 |
