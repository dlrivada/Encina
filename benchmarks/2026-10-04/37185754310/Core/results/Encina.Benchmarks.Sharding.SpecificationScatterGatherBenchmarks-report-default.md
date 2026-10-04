
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **21,537.0 ns** |   **2,158.75 ns** |    **118.33 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     192.6 ns |     124.03 ns |      6.80 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  22,211.4 ns |   2,988.80 ns |    163.83 ns | 1.031 |    0.01 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  22,775.5 ns |  25,027.28 ns |  1,371.83 ns | 1.058 |    0.06 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  22,200.8 ns |   3,188.50 ns |    174.77 ns | 1.031 |    0.01 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **45,709.7 ns** |   **2,974.29 ns** |    **163.03 ns** | **1.000** |    **0.00** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     258.9 ns |      42.89 ns |      2.35 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  46,689.5 ns |  34,369.44 ns |  1,883.91 ns | 1.021 |    0.04 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  45,878.1 ns |   4,647.57 ns |    254.75 ns | 1.004 |    0.01 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  54,515.3 ns |  21,499.84 ns |  1,178.48 ns | 1.193 |    0.02 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **38,153.5 ns** |  **12,222.84 ns** |    **669.98 ns** |  **1.00** |    **0.02** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     817.2 ns |     230.23 ns |     12.62 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  38,540.2 ns |  11,810.89 ns |    647.39 ns |  1.01 |    0.02 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  38,799.2 ns |   5,469.00 ns |    299.77 ns |  1.02 |    0.02 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  47,319.3 ns |  27,147.13 ns |  1,488.03 ns |  1.24 |    0.04 |    3 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **321,065.1 ns** |  **22,784.75 ns** |  **1,248.91 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,305.3 ns |     950.60 ns |     52.11 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 316,560.2 ns |  73,863.49 ns |  4,048.71 ns | 0.986 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 304,595.4 ns |   7,932.77 ns |    434.82 ns | 0.949 |    0.00 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 497,917.6 ns | 344,340.60 ns | 18,874.47 ns | 1.551 |    0.05 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
