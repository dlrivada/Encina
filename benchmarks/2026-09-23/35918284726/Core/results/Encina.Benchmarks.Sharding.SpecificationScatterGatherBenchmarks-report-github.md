```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **40,107.5 ns** |   **2,460.20 ns** |   **134.85 ns** | **1.000** |    **0.00** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     351.6 ns |      49.81 ns |     2.73 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  42,394.6 ns |  19,323.53 ns | 1,059.19 ns | 1.057 |    0.02 |    2 | 0.0610 |      - |    6850 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  41,470.5 ns |  21,683.57 ns | 1,188.55 ns | 1.034 |    0.03 |    2 | 0.0610 |      - |    6539 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  41,831.0 ns |   5,540.35 ns |   303.69 ns | 1.043 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **92,440.8 ns** |  **13,411.21 ns** |   **735.11 ns** | **1.000** |    **0.01** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     562.2 ns |     201.04 ns |    11.02 ns | 0.006 |    0.00 |    1 | 0.0296 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  94,419.5 ns |  10,867.93 ns |   595.71 ns | 1.021 |    0.01 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  92,125.7 ns |  13,283.41 ns |   728.11 ns | 0.997 |    0.01 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 112,077.0 ns |  28,678.10 ns | 1,571.94 ns | 1.212 |    0.02 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **74,510.2 ns** |  **14,924.41 ns** |   **818.06 ns** |  **1.00** |    **0.01** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,586.0 ns |     287.38 ns |    15.75 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  76,842.7 ns |  15,499.72 ns |   849.59 ns |  1.03 |    0.01 |    2 | 0.1221 |      - |   20036 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  80,836.7 ns |   4,006.61 ns |   219.62 ns |  1.08 |    0.01 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  92,533.5 ns |  18,989.03 ns | 1,040.85 ns |  1.24 |    0.02 |    2 | 0.1221 |      - |   19724 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **632,926.1 ns** |  **94,151.78 ns** | **5,160.78 ns** | **1.000** |    **0.01** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,669.4 ns |   1,869.41 ns |   102.47 ns | 0.004 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 633,742.0 ns | 104,538.81 ns | 5,730.12 ns | 1.001 |    0.01 |    2 | 0.9766 |      - |  155029 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 630,028.9 ns |  50,333.97 ns | 2,758.97 ns | 0.995 |    0.01 |    2 | 0.9766 |      - |  155669 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 973,663.7 ns |  91,563.85 ns | 5,018.92 ns | 1.538 |    0.01 |    3 |      - |      - |  154704 B |        1.00 |
