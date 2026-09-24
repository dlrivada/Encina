
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **38,584.0 ns** |  **1,858.83 ns** |   **101.89 ns** | **1.000** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     344.1 ns |     32.14 ns |     1.76 ns | 0.009 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  39,487.9 ns |    355.06 ns |    19.46 ns | 1.023 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  39,030.5 ns |  1,544.84 ns |    84.68 ns | 1.012 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  41,065.3 ns |  4,686.24 ns |   256.87 ns | 1.064 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **89,458.8 ns** | **13,430.10 ns** |   **736.15 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     495.3 ns |     82.06 ns |     4.50 ns | 0.006 |    1 | 0.0296 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  89,997.4 ns |  6,066.99 ns |   332.55 ns | 1.006 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  89,774.0 ns |  4,240.18 ns |   232.42 ns | 1.004 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 108,403.8 ns |    302.12 ns |    16.56 ns | 1.212 |    3 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **72,537.3 ns** |  **6,082.72 ns** |   **333.41 ns** |  **1.00** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,544.6 ns |    121.13 ns |     6.64 ns |  0.02 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  73,639.3 ns |  2,833.06 ns |   155.29 ns |  1.02 |    2 | 0.1221 |      - |   20036 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  77,181.9 ns |  3,076.27 ns |   168.62 ns |  1.06 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  91,892.4 ns |  3,521.36 ns |   193.02 ns |  1.27 |    2 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **620,911.2 ns** | **60,960.93 ns** | **3,341.47 ns** | **1.000** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,445.7 ns |    159.54 ns |     8.74 ns | 0.004 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 622,003.1 ns |  2,372.87 ns |   130.06 ns | 1.002 |    2 | 0.9766 |      - |  155029 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 623,729.3 ns |  9,857.09 ns |   540.30 ns | 1.005 |    2 | 0.9766 |      - |  155669 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 936,313.3 ns | 64,220.60 ns | 3,520.15 ns | 1.508 |    3 |      - |      - |  154704 B |        1.00 |
