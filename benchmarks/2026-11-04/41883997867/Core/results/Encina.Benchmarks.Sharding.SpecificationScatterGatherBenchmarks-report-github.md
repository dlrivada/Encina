```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.26GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **23,046.9 ns** |  **11,066.73 ns** |   **606.60 ns** | **1.000** |    **0.03** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     188.6 ns |      12.60 ns |     0.69 ns | 0.008 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  23,377.8 ns |  17,928.51 ns |   982.72 ns | 1.015 |    0.04 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  22,890.2 ns |   6,018.75 ns |   329.91 ns | 0.994 |    0.03 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  22,204.3 ns |   4,937.93 ns |   270.66 ns | 0.964 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **46,738.1 ns** |  **29,434.94 ns** | **1,613.43 ns** | **1.001** |    **0.04** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     320.5 ns |     243.52 ns |    13.35 ns | 0.007 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  48,081.3 ns |   5,493.58 ns |   301.12 ns | 1.030 |    0.03 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  47,076.0 ns |   3,647.80 ns |   199.95 ns | 1.008 |    0.03 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  56,472.6 ns |   2,725.54 ns |   149.40 ns | 1.209 |    0.04 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **40,007.3 ns** |   **3,064.31 ns** |   **167.97 ns** |  **1.00** |    **0.01** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |     994.3 ns |     143.15 ns |     7.85 ns |  0.02 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  41,841.7 ns |   3,362.81 ns |   184.33 ns |  1.05 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  40,449.3 ns |   1,203.91 ns |    65.99 ns |  1.01 |    0.00 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  48,975.3 ns |  14,369.19 ns |   787.62 ns |  1.22 |    0.02 |    2 | 1.1597 | 1.0986 |   19744 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **330,023.2 ns** | **172,974.60 ns** | **9,481.32 ns** | **1.001** |    **0.03** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,664.7 ns |     194.76 ns |    10.68 ns | 0.005 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 334,655.5 ns |  78,402.97 ns | 4,297.53 ns | 1.015 |    0.03 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 311,778.6 ns |  71,690.00 ns | 3,929.57 ns | 0.945 |    0.03 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 469,435.8 ns |  44,174.17 ns | 2,421.34 ns | 1.423 |    0.04 |    3 | 8.7891 | 4.3945 |  154735 B |        1.00 |
