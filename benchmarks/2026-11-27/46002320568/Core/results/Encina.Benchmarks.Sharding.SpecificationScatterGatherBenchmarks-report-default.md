
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.19GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **31,940.5 ns** |  **51,173.38 ns** |  **2,804.99 ns** | **1.005** |    **0.11** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     274.0 ns |     104.80 ns |      5.74 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  33,374.3 ns |  12,570.21 ns |    689.02 ns | 1.050 |    0.08 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  31,337.5 ns |  48,885.70 ns |  2,679.59 ns | 0.986 |    0.10 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  33,161.6 ns |  43,407.31 ns |  2,379.30 ns | 1.043 |    0.10 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **66,590.5 ns** |   **4,746.33 ns** |    **260.16 ns** | **1.000** |    **0.00** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     529.2 ns |      65.94 ns |      3.61 ns | 0.008 |    0.00 |    1 | 0.0296 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  67,521.5 ns |   6,873.99 ns |    376.79 ns | 1.014 |    0.01 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  67,884.8 ns |  19,052.61 ns |  1,044.34 ns | 1.019 |    0.01 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  79,599.3 ns |   1,433.50 ns |     78.58 ns | 1.195 |    0.00 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **58,209.4 ns** |  **67,670.24 ns** |  **3,709.23 ns** |  **1.00** |    **0.08** |    **2** | **0.1831** | **0.1221** |   **19739 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,347.3 ns |     944.30 ns |     51.76 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  61,805.0 ns |  72,549.86 ns |  3,976.70 ns |  1.06 |    0.08 |    2 | 0.1831 | 0.1221 |   20050 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  61,062.6 ns |  86,203.49 ns |  4,725.10 ns |  1.05 |    0.09 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  67,615.1 ns |   5,831.81 ns |    319.66 ns |  1.16 |    0.06 |    2 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **441,588.6 ns** |  **14,095.03 ns** |    **772.60 ns** | **1.000** |    **0.00** |    **2** | **1.4648** | **0.9766** |  **154731 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,522.2 ns |   1,100.32 ns |     60.31 ns | 0.006 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 457,775.9 ns | 276,867.26 ns | 15,176.03 ns | 1.037 |    0.03 |    2 | 1.4648 | 0.9766 |  155043 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 448,825.1 ns | 236,884.10 ns | 12,984.42 ns | 1.016 |    0.03 |    2 | 1.4648 | 0.9766 |  155683 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 777,187.9 ns | 556,761.19 ns | 30,517.96 ns | 1.760 |    0.06 |    3 | 0.9766 |      - |  154717 B |        1.00 |
