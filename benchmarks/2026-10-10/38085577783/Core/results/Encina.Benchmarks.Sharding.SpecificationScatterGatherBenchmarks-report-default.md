
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **86,538.1 ns** |  **2,276.59 ns** |   **124.79 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     334.2 ns |      4.28 ns |     0.23 ns | 0.004 |    1 | 0.0148 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  88,062.1 ns |  1,861.24 ns |   102.02 ns | 1.018 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  87,067.4 ns |  4,203.44 ns |   230.40 ns | 1.006 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  87,059.2 ns |  2,498.43 ns |   136.95 ns | 1.006 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **130,175.2 ns** | **18,117.50 ns** |   **993.08 ns** | **1.000** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     560.4 ns |    158.74 ns |     8.70 ns | 0.004 |    1 | 0.1001 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 132,615.5 ns |  8,055.96 ns |   441.57 ns | 1.019 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 130,592.9 ns |  5,999.73 ns |   328.87 ns | 1.003 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 143,914.2 ns |  5,481.88 ns |   300.48 ns | 1.106 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **117,057.9 ns** |  **3,952.33 ns** |   **216.64 ns** |  **1.00** |    **2** | **0.7324** | **0.6104** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,485.2 ns |     38.19 ns |     2.09 ns |  0.01 |    1 | 0.0839 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 122,002.0 ns |  9,865.11 ns |   540.74 ns |  1.04 |    2 | 0.7324 | 0.4883 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 121,886.7 ns | 36,254.42 ns | 1,987.23 ns |  1.04 |    2 | 0.7324 | 0.6104 |   20685 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 131,992.0 ns | 13,273.84 ns |   727.58 ns |  1.13 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
                                                       |            |               |              |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **622,394.9 ns** | **13,924.49 ns** |   **763.25 ns** | **1.000** |    **2** | **5.8594** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,723.5 ns |    589.81 ns |    32.33 ns | 0.004 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 623,993.9 ns | 21,770.30 ns | 1,193.30 ns | 1.003 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 625,882.8 ns | 60,620.00 ns | 3,322.79 ns | 1.006 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 908,170.8 ns |  8,777.69 ns |   481.13 ns | 1.459 |    3 | 5.8594 | 3.9063 |  154727 B |        1.00 |
