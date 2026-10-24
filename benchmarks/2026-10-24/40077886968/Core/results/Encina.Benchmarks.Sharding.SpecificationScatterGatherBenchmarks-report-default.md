
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **23,788.3 ns** |  **15,068.55 ns** |    **825.96 ns** | **1.001** |    **0.04** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     200.7 ns |     154.62 ns |      8.48 ns | 0.008 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  23,875.2 ns |   6,811.05 ns |    373.34 ns | 1.004 |    0.03 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  24,350.4 ns |  43,766.50 ns |  2,398.99 ns | 1.024 |    0.09 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  23,359.0 ns |   1,162.29 ns |     63.71 ns | 0.983 |    0.03 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **47,776.0 ns** |   **4,877.86 ns** |    **267.37 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     267.3 ns |     137.20 ns |      7.52 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  51,135.7 ns |  38,876.94 ns |  2,130.98 ns | 1.070 |    0.04 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  49,417.7 ns |   2,460.14 ns |    134.85 ns | 1.034 |    0.01 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  54,939.8 ns |  11,098.55 ns |    608.35 ns | 1.150 |    0.01 |    2 | 1.3428 | 1.2207 |   22736 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **40,656.1 ns** |   **1,850.51 ns** |    **101.43 ns** |  **1.00** |    **0.00** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     949.8 ns |      67.79 ns |      3.72 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  40,158.7 ns |   4,132.47 ns |    226.51 ns |  0.99 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  41,843.8 ns |   4,899.69 ns |    268.57 ns |  1.03 |    0.01 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  50,210.1 ns |   1,995.64 ns |    109.39 ns |  1.24 |    0.00 |    3 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **317,895.4 ns** |  **38,021.92 ns** |  **2,084.11 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,428.9 ns |     768.00 ns |     42.10 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 343,182.5 ns | 207,862.05 ns | 11,393.62 ns | 1.080 |    0.03 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 345,134.4 ns | 213,523.89 ns | 11,703.97 ns | 1.086 |    0.03 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 510,486.7 ns |  58,776.06 ns |  3,221.71 ns | 1.606 |    0.01 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
