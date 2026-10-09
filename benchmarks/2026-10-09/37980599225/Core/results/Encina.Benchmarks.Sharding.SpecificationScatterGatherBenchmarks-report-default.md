
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **21,347.7 ns** |   **1,543.20 ns** |     **84.59 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     194.6 ns |     262.96 ns |     14.41 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  22,499.7 ns |   2,732.79 ns |    149.79 ns | 1.054 |    0.01 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  21,769.1 ns |   7,780.39 ns |    426.47 ns | 1.020 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  21,955.2 ns |   3,978.12 ns |    218.05 ns | 1.028 |    0.01 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **46,177.4 ns** |   **3,459.60 ns** |    **189.63 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     278.8 ns |     278.25 ns |     15.25 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  46,850.3 ns |  14,804.73 ns |    811.50 ns | 1.015 |    0.02 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  47,525.9 ns |  14,855.69 ns |    814.29 ns | 1.029 |    0.02 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  56,671.8 ns |  23,764.50 ns |  1,302.61 ns | 1.227 |    0.02 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **39,413.2 ns** |   **3,695.23 ns** |    **202.55 ns** |  **1.00** |    **0.01** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     886.5 ns |      82.87 ns |      4.54 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  42,191.9 ns |  38,463.06 ns |  2,108.29 ns |  1.07 |    0.05 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  41,491.8 ns |  27,676.91 ns |  1,517.06 ns |  1.05 |    0.03 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  46,442.0 ns |   5,543.91 ns |    303.88 ns |  1.18 |    0.01 |    2 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **318,955.3 ns** | **144,131.60 ns** |  **7,900.34 ns** | **1.000** |    **0.03** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,335.6 ns |   2,125.36 ns |    116.50 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 321,994.3 ns | 242,159.86 ns | 13,273.60 ns | 1.010 |    0.04 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 302,770.1 ns | 125,883.70 ns |  6,900.11 ns | 0.950 |    0.03 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 469,960.9 ns |  37,861.26 ns |  2,075.30 ns | 1.474 |    0.03 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
