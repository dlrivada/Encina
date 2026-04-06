```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **115,551.4 ns** |   **3,524.87 ns** |   **193.21 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     333.6 ns |      14.16 ns |     0.78 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 119,306.1 ns |   9,123.83 ns |   500.11 ns | 1.032 |    0.00 |    2 | 0.3662 | 0.2441 |    6847 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 117,172.9 ns |   8,426.40 ns |   461.88 ns | 1.014 |    0.00 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 116,776.8 ns |   5,727.05 ns |   313.92 ns | 1.011 |    0.00 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **155,079.5 ns** |  **12,735.59 ns** |   **698.08 ns** | **1.000** |    **0.01** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     478.6 ns |     180.27 ns |     9.88 ns | 0.003 |    0.00 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 155,187.7 ns |  22,595.82 ns | 1,238.55 ns | 1.001 |    0.01 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 157,947.7 ns |  33,137.39 ns | 1,816.37 ns | 1.019 |    0.01 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 168,435.4 ns |   7,694.73 ns |   421.77 ns | 1.086 |    0.00 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **10**            | **123,111.1 ns** |   **6,101.67 ns** |   **334.45 ns** | **1.000** |    **0.00** |    **2** | **0.4883** | **0.2441** |   **10730 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 10            |     695.8 ns |      13.76 ns |     0.75 ns | 0.006 |    0.00 |    1 | 0.0553 |      - |     936 B |        0.09 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 10            | 127,289.6 ns |  11,252.04 ns |   616.76 ns | 1.034 |    0.00 |    2 | 0.4883 | 0.2441 |   11042 B |        1.03 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 10            | 125,337.0 ns |   3,416.30 ns |   187.26 ns | 1.018 |    0.00 |    2 | 0.4883 | 0.2441 |   10730 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 10            | 127,970.2 ns |   5,116.64 ns |   280.46 ns | 1.039 |    0.00 |    2 | 0.4883 | 0.2441 |   10730 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **10**         | **100**           | **269,141.8 ns** |   **7,224.14 ns** |   **395.98 ns** | **1.000** |    **0.00** |    **2** | **3.4180** | **2.9297** |   **64733 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 10         | 100           |   1,231.4 ns |     376.23 ns |    20.62 ns | 0.005 |    0.00 |    1 | 0.4845 | 0.0134 |    8136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 10         | 100           | 272,891.5 ns |  87,940.68 ns | 4,820.33 ns | 1.014 |    0.02 |    2 | 3.4180 | 2.9297 |   65045 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 10         | 100           | 272,458.5 ns |  35,105.11 ns | 1,924.23 ns | 1.012 |    0.01 |    2 | 3.9063 | 3.4180 |   65688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 10         | 100           | 368,080.1 ns |  12,777.00 ns |   700.35 ns | 1.368 |    0.00 |    3 | 3.4180 | 2.9297 |   64733 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **145,081.8 ns** |  **25,527.63 ns** | **1,399.26 ns** | **1.000** |    **0.01** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,423.3 ns |      41.98 ns |     2.30 ns | 0.010 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 147,767.1 ns |  10,456.75 ns |   573.17 ns | 1.019 |    0.01 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 147,189.8 ns |  15,932.13 ns |   873.29 ns | 1.015 |    0.01 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 160,519.5 ns |  21,251.82 ns | 1,164.88 ns | 1.106 |    0.01 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **595,117.4 ns** |  **21,494.69 ns** | **1,178.20 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,713.5 ns |     961.39 ns |    52.70 ns | 0.005 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 598,491.7 ns |  58,615.68 ns | 3,212.92 ns | 1.006 |    0.00 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 600,480.1 ns |  44,967.23 ns | 2,464.81 ns | 1.009 |    0.00 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 891,863.3 ns | 142,740.72 ns | 7,824.10 ns | 1.499 |    0.01 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
