```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **115,117.5 ns** |  **3,640.67 ns** |   **199.56 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     318.3 ns |      5.70 ns |     0.31 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 117,469.1 ns |  7,839.14 ns |   429.69 ns | 1.020 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 115,950.3 ns |    352.10 ns |    19.30 ns | 1.007 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 114,901.6 ns |  7,283.73 ns |   399.25 ns | 0.998 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **151,756.8 ns** | **11,246.68 ns** |   **616.47 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     458.4 ns |    108.07 ns |     5.92 ns | 0.003 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 155,169.3 ns |  6,724.39 ns |   368.59 ns | 1.022 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 153,987.8 ns |  1,728.88 ns |    94.77 ns | 1.015 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 167,952.8 ns | 19,059.75 ns | 1,044.73 ns | 1.107 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **142,227.8 ns** |  **8,554.75 ns** |   **468.91 ns** |  **1.00** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,474.8 ns |     48.37 ns |     2.65 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 146,172.6 ns | 23,776.51 ns | 1,303.27 ns |  1.03 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 144,315.3 ns | 16,464.94 ns |   902.50 ns |  1.01 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 157,238.6 ns | 14,316.52 ns |   784.74 ns |  1.11 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **587,365.9 ns** | **25,283.74 ns** | **1,385.89 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,519.2 ns |    552.69 ns |    30.29 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 595,742.4 ns | 86,488.57 ns | 4,740.73 ns | 1.014 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 590,450.4 ns | 19,509.50 ns | 1,069.38 ns | 1.005 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 885,262.2 ns | 28,762.73 ns | 1,576.58 ns | 1.507 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
