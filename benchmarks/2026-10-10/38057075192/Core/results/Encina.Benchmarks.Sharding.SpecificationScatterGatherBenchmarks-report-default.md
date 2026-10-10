
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **78,854.4 ns** |   **6,047.56 ns** |   **331.49 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     252.5 ns |      54.87 ns |     3.01 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  80,927.5 ns |   4,389.16 ns |   240.58 ns | 1.026 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  77,740.0 ns |   6,617.90 ns |   362.75 ns | 0.986 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  78,712.6 ns |   6,144.58 ns |   336.81 ns | 0.998 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |               |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **110,377.3 ns** |  **11,389.99 ns** |   **624.32 ns** | **1.000** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     351.6 ns |     174.21 ns |     9.55 ns | 0.003 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 113,603.2 ns |     809.33 ns |    44.36 ns | 1.029 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 113,386.5 ns |  17,163.41 ns |   940.78 ns | 1.027 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 123,573.4 ns |  13,345.18 ns |   731.49 ns | 1.120 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |               |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **101,996.7 ns** |  **10,880.14 ns** |   **596.38 ns** |  **1.00** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,175.7 ns |     378.71 ns |    20.76 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 103,503.8 ns |   2,038.94 ns |   111.76 ns |  1.01 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 103,786.3 ns |   9,657.11 ns |   529.34 ns |  1.02 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 113,132.3 ns |  20,616.46 ns | 1,130.06 ns |  1.11 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
                                                       |            |               |              |               |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **477,934.1 ns** |  **34,866.56 ns** | **1,911.15 ns** | **1.000** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,865.8 ns |     562.14 ns |    30.81 ns | 0.004 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 482,019.1 ns |   8,523.73 ns |   467.21 ns | 1.009 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 474,350.0 ns |  18,415.15 ns | 1,009.40 ns | 0.993 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 696,437.0 ns | 109,807.89 ns | 6,018.94 ns | 1.457 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
