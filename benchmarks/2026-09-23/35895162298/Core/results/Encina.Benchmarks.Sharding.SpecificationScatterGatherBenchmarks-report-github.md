```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **79,204.7 ns** |  **9,305.41 ns** |   **510.06 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     258.3 ns |     15.35 ns |     0.84 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  80,559.2 ns |  6,763.34 ns |   370.72 ns | 1.017 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  79,542.1 ns | 20,079.91 ns | 1,100.65 ns | 1.004 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  83,245.6 ns | 73,883.18 ns | 4,049.79 ns | 1.051 |    0.04 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **112,437.4 ns** |  **4,874.28 ns** |   **267.18 ns** | **1.000** |    **0.00** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     374.4 ns |    121.13 ns |     6.64 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 114,129.9 ns |  7,736.33 ns |   424.05 ns | 1.015 |    0.00 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 116,306.0 ns | 37,477.35 ns | 2,054.26 ns | 1.034 |    0.02 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 123,028.8 ns |  8,322.76 ns |   456.20 ns | 1.094 |    0.00 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **103,105.4 ns** |  **9,576.90 ns** |   **524.94 ns** |  **1.00** |    **0.01** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,164.2 ns |    684.19 ns |    37.50 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 104,407.9 ns |  8,313.89 ns |   455.71 ns |  1.01 |    0.01 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 105,285.2 ns |  5,252.78 ns |   287.92 ns |  1.02 |    0.01 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 114,899.1 ns | 55,706.84 ns | 3,053.48 ns |  1.11 |    0.03 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **478,379.0 ns** | **19,002.52 ns** | **1,041.59 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,953.7 ns |    877.76 ns |    48.11 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 476,942.7 ns | 22,841.73 ns | 1,252.03 ns | 0.997 |    0.00 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 481,381.3 ns | 24,571.99 ns | 1,346.87 ns | 1.006 |    0.00 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 682,304.7 ns | 52,392.71 ns | 2,871.82 ns | 1.426 |    0.01 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
