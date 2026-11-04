
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.65GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **79,939.8 ns** |   **8,180.68 ns** |   **448.41 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     254.3 ns |      13.78 ns |     0.76 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  81,324.1 ns |   8,465.59 ns |   464.03 ns | 1.017 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  80,929.8 ns |  12,483.63 ns |   684.27 ns | 1.012 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  80,278.0 ns |   3,811.23 ns |   208.91 ns | 1.004 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **115,757.7 ns** |  **43,448.04 ns** | **2,381.53 ns** | **1.000** |    **0.03** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     365.5 ns |     193.74 ns |    10.62 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 117,294.6 ns |   8,404.04 ns |   460.65 ns | 1.014 |    0.02 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 118,124.7 ns |  17,229.18 ns |   944.39 ns | 1.021 |    0.02 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 126,188.5 ns |  12,616.65 ns |   691.56 ns | 1.090 |    0.02 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **104,833.5 ns** |  **15,228.87 ns** |   **834.75 ns** |  **1.00** |    **0.01** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,237.0 ns |     194.82 ns |    10.68 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 107,084.4 ns |  36,161.67 ns | 1,982.14 ns |  1.02 |    0.02 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 106,395.1 ns |  30,883.23 ns | 1,692.81 ns |  1.01 |    0.02 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 116,340.5 ns |   4,712.99 ns |   258.34 ns |  1.11 |    0.01 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **503,733.8 ns** |  **31,098.80 ns** | **1,704.63 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,062.1 ns |   2,819.79 ns |   154.56 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 509,408.9 ns | 107,734.53 ns | 5,905.29 ns | 1.011 |    0.01 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 506,464.6 ns |  27,494.84 ns | 1,507.08 ns | 1.005 |    0.00 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 730,780.9 ns |  17,555.32 ns |   962.27 ns | 1.451 |    0.00 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
