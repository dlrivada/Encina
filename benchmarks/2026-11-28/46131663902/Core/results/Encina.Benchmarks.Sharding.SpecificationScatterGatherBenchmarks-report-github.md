```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.07GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **29,479.6 ns** |  **3,225.14 ns** |   **176.78 ns** | **1.000** |    **0.01** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     255.2 ns |     31.18 ns |     1.71 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  32,012.0 ns | 36,378.90 ns | 1,994.05 ns | 1.086 |    0.06 |    2 | 0.0610 |      - |    6850 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  28,655.5 ns |  1,778.25 ns |    97.47 ns | 0.972 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  31,187.3 ns |    827.05 ns |    45.33 ns | 1.058 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **64,941.8 ns** |  **9,119.06 ns** |   **499.85 ns** | **1.000** |    **0.01** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     397.5 ns |    301.66 ns |    16.53 ns | 0.006 |    0.00 |    1 | 0.0300 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  65,275.1 ns |  9,768.02 ns |   535.42 ns | 1.005 |    0.01 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  66,932.6 ns |  6,228.34 ns |   341.40 ns | 1.031 |    0.01 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  80,338.1 ns |  5,301.30 ns |   290.58 ns | 1.237 |    0.01 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **56,192.8 ns** |  **2,988.12 ns** |   **163.79 ns** |  **1.00** |    **0.00** |    **2** | **0.1831** | **0.1221** |   **19739 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,216.3 ns |    649.35 ns |    35.59 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  57,843.0 ns | 15,482.25 ns |   848.63 ns |  1.03 |    0.01 |    2 | 0.1831 | 0.1221 |   20050 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  60,289.2 ns | 34,474.52 ns | 1,889.66 ns |  1.07 |    0.03 |    2 | 0.2441 | 0.1831 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  68,428.0 ns |  7,616.85 ns |   417.51 ns |  1.22 |    0.01 |    2 | 0.1221 |      - |   19724 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **504,573.1 ns** | **63,878.49 ns** | **3,501.40 ns** | **1.000** |    **0.01** |    **2** | **1.4648** | **0.9766** |  **154731 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,227.2 ns |  1,649.22 ns |    90.40 ns | 0.004 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 443,518.6 ns | 15,224.04 ns |   834.48 ns | 0.879 |    0.01 |    2 | 1.4648 | 0.9766 |  155043 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 449,759.2 ns |  7,538.65 ns |   413.22 ns | 0.891 |    0.01 |    2 | 1.4648 | 0.9766 |  155683 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 673,174.9 ns | 54,960.50 ns | 3,012.57 ns | 1.334 |    0.01 |    3 | 0.9766 |      - |  154717 B |        1.00 |
