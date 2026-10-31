```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.51GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **20,044.1 ns** |   **4,802.30 ns** |    **263.23 ns** | **1.000** |    **0.02** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     180.3 ns |     134.66 ns |      7.38 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  20,593.9 ns |   4,448.64 ns |    243.84 ns | 1.028 |    0.02 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  20,620.7 ns |   6,098.58 ns |    334.28 ns | 1.029 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  20,586.9 ns |  15,633.94 ns |    856.95 ns | 1.027 |    0.04 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **44,632.8 ns** |  **21,630.84 ns** |  **1,185.66 ns** | **1.000** |    **0.03** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     244.1 ns |      58.43 ns |      3.20 ns | 0.005 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  43,918.8 ns |   7,681.22 ns |    421.03 ns | 0.984 |    0.02 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  43,865.8 ns |  19,383.16 ns |  1,062.46 ns | 0.983 |    0.03 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  52,657.1 ns |  21,449.30 ns |  1,175.71 ns | 1.180 |    0.04 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **36,248.4 ns** |   **1,660.10 ns** |     **91.00 ns** |  **1.00** |    **0.00** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |     798.8 ns |     202.86 ns |     11.12 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  36,116.8 ns |   7,531.76 ns |    412.84 ns |  1.00 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  36,203.6 ns |  10,398.34 ns |    569.97 ns |  1.00 |    0.01 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  44,859.4 ns |  21,757.81 ns |  1,192.62 ns |  1.24 |    0.03 |    3 | 1.1597 | 1.0986 |   19744 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **319,919.0 ns** | **100,482.27 ns** |  **5,507.77 ns** | **1.000** |    **0.02** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,258.1 ns |      87.54 ns |      4.80 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 287,855.7 ns |  13,944.01 ns |    764.32 ns | 0.900 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 314,901.2 ns | 250,736.35 ns | 13,743.71 ns | 0.985 |    0.04 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 467,658.7 ns |  48,222.16 ns |  2,643.22 ns | 1.462 |    0.02 |    3 | 8.7891 | 4.3945 |  154735 B |        1.00 |
