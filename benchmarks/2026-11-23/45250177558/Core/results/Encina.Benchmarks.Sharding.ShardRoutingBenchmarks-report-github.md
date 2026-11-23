```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  InvocationCount=1  IterationCount=3  
LaunchCount=1  UnrollFactor=1  WarmupCount=3  

```
| Method                           | ShardCount | Mean      | Error      | StdDev    | Ratio | RatioSD | Rank | Allocated | Alloc Ratio |
|--------------------------------- |----------- |----------:|-----------:|----------:|------:|--------:|-----:|----------:|------------:|
| **&#39;Hash routing&#39;**                   | **3**          |  **3.884 μs** |  **6.0675 μs** | **0.3326 μs** |  **1.00** |    **0.10** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 3          |  2.162 μs |  0.7321 μs | 0.0401 μs |  0.56 |    0.04 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 3          |  3.660 μs |  5.6154 μs | 0.3078 μs |  0.95 |    0.10 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 3          |  5.288 μs | 15.2717 μs | 0.8371 μs |  1.37 |    0.21 |    3 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 3          |  8.039 μs | 21.0120 μs | 1.1517 μs |  2.08 |    0.30 |    5 |     416 B |        7.43 |
| GetAllShardIds                   | 3          |  3.816 μs |  8.8575 μs | 0.4855 μs |  0.99 |    0.13 |    2 |     104 B |        1.86 |
| GetShardConnectionString         | 3          |  5.219 μs | 11.0867 μs | 0.6077 μs |  1.35 |    0.17 |    3 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 3          |  6.447 μs | 26.2700 μs | 1.4399 μs |  1.67 |    0.34 |    4 |     152 B |        2.71 |
|                                  |            |           |            |           |       |         |      |           |             |
| **&#39;Hash routing&#39;**                   | **50**         |  **4.118 μs** | **10.1937 μs** | **0.5588 μs** |  **1.01** |    **0.16** |    **2** |      **56 B** |        **1.00** |
| &#39;Range routing&#39;                  | 50         |  3.380 μs | 11.4740 μs | 0.6289 μs |  0.83 |    0.16 |    1 |      48 B |        0.86 |
| &#39;Directory routing&#39;              | 50         |  4.013 μs |  0.9122 μs | 0.0500 μs |  0.99 |    0.11 |    2 |      24 B |        0.43 |
| &#39;Geo routing&#39;                    | 50         |  5.274 μs |  9.2146 μs | 0.5051 μs |  1.30 |    0.18 |    2 |      96 B |        1.71 |
| &#39;Hash routing (miss → re-route)&#39; | 50         | 10.764 μs | 19.3286 μs | 1.0595 μs |  2.64 |    0.37 |    4 |     416 B |        7.43 |
| GetAllShardIds                   | 50         |  4.553 μs | 16.4680 μs | 0.9027 μs |  1.12 |    0.23 |    2 |     480 B |        8.57 |
| GetShardConnectionString         | 50         |  5.325 μs | 14.3453 μs | 0.7863 μs |  1.31 |    0.22 |    2 |      64 B |        1.14 |
| &#39;Directory add + lookup&#39;         | 50         |  7.247 μs | 12.4514 μs | 0.6825 μs |  1.78 |    0.25 |    3 |     152 B |        2.71 |
