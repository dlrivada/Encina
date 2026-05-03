```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.203
  [Host]    : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.7 (10.0.7, 10.0.726.21808), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev       | Median       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|-------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **116,706.9 ns** |    **721.03 ns** |  **1,010.79 ns** | **116,668.8 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     316.9 ns |      2.48 ns |      3.63 ns |     316.1 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 119,365.7 ns |    771.25 ns |  1,154.37 ns | 119,168.7 ns | 1.023 |    0.01 |    2 | 0.3662 | 0.2441 |    6853 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 116,510.6 ns |    738.87 ns |  1,083.03 ns | 116,122.2 ns | 0.998 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 118,296.1 ns |    624.53 ns |    915.42 ns | 118,358.3 ns | 1.014 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
|                                                       |            |               |              |              |              |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **155,656.0 ns** |    **843.72 ns** |  **1,210.04 ns** | **155,445.9 ns** | **1.000** |    **0.01** |    **2** | **1.2207** | **0.9766** |   **22742 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     441.8 ns |      3.98 ns |      5.71 ns |     439.8 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 156,378.5 ns |  2,316.01 ns |  3,466.49 ns | 154,466.2 ns | 1.005 |    0.02 |    2 | 1.2207 | 0.9766 |   23053 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 154,061.4 ns |    426.81 ns |    612.12 ns | 154,074.4 ns | 0.990 |    0.01 |    2 | 1.2207 | 0.9766 |   23693 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 166,805.7 ns |    400.59 ns |    561.57 ns | 166,777.7 ns | 1.072 |    0.01 |    3 | 1.2207 | 0.9766 |   22742 B |        1.00 |
|                                                       |            |               |              |              |              |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **143,980.7 ns** |    **521.51 ns** |    **764.42 ns** | **143,917.6 ns** | **1.000** |    **0.01** |    **2** | **0.9766** | **0.7324** |   **19740 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,371.8 ns |      9.45 ns |     14.15 ns |   1,374.8 ns | 0.010 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 145,977.5 ns |    728.87 ns |  1,090.94 ns | 145,851.3 ns | 1.014 |    0.01 |    2 | 0.9766 | 0.7324 |   20052 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 145,630.5 ns |    688.53 ns |    987.46 ns | 145,315.4 ns | 1.011 |    0.01 |    2 | 1.2207 | 0.9766 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 158,119.4 ns |    656.49 ns |    962.28 ns | 158,209.9 ns | 1.098 |    0.01 |    3 | 0.9766 | 0.7324 |   19740 B |        1.00 |
|                                                       |            |               |              |              |              |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **600,926.1 ns** | **18,920.69 ns** | **25,898.86 ns** | **600,509.0 ns** | **1.002** |    **0.06** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,468.6 ns |     89.66 ns |    125.69 ns |   2,415.0 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 581,682.2 ns |  2,506.55 ns |  3,751.69 ns | 582,735.5 ns | 0.970 |    0.04 |    2 | 8.7891 | 3.9063 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 575,501.0 ns |  1,589.90 ns |  2,330.45 ns | 575,264.3 ns | 0.959 |    0.04 |    2 | 8.7891 | 3.9063 |  155687 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 880,146.9 ns | 15,736.86 ns | 21,008.26 ns | 891,073.7 ns | 1.467 |    0.07 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
