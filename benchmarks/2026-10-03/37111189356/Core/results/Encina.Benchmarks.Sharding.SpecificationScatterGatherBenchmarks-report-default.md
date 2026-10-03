
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **35,664.6 ns** |  **5,937.27 ns** |   **325.44 ns** | **1.000** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     346.9 ns |     33.62 ns |     1.84 ns | 0.010 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  38,296.9 ns |  5,150.15 ns |   282.30 ns | 1.074 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  36,737.4 ns |  1,728.11 ns |    94.72 ns | 1.030 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  37,063.2 ns |  3,300.00 ns |   180.88 ns | 1.039 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **83,916.0 ns** |  **2,745.24 ns** |   **150.48 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     492.4 ns |    307.88 ns |    16.88 ns | 0.006 |    1 | 0.0296 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  83,262.2 ns | 17,691.07 ns |   969.71 ns | 0.992 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  83,684.2 ns |  8,338.84 ns |   457.08 ns | 0.997 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  97,997.2 ns |  7,169.25 ns |   392.97 ns | 1.168 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **67,439.3 ns** | **12,241.98 ns** |   **671.02 ns** |  **1.00** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,448.9 ns |     66.47 ns |     3.64 ns |  0.02 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  69,571.0 ns | 12,434.22 ns |   681.56 ns |  1.03 |    2 | 0.1221 |      - |   20036 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  71,567.4 ns | 10,697.57 ns |   586.37 ns |  1.06 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  85,096.4 ns |  5,515.63 ns |   302.33 ns |  1.26 |    2 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **579,838.3 ns** | **26,803.42 ns** | **1,469.19 ns** | **1.000** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,430.6 ns |    807.43 ns |    44.26 ns | 0.004 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 574,493.0 ns | 14,180.15 ns |   777.26 ns | 0.991 |    2 | 0.9766 |      - |  155029 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 582,876.2 ns | 85,290.44 ns | 4,675.06 ns | 1.005 |    2 | 0.9766 |      - |  155669 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 882,215.1 ns | 28,690.28 ns | 1,572.61 ns | 1.521 |    3 | 0.9766 |      - |  154717 B |        1.00 |
