```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **88,126.6 ns** |  **7,933.55 ns** |   **434.86 ns** | **1.000** |    **0.01** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     342.1 ns |     27.52 ns |     1.51 ns | 0.004 |    0.00 |    1 | 0.0148 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  89,597.8 ns |  8,525.14 ns |   467.29 ns | 1.017 |    0.01 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  91,801.2 ns | 18,650.62 ns | 1,022.30 ns | 1.042 |    0.01 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  91,451.6 ns |  5,243.25 ns |   287.40 ns | 1.038 |    0.01 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **133,360.4 ns** | **33,880.23 ns** | **1,857.09 ns** | **1.000** |    **0.02** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     591.0 ns |    214.73 ns |    11.77 ns | 0.004 |    0.00 |    1 | 0.1001 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 135,906.7 ns |  7,034.16 ns |   385.57 ns | 1.019 |    0.01 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 136,997.3 ns | 17,718.51 ns |   971.21 ns | 1.027 |    0.01 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 150,382.9 ns | 13,154.34 ns |   721.03 ns | 1.128 |    0.01 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **121,924.3 ns** | **11,909.50 ns** |   **652.80 ns** |  **1.00** |    **0.01** |    **2** | **0.7324** | **0.4883** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,496.3 ns |     63.66 ns |     3.49 ns |  0.01 |    0.00 |    1 | 0.0839 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 121,369.7 ns |  4,152.31 ns |   227.60 ns |  1.00 |    0.00 |    2 | 0.7324 | 0.6104 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 120,868.2 ns |  3,354.50 ns |   183.87 ns |  0.99 |    0.00 |    2 | 0.7324 | 0.6104 |   20685 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 134,203.0 ns |  7,240.77 ns |   396.89 ns |  1.10 |    0.01 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **623,924.2 ns** | **40,592.29 ns** | **2,225.00 ns** | **1.000** |    **0.00** |    **2** | **5.8594** | **2.9297** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   3,515.8 ns |  1,847.69 ns |   101.28 ns | 0.006 |    0.00 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 634,421.5 ns | 35,236.73 ns | 1,931.44 ns | 1.017 |    0.00 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 635,991.4 ns | 15,799.47 ns |   866.02 ns | 1.019 |    0.00 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 921,202.7 ns | 20,389.67 ns | 1,117.63 ns | 1.476 |    0.00 |    3 | 5.8594 | 3.9063 |  154727 B |        1.00 |
