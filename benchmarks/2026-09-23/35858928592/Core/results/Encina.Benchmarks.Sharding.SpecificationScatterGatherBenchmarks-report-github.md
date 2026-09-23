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
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **41,078.1 ns** |   **8,350.05 ns** |   **457.69 ns** | **1.000** |    **0.01** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     356.8 ns |      38.72 ns |     2.12 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  42,471.9 ns |   2,535.27 ns |   138.97 ns | 1.034 |    0.01 |    2 | 0.0610 |      - |    6850 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  44,071.8 ns |  20,204.37 ns | 1,107.47 ns | 1.073 |    0.03 |    2 | 0.0610 |      - |    6539 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  42,419.6 ns |   5,670.01 ns |   310.79 ns | 1.033 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **92,377.7 ns** |  **17,363.42 ns** |   **951.75 ns** | **1.000** |    **0.01** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     575.1 ns |     204.86 ns |    11.23 ns | 0.006 |    0.00 |    1 | 0.0296 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  93,303.1 ns |   4,523.59 ns |   247.95 ns | 1.010 |    0.01 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  92,931.5 ns |   3,863.99 ns |   211.80 ns | 1.006 |    0.01 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 110,374.7 ns |   5,655.69 ns |   310.01 ns | 1.195 |    0.01 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **76,538.8 ns** |  **24,213.95 ns** | **1,327.25 ns** |  **1.00** |    **0.02** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,592.3 ns |      96.00 ns |     5.26 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  77,132.6 ns |  23,513.08 ns | 1,288.83 ns |  1.01 |    0.02 |    2 | 0.1221 |      - |   20036 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  80,853.2 ns |   7,013.07 ns |   384.41 ns |  1.06 |    0.02 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  93,733.0 ns |   1,649.29 ns |    90.40 ns |  1.22 |    0.02 |    2 | 0.1221 |      - |   19724 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **642,417.4 ns** |  **22,189.59 ns** | **1,216.29 ns** | **1.000** |    **0.00** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   3,281.4 ns |   2,520.03 ns |   138.13 ns | 0.005 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 644,862.7 ns | 155,539.75 ns | 8,525.66 ns | 1.004 |    0.01 |    2 | 0.9766 |      - |  155029 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 651,365.3 ns | 178,082.73 ns | 9,761.32 ns | 1.014 |    0.01 |    2 | 0.9766 |      - |  155669 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 977,958.2 ns |  58,022.27 ns | 3,180.40 ns | 1.522 |    0.00 |    3 |      - |      - |  154704 B |        1.00 |
