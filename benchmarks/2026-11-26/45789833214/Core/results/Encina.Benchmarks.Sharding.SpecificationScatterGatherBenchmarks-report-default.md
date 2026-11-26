
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **41,121.5 ns** |   **3,621.36 ns** |    **198.50 ns** | **1.000** |    **0.01** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     329.9 ns |      69.66 ns |      3.82 ns | 0.008 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  38,324.0 ns |   4,374.06 ns |    239.76 ns | 0.932 |    0.01 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  39,016.7 ns |   3,949.25 ns |    216.47 ns | 0.949 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  40,030.4 ns |   4,000.64 ns |    219.29 ns | 0.973 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **90,462.0 ns** | **121,691.41 ns** |  **6,670.32 ns** | **1.003** |    **0.09** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     450.5 ns |     112.57 ns |      6.17 ns | 0.005 |    0.00 |    1 | 0.0296 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  85,428.7 ns |  14,932.26 ns |    818.49 ns | 0.948 |    0.06 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  88,987.1 ns |   8,529.08 ns |    467.51 ns | 0.987 |    0.06 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 100,314.6 ns |  51,749.52 ns |  2,836.57 ns | 1.113 |    0.07 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **67,645.3 ns** |   **1,702.31 ns** |     **93.31 ns** |  **1.00** |    **0.00** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,374.4 ns |     322.30 ns |     17.67 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  67,031.6 ns |   6,944.69 ns |    380.66 ns |  0.99 |    0.01 |    2 | 0.1221 |      - |   20036 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  68,976.9 ns |   5,188.12 ns |    284.38 ns |  1.02 |    0.00 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  82,774.8 ns |   4,786.54 ns |    262.37 ns |  1.22 |    0.00 |    3 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **567,921.7 ns** | **273,165.27 ns** | **14,973.11 ns** | **1.000** |    **0.03** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,336.0 ns |     280.87 ns |     15.40 ns | 0.004 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 559,203.5 ns |  76,703.62 ns |  4,204.38 ns | 0.985 |    0.02 |    2 | 0.9766 |      - |  155029 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 557,571.3 ns |  61,796.02 ns |  3,387.25 ns | 0.982 |    0.02 |    2 | 0.9766 |      - |  155669 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 860,196.9 ns | 148,499.27 ns |  8,139.75 ns | 1.515 |    0.04 |    3 | 0.9766 |      - |  154717 B |        1.00 |
