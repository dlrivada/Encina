
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **23,171.0 ns** | **17,618.10 ns** |   **965.71 ns** | **1.001** |    **0.05** |    **2** | **0.3662** | **0.3357** |    **6542 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     197.9 ns |     55.94 ns |     3.07 ns | 0.009 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  22,846.9 ns |  5,676.20 ns |   311.13 ns | 0.987 |    0.04 |    2 | 0.3967 | 0.3662 |    6855 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  23,514.7 ns |  9,472.93 ns |   519.24 ns | 1.016 |    0.04 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  23,470.3 ns |  5,636.77 ns |   308.97 ns | 1.014 |    0.04 |    2 | 0.3662 | 0.3357 |    6542 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **49,346.7 ns** | **29,001.18 ns** | **1,589.65 ns** | **1.001** |    **0.04** |    **2** | **1.3428** | **1.2817** |   **22744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     377.5 ns |     64.41 ns |     3.53 ns | 0.008 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  46,831.0 ns |  3,409.64 ns |   186.89 ns | 0.950 |    0.03 |    2 | 1.3428 | 1.2817 |   23055 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  49,377.2 ns |  2,063.79 ns |   113.12 ns | 1.001 |    0.03 |    2 | 1.4038 | 1.3428 |   23696 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  60,230.2 ns |  6,579.66 ns |   360.65 ns | 1.221 |    0.03 |    3 | 1.3428 | 1.2817 |   22744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **41,349.5 ns** |  **7,867.80 ns** |   **431.26 ns** |  **1.00** |    **0.01** |    **2** | **1.1597** | **1.0986** |   **19744 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     966.4 ns |    114.85 ns |     6.30 ns |  0.02 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  40,366.0 ns |  4,600.45 ns |   252.17 ns |  0.98 |    0.01 |    2 | 1.1597 | 1.0986 |   20055 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  41,139.0 ns | 10,359.17 ns |   567.82 ns |  0.99 |    0.01 |    2 | 1.2207 | 1.1597 |   20696 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  48,456.4 ns |  2,496.38 ns |   136.84 ns |  1.17 |    0.01 |    2 | 1.1597 | 1.0986 |   19744 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **318,542.1 ns** | **20,092.56 ns** | **1,101.34 ns** | **1.000** |    **0.00** |    **2** | **8.7891** | **4.3945** |  **154735 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   1,750.5 ns |    537.21 ns |    29.45 ns | 0.005 |    0.00 |    1 | 1.2016 | 0.0782 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 349,953.7 ns | 78,140.70 ns | 4,283.16 ns | 1.099 |    0.01 |    2 | 8.7891 | 4.3945 |  155047 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 340,246.5 ns |  2,597.70 ns |   142.39 ns | 1.068 |    0.00 |    2 | 9.2773 | 4.3945 |  155688 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 514,757.6 ns | 54,852.80 ns | 3,006.67 ns | 1.616 |    0.01 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
