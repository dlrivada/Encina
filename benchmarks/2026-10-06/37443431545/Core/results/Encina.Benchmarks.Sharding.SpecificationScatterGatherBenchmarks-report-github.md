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
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **38,199.9 ns** |  **20,569.91 ns** | **1,127.51 ns** | **1.001** |    **0.04** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     323.4 ns |      40.65 ns |     2.23 ns | 0.008 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  38,730.5 ns |   3,392.04 ns |   185.93 ns | 1.014 |    0.03 |    2 | 0.0610 |      - |    6850 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  36,557.5 ns |   7,606.00 ns |   416.91 ns | 0.958 |    0.03 |    2 | 0.0610 |      - |    6539 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  37,662.1 ns |   1,972.32 ns |   108.11 ns | 0.986 |    0.03 |    2 | 0.0610 |      - |    6539 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **83,897.5 ns** |   **3,887.07 ns** |   **213.06 ns** | **1.000** |    **0.00** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     536.7 ns |     327.57 ns |    17.96 ns | 0.006 |    0.00 |    1 | 0.0296 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  84,813.5 ns |  41,035.52 ns | 2,249.30 ns | 1.011 |    0.02 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  85,130.6 ns |  38,423.16 ns | 2,106.10 ns | 1.015 |    0.02 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 100,333.5 ns |   2,062.83 ns |   113.07 ns | 1.196 |    0.00 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **69,327.7 ns** |   **4,477.96 ns** |   **245.45 ns** |  **1.00** |    **0.00** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,451.9 ns |     220.77 ns |    12.10 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  68,787.6 ns |  10,438.52 ns |   572.17 ns |  0.99 |    0.01 |    2 | 0.1221 |      - |   20036 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  71,664.1 ns |  12,297.27 ns |   674.05 ns |  1.03 |    0.01 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  85,666.2 ns |   3,754.65 ns |   205.80 ns |  1.24 |    0.00 |    2 | 0.1221 |      - |   19724 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **587,594.0 ns** | **175,885.52 ns** | **9,640.88 ns** | **1.000** |    **0.02** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,501.5 ns |   1,744.53 ns |    95.62 ns | 0.004 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 577,102.7 ns |  89,483.80 ns | 4,904.91 ns | 0.982 |    0.02 |    2 | 0.9766 |      - |  155029 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 584,605.0 ns |  66,610.28 ns | 3,651.13 ns | 0.995 |    0.02 |    2 | 0.9766 |      - |  155669 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 875,440.3 ns | 145,870.27 ns | 7,995.64 ns | 1.490 |    0.02 |    3 | 0.9766 |      - |  154717 B |        1.00 |
