```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **21,080.2 ns** |   **3,258.96 ns** |   **178.63 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     178.8 ns |      46.07 ns |     2.53 ns | 0.008 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  21,459.3 ns |   8,544.03 ns |   468.33 ns | 1.018 |    0.02 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  20,868.2 ns |   5,811.09 ns |   318.53 ns | 0.990 |    0.01 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  21,022.0 ns |   7,011.47 ns |   384.32 ns | 0.997 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **44,633.7 ns** |  **11,873.59 ns** |   **650.83 ns** | **1.000** |    **0.02** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     262.6 ns |     356.67 ns |    19.55 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  44,681.7 ns |   4,608.49 ns |   252.61 ns | 1.001 |    0.01 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  45,776.8 ns |   2,768.98 ns |   151.78 ns | 1.026 |    0.01 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  53,156.0 ns |   4,602.23 ns |   252.26 ns | 1.191 |    0.02 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **39,069.2 ns** |  **33,093.20 ns** | **1,813.95 ns** |  **1.00** |    **0.06** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |     821.7 ns |     102.03 ns |     5.59 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  37,679.8 ns |   4,560.08 ns |   249.95 ns |  0.97 |    0.04 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  37,411.9 ns |  16,906.16 ns |   926.68 ns |  0.96 |    0.04 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  45,808.4 ns |   8,850.85 ns |   485.14 ns |  1.17 |    0.05 |    2 | 1.1597 | 1.0986 |   19744 B |        1.00 |
|                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **311,564.6 ns** |  **42,051.81 ns** | **2,305.00 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,233.3 ns |     156.15 ns |     8.56 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 310,640.9 ns |  32,813.28 ns | 1,798.61 ns | 0.997 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 313,760.1 ns |  32,060.72 ns | 1,757.36 ns | 1.007 |    0.01 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 480,441.6 ns | 141,287.22 ns | 7,744.43 ns | 1.542 |    0.02 |    3 | 8.7891 | 4.3945 |  154735 B |        1.00 |
