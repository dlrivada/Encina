```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **20,200.2 ns** |   **3,651.65 ns** |   **200.16 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     189.5 ns |     216.76 ns |    11.88 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  20,216.1 ns |   1,767.11 ns |    96.86 ns | 1.001 |    0.01 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  19,891.3 ns |   7,151.38 ns |   391.99 ns | 0.985 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  20,405.3 ns |   2,637.94 ns |   144.59 ns | 1.010 |    0.01 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **42,914.7 ns** |   **7,114.97 ns** |   **390.00 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     243.1 ns |      18.65 ns |     1.02 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  43,234.9 ns |   3,596.40 ns |   197.13 ns | 1.008 |    0.01 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  43,113.7 ns |   6,726.92 ns |   368.73 ns | 1.005 |    0.01 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  49,827.7 ns |   8,579.72 ns |   470.28 ns | 1.161 |    0.01 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **34,730.4 ns** |   **6,436.22 ns** |   **352.79 ns** |  **1.00** |    **0.01** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |     788.6 ns |     155.64 ns |     8.53 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  36,471.7 ns |   1,924.49 ns |   105.49 ns |  1.05 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  37,094.9 ns |   4,359.47 ns |   238.96 ns |  1.07 |    0.01 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  44,858.1 ns |  14,522.12 ns |   796.01 ns |  1.29 |    0.02 |    3 | 1.1597 | 1.0986 |   19744 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **316,161.6 ns** | **121,821.36 ns** | **6,677.44 ns** | **1.000** |    **0.03** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,205.4 ns |      44.89 ns |     2.46 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 309,186.2 ns |  20,555.10 ns | 1,126.69 ns | 0.978 |    0.02 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 307,896.6 ns |  34,838.90 ns | 1,909.64 ns | 0.974 |    0.02 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 465,123.2 ns |  38,737.97 ns | 2,123.36 ns | 1.472 |    0.03 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
