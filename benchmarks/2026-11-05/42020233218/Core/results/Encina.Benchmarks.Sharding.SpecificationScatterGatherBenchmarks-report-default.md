
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.74GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **115,741.6 ns** |  **10,762.26 ns** |    **589.92 ns** | **1.000** |    **0.01** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     315.2 ns |      98.93 ns |      5.42 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 120,278.9 ns |  21,559.25 ns |  1,181.74 ns | 1.039 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 116,391.8 ns |  18,448.67 ns |  1,011.23 ns | 1.006 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 118,888.8 ns |  20,926.59 ns |  1,147.06 ns | 1.027 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **158,680.9 ns** |  **38,149.28 ns** |  **2,091.09 ns** | **1.000** |    **0.02** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     462.3 ns |      81.36 ns |      4.46 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 159,480.4 ns |  39,454.59 ns |  2,162.64 ns | 1.005 |    0.02 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 157,998.4 ns | 125,775.09 ns |  6,894.16 ns | 0.996 |    0.04 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 172,720.5 ns | 103,381.95 ns |  5,666.71 ns | 1.089 |    0.03 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **150,804.0 ns** | **101,324.65 ns** |  **5,553.95 ns** | **1.001** |    **0.05** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,457.9 ns |     825.98 ns |     45.27 ns | 0.010 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 149,717.3 ns |  81,469.22 ns |  4,465.60 ns | 0.994 |    0.04 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 149,881.7 ns | 121,308.44 ns |  6,649.32 ns | 0.995 |    0.05 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 162,380.7 ns | 140,682.27 ns |  7,711.27 ns | 1.078 |    0.06 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **602,916.7 ns** | **192,249.02 ns** | **10,537.82 ns** | **1.000** |    **0.02** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,427.0 ns |   1,083.20 ns |     59.37 ns | 0.004 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 607,051.1 ns |  74,718.31 ns |  4,095.56 ns | 1.007 |    0.02 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 619,097.4 ns | 116,119.69 ns |  6,364.91 ns | 1.027 |    0.02 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 912,154.6 ns |  65,129.04 ns |  3,569.94 ns | 1.513 |    0.02 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
