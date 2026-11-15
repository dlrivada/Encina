```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.24GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          |  **3.788 μs** |  **5.3344 μs** | **0.2924 μs** |  **1.00** |    **0.10** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |  2.075 μs |  1.6352 μs | 0.0896 μs |  0.55 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |  3.032 μs |  2.0343 μs | 0.1115 μs |  0.80 |    0.06 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |  5.434 μs | 26.4362 μs | 1.4491 μs |  1.44 |    0.35 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          |  7.403 μs |  3.4821 μs | 0.1909 μs |  1.96 |    0.14 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |  3.784 μs |  4.9699 μs | 0.2724 μs |  1.00 |    0.09 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |  3.286 μs |  4.6616 μs | 0.2555 μs |  0.87 |    0.08 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          |  7.881 μs | 22.4505 μs | 1.2306 μs |  2.09 |    0.32 |    4 |     152 B |        2.71 |
|                                  |            |           |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |  **3.521 μs** |  **4.5317 μs** | **0.2484 μs** |  **1.00** |    **0.09** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |  2.120 μs |  0.7275 μs | 0.0399 μs |  0.60 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         |  3.917 μs | 21.6326 μs | 1.1858 μs |  1.12 |    0.30 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         |  4.381 μs | 12.6199 μs | 0.6917 μs |  1.25 |    0.19 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 10.172 μs |  9.5701 μs | 0.5246 μs |  2.90 |    0.22 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |  3.287 μs |  2.2786 μs | 0.1249 μs |  0.94 |    0.06 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         |  3.853 μs |  3.1955 μs | 0.1752 μs |  1.10 |    0.08 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         |  6.512 μs | 16.9971 μs | 0.9317 μs |  1.86 |    0.26 |    4 |     152 B |        2.71 |
