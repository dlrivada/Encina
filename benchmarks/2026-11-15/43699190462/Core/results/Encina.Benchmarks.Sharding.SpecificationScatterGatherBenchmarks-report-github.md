```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **93,536.3 ns** | **32,532.35 ns** | **1,783.21 ns** | **1.000** |    **0.02** |    **2** | **0.2441** | **0.1221** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     375.0 ns |     21.02 ns |     1.15 ns | 0.004 |    0.00 |    1 | 0.0148 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  94,523.7 ns | 13,610.31 ns |   746.03 ns | 1.011 |    0.02 |    2 | 0.2441 | 0.1221 |    6846 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  93,383.0 ns |  6,320.84 ns |   346.47 ns | 0.999 |    0.02 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  94,373.6 ns | 18,049.90 ns |   989.38 ns | 1.009 |    0.02 |    2 | 0.2441 | 0.1221 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **135,664.2 ns** |  **7,399.92 ns** |   **405.61 ns** | **1.000** |    **0.00** |    **2** | **0.7324** | **0.4883** |   **22731 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     630.1 ns |    324.84 ns |    17.81 ns | 0.005 |    0.00 |    1 | 0.1001 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 137,456.7 ns |  6,158.50 ns |   337.57 ns | 1.013 |    0.00 |    2 | 0.7324 | 0.4883 |   23043 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 136,455.4 ns | 13,953.51 ns |   764.84 ns | 1.006 |    0.01 |    2 | 0.7324 | 0.4883 |   23683 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 148,757.7 ns | 14,952.30 ns |   819.59 ns | 1.097 |    0.01 |    2 | 0.7324 | 0.4883 |   22731 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **121,229.2 ns** |  **4,982.22 ns** |   **273.09 ns** |  **1.00** |    **0.00** |    **2** | **0.7324** | **0.6104** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,600.2 ns |    137.89 ns |     7.56 ns |  0.01 |    0.00 |    1 | 0.0839 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 126,972.0 ns | 15,886.25 ns |   870.78 ns |  1.05 |    0.01 |    2 | 0.7324 | 0.4883 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 124,094.6 ns | 11,804.21 ns |   647.03 ns |  1.02 |    0.01 |    2 | 0.7324 | 0.4883 |   20685 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 136,391.7 ns |  1,796.59 ns |    98.48 ns |  1.13 |    0.00 |    2 | 0.7324 | 0.4883 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **633,641.1 ns** | **47,127.39 ns** | **2,583.21 ns** | **1.000** |    **0.00** |    **2** | **5.8594** | **2.9297** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   3,665.8 ns |  3,167.12 ns |   173.60 ns | 0.006 |    0.00 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 632,344.4 ns | 13,700.41 ns |   750.97 ns | 0.998 |    0.00 |    2 | 5.8594 | 4.8828 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 628,425.8 ns | 71,277.56 ns | 3,906.96 ns | 0.992 |    0.01 |    2 | 5.8594 | 2.9297 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 912,349.1 ns | 55,688.79 ns | 3,052.49 ns | 1.440 |    0.01 |    3 | 5.8594 | 3.9063 |  154727 B |        1.00 |
