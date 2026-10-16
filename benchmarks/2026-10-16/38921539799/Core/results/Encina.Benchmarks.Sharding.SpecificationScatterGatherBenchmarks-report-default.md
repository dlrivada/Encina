
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **19,751.6 ns** |  **4,399.69 ns** |   **241.16 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     172.5 ns |     23.20 ns |     1.27 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  19,590.4 ns |  1,190.26 ns |    65.24 ns | 0.992 |    0.01 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  19,588.4 ns |  2,311.85 ns |   126.72 ns | 0.992 |    0.01 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  19,934.3 ns |  4,522.28 ns |   247.88 ns | 1.009 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **42,563.2 ns** |  **4,395.57 ns** |   **240.94 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     240.4 ns |     18.16 ns |     1.00 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  42,716.5 ns |    697.58 ns |    38.24 ns | 1.004 |    0.00 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  43,256.1 ns |  6,791.43 ns |   372.26 ns | 1.016 |    0.01 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  50,797.1 ns |  6,075.95 ns |   333.04 ns | 1.193 |    0.01 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **36,098.4 ns** |  **1,547.38 ns** |    **84.82 ns** |  **1.00** |    **0.00** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     794.7 ns |     67.07 ns |     3.68 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  36,120.7 ns |  6,082.38 ns |   333.40 ns |  1.00 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  36,440.0 ns |  2,514.91 ns |   137.85 ns |  1.01 |    0.00 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  45,251.4 ns | 31,242.45 ns | 1,712.50 ns |  1.25 |    0.04 |    3 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **304,321.5 ns** |  **8,113.15 ns** |   **444.71 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,297.0 ns |    321.39 ns |    17.62 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 283,484.9 ns | 33,030.46 ns | 1,810.51 ns | 0.932 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 305,429.2 ns | 33,352.53 ns | 1,828.16 ns | 1.004 |    0.01 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 463,081.0 ns | 13,166.30 ns |   721.69 ns | 1.522 |    0.00 |    3 | 8.7891 | 4.3945 |  154735 B |        1.00 |
