```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          |  **5.630 μs** | **22.777 μs** | **1.2485 μs** |  **1.04** |    **0.31** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |  2.511 μs |  4.984 μs | 0.2732 μs |  0.46 |    0.11 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |  4.144 μs | 14.930 μs | 0.8184 μs |  0.76 |    0.21 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |  5.027 μs |  7.136 μs | 0.3912 μs |  0.93 |    0.21 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          |  6.277 μs | 12.413 μs | 0.6804 μs |  1.16 |    0.28 |    2 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |  3.605 μs |  5.930 μs | 0.3250 μs |  0.67 |    0.16 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |  5.298 μs | 23.753 μs | 1.3020 μs |  0.98 |    0.30 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          |  8.242 μs | 17.239 μs | 0.9449 μs |  1.52 |    0.37 |    3 |     152 B |        2.71 |
|                                  |            |           |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |  **5.788 μs** | **11.245 μs** | **0.6164 μs** |  **1.01** |    **0.13** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |  2.596 μs |  7.463 μs | 0.4091 μs |  0.45 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         |  3.950 μs |  7.452 μs | 0.4085 μs |  0.69 |    0.09 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         |  5.484 μs | 21.400 μs | 1.1730 μs |  0.95 |    0.20 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 14.476 μs | 22.609 μs | 1.2393 μs |  2.52 |    0.29 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |  5.713 μs | 25.364 μs | 1.3903 μs |  0.99 |    0.23 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         |  4.274 μs | 19.343 μs | 1.0603 μs |  0.74 |    0.17 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         |  9.850 μs | 35.582 μs | 1.9504 μs |  1.71 |    0.33 |    4 |     152 B |        2.71 |
