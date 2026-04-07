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
| **&#39;Hash routing&#39;**                   | **3**          | **17,657.2 μs** |    **NA** |  **1.00** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 18,102.8 μs |    NA |  1.03 |    6 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 17,245.3 μs |    NA |  0.98 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 20,775.3 μs |    NA |  1.18 |    8 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 18,049.8 μs |    NA |  1.02 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |    582.2 μs |    NA |  0.03 |    1 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 18,670.7 μs |    NA |  1.06 |    7 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 17,683.1 μs |    NA |  1.00 |    4 |     152 B |        2.71 |
|                                  |            |             |       |       |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **17,996.4 μs** |    **NA** |  **1.00** |    **5** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 18,415.7 μs |    NA |  1.02 |    7 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 17,210.9 μs |    NA |  0.96 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 20,038.8 μs |    NA |  1.11 |    8 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 17,894.2 μs |    NA |  0.99 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |    578.7 μs |    NA |  0.03 |    1 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 18,227.4 μs |    NA |  1.01 |    6 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 17,817.4 μs |    NA |  0.99 |    3 |     152 B |        2.71 |
