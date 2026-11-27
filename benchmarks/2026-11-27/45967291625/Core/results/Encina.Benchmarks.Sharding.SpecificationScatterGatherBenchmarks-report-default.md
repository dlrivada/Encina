
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **39,708.9 ns** |   **3,441.16 ns** |   **188.62 ns** | **1.000** |    **0.01** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     368.5 ns |      23.27 ns |     1.28 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  41,205.8 ns |   7,694.54 ns |   421.76 ns | 1.038 |    0.01 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  41,668.7 ns |   2,416.59 ns |   132.46 ns | 1.049 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  40,347.3 ns |   2,797.83 ns |   153.36 ns | 1.016 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **89,737.0 ns** |   **6,427.79 ns** |   **352.33 ns** | **1.000** |    **0.00** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     511.6 ns |     138.03 ns |     7.57 ns | 0.006 |    0.00 |    1 | 0.0296 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  92,495.6 ns |   5,933.88 ns |   325.26 ns | 1.031 |    0.00 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  90,206.8 ns |   7,103.95 ns |   389.39 ns | 1.005 |    0.01 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 108,978.7 ns |   4,166.79 ns |   228.40 ns | 1.214 |    0.00 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **96,207.9 ns** |  **52,884.32 ns** | **2,898.77 ns** |  **1.00** |    **0.04** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,552.6 ns |      46.46 ns |     2.55 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  76,012.7 ns |  22,941.01 ns | 1,257.47 ns |  0.79 |    0.02 |    2 | 0.1221 |      - |   20036 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  78,688.7 ns |  12,705.32 ns |   696.42 ns |  0.82 |    0.02 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  92,798.9 ns |   5,607.40 ns |   307.36 ns |  0.97 |    0.03 |    2 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **622,496.5 ns** |  **22,125.88 ns** | **1,212.79 ns** | **1.000** |    **0.00** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,739.0 ns |     375.54 ns |    20.58 ns | 0.004 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 625,472.0 ns |  44,683.66 ns | 2,449.26 ns | 1.005 |    0.00 |    2 | 0.9766 |      - |  155029 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 632,036.4 ns | 106,457.22 ns | 5,835.28 ns | 1.015 |    0.01 |    2 | 0.9766 |      - |  155669 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 958,561.5 ns |  44,449.10 ns | 2,436.41 ns | 1.540 |    0.00 |    3 |      - |      - |  154704 B |        1.00 |
