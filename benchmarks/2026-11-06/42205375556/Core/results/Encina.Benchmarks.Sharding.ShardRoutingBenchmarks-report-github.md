```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.175 μs** |  **3.961 μs** | **0.2171 μs** |  **1.00** |    **0.08** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 2.964 μs |  6.014 μs | 0.3297 μs |  0.94 |    0.10 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 3.088 μs | 10.939 μs | 0.5996 μs |  0.98 |    0.17 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 4.034 μs |  5.851 μs | 0.3207 μs |  1.27 |    0.11 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 7.293 μs | 20.017 μs | 1.0972 μs |  2.30 |    0.33 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 4.378 μs | 16.508 μs | 0.9048 μs |  1.38 |    0.26 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 4.291 μs | 11.676 μs | 0.6400 μs |  1.36 |    0.19 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 6.184 μs | 14.604 μs | 0.8005 μs |  1.95 |    0.25 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **3.662 μs** |  **6.494 μs** | **0.3560 μs** |  **1.01** |    **0.12** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 3.203 μs |  5.763 μs | 0.3159 μs |  0.88 |    0.10 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 3.348 μs |  9.600 μs | 0.5262 μs |  0.92 |    0.15 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 4.178 μs |  8.502 μs | 0.4660 μs |  1.15 |    0.14 |    1 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 8.905 μs | 12.703 μs | 0.6963 μs |  2.45 |    0.26 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 3.937 μs |  7.925 μs | 0.4344 μs |  1.08 |    0.13 |    1 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 5.123 μs |  8.798 μs | 0.4822 μs |  1.41 |    0.16 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 8.004 μs | 13.370 μs | 0.7329 μs |  2.20 |    0.25 |    3 |     152 B |        2.71 |
