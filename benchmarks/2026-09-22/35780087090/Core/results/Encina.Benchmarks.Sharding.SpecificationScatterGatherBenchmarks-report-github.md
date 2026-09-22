```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **35,986.6 ns** |   **8,515.88 ns** |    **466.78 ns** | **1.000** |    **0.02** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     317.1 ns |     131.09 ns |      7.19 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  36,725.4 ns |  22,236.68 ns |  1,218.87 ns | 1.021 |    0.03 |    2 | 0.0610 |      - |    6850 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  35,445.1 ns |   1,027.71 ns |     56.33 ns | 0.985 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  35,808.1 ns |   1,432.22 ns |     78.50 ns | 0.995 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **80,742.1 ns** |   **9,064.01 ns** |    **496.83 ns** | **1.000** |    **0.01** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     462.6 ns |     263.90 ns |     14.47 ns | 0.006 |    0.00 |    1 | 0.0300 |      - |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  81,832.7 ns |   3,029.07 ns |    166.03 ns | 1.014 |    0.01 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  82,500.4 ns |   3,816.04 ns |    209.17 ns | 1.022 |    0.01 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  97,303.0 ns |  21,079.94 ns |  1,155.46 ns | 1.205 |    0.01 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **65,595.3 ns** |  **10,490.57 ns** |    **575.02 ns** |  **1.00** |    **0.01** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,425.5 ns |      18.46 ns |      1.01 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  68,979.1 ns |   1,560.79 ns |     85.55 ns |  1.05 |    0.01 |    2 | 0.1221 |      - |   20036 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  73,558.8 ns |   9,758.59 ns |    534.90 ns |  1.12 |    0.01 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  82,000.4 ns |   3,987.71 ns |    218.58 ns |  1.25 |    0.01 |    2 | 0.1221 |      - |   19724 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **562,344.3 ns** | **143,389.42 ns** |  **7,859.66 ns** | **1.000** |    **0.02** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,473.3 ns |   2,400.65 ns |    131.59 ns | 0.004 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 601,898.3 ns | 570,274.98 ns | 31,258.70 ns | 1.070 |    0.05 |    2 | 0.9766 |      - |  155029 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 578,347.7 ns | 231,415.88 ns | 12,684.69 ns | 1.029 |    0.02 |    2 | 0.9766 |      - |  155669 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 852,056.9 ns |  72,738.09 ns |  3,987.02 ns | 1.515 |    0.02 |    3 | 0.9766 |      - |  154717 B |        1.00 |
