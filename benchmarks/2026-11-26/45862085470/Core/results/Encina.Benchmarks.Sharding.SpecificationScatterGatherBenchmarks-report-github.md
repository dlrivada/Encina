```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.38GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
| Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
|------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **10**            |  **20,181.1 ns** |   **2,829.41 ns** |    **155.09 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 10            |     175.1 ns |      14.74 ns |      0.81 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 10            |  21,444.4 ns |   8,909.02 ns |    488.33 ns | 1.063 |    0.02 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 10            |  21,667.9 ns |   1,831.48 ns |    100.39 ns | 1.074 |    0.01 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 10            |  22,670.7 ns |   3,665.63 ns |    200.93 ns | 1.123 |    0.01 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **3**          | **100**           |  **44,346.6 ns** |   **5,075.68 ns** |    **278.22 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 3          | 100           |     272.9 ns |     160.60 ns |      8.80 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 3          | 100           |  46,068.6 ns |  18,676.12 ns |  1,023.70 ns | 1.039 |    0.02 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 3          | 100           |  47,342.6 ns |  14,919.99 ns |    817.82 ns | 1.068 |    0.02 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
| &#39;MergeAndOrder with descending ordering&#39;              | 3          | 100           |  54,055.9 ns |  22,027.49 ns |  1,207.40 ns | 1.219 |    0.02 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **10**            |  **40,407.0 ns** |  **73,296.63 ns** |  **4,017.64 ns** |  **1.01** |    **0.12** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 10            |     872.3 ns |     267.32 ns |     14.65 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 10            |  39,464.3 ns |   8,660.71 ns |    474.72 ns |  0.98 |    0.08 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 10            |  39,597.2 ns |  10,277.23 ns |    563.33 ns |  0.99 |    0.08 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 10            |  48,853.7 ns |  12,631.77 ns |    692.39 ns |  1.22 |    0.10 |    3 | 1.1597 | 1.0986 |   19744 B |        1.00 |
|                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
| **&#39;MergeAndOrder with ascending ordering&#39;**               | **25**         | **100**           | **321,846.8 ns** |  **44,767.56 ns** |  **2,453.86 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
| &#39;MergeAndOrder without ordering&#39;                      | 25         | 100           |   1,463.9 ns |   1,188.93 ns |     65.17 ns | 0.005 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
| &#39;MergeOrderAndPaginate overfetch (page 1, size 20)&#39;   | 25         | 100           | 333,370.4 ns | 540,652.17 ns | 29,634.97 ns | 1.036 |    0.08 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
| &#39;MergeOrderAndPaginate large page (page 2, size 100)&#39; | 25         | 100           | 310,338.6 ns |  45,125.63 ns |  2,473.49 ns | 0.964 |    0.01 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
| &#39;MergeAndOrder with descending ordering&#39;              | 25         | 100           | 493,913.0 ns | 791,177.62 ns | 43,367.12 ns | 1.535 |    0.12 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
