```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 3.47GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **88,190.1 ns** |  **3,069.37 ns** |   **168.24 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     350.1 ns |      9.83 ns |     0.54 ns | 0.004 |    1 | 0.0148 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  92,388.8 ns |  6,587.81 ns |   361.10 ns | 1.048 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  89,236.8 ns | 11,972.94 ns |   656.28 ns | 1.012 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  90,427.7 ns |  6,584.91 ns |   360.94 ns | 1.025 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **133,780.9 ns** | **10,263.09 ns** |   **562.55 ns** | **1.000** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     553.1 ns |    146.63 ns |     8.04 ns | 0.004 |    1 | 0.1001 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 136,077.9 ns | 19,524.36 ns | 1,070.20 ns | 1.017 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 135,153.1 ns | 12,268.91 ns |   672.50 ns | 1.010 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 147,806.0 ns |  2,931.64 ns |   160.69 ns | 1.105 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **121,701.5 ns** |  **3,994.81 ns** |   **218.97 ns** |  **1.00** |    **2** | **0.7324** | **0.6104** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,484.8 ns |     25.59 ns |     1.40 ns |  0.01 |    1 | 0.0839 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 123,956.1 ns |  5,403.52 ns |   296.18 ns |  1.02 |    2 | 0.7324 | 0.4883 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 121,473.5 ns |  4,645.96 ns |   254.66 ns |  1.00 |    2 | 0.7324 | 0.6104 |   20685 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 135,685.4 ns | 10,013.23 ns |   548.86 ns |  1.11 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **645,748.4 ns** | **22,305.56 ns** | **1,222.64 ns** | **1.000** |    **2** | **5.8594** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,803.5 ns |  1,523.93 ns |    83.53 ns | 0.004 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 642,125.8 ns | 23,625.67 ns | 1,295.00 ns | 0.994 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 645,175.3 ns | 12,294.73 ns |   673.92 ns | 0.999 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 931,736.9 ns | 25,170.92 ns | 1,379.70 ns | 1.443 |    3 | 5.8594 | 2.9297 |  154727 B |        1.00 |
