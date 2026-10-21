```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **113,901.0 ns** |  **10,922.23 ns** |    **598.68 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     325.1 ns |       7.68 ns |      0.42 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 115,879.4 ns |  11,627.02 ns |    637.32 ns | 1.017 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 113,973.0 ns |  10,404.38 ns |    570.30 ns | 1.001 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 117,674.3 ns |   7,572.69 ns |    415.08 ns | 1.033 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **154,901.5 ns** |  **37,057.73 ns** |  **2,031.26 ns** | **1.000** |    **0.02** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     457.1 ns |     213.29 ns |     11.69 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 154,154.5 ns |   6,707.66 ns |    367.67 ns | 0.995 |    0.01 |    2 | 1.2207 | 0.9766 |   23048 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 158,746.6 ns |  54,074.11 ns |  2,963.98 ns | 1.025 |    0.02 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 168,624.8 ns |  20,995.86 ns |  1,150.85 ns | 1.089 |    0.01 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **141,043.3 ns** |  **14,410.33 ns** |    **789.88 ns** |  **1.00** |    **0.01** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,415.3 ns |     343.27 ns |     18.82 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 142,258.7 ns |  17,074.63 ns |    935.92 ns |  1.01 |    0.01 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 145,074.1 ns |  13,360.59 ns |    732.34 ns |  1.03 |    0.01 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 155,740.5 ns |  12,135.63 ns |    665.19 ns |  1.10 |    0.01 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **587,176.2 ns** |  **21,002.15 ns** |  **1,151.20 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,400.2 ns |     299.47 ns |     16.41 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 588,690.8 ns |  23,246.92 ns |  1,274.24 ns | 1.003 |    0.00 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 595,818.2 ns | 255,171.62 ns | 13,986.82 ns | 1.015 |    0.02 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 878,966.2 ns |  25,810.71 ns |  1,414.77 ns | 1.497 |    0.00 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
