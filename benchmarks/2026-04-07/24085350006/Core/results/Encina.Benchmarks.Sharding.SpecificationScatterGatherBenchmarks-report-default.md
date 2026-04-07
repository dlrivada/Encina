
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **111,961.8 ns** |  **9,036.99 ns** |   **495.35 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     325.1 ns |      5.20 ns |     0.29 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 114,245.8 ns |  4,943.04 ns |   270.94 ns | 1.020 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 112,356.0 ns |  1,177.06 ns |    64.52 ns | 1.004 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 112,370.1 ns |  8,585.67 ns |   470.61 ns | 1.004 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **150,594.8 ns** |  **9,177.90 ns** |   **503.07 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     472.7 ns |    231.41 ns |    12.68 ns | 0.003 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 152,317.6 ns |  9,170.51 ns |   502.67 ns | 1.011 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 152,530.6 ns |  6,289.71 ns |   344.76 ns | 1.013 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 165,761.9 ns |  6,308.30 ns |   345.78 ns | 1.101 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **139,659.0 ns** | **11,638.20 ns** |   **637.93 ns** |  **1.00** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,429.0 ns |    150.03 ns |     8.22 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 142,108.6 ns |  6,367.42 ns |   349.02 ns |  1.02 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 143,549.2 ns | 24,025.02 ns | 1,316.89 ns |  1.03 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 155,464.8 ns | 11,729.94 ns |   642.96 ns |  1.11 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **587,340.4 ns** | **28,469.62 ns** | **1,560.52 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,459.7 ns |  1,383.38 ns |    75.83 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 590,534.5 ns | 12,179.07 ns |   667.58 ns | 1.005 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 591,835.8 ns | 53,495.99 ns | 2,932.30 ns | 1.008 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 879,363.9 ns | 36,557.23 ns | 2,003.82 ns | 1.497 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
