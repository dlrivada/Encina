```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **80,622.1 ns** |  **6,394.40 ns** |   **350.50 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     249.8 ns |     58.02 ns |     3.18 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  79,963.0 ns |  3,850.02 ns |   211.03 ns | 0.992 |    0.00 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  80,399.6 ns |  6,423.24 ns |   352.08 ns | 0.997 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  79,420.1 ns |  2,715.59 ns |   148.85 ns | 0.985 |    0.00 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **110,867.1 ns** | **10,830.25 ns** |   **593.64 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     370.9 ns |    452.14 ns |    24.78 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 115,251.9 ns | 46,089.57 ns | 2,526.32 ns | 1.040 |    0.02 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 112,621.7 ns | 10,572.58 ns |   579.52 ns | 1.016 |    0.01 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 123,054.8 ns | 15,552.97 ns |   852.51 ns | 1.110 |    0.01 |    2 | 1.3428 | 1.2207 |   22736 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **101,306.1 ns** |  **7,082.81 ns** |   **388.23 ns** |  **1.00** |    **0.00** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,132.2 ns |    282.81 ns |    15.50 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 105,182.4 ns |  1,761.54 ns |    96.56 ns |  1.04 |    0.00 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 107,324.3 ns | 32,947.04 ns | 1,805.94 ns |  1.06 |    0.02 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 113,693.7 ns |  2,607.34 ns |   142.92 ns |  1.12 |    0.00 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **474,893.9 ns** | **14,730.34 ns** |   **807.42 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,921.3 ns |    826.24 ns |    45.29 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 477,194.3 ns | 35,836.89 ns | 1,964.34 ns | 1.005 |    0.00 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 475,841.0 ns | 47,000.49 ns | 2,576.26 ns | 1.002 |    0.00 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 682,008.1 ns | 69,756.50 ns | 3,823.59 ns | 1.436 |    0.01 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
