
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **20,688.8 ns** |   **7,362.42 ns** |   **403.56 ns** | **1.000** |    **0.02** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     177.1 ns |      82.66 ns |     4.53 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  21,198.5 ns |   4,878.14 ns |   267.39 ns | 1.025 |    0.02 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  20,693.7 ns |   8,951.25 ns |   490.65 ns | 1.000 |    0.03 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  21,856.6 ns |   1,685.18 ns |    92.37 ns | 1.057 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **44,203.0 ns** |   **3,390.59 ns** |   **185.85 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     247.1 ns |       7.23 ns |     0.40 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  44,254.1 ns |   9,898.83 ns |   542.59 ns | 1.001 |    0.01 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  44,425.8 ns |  16,510.74 ns |   905.01 ns | 1.005 |    0.02 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  52,546.2 ns |  28,909.04 ns | 1,584.60 ns | 1.189 |    0.03 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **37,291.0 ns** |  **21,070.15 ns** | **1,154.93 ns** |  **1.00** |    **0.04** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     826.2 ns |     191.75 ns |    10.51 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  36,445.1 ns |   3,551.08 ns |   194.65 ns |  0.98 |    0.03 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  37,801.6 ns |  47,287.63 ns | 2,591.99 ns |  1.01 |    0.07 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  44,854.6 ns |   6,609.20 ns |   362.27 ns |  1.20 |    0.03 |    3 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **308,954.0 ns** | **158,617.12 ns** | **8,694.34 ns** | **1.001** |    **0.03** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,195.3 ns |     178.72 ns |     9.80 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 304,986.3 ns |  25,590.12 ns | 1,402.68 ns | 0.988 |    0.02 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 303,533.4 ns |  21,024.26 ns | 1,152.41 ns | 0.983 |    0.02 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 458,542.7 ns |  25,068.27 ns | 1,374.08 ns | 1.485 |    0.04 |    3 | 8.7891 | 4.3945 |  154735 B |        1.00 |
