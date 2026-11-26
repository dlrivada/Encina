```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **80,600.6 ns** |  **9,539.25 ns** |   **522.88 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     263.8 ns |     14.11 ns |     0.77 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  81,913.7 ns |  8,161.12 ns |   447.34 ns | 1.016 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  80,164.3 ns |  9,466.13 ns |   518.87 ns | 0.995 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  78,218.1 ns |  5,930.80 ns |   325.09 ns | 0.970 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **111,440.6 ns** |  **6,299.46 ns** |   **345.29 ns** | **1.000** |    **0.00** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     375.5 ns |    316.41 ns |    17.34 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 114,632.0 ns | 10,331.33 ns |   566.30 ns | 1.029 |    0.01 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 115,407.9 ns | 37,029.32 ns | 2,029.70 ns | 1.036 |    0.02 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 123,710.3 ns |  5,608.28 ns |   307.41 ns | 1.110 |    0.00 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **102,833.8 ns** |  **4,145.11 ns** |   **227.21 ns** |  **1.00** |    **0.00** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,183.0 ns |    609.24 ns |    33.39 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 106,172.3 ns |  4,529.51 ns |   248.28 ns |  1.03 |    0.00 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 105,558.3 ns |  4,484.80 ns |   245.83 ns |  1.03 |    0.00 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 113,968.1 ns | 10,706.89 ns |   586.88 ns |  1.11 |    0.01 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **479,136.7 ns** | **23,952.87 ns** | **1,312.94 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,958.3 ns |  1,362.86 ns |    74.70 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 478,544.8 ns | 14,085.09 ns |   772.05 ns | 0.999 |    0.00 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 484,055.0 ns | 79,027.96 ns | 4,331.79 ns | 1.010 |    0.01 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 683,293.7 ns | 25,964.17 ns | 1,423.18 ns | 1.426 |    0.00 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
