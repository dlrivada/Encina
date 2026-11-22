
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]    : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error       | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **80,388.9 ns** |   **665.43 ns** |    **995.98 ns** | **1.000** |    **0.02** |    **2** | **0.3662** | **0.2441** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     247.9 ns |     1.91 ns |      2.85 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  83,969.5 ns | 1,588.76 ns |  2,377.98 ns | 1.045 |    0.03 |    2 | 0.3662 | 0.2441 |    6853 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  80,213.4 ns |   887.18 ns |  1,300.41 ns | 0.998 |    0.02 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  80,512.0 ns | 1,330.27 ns |  1,820.89 ns | 1.002 |    0.03 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
                                                       |            |               |              |             |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **112,632.0 ns** |   **294.42 ns** |    **440.67 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2207** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     343.8 ns |     2.95 ns |      4.32 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 115,662.0 ns | 1,480.67 ns |  2,170.34 ns | 1.027 |    0.02 |    2 | 1.3428 | 1.2207 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 118,860.9 ns | 1,337.82 ns |  2,002.38 ns | 1.055 |    0.02 |    2 | 1.3428 | 1.2207 |   23695 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 127,447.9 ns | 1,884.16 ns |  2,820.12 ns | 1.132 |    0.03 |    3 | 1.2207 | 0.9766 |   22742 B |        1.00 |
                                                       |            |               |              |             |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **104,252.3 ns** | **1,408.24 ns** |  **2,019.66 ns** |  **1.00** |    **0.03** |    **2** | **1.0986** | **0.9766** |   **19742 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,215.7 ns |    29.49 ns |     44.14 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 109,582.9 ns | 1,779.73 ns |  2,608.70 ns |  1.05 |    0.03 |    2 | 1.0986 | 0.9766 |   20054 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 108,030.8 ns |   681.86 ns |    999.45 ns |  1.04 |    0.02 |    2 | 1.2207 | 1.0986 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 120,479.0 ns | 2,516.81 ns |  3,767.04 ns |  1.16 |    0.04 |    3 | 1.0986 | 0.9766 |   19742 B |        1.00 |
                                                       |            |               |              |             |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **502,000.2 ns** | **5,609.01 ns** |  **8,221.62 ns** | **1.000** |    **0.02** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,961.2 ns |    33.12 ns |     48.55 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 490,849.7 ns | 8,875.88 ns | 13,010.15 ns | 0.978 |    0.03 |    2 | 8.7891 | 3.9063 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 498,276.3 ns | 3,501.60 ns |  5,132.60 ns | 0.993 |    0.02 |    2 | 8.7891 | 3.9063 |  155687 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 696,565.8 ns | 5,391.28 ns |  7,732.02 ns | 1.388 |    0.03 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
