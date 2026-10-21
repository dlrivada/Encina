```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2.748 μs** |  **0.7478 μs** | **0.0410 μs** |  **1.00** |    **0.02** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 1.566 μs |  1.3415 μs | 0.0735 μs |  0.57 |    0.02 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2.230 μs |  1.8450 μs | 0.1011 μs |  0.81 |    0.03 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 3.365 μs |  8.1411 μs | 0.4462 μs |  1.22 |    0.14 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 4.296 μs |  8.8799 μs | 0.4867 μs |  1.56 |    0.15 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2.798 μs |  4.2550 μs | 0.2332 μs |  1.02 |    0.07 |    3 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 2.386 μs |  9.8960 μs | 0.5424 μs |  0.87 |    0.17 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 4.348 μs |  7.8021 μs | 0.4277 μs |  1.58 |    0.14 |    5 |     152 B |        2.71 |
|                                  |            |          |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **2.931 μs** |  **5.9090 μs** | **0.3239 μs** |  **1.01** |    **0.13** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1.935 μs |  2.3244 μs | 0.1274 μs |  0.67 |    0.07 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 2.144 μs |  1.3774 μs | 0.0755 μs |  0.74 |    0.07 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 3.956 μs | 12.7980 μs | 0.7015 μs |  1.36 |    0.24 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 5.765 μs |  9.5075 μs | 0.5211 μs |  1.98 |    0.24 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 2.831 μs |  4.9067 μs | 0.2690 μs |  0.97 |    0.12 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 3.185 μs |  5.6729 μs | 0.3110 μs |  1.10 |    0.14 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4.754 μs |  7.5196 μs | 0.4122 μs |  1.63 |    0.19 |    3 |     152 B |        2.71 |
