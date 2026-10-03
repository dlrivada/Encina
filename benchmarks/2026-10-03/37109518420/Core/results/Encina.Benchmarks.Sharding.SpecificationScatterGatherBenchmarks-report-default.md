
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **20,258.3 ns** |   **7,549.68 ns** |    **413.82 ns** | **1.000** |    **0.02** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     172.8 ns |     113.81 ns |      6.24 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  20,000.3 ns |   6,494.04 ns |    355.96 ns | 0.988 |    0.02 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  19,861.9 ns |   1,648.40 ns |     90.35 ns | 0.981 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  20,569.5 ns |   3,321.87 ns |    182.08 ns | 1.016 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **40,446.0 ns** |   **9,383.19 ns** |    **514.32 ns** | **1.000** |    **0.02** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     238.9 ns |      19.94 ns |      1.09 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  44,562.2 ns |  38,425.95 ns |  2,106.26 ns | 1.102 |    0.05 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  42,835.4 ns |  26,872.07 ns |  1,472.95 ns | 1.059 |    0.03 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  52,031.7 ns |  38,048.08 ns |  2,085.54 ns | 1.287 |    0.05 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **34,867.7 ns** |   **1,438.50 ns** |     **78.85 ns** |  **1.00** |    **0.00** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     794.1 ns |     271.30 ns |     14.87 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  36,198.9 ns |   5,239.51 ns |    287.20 ns |  1.04 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  35,693.7 ns |   3,480.81 ns |    190.80 ns |  1.02 |    0.01 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  45,005.3 ns |  44,983.03 ns |  2,465.67 ns |  1.29 |    0.06 |    3 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **302,147.3 ns** |  **29,306.97 ns** |  **1,606.41 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,215.7 ns |   1,037.84 ns |     56.89 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 321,161.6 ns | 286,386.67 ns | 15,697.82 ns | 1.063 |    0.05 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 302,288.2 ns |  26,803.42 ns |  1,469.19 ns | 1.000 |    0.01 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 466,350.2 ns | 187,872.17 ns | 10,297.91 ns | 1.543 |    0.03 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
