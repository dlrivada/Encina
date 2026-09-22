
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **118,379.4 ns** |  **7,859.71 ns** |   **430.82 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     329.2 ns |     15.80 ns |     0.87 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 119,488.6 ns |  2,516.47 ns |   137.94 ns | 1.009 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 118,721.9 ns |  4,448.00 ns |   243.81 ns | 1.003 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 118,981.7 ns |  1,807.54 ns |    99.08 ns | 1.005 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **155,731.8 ns** |  **3,114.37 ns** |   **170.71 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     521.4 ns |     69.09 ns |     3.79 ns | 0.003 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 158,387.6 ns | 13,609.49 ns |   745.98 ns | 1.017 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 158,360.0 ns |  2,367.75 ns |   129.78 ns | 1.017 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 172,878.9 ns | 29,002.75 ns | 1,589.74 ns | 1.110 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **144,389.8 ns** | **19,690.36 ns** | **1,079.30 ns** |  **1.00** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,501.8 ns |    150.02 ns |     8.22 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 147,991.1 ns | 15,675.88 ns |   859.25 ns |  1.02 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 148,090.1 ns |  7,031.17 ns |   385.40 ns |  1.03 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 159,188.3 ns |  6,284.13 ns |   344.45 ns |  1.10 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **592,293.1 ns** | **20,215.88 ns** | **1,108.10 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,809.3 ns |    543.73 ns |    29.80 ns | 0.005 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 602,507.4 ns | 70,500.95 ns | 3,864.39 ns | 1.017 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 595,497.6 ns |  9,017.96 ns |   494.31 ns | 1.005 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 876,563.3 ns | 33,945.01 ns | 1,860.64 ns | 1.480 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
