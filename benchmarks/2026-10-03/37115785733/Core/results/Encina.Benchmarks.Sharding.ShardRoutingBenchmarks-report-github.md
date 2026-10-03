```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          |  **4.358 μs** |  **5.165 μs** | **0.2831 μs** |  **1.00** |    **0.08** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |  2.359 μs |  1.448 μs | 0.0794 μs |  0.54 |    0.03 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |  2.642 μs |  2.930 μs | 0.1606 μs |  0.61 |    0.05 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |  4.505 μs |  9.542 μs | 0.5230 μs |  1.04 |    0.12 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          |  7.856 μs | 10.329 μs | 0.5662 μs |  1.81 |    0.15 |    3 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |  3.515 μs | 16.568 μs | 0.9082 μs |  0.81 |    0.19 |    1 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |  4.462 μs |  4.111 μs | 0.2253 μs |  1.03 |    0.07 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          |  7.412 μs |  7.772 μs | 0.4260 μs |  1.71 |    0.13 |    3 |     152 B |        2.71 |
|                                  |            |           |           |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |  **4.322 μs** |  **3.523 μs** | **0.1931 μs** |  **1.00** |    **0.05** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |  3.341 μs |  4.251 μs | 0.2330 μs |  0.77 |    0.06 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         |  3.229 μs |  6.131 μs | 0.3360 μs |  0.75 |    0.07 |    1 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         |  4.642 μs | 18.554 μs | 1.0170 μs |  1.08 |    0.21 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 10.773 μs |  9.141 μs | 0.5011 μs |  2.50 |    0.14 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |  4.091 μs |  7.527 μs | 0.4126 μs |  0.95 |    0.09 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         |  5.139 μs |  9.170 μs | 0.5027 μs |  1.19 |    0.11 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         |  7.066 μs |  7.136 μs | 0.3911 μs |  1.64 |    0.10 |    3 |     152 B |        2.71 |
