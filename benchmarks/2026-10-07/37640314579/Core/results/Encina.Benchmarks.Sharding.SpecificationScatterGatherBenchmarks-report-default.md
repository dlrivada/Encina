
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **118,000.6 ns** |  **5,955.19 ns** |   **326.42 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     332.9 ns |     16.64 ns |     0.91 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 120,366.5 ns |  5,812.48 ns |   318.60 ns | 1.020 |    0.00 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 117,190.8 ns | 27,496.63 ns | 1,507.18 ns | 0.993 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 118,009.9 ns |  5,937.35 ns |   325.45 ns | 1.000 |    0.00 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **155,168.6 ns** | **38,933.91 ns** | **2,134.10 ns** | **1.000** |    **0.02** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     513.9 ns |    380.61 ns |    20.86 ns | 0.003 |    0.00 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 159,648.7 ns | 27,034.44 ns | 1,481.85 ns | 1.029 |    0.01 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 159,445.7 ns | 11,841.25 ns |   649.06 ns | 1.028 |    0.01 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 171,963.5 ns |  8,869.38 ns |   486.16 ns | 1.108 |    0.01 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **144,936.2 ns** | **16,565.32 ns** |   **908.00 ns** | **1.000** |    **0.01** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,398.6 ns |     59.13 ns |     3.24 ns | 0.010 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 145,714.1 ns |  8,737.22 ns |   478.92 ns | 1.005 |    0.01 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 146,077.5 ns | 13,892.78 ns |   761.51 ns | 1.008 |    0.01 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 158,759.4 ns | 14,045.83 ns |   769.90 ns | 1.095 |    0.01 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **585,873.3 ns** | **30,324.66 ns** | **1,662.20 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,665.0 ns |  1,155.37 ns |    63.33 ns | 0.005 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 591,892.9 ns | 18,653.87 ns | 1,022.48 ns | 1.010 |    0.00 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 592,319.9 ns | 43,681.81 ns | 2,394.35 ns | 1.011 |    0.00 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 882,013.9 ns | 59,963.66 ns | 3,286.81 ns | 1.505 |    0.01 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
