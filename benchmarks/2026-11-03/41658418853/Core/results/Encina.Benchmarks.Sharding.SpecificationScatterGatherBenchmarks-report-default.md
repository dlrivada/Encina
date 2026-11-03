
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.40GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **88,016.3 ns** | **28,064.04 ns** | **1,538.28 ns** | **1.000** |    **0.02** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     350.7 ns |     14.78 ns |     0.81 ns | 0.004 |    0.00 |    1 | 0.0148 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  92,667.4 ns |  8,696.71 ns |   476.70 ns | 1.053 |    0.02 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  92,037.8 ns | 55,580.48 ns | 3,046.55 ns | 1.046 |    0.03 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  90,916.3 ns |  4,509.58 ns |   247.19 ns | 1.033 |    0.02 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **132,650.1 ns** | **42,188.82 ns** | **2,312.51 ns** | **1.000** |    **0.02** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     601.1 ns |     61.29 ns |     3.36 ns | 0.005 |    0.00 |    1 | 0.1001 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 133,644.0 ns | 19,441.25 ns | 1,065.64 ns | 1.008 |    0.02 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 138,865.5 ns |  9,314.01 ns |   510.53 ns | 1.047 |    0.02 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 146,560.2 ns | 10,730.49 ns |   588.17 ns | 1.105 |    0.02 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **119,124.4 ns** |  **3,691.12 ns** |   **202.32 ns** |  **1.00** |    **0.00** |    **2** | **0.7324** | **0.6104** |   **19734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,551.1 ns |    199.73 ns |    10.95 ns |  0.01 |    0.00 |    1 | 0.0839 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 125,627.4 ns |  8,288.51 ns |   454.32 ns |  1.05 |    0.00 |    2 | 0.7324 | 0.4883 |   20046 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 120,908.7 ns | 19,084.65 ns | 1,046.09 ns |  1.01 |    0.01 |    2 | 0.7324 | 0.6104 |   20685 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 139,570.7 ns | 19,404.31 ns | 1,063.62 ns |  1.17 |    0.01 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **652,419.3 ns** | **34,362.63 ns** | **1,883.53 ns** | **1.000** |    **0.00** |    **2** | **5.8594** | **2.9297** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   3,509.2 ns |    949.78 ns |    52.06 ns | 0.005 |    0.00 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 642,956.6 ns | 15,944.87 ns |   873.99 ns | 0.986 |    0.00 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 665,698.8 ns | 21,201.62 ns | 1,162.13 ns | 1.020 |    0.00 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 941,524.5 ns | 27,996.81 ns | 1,534.60 ns | 1.443 |    0.00 |    3 | 5.8594 | 3.9063 |  154727 B |        1.00 |
