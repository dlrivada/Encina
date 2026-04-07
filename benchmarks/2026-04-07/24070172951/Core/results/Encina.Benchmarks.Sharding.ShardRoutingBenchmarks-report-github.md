```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **4.128 μs** | **19.7327 μs** | **1.0816 μs** |  **1.04** |    **0.32** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 3.301 μs |  4.3369 μs | 0.2377 μs |  0.83 |    0.18 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 3.905 μs |  8.0775 μs | 0.4428 μs |  0.99 |    0.22 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 5.409 μs |  5.3741 μs | 0.2946 μs |  1.37 |    0.29 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 5.752 μs | 10.6159 μs | 0.5819 μs |  1.45 |    0.32 |    2 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.676 μs |  9.4904 μs | 0.5202 μs |  0.93 |    0.22 |    1 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.830 μs | 10.7905 μs | 0.5915 μs |  0.97 |    0.24 |    1 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.493 μs | 22.2071 μs | 1.2172 μs |  1.64 |    0.43 |    2 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.804 μs** |  **7.6773 μs** | **0.4208 μs** |  **1.01** |    **0.14** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.473 μs |  3.3345 μs | 0.1828 μs |  0.66 |    0.08 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.175 μs |  5.0068 μs | 0.2744 μs |  0.84 |    0.11 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.947 μs |  3.7724 μs | 0.2068 μs |  1.31 |    0.14 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 7.175 μs | 20.3602 μs | 1.1160 μs |  1.90 |    0.32 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.858 μs |  6.4252 μs | 0.3522 μs |  1.02 |    0.13 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 5.051 μs |  0.8206 μs | 0.0450 μs |  1.34 |    0.14 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 6.580 μs | 11.6292 μs | 0.6374 μs |  1.74 |    0.23 |    4 |     152 B |        2.71 |
