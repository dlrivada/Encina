
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.53GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **19,987.7 ns** |  **1,200.49 ns** |    **65.80 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     172.6 ns |      9.56 ns |     0.52 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  20,004.0 ns |  2,125.85 ns |   116.52 ns | 1.001 |    0.01 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  19,547.1 ns |    835.60 ns |    45.80 ns | 0.978 |    0.00 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  19,845.8 ns |    980.98 ns |    53.77 ns | 0.993 |    0.00 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **40,405.2 ns** |  **4,120.05 ns** |   **225.83 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     253.2 ns |     14.74 ns |     0.81 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  40,757.0 ns |  3,039.80 ns |   166.62 ns | 1.009 |    0.01 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  42,835.4 ns |  4,856.53 ns |   266.20 ns | 1.060 |    0.01 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  49,767.5 ns |  4,091.58 ns |   224.27 ns | 1.232 |    0.01 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **35,237.6 ns** |    **981.94 ns** |    **53.82 ns** |  **1.00** |    **0.00** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     788.3 ns |    123.21 ns |     6.75 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  36,016.2 ns |  2,026.12 ns |   111.06 ns |  1.02 |    0.00 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  37,269.3 ns | 37,769.60 ns | 2,070.28 ns |  1.06 |    0.05 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  44,256.7 ns |  7,564.40 ns |   414.63 ns |  1.26 |    0.01 |    3 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **303,174.4 ns** | **46,473.59 ns** | **2,547.37 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,304.3 ns |    137.90 ns |     7.56 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 299,235.4 ns |  9,501.68 ns |   520.82 ns | 0.987 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 274,735.6 ns | 12,613.29 ns |   691.38 ns | 0.906 |    0.01 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 457,897.2 ns | 19,501.09 ns | 1,068.92 ns | 1.510 |    0.01 |    3 | 8.7891 | 4.3945 |  154735 B |        1.00 |
