
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **116,639.1 ns** |  **6,856.81 ns** |   **375.85 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     320.1 ns |     45.26 ns |     2.48 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 119,059.4 ns |  9,879.39 ns |   541.52 ns | 1.021 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 115,602.9 ns | 14,016.05 ns |   768.27 ns | 0.991 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 117,613.8 ns |  1,206.51 ns |    66.13 ns | 1.008 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **155,183.9 ns** | **12,707.68 ns** |   **696.55 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     445.4 ns |      5.17 ns |     0.28 ns | 0.003 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 155,561.2 ns |  3,935.82 ns |   215.74 ns | 1.002 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 156,062.0 ns |  5,139.84 ns |   281.73 ns | 1.006 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 168,967.2 ns |  9,934.52 ns |   544.54 ns | 1.089 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **141,480.4 ns** | **16,686.80 ns** |   **914.66 ns** | **1.000** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,402.4 ns |    275.68 ns |    15.11 ns | 0.010 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 143,506.6 ns | 17,599.38 ns |   964.68 ns | 1.014 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 146,500.8 ns |  6,830.63 ns |   374.41 ns | 1.036 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 157,147.9 ns | 14,907.61 ns |   817.14 ns | 1.111 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **586,922.5 ns** | **34,536.79 ns** | **1,893.08 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,421.0 ns |    107.24 ns |     5.88 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 592,734.8 ns | 45,873.79 ns | 2,514.50 ns | 1.010 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 591,093.4 ns |  5,973.01 ns |   327.40 ns | 1.007 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 880,267.6 ns | 27,809.69 ns | 1,524.34 ns | 1.500 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
