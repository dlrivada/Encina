```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            | **113,857.4 ns** |   **2,864.55 ns** |    **157.02 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     327.0 ns |      44.36 ns |      2.43 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            | 118,279.0 ns |   5,015.98 ns |    274.94 ns | 1.039 |    0.00 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            | 114,827.4 ns |   9,787.01 ns |    536.46 ns | 1.009 |    0.00 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            | 115,101.3 ns |   5,889.95 ns |    322.85 ns | 1.011 |    0.00 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           | **153,497.7 ns** |  **19,596.53 ns** |  **1,074.15 ns** | **1.000** |    **0.01** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     473.8 ns |     188.84 ns |     10.35 ns | 0.003 |    0.00 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           | 154,320.5 ns |   2,548.97 ns |    139.72 ns | 1.005 |    0.01 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           | 153,216.6 ns |  20,981.20 ns |  1,150.05 ns | 0.998 |    0.01 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           | 167,853.5 ns |   6,761.29 ns |    370.61 ns | 1.094 |    0.01 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            | **141,050.2 ns** |   **5,607.34 ns** |    **307.36 ns** |  **1.00** |    **0.00** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |   1,471.0 ns |     126.12 ns |      6.91 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            | 143,692.3 ns |  11,936.16 ns |    654.26 ns |  1.02 |    0.00 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            | 145,400.9 ns |  10,885.57 ns |    596.67 ns |  1.03 |    0.00 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            | 156,196.9 ns |   1,629.03 ns |     89.29 ns |  1.11 |    0.00 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **592,377.4 ns** |  **51,581.21 ns** |  **2,827.34 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   2,636.6 ns |     462.55 ns |     25.35 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 601,193.0 ns | 230,674.39 ns | 12,644.04 ns | 1.015 |    0.02 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 605,708.6 ns |  34,343.98 ns |  1,882.51 ns | 1.023 |    0.01 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 879,243.3 ns |  42,311.76 ns |  2,319.25 ns | 1.484 |    0.01 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
