```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error       | StdDev       | Median       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|------------:|-------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **113,128.4 ns** |   **552.59 ns** |    **809.98 ns** | **113,140.9 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     319.4 ns |     2.18 ns |      3.19 ns |     319.0 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 115,647.6 ns |   259.62 ns |    380.55 ns | 115,699.7 ns | 1.022 |    0.01 |    2 | 0.3662 | 0.2441 |    6853 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 112,738.5 ns |   433.09 ns |    648.22 ns | 112,807.8 ns | 0.997 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 113,778.2 ns |   322.36 ns |    451.91 ns | 113,850.1 ns | 1.006 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
|                                                       |            |               |              |             |              |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **150,478.6 ns** |   **321.07 ns** |    **480.56 ns** | **150,450.4 ns** | **1.000** |    **0.00** |    **2** | **1.2207** | **0.9766** |   **22742 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     460.6 ns |     7.37 ns |     10.80 ns |     460.3 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 153,151.8 ns |   552.87 ns |    827.50 ns | 153,121.1 ns | 1.018 |    0.01 |    2 | 1.2207 | 0.9766 |   23053 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 151,909.8 ns |   314.06 ns |    460.34 ns | 151,882.5 ns | 1.010 |    0.00 |    2 | 1.2207 | 0.9766 |   23693 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 167,190.9 ns |   267.82 ns |    384.10 ns | 167,121.7 ns | 1.111 |    0.00 |    3 | 1.2207 | 0.9766 |   22742 B |        1.00 |
|                                                       |            |               |              |             |              |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **139,290.5 ns** |   **361.32 ns** |    **540.81 ns** | **139,259.6 ns** |  **1.00** |    **0.01** |    **2** | **0.9766** | **0.7324** |   **19740 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,470.3 ns |    62.07 ns |     90.99 ns |   1,400.0 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 141,885.4 ns |   286.48 ns |    410.86 ns | 141,844.2 ns |  1.02 |    0.00 |    2 | 0.9766 | 0.7324 |   20052 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 143,666.7 ns |   170.68 ns |    244.79 ns | 143,667.0 ns |  1.03 |    0.00 |    2 | 1.2207 | 0.9766 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 154,540.3 ns |   373.44 ns |    547.38 ns | 154,529.4 ns |  1.11 |    0.01 |    3 | 0.9766 | 0.7324 |   19740 B |        1.00 |
|                                                       |            |               |              |             |              |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **587,347.0 ns** | **1,456.37 ns** |  **2,134.73 ns** | **586,850.4 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,427.4 ns |    23.46 ns |     35.11 ns |   2,426.9 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 588,859.5 ns |   803.00 ns |  1,177.03 ns | 588,966.9 ns | 1.003 |    0.00 |    2 | 8.7891 | 3.9063 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 589,271.7 ns | 1,212.72 ns |  1,815.14 ns | 589,271.2 ns | 1.003 |    0.00 |    2 | 8.7891 | 3.9063 |  155687 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 908,226.7 ns | 6,852.50 ns | 10,044.31 ns | 915,542.3 ns | 1.546 |    0.02 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
