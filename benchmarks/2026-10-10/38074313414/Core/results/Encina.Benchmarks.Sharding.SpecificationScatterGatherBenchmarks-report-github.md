```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **78,361.4 ns** |  **8,541.71 ns** |   **468.20 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     261.0 ns |     47.66 ns |     2.61 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  80,027.1 ns |  5,965.63 ns |   327.00 ns | 1.021 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  78,216.5 ns |  6,997.75 ns |   383.57 ns | 0.998 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  77,832.9 ns |  6,063.23 ns |   332.35 ns | 0.993 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **111,837.6 ns** |  **9,430.41 ns** |   **516.91 ns** | **1.000** |    **2** | **1.3428** | **1.2207** |   **22736 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     347.7 ns |     85.34 ns |     4.68 ns | 0.003 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 112,174.0 ns |  5,909.85 ns |   323.94 ns | 1.003 |    2 | 1.3428 | 1.2207 |   23047 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 112,869.7 ns |  7,635.58 ns |   418.53 ns | 1.009 |    2 | 1.3428 | 1.2207 |   23687 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 123,186.1 ns | 16,872.46 ns |   924.84 ns | 1.101 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **103,083.9 ns** |  **4,562.70 ns** |   **250.10 ns** |  **1.00** |    **2** | **1.0986** | **0.9766** |   **19734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,231.7 ns |    834.58 ns |    45.75 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 103,210.7 ns |  7,610.57 ns |   417.16 ns |  1.00 |    2 | 1.0986 | 0.9766 |   20046 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 103,993.2 ns | 13,252.63 ns |   726.42 ns |  1.01 |    2 | 1.2207 | 1.0986 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 111,832.5 ns |  3,943.80 ns |   216.17 ns |  1.08 |    2 | 1.0986 | 0.9766 |   19734 B |        1.00 |
|                                                       |            |               |              |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **472,875.8 ns** |  **4,245.47 ns** |   **232.71 ns** | **1.000** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,976.1 ns |    218.61 ns |    11.98 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 473,223.3 ns | 24,416.37 ns | 1,338.34 ns | 1.001 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 475,437.0 ns | 19,453.77 ns | 1,066.33 ns | 1.005 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 690,736.5 ns | 33,623.01 ns | 1,842.99 ns | 1.461 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
