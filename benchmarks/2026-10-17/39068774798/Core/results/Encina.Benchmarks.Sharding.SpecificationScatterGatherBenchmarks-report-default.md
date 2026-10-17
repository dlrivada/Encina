
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean           | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |---------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |    **79,237.6 ns** |  **6,389.43 ns** |   **350.23 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |       264.3 ns |     18.70 ns |     1.03 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |    79,835.5 ns |  5,385.29 ns |   295.19 ns | 1.008 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |    79,682.5 ns |  2,872.10 ns |   157.43 ns | 1.006 |    0.00 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |    79,613.0 ns | 10,911.69 ns |   598.11 ns | 1.005 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |                |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |   **114,745.3 ns** |  **5,641.08 ns** |   **309.21 ns** | **1.000** |    **0.00** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |       406.7 ns |    218.27 ns |    11.96 ns | 0.004 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |   118,258.6 ns | 32,315.36 ns | 1,771.31 ns | 1.031 |    0.01 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |   117,815.3 ns | 15,216.05 ns |   834.04 ns | 1.027 |    0.01 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |   178,060.5 ns |  6,220.75 ns |   340.98 ns | 1.552 |    0.00 |    3 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |                |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |   **102,587.0 ns** |  **7,167.53 ns** |   **392.88 ns** |  **1.00** |    **0.00** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     1,369.1 ns |    429.02 ns |    23.52 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |   106,345.8 ns |  6,287.36 ns |   344.63 ns |  1.04 |    0.00 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |   107,500.7 ns |  1,853.52 ns |   101.60 ns |  1.05 |    0.00 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |   115,355.6 ns |  2,008.45 ns |   110.09 ns |  1.12 |    0.00 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
                                                       |            |               |                |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           |   **499,865.3 ns** | **97,583.79 ns** | **5,348.90 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |     2,241.3 ns |  1,074.74 ns |    58.91 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           |   502,896.6 ns | 12,758.51 ns |   699.34 ns | 1.006 |    0.01 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           |   512,042.0 ns | 40,995.84 ns | 2,247.12 ns | 1.024 |    0.01 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 1,470,835.0 ns | 42,809.34 ns | 2,346.52 ns | 2.943 |    0.03 |    3 | 7.8125 | 1.9531 |  154724 B |        1.00 |
