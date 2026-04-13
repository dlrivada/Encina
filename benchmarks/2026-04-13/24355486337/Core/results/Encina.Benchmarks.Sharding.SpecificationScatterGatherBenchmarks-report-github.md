```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **115,455.9 ns** |   **548.32 ns** |   **820.70 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     338.9 ns |     2.11 ns |     3.10 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 117,264.6 ns |   637.69 ns |   934.72 ns | 1.016 |    0.01 |    2 | 0.3662 | 0.2441 |    6853 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 116,068.6 ns |   410.31 ns |   614.13 ns | 1.005 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 116,834.5 ns | 1,091.28 ns | 1,633.38 ns | 1.012 |    0.02 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
|                                                       |            |               |              |             |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **155,147.6 ns** |   **544.28 ns** |   **797.80 ns** | **1.000** |    **0.01** |    **2** | **1.2207** | **0.9766** |   **22742 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     486.0 ns |     9.81 ns |    14.38 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 155,025.2 ns |   625.18 ns |   916.38 ns | 0.999 |    0.01 |    2 | 1.2207 | 0.9766 |   23053 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 156,574.7 ns |   425.52 ns |   623.72 ns | 1.009 |    0.01 |    2 | 1.2207 | 0.9766 |   23693 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 169,450.4 ns |   493.20 ns |   707.33 ns | 1.092 |    0.01 |    3 | 1.2207 | 0.9766 |   22742 B |        1.00 |
|                                                       |            |               |              |             |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **141,845.2 ns** | **1,258.61 ns** | **1,844.85 ns** | **1.000** |    **0.02** |    **2** | **0.9766** | **0.7324** |   **19740 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,407.6 ns |    10.06 ns |    15.06 ns | 0.010 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 144,715.9 ns |   601.31 ns |   862.38 ns | 1.020 |    0.01 |    2 | 0.9766 | 0.7324 |   20052 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 145,264.5 ns |   613.71 ns |   918.57 ns | 1.024 |    0.01 |    2 | 1.2207 | 0.9766 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 157,952.2 ns |   501.75 ns |   735.46 ns | 1.114 |    0.02 |    3 | 0.9766 | 0.7324 |   19740 B |        1.00 |
|                                                       |            |               |              |             |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **589,992.2 ns** | **2,498.70 ns** | **3,662.56 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,592.3 ns |    55.02 ns |    80.65 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 593,638.1 ns | 1,978.02 ns | 2,836.82 ns | 1.006 |    0.01 |    2 | 8.7891 | 3.9063 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 598,782.4 ns | 1,164.14 ns | 1,706.38 ns | 1.015 |    0.01 |    2 | 8.7891 | 3.9063 |  155687 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 890,111.7 ns | 2,221.10 ns | 3,255.66 ns | 1.509 |    0.01 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
