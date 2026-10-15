
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **78,755.3 ns** |   **3,176.9 ns** |   **174.14 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     255.4 ns |     104.6 ns |     5.73 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  80,327.2 ns |   8,186.7 ns |   448.74 ns | 1.020 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  79,759.5 ns |   7,727.3 ns |   423.56 ns | 1.013 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  80,090.7 ns |  17,813.5 ns |   976.42 ns | 1.017 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **112,595.6 ns** |   **6,171.7 ns** |   **338.29 ns** | **1.000** |    **0.00** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     350.0 ns |     147.0 ns |     8.06 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 113,520.0 ns |   3,615.9 ns |   198.20 ns | 1.008 |    0.00 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 114,283.5 ns |  10,573.5 ns |   579.57 ns | 1.015 |    0.01 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 124,277.9 ns |   4,358.6 ns |   238.91 ns | 1.104 |    0.00 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **104,580.9 ns** |   **6,838.1 ns** |   **374.82 ns** |  **1.00** |    **0.00** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,204.0 ns |     102.4 ns |     5.62 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 104,052.8 ns |  12,590.9 ns |   690.15 ns |  0.99 |    0.01 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 104,814.6 ns |  34,862.8 ns | 1,910.95 ns |  1.00 |    0.02 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 115,585.0 ns |  15,227.9 ns |   834.69 ns |  1.11 |    0.01 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **501,182.2 ns** | **158,956.6 ns** | **8,712.95 ns** | **1.000** |    **0.02** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,861.1 ns |     482.8 ns |    26.46 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 500,277.7 ns |  14,105.9 ns |   773.19 ns | 0.998 |    0.01 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 496,712.6 ns |  21,188.3 ns | 1,161.40 ns | 0.991 |    0.01 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 720,327.1 ns | 168,558.4 ns | 9,239.26 ns | 1.438 |    0.03 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
