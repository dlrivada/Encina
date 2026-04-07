```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error       | StdDev      | Median       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|------------:|------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **87,538.8 ns** |   **627.06 ns** |   **919.13 ns** |  **87,198.2 ns** | **1.000** |    **0.01** |    **2** | **0.2441** | **0.1221** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     338.1 ns |     0.94 ns |     1.35 ns |     338.0 ns | 0.004 |    0.00 |    1 | 0.0148 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  88,909.4 ns |   461.20 ns |   646.54 ns |  88,669.1 ns | 1.016 |    0.01 |    2 | 0.2441 | 0.1221 |    6853 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  88,085.9 ns | 1,129.75 ns | 1,690.96 ns |  87,321.0 ns | 1.006 |    0.02 |    2 | 0.2441 | 0.1221 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  89,366.1 ns | 1,380.19 ns | 2,023.06 ns |  88,267.2 ns | 1.021 |    0.02 |    2 | 0.2441 | 0.1221 |    6542 B |        1.00 |
|                                                       |            |               |              |             |             |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **133,749.1 ns** | **1,211.12 ns** | **1,812.75 ns** | **133,880.3 ns** | **1.000** |    **0.02** |    **2** | **0.7324** | **0.4883** |   **22739 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     589.3 ns |     5.22 ns |     7.32 ns |     591.7 ns | 0.004 |    0.00 |    1 | 0.1001 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 137,078.3 ns |   372.25 ns |   521.85 ns | 137,117.9 ns | 1.025 |    0.01 |    2 | 0.7324 | 0.4883 |   23051 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 135,690.7 ns | 1,116.92 ns | 1,637.17 ns | 136,054.5 ns | 1.015 |    0.02 |    2 | 0.7324 | 0.4883 |   23691 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 147,376.6 ns | 1,297.63 ns | 1,861.02 ns | 146,218.1 ns | 1.102 |    0.02 |    3 | 0.7324 | 0.4883 |   22739 B |        1.00 |
|                                                       |            |               |              |             |             |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **120,713.0 ns** | **1,377.85 ns** | **1,931.55 ns** | **122,120.4 ns** |  **1.00** |    **0.02** |    **2** | **0.7324** | **0.6104** |   **19742 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,487.5 ns |    21.33 ns |    29.20 ns |   1,490.1 ns |  0.01 |    0.00 |    1 | 0.0839 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 122,621.4 ns | 1,686.42 ns | 2,418.62 ns | 122,639.3 ns |  1.02 |    0.03 |    2 | 0.7324 | 0.4883 |   20054 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 123,007.6 ns | 1,658.23 ns | 2,378.18 ns | 122,966.2 ns |  1.02 |    0.03 |    2 | 0.7324 | 0.6104 |   20693 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 137,066.7 ns |   276.14 ns |   404.76 ns | 137,034.8 ns |  1.14 |    0.02 |    3 | 0.7324 | 0.4883 |   19742 B |        1.00 |
|                                                       |            |               |              |             |             |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **643,017.6 ns** | **2,790.13 ns** | **4,089.74 ns** | **641,028.7 ns** | **1.000** |    **0.01** |    **2** | **5.8594** | **3.9063** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   3,278.3 ns |    79.99 ns |   119.73 ns |   3,286.0 ns | 0.005 |    0.00 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 645,736.0 ns | 3,121.78 ns | 4,672.54 ns | 646,150.5 ns | 1.004 |    0.01 |    2 | 5.8594 | 4.8828 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 645,461.9 ns | 4,648.49 ns | 6,957.64 ns | 645,693.9 ns | 1.004 |    0.01 |    2 | 5.8594 | 2.9297 |  155687 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 931,038.0 ns | 3,785.73 ns | 5,666.30 ns | 933,945.7 ns | 1.448 |    0.01 |    3 | 5.8594 | 3.9063 |  154735 B |        1.00 |
