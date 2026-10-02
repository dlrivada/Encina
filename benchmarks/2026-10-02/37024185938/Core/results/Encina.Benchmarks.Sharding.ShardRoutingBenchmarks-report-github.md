```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.724 μs** |  **8.337 μs** | **0.4570 μs** |  **1.02** |    **0.20** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.513 μs |  1.749 μs | 0.0959 μs |  0.57 |    0.08 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.245 μs |  2.304 μs | 0.1263 μs |  0.84 |    0.12 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.008 μs |  5.821 μs | 0.3191 μs |  1.12 |    0.19 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 3.925 μs |  9.094 μs | 0.4984 μs |  1.47 |    0.26 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.678 μs |  9.424 μs | 0.5165 μs |  1.00 |    0.22 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.849 μs |  7.307 μs | 0.4005 μs |  1.06 |    0.20 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.370 μs |  6.021 μs | 0.3300 μs |  1.63 |    0.25 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.355 μs** |  **7.603 μs** | **0.4167 μs** |  **1.01** |    **0.15** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.965 μs |  3.520 μs | 0.1930 μs |  0.59 |    0.08 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.286 μs |  2.281 μs | 0.1250 μs |  0.69 |    0.08 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.465 μs | 11.151 μs | 0.6112 μs |  1.04 |    0.19 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 6.397 μs |  6.639 μs | 0.3639 μs |  1.92 |    0.22 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.085 μs |  1.284 μs | 0.0704 μs |  0.93 |    0.10 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.380 μs |  5.357 μs | 0.2937 μs |  1.02 |    0.13 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.487 μs |  7.933 μs | 0.4349 μs |  1.35 |    0.18 |    3 |     152 B |        2.71 |
