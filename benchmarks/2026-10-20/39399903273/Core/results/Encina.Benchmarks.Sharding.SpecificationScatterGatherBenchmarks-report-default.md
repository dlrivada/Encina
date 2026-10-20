
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **116,431.7 ns** |  **3,131.58 ns** |   **171.65 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     338.2 ns |     27.95 ns |     1.53 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 117,662.0 ns |  9,507.36 ns |   521.13 ns | 1.011 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 116,209.5 ns |  5,899.20 ns |   323.35 ns | 0.998 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 116,959.6 ns |  6,185.42 ns |   339.04 ns | 1.005 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **154,478.0 ns** | **16,357.45 ns** |   **896.61 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     453.1 ns |    110.43 ns |     6.05 ns | 0.003 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 158,575.3 ns |  8,165.28 ns |   447.57 ns | 1.027 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 156,359.7 ns |  5,409.66 ns |   296.52 ns | 1.012 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 170,124.7 ns | 11,761.52 ns |   644.69 ns | 1.101 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **143,896.8 ns** | **15,469.58 ns** |   **847.94 ns** | **1.000** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,420.1 ns |    301.10 ns |    16.50 ns | 0.010 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 145,392.4 ns | 18,406.13 ns | 1,008.90 ns | 1.010 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 146,269.4 ns | 11,223.50 ns |   615.20 ns | 1.017 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 158,943.3 ns | 11,720.69 ns |   642.45 ns | 1.105 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **593,172.1 ns** | **53,897.90 ns** | **2,954.33 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,492.9 ns |    593.53 ns |    32.53 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 591,291.0 ns | 14,478.30 ns |   793.60 ns | 0.997 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 592,732.7 ns | 46,961.16 ns | 2,574.10 ns | 0.999 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 880,520.5 ns | 63,093.52 ns | 3,458.37 ns | 1.484 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
