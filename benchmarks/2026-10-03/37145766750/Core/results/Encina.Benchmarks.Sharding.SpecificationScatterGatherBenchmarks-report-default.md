
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **19,771.2 ns** |  **4,878.71 ns** |   **267.42 ns** | **1.000** |    **0.02** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     168.9 ns |     25.60 ns |     1.40 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  22,397.2 ns | 34,929.26 ns | 1,914.59 ns | 1.133 |    0.08 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  20,388.0 ns |  7,173.19 ns |   393.19 ns | 1.031 |    0.02 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  21,680.7 ns | 22,250.43 ns | 1,219.62 ns | 1.097 |    0.05 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **43,034.0 ns** |  **6,311.93 ns** |   **345.98 ns** | **1.000** |    **0.01** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     236.7 ns |      4.33 ns |     0.24 ns | 0.006 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  44,434.1 ns |  8,707.96 ns |   477.31 ns | 1.033 |    0.01 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  43,304.0 ns | 43,488.54 ns | 2,383.75 ns | 1.006 |    0.05 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  51,699.4 ns | 12,380.91 ns |   678.64 ns | 1.201 |    0.02 |    2 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **36,706.9 ns** |  **4,548.39 ns** |   **249.31 ns** |  **1.00** |    **0.01** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     775.0 ns |    252.68 ns |    13.85 ns |  0.02 |    0.00 |    1 | 0.1268 | 0.0010 |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  37,276.3 ns |  2,619.62 ns |   143.59 ns |  1.02 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  36,807.7 ns |    911.67 ns |    49.97 ns |  1.00 |    0.01 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  43,519.0 ns |  4,186.21 ns |   229.46 ns |  1.19 |    0.01 |    2 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **293,613.6 ns** | **21,584.73 ns** | **1,183.13 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **3.9063** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,181.9 ns |     85.23 ns |     4.67 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 303,749.8 ns | 50,890.70 ns | 2,789.49 ns | 1.035 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 286,316.0 ns | 22,151.62 ns | 1,214.20 ns | 0.975 |    0.00 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 457,360.7 ns | 54,953.27 ns | 3,012.17 ns | 1.558 |    0.01 |    3 | 8.7891 | 4.3945 |  154735 B |        1.00 |
