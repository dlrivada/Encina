```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.126 μs** |  **7.585 μs** | **0.4157 μs** |  **1.01** |    **0.16** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.964 μs |  4.008 μs | 0.2197 μs |  0.64 |    0.09 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.844 μs |  7.110 μs | 0.3897 μs |  0.92 |    0.15 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.400 μs |  2.199 μs | 0.1206 μs |  1.42 |    0.16 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 5.595 μs |  8.061 μs | 0.4419 μs |  1.81 |    0.23 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 3.488 μs |  5.126 μs | 0.2810 μs |  1.13 |    0.15 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 3.335 μs |  7.121 μs | 0.3903 μs |  1.08 |    0.16 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 5.989 μs | 14.727 μs | 0.8073 μs |  1.94 |    0.31 |    4 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.628 μs** |  **8.145 μs** | **0.4465 μs** |  **1.01** |    **0.15** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 2.707 μs |  2.739 μs | 0.1501 μs |  0.75 |    0.09 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 4.019 μs | 15.548 μs | 0.8523 μs |  1.12 |    0.24 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.014 μs | 11.971 μs | 0.6562 μs |  1.12 |    0.20 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 9.084 μs | 26.684 μs | 1.4627 μs |  2.53 |    0.44 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.981 μs | 18.578 μs | 1.0183 μs |  1.11 |    0.27 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 4.333 μs |  8.219 μs | 0.4505 μs |  1.21 |    0.17 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 8.843 μs | 61.392 μs | 3.3651 μs |  2.46 |    0.86 |    3 |     152 B |        2.71 |
