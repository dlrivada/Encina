```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **89,565.4 ns** |  **5,675.34 ns** |   **311.08 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     360.5 ns |    440.73 ns |    24.16 ns | 0.004 |    1 | 0.0148 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  87,972.2 ns |  3,487.35 ns |   191.15 ns | 0.982 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  86,055.5 ns |  4,470.99 ns |   245.07 ns | 0.961 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  87,304.6 ns | 12,613.88 ns |   691.41 ns | 0.975 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **127,231.0 ns** |  **7,164.88 ns** |   **392.73 ns** | **1.000** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     501.5 ns |     39.80 ns |     2.18 ns | 0.004 |    1 | 0.1001 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 130,428.9 ns | 17,293.05 ns |   947.89 ns | 1.025 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 130,814.0 ns | 16,882.45 ns |   925.38 ns | 1.028 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 142,310.6 ns | 11,295.35 ns |   619.14 ns | 1.119 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **116,632.1 ns** | **10,473.80 ns** |   **574.10 ns** |  **1.00** |    **2** | **0.7324** | **0.6104** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,496.0 ns |    237.27 ns |    13.01 ns |  0.01 |    1 | 0.0839 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 117,556.4 ns |  4,338.42 ns |   237.80 ns |  1.01 |    2 | 0.7324 | 0.6104 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 117,800.8 ns |  5,306.18 ns |   290.85 ns |  1.01 |    2 | 0.7324 | 0.6104 |   20685 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 129,776.1 ns | 10,637.83 ns |   583.10 ns |  1.11 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **612,181.8 ns** | **34,773.44 ns** | **1,906.05 ns** | **1.000** |    **2** | **5.8594** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,593.5 ns |    317.63 ns |    17.41 ns | 0.004 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 620,816.4 ns | 35,165.64 ns | 1,927.55 ns | 1.014 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 612,816.9 ns | 41,215.36 ns | 2,259.15 ns | 1.001 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 888,673.2 ns | 14,565.50 ns |   798.38 ns | 1.452 |    3 | 5.8594 | 3.9063 |  154727 B |        1.00 |
