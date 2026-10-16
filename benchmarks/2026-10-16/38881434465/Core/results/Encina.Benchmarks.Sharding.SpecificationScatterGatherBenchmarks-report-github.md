```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **89,669.5 ns** | **24,794.34 ns** | **1,359.06 ns** | **1.000** |    **0.02** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     346.6 ns |     16.68 ns |     0.91 ns | 0.004 |    0.00 |    1 | 0.0148 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  90,608.1 ns |  7,292.39 ns |   399.72 ns | 1.011 |    0.01 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  89,602.9 ns | 13,006.91 ns |   712.95 ns | 0.999 |    0.01 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  89,773.7 ns | 11,018.29 ns |   603.95 ns | 1.001 |    0.01 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **133,700.7 ns** |  **5,931.85 ns** |   **325.14 ns** | **1.000** |    **0.00** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     591.9 ns |    124.22 ns |     6.81 ns | 0.004 |    0.00 |    1 | 0.1001 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 137,512.6 ns | 11,562.69 ns |   633.79 ns | 1.029 |    0.00 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 133,460.8 ns |  4,812.84 ns |   263.81 ns | 0.998 |    0.00 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 145,623.6 ns | 14,153.07 ns |   775.78 ns | 1.089 |    0.01 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **121,106.1 ns** | **23,084.07 ns** | **1,265.32 ns** |  **1.00** |    **0.01** |    **2** | **0.7324** | **0.6104** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,520.0 ns |     73.49 ns |     4.03 ns |  0.01 |    0.00 |    1 | 0.0839 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 123,341.4 ns | 17,004.91 ns |   932.10 ns |  1.02 |    0.01 |    2 | 0.7324 | 0.4883 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 123,349.3 ns | 12,872.70 ns |   705.60 ns |  1.02 |    0.01 |    2 | 0.7324 | 0.4883 |   20685 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 136,296.8 ns |  9,364.91 ns |   513.32 ns |  1.13 |    0.01 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **650,521.4 ns** | **60,315.05 ns** | **3,306.07 ns** | **1.000** |    **0.01** |    **2** | **5.8594** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   3,157.6 ns |  1,057.92 ns |    57.99 ns | 0.005 |    0.00 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 656,922.9 ns | 16,608.99 ns |   910.39 ns | 1.010 |    0.00 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 654,674.5 ns | 33,101.75 ns | 1,814.42 ns | 1.006 |    0.01 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 947,628.2 ns | 55,993.55 ns | 3,069.20 ns | 1.457 |    0.01 |    3 | 5.8594 | 3.9063 |  154727 B |        1.00 |
