```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.52GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean       | Error       | StdDev     | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |-----------:|------------:|-----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **2,405.0 ns** |  **4,389.0 ns** |   **240.6 ns** | **2,398.0 ns** |  **1.01** |    **0.12** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |   868.0 ns | 13,716.8 ns |   751.9 ns |   571.0 ns |  0.36 |    0.28 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 1,262.0 ns | 16,380.4 ns |   897.9 ns | 1,062.0 ns |  0.53 |    0.33 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 1,884.3 ns | 32,939.9 ns | 1,805.5 ns | 1,006.0 ns |  0.79 |    0.66 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 2,759.0 ns | 25,268.8 ns | 1,385.1 ns | 2,228.0 ns |  1.15 |    0.51 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 1,463.2 ns | 21,858.7 ns | 1,198.1 ns |   905.5 ns |  0.61 |    0.44 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 1,343.7 ns | 22,072.8 ns | 1,209.9 ns |   706.0 ns |  0.56 |    0.44 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 2,640.7 ns | 34,229.9 ns | 1,876.3 ns | 1,823.0 ns |  1.11 |    0.69 |    4 |     152 B |        2.71 |
|                                  |            |            |             |            |            |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3,279.5 ns** |  **6,675.8 ns** |   **365.9 ns** | **3,109.5 ns** |  **1.01** |    **0.13** |    **5** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1,061.7 ns | 12,087.8 ns |   662.6 ns |   811.0 ns |  0.33 |    0.18 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 1,360.3 ns | 13,543.6 ns |   742.4 ns | 1,047.0 ns |  0.42 |    0.20 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 2,070.0 ns | 22,076.8 ns | 1,210.1 ns | 1,442.0 ns |  0.64 |    0.33 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 4,862.3 ns | 19,822.9 ns | 1,086.6 ns | 4,692.0 ns |  1.49 |    0.32 |    6 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 1,851.0 ns | 16,847.0 ns |   923.4 ns | 1,437.0 ns |  0.57 |    0.25 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 1,783.5 ns | 21,445.9 ns | 1,175.5 ns | 1,175.5 ns |  0.55 |    0.32 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 3,203.3 ns | 23,210.7 ns | 1,272.3 ns | 2,499.0 ns |  0.98 |    0.35 |    4 |     152 B |        2.71 |
