
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.33GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **22,248.8 ns** |  **16,132.17 ns** |   **884.26 ns** | **1.001** |    **0.05** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     188.3 ns |      46.50 ns |     2.55 ns | 0.008 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  22,678.0 ns |  11,972.24 ns |   656.24 ns | 1.020 |    0.04 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  21,546.7 ns |   9,074.99 ns |   497.43 ns | 0.969 |    0.04 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  21,697.0 ns |   1,070.75 ns |    58.69 ns | 0.976 |    0.03 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **45,777.3 ns** |   **7,773.88 ns** |   **426.11 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     287.6 ns |     169.15 ns |     9.27 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  47,543.9 ns |   5,400.63 ns |   296.03 ns | 1.039 |    0.01 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  47,125.1 ns |   4,483.32 ns |   245.75 ns | 1.030 |    0.01 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  54,842.9 ns |  11,261.06 ns |   617.26 ns | 1.198 |    0.02 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **40,398.9 ns** |  **20,375.11 ns** | **1,116.83 ns** |  **1.00** |    **0.03** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     919.1 ns |     219.79 ns |    12.05 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  40,129.9 ns |   3,823.46 ns |   209.58 ns |  0.99 |    0.02 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  41,339.6 ns |  17,942.08 ns |   983.47 ns |  1.02 |    0.03 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  47,697.9 ns |   9,578.32 ns |   525.02 ns |  1.18 |    0.03 |    2 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |               |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **329,843.3 ns** | **104,271.22 ns** | **5,715.46 ns** | **1.000** |    **0.02** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,535.9 ns |   1,492.02 ns |    81.78 ns | 0.005 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 316,478.3 ns |  23,344.01 ns | 1,279.56 ns | 0.960 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 317,106.2 ns | 105,163.70 ns | 5,764.38 ns | 0.962 |    0.02 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 468,029.9 ns | 120,639.26 ns | 6,612.64 ns | 1.419 |    0.03 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
