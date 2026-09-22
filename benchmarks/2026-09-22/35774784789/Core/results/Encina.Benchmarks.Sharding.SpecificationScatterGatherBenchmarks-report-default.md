
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.28GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **23,094.1 ns** |  **14,549.96 ns** |   **797.53 ns** | **1.001** |    **0.04** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     185.5 ns |      68.62 ns |     3.76 ns | 0.008 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  21,166.1 ns |   3,184.31 ns |   174.54 ns | 0.917 |    0.03 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  21,351.6 ns |   9,752.40 ns |   534.56 ns | 0.925 |    0.03 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  21,068.4 ns |  10,826.14 ns |   593.42 ns | 0.913 |    0.04 |    2 | 0.3662 | 0.3052 |    6542 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **46,290.6 ns** |  **45,271.25 ns** | **2,481.47 ns** | **1.002** |    **0.07** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     288.7 ns |     153.48 ns |     8.41 ns | 0.006 |    0.00 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  46,966.6 ns |  14,992.81 ns |   821.81 ns | 1.017 |    0.05 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  47,782.0 ns |   6,127.42 ns |   335.86 ns | 1.034 |    0.05 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  56,914.2 ns |   4,835.70 ns |   265.06 ns | 1.232 |    0.06 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **39,401.6 ns** |   **4,134.07 ns** |   **226.60 ns** |  **1.00** |    **0.01** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     858.1 ns |     174.20 ns |     9.55 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  40,459.8 ns |   7,446.84 ns |   408.19 ns |  1.03 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  39,816.1 ns |  25,638.14 ns | 1,405.31 ns |  1.01 |    0.03 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  46,509.1 ns |  18,714.36 ns | 1,025.80 ns |  1.18 |    0.02 |    2 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **309,072.1 ns** |  **23,661.09 ns** | **1,296.94 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,201.8 ns |      27.38 ns |     1.50 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 307,453.5 ns |  90,575.43 ns | 4,964.74 ns | 0.995 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 309,548.8 ns |  20,829.26 ns | 1,141.72 ns | 1.002 |    0.00 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 475,556.8 ns | 145,612.80 ns | 7,981.53 ns | 1.539 |    0.02 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
