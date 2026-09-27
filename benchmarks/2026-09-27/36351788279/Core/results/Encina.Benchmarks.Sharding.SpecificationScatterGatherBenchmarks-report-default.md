
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **86,991.8 ns** | **17,194.30 ns** |   **942.48 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     355.0 ns |     10.50 ns |     0.58 ns | 0.004 |    1 | 0.0148 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  90,403.1 ns |  3,410.49 ns |   186.94 ns | 1.039 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  88,998.0 ns |  1,007.13 ns |    55.20 ns | 1.023 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  88,595.1 ns |  9,072.38 ns |   497.29 ns | 1.019 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **132,567.6 ns** |  **6,581.00 ns** |   **360.73 ns** | **1.000** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     571.4 ns |     53.15 ns |     2.91 ns | 0.004 |    1 | 0.1001 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 131,913.0 ns | 14,332.87 ns |   785.63 ns | 0.995 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 132,295.7 ns |  6,140.66 ns |   336.59 ns | 0.998 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 145,827.0 ns | 19,272.99 ns | 1,056.42 ns | 1.100 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **119,528.6 ns** |  **1,934.90 ns** |   **106.06 ns** |  **1.00** |    **2** | **0.7324** | **0.6104** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,488.8 ns |     70.29 ns |     3.85 ns |  0.01 |    1 | 0.0839 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 121,189.7 ns | 20,881.06 ns | 1,144.56 ns |  1.01 |    2 | 0.7324 | 0.6104 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 120,779.2 ns |  4,102.83 ns |   224.89 ns |  1.01 |    2 | 0.7324 | 0.6104 |   20685 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 132,315.9 ns | 13,276.59 ns |   727.73 ns |  1.11 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **630,933.6 ns** | **48,393.09 ns** | **2,652.59 ns** | **1.000** |    **2** | **5.8594** | **2.9297** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,968.6 ns |  1,068.89 ns |    58.59 ns | 0.005 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 626,723.8 ns | 75,168.04 ns | 4,120.21 ns | 0.993 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 620,808.1 ns | 28,392.48 ns | 1,556.29 ns | 0.984 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 910,000.5 ns | 15,320.73 ns |   839.78 ns | 1.442 |    3 | 5.8594 | 3.9063 |  154727 B |        1.00 |
