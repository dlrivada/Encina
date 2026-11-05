
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean           | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |---------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |   **111,613.0 ns** |  **9,771.20 ns** |   **535.59 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |       384.8 ns |    139.90 ns |     7.67 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |   113,913.4 ns |  6,123.01 ns |   335.62 ns | 1.021 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |   110,242.2 ns | 14,565.64 ns |   798.39 ns | 0.988 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |   112,453.1 ns |  5,581.51 ns |   305.94 ns | 1.008 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |                |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |   **159,792.6 ns** | **17,110.78 ns** |   **937.90 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |       519.7 ns |     97.78 ns |     5.36 ns | 0.003 |    1 | 0.1507 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |   164,751.2 ns | 24,138.62 ns | 1,323.12 ns | 1.031 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |   161,783.3 ns | 10,011.95 ns |   548.79 ns | 1.012 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |   181,921.8 ns |  4,778.17 ns |   261.91 ns | 1.139 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |                |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |   **145,234.6 ns** | **18,339.68 ns** | **1,005.26 ns** |  **1.00** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     1,773.4 ns |    409.13 ns |    22.43 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |   148,560.1 ns | 17,030.11 ns |   933.48 ns |  1.02 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |   150,974.1 ns | 17,629.44 ns |   966.33 ns |  1.04 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |   164,714.7 ns | 15,839.95 ns |   868.24 ns |  1.13 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |                |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           |   **724,047.8 ns** | **49,518.38 ns** | **2,714.27 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |     2,870.8 ns |    807.54 ns |    44.26 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           |   721,223.6 ns | 43,249.22 ns | 2,370.64 ns | 0.996 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           |   731,593.7 ns | 15,349.03 ns |   841.33 ns | 1.010 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 1,088,925.5 ns | 51,632.29 ns | 2,830.14 ns | 1.504 |    3 | 7.8125 | 1.9531 |  154724 B |        1.00 |
