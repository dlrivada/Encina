```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 3.99GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean     | Error     | StdDev    | Median   | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |---------:|----------:|----------:|---------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          | **3.828 μs** | **27.701 μs** | **1.5184 μs** | **3.703 μs** |  **1.12** |    **0.57** |    **1** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          | 3.087 μs |  3.397 μs | 0.1862 μs | 3.058 μs |  0.90 |    0.32 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          | 3.562 μs |  4.950 μs | 0.2713 μs | 3.674 μs |  1.04 |    0.38 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          | 5.558 μs | 15.081 μs | 0.8266 μs | 5.438 μs |  1.62 |    0.62 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          | 7.779 μs | 40.657 μs | 2.2285 μs | 8.409 μs |  2.27 |    1.00 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          | 4.186 μs | 16.691 μs | 0.9149 μs | 4.082 μs |  1.22 |    0.50 |    1 |     104 B |        1.86 |
| GetShardConnectionString         | 3          | 5.436 μs | 27.484 μs | 1.5065 μs | 5.045 μs |  1.59 |    0.69 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          | 7.841 μs | 38.797 μs | 2.1266 μs | 7.252 μs |  2.29 |    0.99 |    3 |     152 B |        2.71 |
|                                  |            |          |           |           |          |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         | **4.906 μs** |  **3.711 μs** | **0.2034 μs** | **4.857 μs** |  **1.00** |    **0.05** |    **3** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         | 3.985 μs | 13.787 μs | 0.7557 μs | 3.716 μs |  0.81 |    0.14 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         | 4.005 μs | 36.929 μs | 2.0242 μs | 2.841 μs |  0.82 |    0.36 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         | 6.206 μs |  5.740 μs | 0.3146 μs | 6.217 μs |  1.27 |    0.07 |    4 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 9.697 μs |  1.701 μs | 0.0932 μs | 9.729 μs |  1.98 |    0.07 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 50         | 4.243 μs | 11.587 μs | 0.6351 μs | 4.165 μs |  0.87 |    0.12 |    3 |     480 B |        8.57 |
| GetShardConnectionString         | 50         | 6.077 μs |  1.465 μs | 0.0803 μs | 6.121 μs |  1.24 |    0.05 |    4 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         | 9.565 μs | 16.949 μs | 0.9291 μs | 9.083 μs |  1.95 |    0.18 |    5 |     152 B |        2.71 |
