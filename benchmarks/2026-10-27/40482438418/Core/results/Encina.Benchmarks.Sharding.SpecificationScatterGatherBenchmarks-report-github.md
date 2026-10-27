```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 3.05GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean           | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |---------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |   **114,685.5 ns** |  **4,275.34 ns** |   **234.35 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |       314.6 ns |     50.78 ns |     2.78 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |   116,163.4 ns | 13,649.76 ns |   748.19 ns | 1.013 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |   114,201.0 ns | 13,303.36 ns |   729.20 ns | 0.996 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |   117,590.6 ns | 18,788.53 ns | 1,029.86 ns | 1.025 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |                |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |   **153,206.6 ns** |  **1,985.35 ns** |   **108.82 ns** | **1.000** |    **0.00** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |       474.0 ns |    142.77 ns |     7.83 ns | 0.003 |    0.00 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |   155,952.9 ns | 17,367.67 ns |   951.98 ns | 1.018 |    0.01 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |   155,115.7 ns |  4,605.15 ns |   252.42 ns | 1.012 |    0.00 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |   169,544.8 ns | 15,695.77 ns |   860.34 ns | 1.107 |    0.00 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |                |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |   **141,806.6 ns** | **22,793.26 ns** | **1,249.38 ns** | **1.000** |    **0.01** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |     1,416.3 ns |    163.36 ns |     8.95 ns | 0.010 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |   144,043.2 ns | 22,070.41 ns | 1,209.75 ns | 1.016 |    0.01 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |   144,152.8 ns |  7,739.24 ns |   424.21 ns | 1.017 |    0.01 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |   159,076.2 ns | 63,200.44 ns | 3,464.23 ns | 1.122 |    0.02 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
|                                                       |            |               |                |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           |   **593,391.3 ns** | **18,186.97 ns** |   **996.89 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |     2,492.1 ns |    939.98 ns |    51.52 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           |   595,588.4 ns | 15,043.78 ns |   824.60 ns | 1.004 |    0.00 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           |   597,379.4 ns | 39,638.44 ns | 2,172.72 ns | 1.007 |    0.00 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 1,716,527.0 ns | 70,732.91 ns | 3,877.11 ns | 2.893 |    0.01 |    3 | 7.8125 | 1.9531 |  154724 B |        1.00 |
