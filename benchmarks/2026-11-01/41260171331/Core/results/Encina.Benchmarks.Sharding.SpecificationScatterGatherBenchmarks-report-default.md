
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error       | StdDev      | Median       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|------------:|------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **79,047.2 ns** |   **368.25 ns** |   **551.17 ns** |  **79,098.7 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     258.6 ns |     3.24 ns |     4.85 ns |     256.9 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  81,668.2 ns |   209.81 ns |   294.12 ns |  81,637.8 ns | 1.033 |    0.01 |    3 | 0.3662 | 0.2441 |    6853 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  78,714.4 ns |   261.14 ns |   374.51 ns |  78,648.6 ns | 0.996 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  79,181.6 ns |   304.00 ns |   416.11 ns |  79,203.5 ns | 1.002 |    0.01 |    2 | 0.3662 | 0.2441 |    6542 B |        1.00 |
                                                       |            |               |              |             |             |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **114,983.3 ns** |   **217.18 ns** |   **318.34 ns** | **114,975.4 ns** | **1.000** |    **0.00** |    **2** | **1.3428** | **1.2207** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     349.3 ns |     4.17 ns |     5.98 ns |     347.7 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 114,824.3 ns |   236.43 ns |   331.44 ns | 114,835.5 ns | 0.999 |    0.00 |    2 | 1.3428 | 1.2207 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 115,688.4 ns |   443.69 ns |   636.33 ns | 115,745.1 ns | 1.006 |    0.01 |    2 | 1.3428 | 1.2207 |   23695 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 125,293.7 ns |   179.97 ns |   258.11 ns | 125,247.9 ns | 1.090 |    0.00 |    3 | 1.2207 | 0.9766 |   22742 B |        1.00 |
                                                       |            |               |              |             |             |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **104,586.8 ns** |   **357.33 ns** |   **500.92 ns** | **104,727.4 ns** |  **1.00** |    **0.01** |    **2** | **1.0986** | **0.9766** |   **19742 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,180.9 ns |     9.56 ns |    13.40 ns |   1,181.2 ns |  0.01 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 106,540.5 ns |   238.27 ns |   356.64 ns | 106,532.7 ns |  1.02 |    0.01 |    2 | 1.0986 | 0.9766 |   20054 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 107,317.0 ns |   862.38 ns | 1,236.80 ns | 106,645.5 ns |  1.03 |    0.01 |    2 | 1.2207 | 1.0986 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 115,921.1 ns | 1,137.66 ns | 1,667.57 ns | 115,122.2 ns |  1.11 |    0.02 |    3 | 1.0986 | 0.9766 |   19742 B |        1.00 |
                                                       |            |               |              |             |             |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **504,464.4 ns** | **3,140.53 ns** | **4,603.35 ns** | **507,227.6 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,954.8 ns |    28.97 ns |    42.47 ns |   1,971.5 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 502,317.4 ns | 1,963.28 ns | 2,877.74 ns | 502,482.9 ns | 0.996 |    0.01 |    2 | 8.7891 | 3.9063 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 504,521.1 ns | 1,613.40 ns | 2,261.76 ns | 505,394.2 ns | 1.000 |    0.01 |    2 | 8.7891 | 3.9063 |  155687 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 732,201.4 ns | 1,039.80 ns | 1,524.12 ns | 731,974.1 ns | 1.452 |    0.01 |    3 | 8.7891 | 3.9063 |  154735 B |        1.00 |
