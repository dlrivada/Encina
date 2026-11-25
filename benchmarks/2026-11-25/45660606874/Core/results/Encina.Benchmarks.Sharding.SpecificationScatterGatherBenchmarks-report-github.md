```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **113,995.4 ns** |     **937.13 ns** |    **51.37 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     331.0 ns |      65.97 ns |     3.62 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 114,956.2 ns |   6,285.08 ns |   344.51 ns | 1.008 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 112,714.8 ns |   3,422.60 ns |   187.60 ns | 0.989 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 115,908.8 ns |  10,445.69 ns |   572.56 ns | 1.017 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |               |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **152,412.0 ns** |  **10,263.49 ns** |   **562.58 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     464.2 ns |     289.52 ns |    15.87 ns | 0.003 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 153,827.6 ns |  14,510.20 ns |   795.35 ns | 1.009 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 152,111.0 ns |   7,921.43 ns |   434.20 ns | 0.998 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 166,429.9 ns |   8,259.70 ns |   452.74 ns | 1.092 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |              |               |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **139,515.0 ns** |   **6,500.63 ns** |   **356.32 ns** |  **1.00** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,408.2 ns |     319.02 ns |    17.49 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 143,576.1 ns |  18,579.69 ns | 1,018.42 ns |  1.03 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 142,366.9 ns |  11,212.30 ns |   614.58 ns |  1.02 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 153,591.6 ns |   4,493.63 ns |   246.31 ns |  1.10 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
|                                                       |            |               |              |               |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **574,808.1 ns** |  **34,252.25 ns** | **1,877.48 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,439.8 ns |     438.94 ns |    24.06 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 582,734.5 ns |  54,980.15 ns | 3,013.65 ns | 1.014 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 579,915.5 ns |  45,727.06 ns | 2,506.45 ns | 1.009 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 854,797.3 ns | 133,661.73 ns | 7,326.45 ns | 1.487 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
