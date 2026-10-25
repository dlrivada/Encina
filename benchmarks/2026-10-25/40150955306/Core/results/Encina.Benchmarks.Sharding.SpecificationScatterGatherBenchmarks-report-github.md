```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error       | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **79,797.8 ns** |   **338.27 ns** |   **474.20 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     255.2 ns |     3.14 ns |     4.60 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  82,205.1 ns |   341.93 ns |   501.19 ns | 1.030 |    0.01 |    3 | 0.3662 | 0.2441 |    6853 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  79,299.1 ns |   703.51 ns | 1,008.96 ns | 0.994 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  79,708.5 ns |   284.34 ns |   416.78 ns | 0.999 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
|                                                       |            |               |              |             |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **112,912.3 ns** |   **946.54 ns** | **1,387.42 ns** | **1.000** |    **0.02** |    **2** | **1.3428** | **1.2207** |   **22744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     361.2 ns |     4.75 ns |     7.10 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 115,703.4 ns |   720.61 ns |   986.38 ns | 1.025 |    0.02 |    2 | 1.3428 | 1.2207 |   23055 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 116,196.6 ns |   750.17 ns | 1,051.64 ns | 1.029 |    0.02 |    2 | 1.3428 | 1.2207 |   23695 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 125,415.8 ns |   874.17 ns | 1,225.46 ns | 1.111 |    0.02 |    3 | 1.2207 | 0.9766 |   22742 B |        1.00 |
|                                                       |            |               |              |             |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **103,552.0 ns** |   **319.76 ns** |   **437.70 ns** |  **1.00** |    **0.01** |    **2** | **1.0986** | **0.9766** |   **19742 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,162.9 ns |    11.43 ns |    17.11 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 105,129.5 ns |   491.30 ns |   688.74 ns |  1.02 |    0.01 |    2 | 1.0986 | 0.9766 |   20054 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 105,790.5 ns |   561.93 ns |   805.90 ns |  1.02 |    0.01 |    2 | 1.2207 | 1.0986 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 114,969.5 ns |   368.07 ns |   491.37 ns |  1.11 |    0.01 |    3 | 1.0986 | 0.9766 |   19742 B |        1.00 |
|                                                       |            |               |              |             |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **496,150.1 ns** | **4,276.43 ns** | **5,853.63 ns** | **1.000** |    **0.02** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,969.7 ns |    38.16 ns |    57.11 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 500,551.2 ns | 4,102.63 ns | 6,013.59 ns | 1.009 |    0.02 |    2 | 8.7891 | 3.9063 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 495,710.5 ns | 1,095.61 ns | 1,571.29 ns | 0.999 |    0.01 |    2 | 8.7891 | 3.9063 |  155687 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 730,029.8 ns | 1,932.86 ns | 2,709.60 ns | 1.472 |    0.02 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
