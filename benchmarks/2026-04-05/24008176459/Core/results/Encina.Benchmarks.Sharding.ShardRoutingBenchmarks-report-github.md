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
| **&#39;Hash routing&#39;**                   | **3**          | **18,138.3 μs** |    **NA** |  **1.00** |    **4** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 18,472.8 μs |    NA |  1.02 |    5 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 17,505.5 μs |    NA |  0.97 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 20,956.5 μs |    NA |  1.16 |    8 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 18,025.5 μs |    NA |  0.99 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |    661.6 μs |    NA |  0.04 |    1 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 19,167.0 μs |    NA |  1.06 |    7 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 18,546.6 μs |    NA |  1.02 |    6 |     152 B |        2.71 |
|                                  |            |             |       |       |      |           |             |
| **&#39;Hash routing&#39;**                   | **10**         | **18,032.7 μs** |    **NA** |  **1.00** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 10         | 18,727.3 μs |    NA |  1.04 |    6 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 10         | 18,334.3 μs |    NA |  1.02 |    4 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 10         | 20,574.8 μs |    NA |  1.14 |    8 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 10         | 18,632.0 μs |    NA |  1.03 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 10         |    712.1 μs |    NA |  0.04 |    1 |     160 B |        2.86 |
| GetShardConnectionString         | 10         | 19,026.3 μs |    NA |  1.06 |    7 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 10         | 18,163.1 μs |    NA |  1.01 |    3 |     152 B |        2.71 |
|                                  |            |             |       |       |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **18,247.1 μs** |    **NA** |  **1.00** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 19,078.2 μs |    NA |  1.05 |    7 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 17,799.1 μs |    NA |  0.98 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 20,658.1 μs |    NA |  1.13 |    8 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 18,621.2 μs |    NA |  1.02 |    6 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |    717.6 μs |    NA |  0.04 |    1 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 18,521.0 μs |    NA |  1.02 |    4 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 18,589.4 μs |    NA |  1.02 |    5 |     152 B |        2.71 |
