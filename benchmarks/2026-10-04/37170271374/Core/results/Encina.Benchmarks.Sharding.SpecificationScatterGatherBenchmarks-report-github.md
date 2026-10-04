```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.37GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **36,379.7 ns** |   **255.49 ns** |   **358.16 ns** | **1.000** |    **0.01** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     300.8 ns |     3.78 ns |     5.66 ns | 0.008 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  36,718.6 ns |   507.60 ns |   711.59 ns | 1.009 |    0.02 |    2 | 0.0610 |      - |    6850 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  36,397.6 ns |   471.23 ns |   705.32 ns | 1.001 |    0.02 |    2 | 0.0610 |      - |    6539 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  36,225.4 ns |   297.93 ns |   427.29 ns | 0.996 |    0.02 |    2 | 0.0610 |      - |    6539 B |        1.00 |
|                                                       |            |               |              |             |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **83,137.6 ns** |   **729.77 ns** | **1,069.68 ns** | **1.000** |    **0.02** |    **2** | **0.2441** | **0.1221** |   **22742 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     463.1 ns |    10.44 ns |    15.62 ns | 0.006 |    0.00 |    1 | 0.0296 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  84,249.7 ns |   667.90 ns |   891.63 ns | 1.014 |    0.02 |    2 | 0.2441 | 0.1221 |   23053 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  84,260.1 ns | 1,359.12 ns | 2,034.27 ns | 1.014 |    0.03 |    2 | 0.2441 | 0.1221 |   23693 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  99,280.9 ns |   501.43 ns |   719.14 ns | 1.194 |    0.02 |    3 | 0.2441 | 0.1221 |   22742 B |        1.00 |
|                                                       |            |               |              |             |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **68,477.5 ns** |   **582.68 ns** |   **835.66 ns** |  **1.00** |    **0.02** |    **2** | **0.1221** |      **-** |   **19732 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,510.0 ns |    32.45 ns |    47.57 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  68,247.9 ns |   446.57 ns |   640.46 ns |  1.00 |    0.02 |    2 | 0.1221 |      - |   20044 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  70,951.7 ns |   651.46 ns |   869.68 ns |  1.04 |    0.02 |    2 | 0.2441 | 0.1221 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  83,728.7 ns |   465.23 ns |   681.93 ns |  1.22 |    0.02 |    3 | 0.1221 |      - |   19732 B |        1.00 |
|                                                       |            |               |              |             |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **565,951.1 ns** | **2,408.61 ns** | **3,454.36 ns** | **1.000** |    **0.01** |    **2** | **0.9766** |      **-** |  **154725 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,260.9 ns |    23.67 ns |    33.18 ns | 0.004 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 577,583.4 ns | 3,324.85 ns | 4,768.40 ns | 1.021 |    0.01 |    2 | 0.9766 |      - |  155037 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 577,751.8 ns | 4,643.00 ns | 6,658.85 ns | 1.021 |    0.01 |    2 | 0.9766 |      - |  155677 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 868,586.9 ns | 6,006.27 ns | 8,419.95 ns | 1.535 |    0.02 |    3 | 0.9766 |      - |  154725 B |        1.00 |
