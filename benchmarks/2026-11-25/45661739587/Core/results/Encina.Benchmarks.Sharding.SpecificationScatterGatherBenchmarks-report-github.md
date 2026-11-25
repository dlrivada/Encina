```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **32,015.0 ns** |   **1,802.19 ns** |    **98.78 ns** | **1.000** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     287.7 ns |      11.65 ns |     0.64 ns | 0.009 |    1 | 0.0043 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  31,981.3 ns |   1,905.17 ns |   104.43 ns | 0.999 |    2 | 0.0610 |      - |    6850 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  30,329.1 ns |   1,204.63 ns |    66.03 ns | 0.947 |    2 | 0.0610 |      - |    6539 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  32,261.0 ns |   1,103.25 ns |    60.47 ns | 1.008 |    2 | 0.0610 |      - |    6539 B |        1.00 |
|                                                       |            |               |              |               |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **69,505.9 ns** |  **13,596.55 ns** |   **745.27 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     480.4 ns |     474.94 ns |    26.03 ns | 0.007 |    1 | 0.0300 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  69,527.5 ns |   9,365.29 ns |   513.34 ns | 1.000 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  70,689.8 ns |  12,576.61 ns |   689.37 ns | 1.017 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  83,436.4 ns |   4,636.38 ns |   254.14 ns | 1.201 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
|                                                       |            |               |              |               |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **59,340.5 ns** |  **10,030.28 ns** |   **549.79 ns** |  **1.00** |    **2** | **0.1831** | **0.1221** |   **19739 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,495.8 ns |      88.35 ns |     4.84 ns |  0.03 |    1 | 0.0248 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  58,968.6 ns |   3,188.31 ns |   174.76 ns |  0.99 |    2 | 0.1831 | 0.1221 |   20050 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  60,341.0 ns |   6,531.57 ns |   358.02 ns |  1.02 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  69,368.7 ns |   7,862.40 ns |   430.96 ns |  1.17 |    2 | 0.1221 |      - |   19724 B |        1.00 |
|                                                       |            |               |              |               |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **472,000.8 ns** |  **25,417.19 ns** | **1,393.20 ns** | **1.000** |    **2** | **1.4648** | **0.9766** |  **154731 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,304.2 ns |     577.55 ns |    31.66 ns | 0.005 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 473,664.5 ns |   4,156.61 ns |   227.84 ns | 1.004 |    2 | 1.4648 | 0.9766 |  155043 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 479,297.9 ns |  17,062.22 ns |   935.24 ns | 1.015 |    2 | 1.4648 | 0.9766 |  155683 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 723,160.1 ns | 107,136.69 ns | 5,872.52 ns | 1.532 |    3 | 0.9766 |      - |  154717 B |        1.00 |
