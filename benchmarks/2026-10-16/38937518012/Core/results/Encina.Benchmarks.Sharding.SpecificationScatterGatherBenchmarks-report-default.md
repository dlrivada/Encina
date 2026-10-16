
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **22,488.6 ns** |  **8,653.08 ns** |   **474.30 ns** | **1.000** |    **0.03** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     200.6 ns |    101.81 ns |     5.58 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  23,241.6 ns | 16,885.71 ns |   925.56 ns | 1.034 |    0.04 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  21,413.2 ns |  4,356.70 ns |   238.81 ns | 0.952 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  22,584.5 ns |  3,481.16 ns |   190.81 ns | 1.005 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **46,561.4 ns** |  **4,394.38 ns** |   **240.87 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     286.5 ns |     99.81 ns |     5.47 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  47,249.9 ns | 25,651.55 ns | 1,406.05 ns | 1.015 |    0.03 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  47,081.0 ns | 27,005.57 ns | 1,480.27 ns | 1.011 |    0.03 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  55,109.1 ns | 25,007.10 ns | 1,370.72 ns | 1.184 |    0.03 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **38,522.1 ns** |  **1,921.21 ns** |   **105.31 ns** |  **1.00** |    **0.00** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     880.5 ns |    670.29 ns |    36.74 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  40,048.3 ns |  5,541.76 ns |   303.76 ns |  1.04 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  41,262.4 ns | 41,539.39 ns | 2,276.91 ns |  1.07 |    0.05 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  46,892.1 ns |  2,627.95 ns |   144.05 ns |  1.22 |    0.00 |    2 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **330,383.3 ns** | **55,968.14 ns** | **3,067.80 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,301.5 ns |    160.32 ns |     8.79 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 328,932.5 ns | 41,778.08 ns | 2,290.00 ns | 0.996 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 322,491.7 ns | 52,115.57 ns | 2,856.63 ns | 0.976 |    0.01 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 496,544.5 ns | 58,471.65 ns | 3,205.03 ns | 1.503 |    0.01 |    3 | 8.7891 | 4.3945 |  154735 B |        1.00 |
