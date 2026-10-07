
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **100,844.9 ns** |   **8,145.14 ns** |   **446.46 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     321.4 ns |      46.16 ns |     2.53 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 103,582.6 ns |  22,060.32 ns | 1,209.20 ns | 1.027 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 101,314.1 ns |  10,117.09 ns |   554.55 ns | 1.005 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 100,467.3 ns |   8,532.74 ns |   467.71 ns | 0.996 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |               |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **141,724.1 ns** |   **9,676.84 ns** |   **530.42 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     463.6 ns |      67.57 ns |     3.70 ns | 0.003 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 142,659.3 ns |  10,286.28 ns |   563.83 ns | 1.007 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 142,533.6 ns |   5,691.81 ns |   311.99 ns | 1.006 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 157,419.7 ns |  16,222.56 ns |   889.21 ns | 1.111 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |               |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **128,625.0 ns** |   **9,874.29 ns** |   **541.24 ns** |  **1.00** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,444.7 ns |     139.01 ns |     7.62 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 131,834.2 ns |  17,274.53 ns |   946.88 ns |  1.02 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 133,600.3 ns |  18,227.85 ns |   999.13 ns |  1.04 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 146,942.3 ns |  19,656.57 ns | 1,077.44 ns |  1.14 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |               |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **600,193.8 ns** |  **56,001.90 ns** | **3,069.65 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,361.5 ns |     266.85 ns |    14.63 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 657,195.6 ns | 108,596.30 ns | 5,952.53 ns | 1.095 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 598,726.0 ns |  11,498.42 ns |   630.27 ns | 0.998 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 899,540.7 ns |  43,791.81 ns | 2,400.38 ns | 1.499 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
