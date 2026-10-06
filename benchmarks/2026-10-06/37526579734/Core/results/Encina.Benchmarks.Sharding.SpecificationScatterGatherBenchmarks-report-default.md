
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error       | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **116,488.4 ns** |  **8,772.3 ns** |   **480.84 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     339.6 ns |    108.9 ns |     5.97 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 119,524.3 ns |  8,978.1 ns |   492.12 ns | 1.026 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 119,013.3 ns |  3,956.7 ns |   216.88 ns | 1.022 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 118,735.1 ns | 14,222.2 ns |   779.57 ns | 1.019 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |             |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **155,058.4 ns** |  **5,470.0 ns** |   **299.83 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     452.3 ns |    190.3 ns |    10.43 ns | 0.003 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 156,688.5 ns | 11,349.2 ns |   622.09 ns | 1.011 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 157,374.3 ns | 17,905.5 ns |   981.46 ns | 1.015 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 171,847.4 ns |  6,878.9 ns |   377.06 ns | 1.108 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |             |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **144,464.8 ns** | **20,676.9 ns** | **1,133.37 ns** | **1.000** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,412.5 ns |    528.1 ns |    28.95 ns | 0.010 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 148,188.5 ns |  7,962.5 ns |   436.45 ns | 1.026 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 145,934.7 ns | 21,261.9 ns | 1,165.44 ns | 1.010 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 158,087.4 ns | 12,997.3 ns |   712.43 ns | 1.094 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |             |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **594,187.7 ns** | **14,514.3 ns** |   **795.58 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,658.1 ns |    981.1 ns |    53.78 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 591,092.4 ns | 67,853.9 ns | 3,719.30 ns | 0.995 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 684,703.7 ns | 48,795.2 ns | 2,674.63 ns | 1.152 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 884,405.4 ns | 13,302.2 ns |   729.14 ns | 1.488 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
