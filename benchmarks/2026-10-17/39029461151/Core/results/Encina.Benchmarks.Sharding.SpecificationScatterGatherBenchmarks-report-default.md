
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **80,478.2 ns** |   **8,546.11 ns** |   **468.44 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     253.5 ns |      18.75 ns |     1.03 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  80,233.4 ns |   4,553.87 ns |   249.61 ns | 0.997 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  79,261.4 ns |  13,502.53 ns |   740.12 ns | 0.985 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  78,734.3 ns |   6,530.90 ns |   357.98 ns | 0.978 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **113,086.6 ns** |   **8,203.56 ns** |   **449.67 ns** | **1.000** |    **0.00** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     374.2 ns |      52.47 ns |     2.88 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 114,881.5 ns |  10,422.97 ns |   571.32 ns | 1.016 |    0.01 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 114,396.0 ns |   8,306.66 ns |   455.32 ns | 1.012 |    0.00 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 124,504.2 ns |  14,631.95 ns |   802.03 ns | 1.101 |    0.01 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **104,628.9 ns** |   **3,392.97 ns** |   **185.98 ns** |  **1.00** |    **0.00** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,206.0 ns |     141.86 ns |     7.78 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 105,595.2 ns |   7,460.09 ns |   408.91 ns |  1.01 |    0.00 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 106,110.4 ns |   9,321.96 ns |   510.97 ns |  1.01 |    0.00 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 115,746.3 ns |   3,188.10 ns |   174.75 ns |  1.11 |    0.00 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **497,762.8 ns** |  **65,010.50 ns** | **3,563.44 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,031.5 ns |     343.67 ns |    18.84 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 506,084.0 ns |  49,445.81 ns | 2,710.29 ns | 1.017 |    0.01 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 505,857.3 ns |  33,381.33 ns | 1,829.74 ns | 1.016 |    0.01 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 728,669.8 ns | 131,094.47 ns | 7,185.73 ns | 1.464 |    0.02 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
