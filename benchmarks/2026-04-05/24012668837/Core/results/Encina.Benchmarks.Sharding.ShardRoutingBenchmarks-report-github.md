```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

```
| Method                           | ShardCount | Mean        | Error | Ratio | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |------------:|------:|------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **17,929.9 μs** |    **NA** |  **1.00** |    **4** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 18,715.2 μs |    NA |  1.04 |    6 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 17,515.7 μs |    NA |  0.98 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 20,968.5 μs |    NA |  1.17 |    8 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 17,874.6 μs |    NA |  1.00 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |    626.8 μs |    NA |  0.03 |    1 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 19,448.7 μs |    NA |  1.08 |    7 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 18,559.1 μs |    NA |  1.04 |    5 |     152 B |        2.71 |
|                                  |            |             |       |       |      |           |             |
| **&#39;Hash routing&#39;**                   | **10**         | **18,137.3 μs** |    **NA** |  **1.00** |    **4** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 10         | 18,801.4 μs |    NA |  1.04 |    7 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 10         | 17,679.5 μs |    NA |  0.97 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 10         | 20,667.9 μs |    NA |  1.14 |    8 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 10         | 18,289.3 μs |    NA |  1.01 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 10         |    622.4 μs |    NA |  0.03 |    1 |     160 B |        2.86 |
| GetShardConnectionString         | 10         | 18,490.8 μs |    NA |  1.02 |    6 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 10         | 18,079.5 μs |    NA |  1.00 |    3 |     152 B |        2.71 |
|                                  |            |             |       |       |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **18,381.9 μs** |    **NA** |  **1.00** |    **6** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 18,353.2 μs |    NA |  1.00 |    5 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 17,448.6 μs |    NA |  0.95 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 20,235.1 μs |    NA |  1.10 |    8 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 18,495.7 μs |    NA |  1.01 |    7 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |    637.6 μs |    NA |  0.03 |    1 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 18,244.8 μs |    NA |  0.99 |    4 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 17,967.5 μs |    NA |  0.98 |    3 |     152 B |        2.71 |
