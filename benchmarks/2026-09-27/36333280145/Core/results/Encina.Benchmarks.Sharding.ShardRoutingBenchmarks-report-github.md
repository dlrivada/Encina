```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean       | Error       | StdDev     | Median     | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |-----------:|------------:|-----------:|-----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **1,702.2 ns** | **16,955.8 ns** |   **929.4 ns** | **1,562.5 ns** |  **1.24** |    **0.88** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |   891.2 ns | 13,142.6 ns |   720.4 ns |   500.5 ns |  0.65 |    0.59 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 2,672.2 ns |  5,908.3 ns |   323.9 ns | 2,618.5 ns |  1.95 |    0.97 |    5 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 2,021.2 ns | 23,402.8 ns | 1,282.8 ns | 1,346.5 ns |  1.47 |    1.14 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 2,774.3 ns | 22,651.7 ns | 1,241.6 ns | 2,224.0 ns |  2.02 |    1.31 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 2,028.3 ns | 36,289.9 ns | 1,989.2 ns |   987.0 ns |  1.48 |    1.56 |    4 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 1,676.3 ns | 22,257.0 ns | 1,220.0 ns |   982.0 ns |  1.22 |    1.04 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 3,381.0 ns | 19,561.7 ns | 1,072.2 ns | 3,064.0 ns |  2.46 |    1.41 |    6 |     152 B |        2.71 |
|                                  |            |            |             |            |            |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **2,141.7 ns** | **21,698.5 ns** | **1,189.4 ns** | **1,638.0 ns** |  **1.19** |    **0.77** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 1,110.3 ns | 12,263.9 ns |   672.2 ns |   826.0 ns |  0.62 |    0.42 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 1,621.3 ns | 21,293.2 ns | 1,167.2 ns | 1,087.0 ns |  0.90 |    0.70 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 2,682.3 ns | 23,187.7 ns | 1,271.0 ns | 2,368.0 ns |  1.50 |    0.87 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 4,381.2 ns | 20,469.9 ns | 1,122.0 ns | 4,341.5 ns |  2.44 |    1.11 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 2,122.3 ns | 24,907.8 ns | 1,365.3 ns | 1,431.0 ns |  1.18 |    0.84 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 2,166.7 ns | 24,218.5 ns | 1,327.5 ns | 1,492.0 ns |  1.21 |    0.83 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 4,804.0 ns | 17,898.9 ns |   981.1 ns | 4,357.0 ns |  2.68 |    1.16 |    5 |     152 B |        2.71 |
