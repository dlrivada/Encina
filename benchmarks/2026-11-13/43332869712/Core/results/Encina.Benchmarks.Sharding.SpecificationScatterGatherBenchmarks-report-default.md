
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.09GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **40,648.9 ns** |   **5,324.95 ns** |    **291.88 ns** | **1.000** |    **0.01** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     364.5 ns |      29.91 ns |      1.64 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  42,500.9 ns |   3,914.59 ns |    214.57 ns | 1.046 |    0.01 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  42,842.3 ns |   2,860.71 ns |    156.81 ns | 1.054 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  42,043.4 ns |   8,677.61 ns |    475.65 ns | 1.034 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **90,729.5 ns** |   **5,033.15 ns** |    **275.88 ns** | **1.000** |    **0.00** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     569.2 ns |      19.24 ns |      1.05 ns | 0.006 |    0.00 |    1 | 0.0296 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  92,592.3 ns |   3,792.59 ns |    207.88 ns | 1.021 |    0.00 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  92,802.9 ns |   4,684.42 ns |    256.77 ns | 1.023 |    0.00 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 110,465.6 ns |   6,293.17 ns |    344.95 ns | 1.218 |    0.00 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **73,848.3 ns** |   **1,463.05 ns** |     **80.19 ns** |  **1.00** |    **0.00** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,617.0 ns |     119.41 ns |      6.55 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  74,848.0 ns |   6,679.92 ns |    366.15 ns |  1.01 |    0.00 |    2 | 0.1221 |      - |   20036 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  77,776.9 ns |   1,737.49 ns |     95.24 ns |  1.05 |    0.00 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  93,339.0 ns |   3,622.38 ns |    198.55 ns |  1.26 |    0.00 |    2 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **633,859.4 ns** |  **46,169.35 ns** |  **2,530.70 ns** | **1.000** |    **0.00** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   3,013.0 ns |     772.36 ns |     42.34 ns | 0.005 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 626,760.7 ns |  47,708.47 ns |  2,615.06 ns | 0.989 |    0.00 |    2 | 0.9766 |      - |  155029 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 629,716.5 ns |  31,976.95 ns |  1,752.76 ns | 0.993 |    0.00 |    2 | 0.9766 |      - |  155669 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 964,455.2 ns | 225,970.74 ns | 12,386.22 ns | 1.522 |    0.02 |    3 |      - |      - |  154704 B |        1.00 |
