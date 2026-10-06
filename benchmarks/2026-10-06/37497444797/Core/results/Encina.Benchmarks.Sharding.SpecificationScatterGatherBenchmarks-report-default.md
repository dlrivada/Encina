
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **88,044.0 ns** |  **9,931.60 ns** |   **544.38 ns** | **1.000** |    **0.01** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     337.2 ns |      6.14 ns |     0.34 ns | 0.004 |    0.00 |    1 | 0.0148 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  88,991.1 ns |  7,896.45 ns |   432.83 ns | 1.011 |    0.01 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  89,216.0 ns |  9,622.75 ns |   527.46 ns | 1.013 |    0.01 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  89,232.5 ns |  9,157.67 ns |   501.96 ns | 1.014 |    0.01 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **132,999.5 ns** |  **8,742.10 ns** |   **479.18 ns** | **1.000** |    **0.00** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     560.7 ns |    101.89 ns |     5.59 ns | 0.004 |    0.00 |    1 | 0.1001 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 135,068.0 ns |  9,175.33 ns |   502.93 ns | 1.016 |    0.00 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 132,983.9 ns |  7,828.04 ns |   429.08 ns | 1.000 |    0.00 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 145,043.9 ns |  8,600.99 ns |   471.45 ns | 1.091 |    0.00 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **118,806.7 ns** | **28,942.70 ns** | **1,586.45 ns** |  **1.00** |    **0.02** |    **2** | **0.7324** | **0.6104** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,470.2 ns |     13.07 ns |     0.72 ns |  0.01 |    0.00 |    1 | 0.0839 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 119,797.3 ns |  8,280.61 ns |   453.89 ns |  1.01 |    0.01 |    2 | 0.7324 | 0.6104 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 123,235.8 ns |  9,337.97 ns |   511.85 ns |  1.04 |    0.01 |    2 | 0.7324 | 0.4883 |   20685 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 135,040.1 ns |  4,189.99 ns |   229.67 ns |  1.14 |    0.01 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **619,522.8 ns** | **23,566.86 ns** | **1,291.78 ns** | **1.000** |    **0.00** |    **2** | **5.8594** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   3,139.8 ns |  2,886.06 ns |   158.19 ns | 0.005 |    0.00 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 629,335.4 ns | 74,889.77 ns | 4,104.96 ns | 1.016 |    0.01 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 627,399.5 ns | 21,357.78 ns | 1,170.69 ns | 1.013 |    0.00 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 910,122.6 ns | 27,015.05 ns | 1,480.79 ns | 1.469 |    0.00 |    3 | 5.8594 | 3.9063 |  154727 B |        1.00 |
