
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev       | Median       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **88,882.1 ns** |  **1,468.17 ns** |  **2,009.64 ns** |  **90,217.9 ns** | **1.000** |    **0.03** |    **2** | **0.2441** | **0.1221** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     351.2 ns |      7.32 ns |     10.96 ns |     351.5 ns | 0.004 |    0.00 |    1 | 0.0148 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  92,705.0 ns |    864.68 ns |  1,294.22 ns |  93,045.6 ns | 1.044 |    0.03 |    3 | 0.2441 | 0.1221 |    6853 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  87,557.4 ns |    244.50 ns |    342.75 ns |  87,623.9 ns | 0.986 |    0.02 |    2 | 0.2441 | 0.1221 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  86,799.1 ns |    251.70 ns |    368.93 ns |  86,853.0 ns | 0.977 |    0.02 |    2 | 0.2441 | 0.1221 |    6542 B |        1.00 |
                                                       |            |               |              |              |              |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **130,942.4 ns** |    **320.23 ns** |    **427.50 ns** | **130,759.1 ns** | **1.000** |    **0.00** |    **2** | **0.7324** | **0.4883** |   **22739 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     491.3 ns |      3.92 ns |      5.36 ns |     492.4 ns | 0.004 |    0.00 |    1 | 0.1001 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 137,311.5 ns |    531.17 ns |    778.58 ns | 137,342.8 ns | 1.049 |    0.01 |    3 | 0.7324 | 0.4883 |   23051 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 132,223.8 ns |    227.95 ns |    341.18 ns | 132,227.0 ns | 1.010 |    0.00 |    2 | 0.7324 | 0.4883 |   23691 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 144,826.7 ns |    426.40 ns |    625.01 ns | 144,766.9 ns | 1.106 |    0.01 |    4 | 0.7324 | 0.4883 |   22739 B |        1.00 |
                                                       |            |               |              |              |              |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **118,709.6 ns** |    **357.14 ns** |    **534.55 ns** | **118,784.6 ns** |  **1.00** |    **0.01** |    **2** | **0.7324** | **0.6104** |   **19742 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,416.3 ns |      7.18 ns |     10.52 ns |   1,416.8 ns |  0.01 |    0.00 |    1 | 0.0839 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 124,707.2 ns |    350.35 ns |    513.53 ns | 124,775.6 ns |  1.05 |    0.01 |    3 | 0.7324 | 0.6104 |   20054 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 120,201.3 ns |    346.00 ns |    473.61 ns | 120,261.4 ns |  1.01 |    0.01 |    2 | 0.7324 | 0.6104 |   20693 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 137,623.8 ns |    294.52 ns |    422.39 ns | 137,530.5 ns |  1.16 |    0.01 |    4 | 0.7324 | 0.4883 |   19742 B |        1.00 |
                                                       |            |               |              |              |              |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **661,927.8 ns** | **17,418.71 ns** | **26,071.51 ns** | **661,124.6 ns** | **1.002** |    **0.05** |    **2** | **5.8594** | **3.9063** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,622.4 ns |     52.11 ns |     76.38 ns |   2,651.9 ns | 0.004 |    0.00 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 647,496.5 ns |  1,923.94 ns |  2,759.26 ns | 647,449.7 ns | 0.980 |    0.04 |    2 | 5.8594 | 4.8828 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 644,336.5 ns |  4,173.08 ns |  5,984.90 ns | 647,867.6 ns | 0.975 |    0.04 |    2 | 5.8594 | 2.9297 |  155687 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 922,631.2 ns |    485.82 ns |    696.75 ns | 922,809.7 ns | 1.396 |    0.05 |    3 | 5.8594 | 2.9297 |  154735 B |        1.00 |
