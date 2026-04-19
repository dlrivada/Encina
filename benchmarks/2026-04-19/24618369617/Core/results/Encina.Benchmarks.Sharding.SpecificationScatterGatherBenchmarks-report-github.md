```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.76GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]    : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error       | StdDev      | Median       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|------------:|------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **86,553.8 ns** |   **930.11 ns** | **1,392.15 ns** |  **86,081.8 ns** | **1.000** |    **0.02** |    **2** | **0.2441** | **0.1221** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     342.1 ns |     4.11 ns |     5.76 ns |     345.3 ns | 0.004 |    0.00 |    1 | 0.0148 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  87,772.8 ns |   383.36 ns |   573.80 ns |  87,936.5 ns | 1.014 |    0.02 |    2 | 0.2441 | 0.1221 |    6853 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  85,745.0 ns |   519.16 ns |   760.98 ns |  85,536.3 ns | 0.991 |    0.02 |    2 | 0.2441 | 0.1221 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  86,995.5 ns |   514.55 ns |   754.22 ns |  86,940.3 ns | 1.005 |    0.02 |    2 | 0.2441 | 0.1221 |    6542 B |        1.00 |
|                                                       |            |               |              |             |             |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **126,688.0 ns** |   **235.14 ns** |   **351.94 ns** | **126,655.1 ns** | **1.000** |    **0.00** |    **2** | **0.7324** | **0.4883** |   **22739 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     551.7 ns |    11.92 ns |    17.09 ns |     549.2 ns | 0.004 |    0.00 |    1 | 0.1001 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 130,245.7 ns |   480.57 ns |   719.30 ns | 130,306.5 ns | 1.028 |    0.01 |    3 | 0.7324 | 0.4883 |   23051 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 130,673.5 ns |   377.88 ns |   565.59 ns | 130,696.0 ns | 1.031 |    0.01 |    3 | 0.7324 | 0.4883 |   23691 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 141,720.1 ns |   430.36 ns |   644.14 ns | 141,697.0 ns | 1.119 |    0.01 |    4 | 0.7324 | 0.4883 |   22739 B |        1.00 |
|                                                       |            |               |              |             |             |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **115,326.0 ns** |   **521.54 ns** |   **780.62 ns** | **115,209.1 ns** |  **1.00** |    **0.01** |    **2** | **0.7324** | **0.6104** |   **19742 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,463.1 ns |     4.51 ns |     6.75 ns |   1,464.9 ns |  0.01 |    0.00 |    1 | 0.0839 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 118,378.6 ns |   663.33 ns |   992.84 ns | 118,387.8 ns |  1.03 |    0.01 |    2 | 0.7324 | 0.6104 |   20054 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 118,464.7 ns |   699.93 ns | 1,047.62 ns | 118,556.6 ns |  1.03 |    0.01 |    2 | 0.7324 | 0.6104 |   20693 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 131,047.7 ns |   318.44 ns |   476.63 ns | 131,076.6 ns |  1.14 |    0.01 |    3 | 0.7324 | 0.4883 |   19742 B |        1.00 |
|                                                       |            |               |              |             |             |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **607,969.6 ns** |   **803.78 ns** | **1,152.76 ns** | **607,639.8 ns** | **1.000** |    **0.00** |    **2** | **5.8594** | **3.9063** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,842.7 ns |    93.93 ns |   140.59 ns |   2,846.2 ns | 0.005 |    0.00 |    1 | 0.8011 | 0.0496 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 612,139.8 ns | 1,385.13 ns | 2,030.31 ns | 612,626.2 ns | 1.007 |    0.00 |    2 | 5.8594 | 4.8828 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 609,887.2 ns |   623.09 ns |   913.32 ns | 609,897.2 ns | 1.003 |    0.00 |    2 | 5.8594 | 2.9297 |  155687 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 886,025.0 ns | 4,396.29 ns | 6,444.03 ns | 882,244.6 ns | 1.457 |    0.01 |    3 | 5.8594 | 3.9063 |  154735 B |        1.00 |
