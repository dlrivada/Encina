```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **21,235.8 ns** |   **1,346.70 ns** |    **73.82 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     182.5 ns |      57.24 ns |     3.14 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  21,406.6 ns |   6,195.24 ns |   339.58 ns | 1.008 |    0.01 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  20,833.1 ns |   1,272.36 ns |    69.74 ns | 0.981 |    0.00 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  21,090.7 ns |   4,356.31 ns |   238.78 ns | 0.993 |    0.01 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **42,737.8 ns** |   **4,659.77 ns** |   **255.42 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     249.9 ns |      69.03 ns |     3.78 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  43,545.5 ns |   8,777.81 ns |   481.14 ns | 1.019 |    0.01 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  44,514.1 ns |  53,697.45 ns | 2,943.34 ns | 1.042 |    0.06 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  52,921.6 ns |  23,089.93 ns | 1,265.64 ns | 1.238 |    0.03 |    3 | 1.3428 | 1.2817 |   22744 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **38,611.4 ns** |  **27,474.26 ns** | **1,505.96 ns** |  **1.00** |    **0.05** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |     971.3 ns |     300.06 ns |    16.45 ns |  0.03 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  37,384.1 ns |  19,499.23 ns | 1,068.82 ns |  0.97 |    0.04 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  37,236.4 ns |   4,042.40 ns |   221.58 ns |  0.97 |    0.03 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  44,058.1 ns |   2,272.54 ns |   124.57 ns |  1.14 |    0.04 |    2 | 1.1597 | 1.0986 |   19744 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **303,689.1 ns** |  **47,474.48 ns** | **2,602.24 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,201.8 ns |     182.91 ns |    10.03 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 304,220.3 ns |  41,060.37 ns | 2,250.66 ns | 1.002 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 306,933.7 ns |  55,225.80 ns | 3,027.11 ns | 1.011 |    0.01 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 482,261.5 ns | 179,048.35 ns | 9,814.24 ns | 1.588 |    0.03 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
