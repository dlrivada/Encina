
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **29,968.8 ns** |    **807.92 ns** |    **44.28 ns** | **1.000** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     274.1 ns |     26.92 ns |     1.48 ns | 0.009 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  30,323.1 ns |  1,612.70 ns |    88.40 ns | 1.012 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  29,860.2 ns |    916.19 ns |    50.22 ns | 0.996 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  32,694.0 ns |  7,079.62 ns |   388.06 ns | 1.091 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **69,167.3 ns** |  **6,428.07 ns** |   **352.34 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     466.9 ns |    157.12 ns |     8.61 ns | 0.007 |    1 | 0.0300 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  70,725.0 ns |  7,167.79 ns |   392.89 ns | 1.023 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  69,551.8 ns |  8,062.14 ns |   441.91 ns | 1.006 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  83,138.8 ns |  6,539.47 ns |   358.45 ns | 1.202 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **58,488.1 ns** |  **7,826.15 ns** |   **428.98 ns** |  **1.00** |    **2** | **0.1831** | **0.1221** |   **19739 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,323.9 ns |    282.62 ns |    15.49 ns |  0.02 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  58,087.0 ns |  6,907.79 ns |   378.64 ns |  0.99 |    2 | 0.1831 | 0.1221 |   20050 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  60,215.4 ns |  2,616.04 ns |   143.39 ns |  1.03 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  70,224.0 ns |  2,973.16 ns |   162.97 ns |  1.20 |    2 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **472,323.9 ns** | **20,561.28 ns** | **1,127.03 ns** | **1.000** |    **2** | **1.4648** | **0.9766** |  **154731 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,203.2 ns |    476.35 ns |    26.11 ns | 0.005 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 477,012.5 ns | 23,862.53 ns | 1,307.99 ns | 1.010 |    2 | 1.4648 | 0.9766 |  155043 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 478,513.7 ns | 12,739.71 ns |   698.31 ns | 1.013 |    2 | 1.4648 | 0.9766 |  155683 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 720,261.4 ns | 13,857.57 ns |   759.58 ns | 1.525 |    3 | 0.9766 |      - |  154717 B |        1.00 |
