```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean           | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |---------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |    **43,630.3 ns** |  **5,302.13 ns** |   **290.63 ns** | **1.000** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |       371.0 ns |     11.84 ns |     0.65 ns | 0.009 |    1 | 0.0043 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |    44,484.8 ns |  1,837.28 ns |   100.71 ns | 1.020 |    2 | 0.0610 |      - |    6850 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |    44,124.9 ns |  2,950.04 ns |   161.70 ns | 1.011 |    2 | 0.0610 |      - |    6539 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |    45,142.0 ns |  2,500.13 ns |   137.04 ns | 1.035 |    2 | 0.0610 |      - |    6539 B |        1.00 |
|                                                       |            |               |                |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |   **100,430.8 ns** |  **2,979.49 ns** |   **163.32 ns** | **1.000** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |       649.7 ns |     29.26 ns |     1.60 ns | 0.006 |    1 | 0.0296 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |   103,365.1 ns |  3,237.68 ns |   177.47 ns | 1.029 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |    99,185.2 ns |  1,157.87 ns |    63.47 ns | 0.988 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |   116,703.7 ns | 16,197.44 ns |   887.84 ns | 1.162 |    2 | 0.2441 |      - |   22734 B |        1.00 |
|                                                       |            |               |                |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |    **81,678.4 ns** |  **3,124.88 ns** |   **171.29 ns** |  **1.00** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |     1,762.0 ns |     47.26 ns |     2.59 ns |  0.02 |    1 | 0.0248 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |    80,638.0 ns |  2,299.38 ns |   126.04 ns |  0.99 |    2 | 0.1221 |      - |   20036 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |    84,706.5 ns |  2,395.27 ns |   131.29 ns |  1.04 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |    99,601.0 ns | 10,690.20 ns |   585.97 ns |  1.22 |    2 | 0.1221 |      - |   19724 B |        1.00 |
|                                                       |            |               |                |              |             |       |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           |   **694,023.3 ns** | **96,740.65 ns** | **5,302.68 ns** | **1.000** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |     3,847.5 ns |    671.48 ns |    36.81 ns | 0.006 |    1 | 0.2365 | 0.0153 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           |   695,682.0 ns | 58,496.79 ns | 3,206.41 ns | 1.002 |    2 | 0.9766 |      - |  155029 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           |   691,508.0 ns | 46,856.46 ns | 2,568.36 ns | 0.996 |    2 | 0.9766 |      - |  155669 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 1,051,241.9 ns | 36,538.75 ns | 2,002.81 ns | 1.515 |    3 |      - |      - |  154704 B |        1.00 |
