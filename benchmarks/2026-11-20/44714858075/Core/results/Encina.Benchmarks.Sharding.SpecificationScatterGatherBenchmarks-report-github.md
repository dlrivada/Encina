```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **80,169.5 ns** |  **9,403.60 ns** |   **515.44 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     248.6 ns |     36.53 ns |     2.00 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  81,315.3 ns |  4,897.44 ns |   268.45 ns | 1.014 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  79,967.6 ns |  4,347.74 ns |   238.31 ns | 0.998 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  80,274.5 ns |  3,704.84 ns |   203.07 ns | 1.001 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **110,988.6 ns** |  **5,140.88 ns** |   **281.79 ns** | **1.000** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     344.5 ns |    149.45 ns |     8.19 ns | 0.003 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 114,270.0 ns |  6,308.18 ns |   345.77 ns | 1.030 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 113,498.9 ns | 10,073.45 ns |   552.16 ns | 1.023 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 122,994.6 ns | 10,301.51 ns |   564.66 ns | 1.108 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **102,074.3 ns** |  **6,156.17 ns** |   **337.44 ns** |  **1.00** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,145.1 ns |    174.54 ns |     9.57 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 104,596.3 ns |    364.95 ns |    20.00 ns |  1.02 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 105,399.7 ns |  1,265.75 ns |    69.38 ns |  1.03 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 111,996.8 ns |  9,405.94 ns |   515.57 ns |  1.10 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **476,197.1 ns** | **23,367.74 ns** | **1,280.86 ns** | **1.000** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,938.9 ns |    389.75 ns |    21.36 ns | 0.004 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 479,925.5 ns | 37,032.72 ns | 2,029.89 ns | 1.008 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 474,999.7 ns | 23,189.27 ns | 1,271.08 ns | 0.997 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 686,826.3 ns | 30,853.74 ns | 1,691.20 ns | 1.442 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
