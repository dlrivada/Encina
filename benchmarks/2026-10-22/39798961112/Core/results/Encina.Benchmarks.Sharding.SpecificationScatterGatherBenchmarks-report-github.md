```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev       | Median       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **112,951.5 ns** |    **203.76 ns** |    **304.98 ns** | **112,916.8 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.2441** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     316.4 ns |      2.52 ns |      3.77 ns |     316.6 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 115,535.3 ns |    208.43 ns |    305.52 ns | 115,587.7 ns | 1.023 |    0.00 |    2 | 0.3662 | 0.2441 |    6853 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 112,873.5 ns |    165.20 ns |    231.58 ns | 112,868.4 ns | 0.999 |    0.00 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 113,438.0 ns |    611.04 ns |    895.65 ns | 113,575.5 ns | 1.004 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
|                                                       |            |               |              |              |              |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **149,989.4 ns** |    **337.70 ns** |    **484.32 ns** | **149,994.7 ns** | **1.000** |    **0.00** |    **2** | **1.2207** | **0.9766** |   **22742 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     447.3 ns |      2.42 ns |      3.55 ns |     446.7 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 152,693.7 ns |    396.30 ns |    568.36 ns | 152,702.7 ns | 1.018 |    0.00 |    2 | 1.2207 | 0.9766 |   23053 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 152,209.6 ns |    243.77 ns |    349.60 ns | 152,102.6 ns | 1.015 |    0.00 |    2 | 1.2207 | 0.9766 |   23693 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 166,544.6 ns |    295.72 ns |    442.61 ns | 166,477.0 ns | 1.110 |    0.00 |    3 | 1.2207 | 0.9766 |   22742 B |        1.00 |
|                                                       |            |               |              |              |              |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **139,430.7 ns** |    **196.83 ns** |    **269.42 ns** | **139,383.6 ns** |  **1.00** |    **0.00** |    **2** | **0.9766** | **0.7324** |   **19740 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,411.0 ns |      6.82 ns |     10.21 ns |   1,410.4 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 142,020.1 ns |    479.21 ns |    702.42 ns | 141,894.8 ns |  1.02 |    0.01 |    2 | 0.9766 | 0.7324 |   20052 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 142,583.2 ns |    500.78 ns |    702.02 ns | 142,253.1 ns |  1.02 |    0.01 |    2 | 1.2207 | 0.9766 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 178,112.6 ns | 16,753.90 ns | 23,486.63 ns | 199,796.9 ns |  1.28 |    0.17 |    3 | 0.9766 | 0.7324 |   19740 B |        1.00 |
|                                                       |            |               |              |              |              |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **583,380.5 ns** |  **2,333.52 ns** |  **3,492.71 ns** | **582,903.7 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,406.3 ns |     46.11 ns |     69.01 ns |   2,404.7 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 587,323.1 ns |  2,907.47 ns |  4,169.81 ns | 587,776.6 ns | 1.007 |    0.01 |    2 | 8.7891 | 3.9063 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 591,023.9 ns |  2,129.14 ns |  3,120.87 ns | 591,038.4 ns | 1.013 |    0.01 |    2 | 8.7891 | 3.9063 |  155687 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 881,681.8 ns |  3,114.68 ns |  4,661.91 ns | 882,172.5 ns | 1.511 |    0.01 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
