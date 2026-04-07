```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.253 μs** |  **2.272 μs** | **0.1245 μs** | **3.187 μs** |  **1.00** |    **0.05** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.704 μs |  2.556 μs | 0.1401 μs | 2.711 μs |  0.83 |    0.05 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 3.231 μs |  8.768 μs | 0.4806 μs | 3.451 μs |  0.99 |    0.13 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.238 μs |  5.665 μs | 0.3105 μs | 4.238 μs |  1.30 |    0.09 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 6.534 μs |  6.484 μs | 0.3554 μs | 6.438 μs |  2.01 |    0.12 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 4.297 μs |  9.617 μs | 0.5271 μs | 4.468 μs |  1.32 |    0.15 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 4.441 μs | 32.505 μs | 1.7817 μs | 3.412 μs |  1.37 |    0.48 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.055 μs | 16.098 μs | 0.8824 μs | 5.731 μs |  1.86 |    0.24 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |          |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **4.468 μs** |  **4.905 μs** | **0.2689 μs** | **4.608 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.925 μs | 10.786 μs | 0.5912 μs | 2.834 μs |  0.66 |    0.12 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.484 μs |  2.040 μs | 0.1118 μs | 2.504 μs |  0.56 |    0.04 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.908 μs |  1.281 μs | 0.0702 μs | 3.877 μs |  0.88 |    0.05 |    1 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 9.440 μs |  7.810 μs | 0.4281 μs | 9.687 μs |  2.12 |    0.14 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.367 μs |  1.139 μs | 0.0624 μs | 3.387 μs |  0.76 |    0.04 |    1 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.964 μs |  2.417 μs | 0.1325 μs | 3.938 μs |  0.89 |    0.05 |    1 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 7.138 μs |  5.183 μs | 0.2841 μs | 7.239 μs |  1.60 |    0.10 |    2 |     152 B |        2.71 |
