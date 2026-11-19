
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.64GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev       | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|-------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **114,543.0 ns** |   **7,250.16 ns** |    **397.41 ns** | **1.000** |    **0.00** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     334.6 ns |      97.45 ns |      5.34 ns | 0.003 |    0.00 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 117,849.7 ns |  15,670.61 ns |    858.96 ns | 1.029 |    0.01 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 114,142.7 ns |  14,810.48 ns |    811.81 ns | 0.997 |    0.01 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 114,391.1 ns |   8,149.15 ns |    446.68 ns | 0.999 |    0.00 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **150,982.7 ns** |  **14,941.87 ns** |    **819.01 ns** | **1.000** |    **0.01** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     455.3 ns |      85.12 ns |      4.67 ns | 0.003 |    0.00 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 153,390.0 ns |  13,283.37 ns |    728.11 ns | 1.016 |    0.01 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 152,925.1 ns |   9,472.90 ns |    519.24 ns | 1.013 |    0.01 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 166,063.1 ns |   9,282.57 ns |    508.81 ns | 1.100 |    0.01 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **140,953.3 ns** |   **7,076.93 ns** |    **387.91 ns** | **1.000** |    **0.00** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,407.6 ns |     480.41 ns |     26.33 ns | 0.010 |    0.00 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 142,651.4 ns |   8,947.88 ns |    490.46 ns | 1.012 |    0.00 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 145,314.4 ns |  12,067.43 ns |    661.46 ns | 1.031 |    0.00 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 156,768.5 ns |   8,370.62 ns |    458.82 ns | 1.112 |    0.00 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |               |              |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **581,738.5 ns** |  **80,383.19 ns** |  **4,406.07 ns** | **1.000** |    **0.01** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,670.7 ns |   1,248.28 ns |     68.42 ns | 0.005 |    0.00 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 578,574.4 ns |  38,389.99 ns |  2,104.29 ns | 0.995 |    0.01 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 590,795.5 ns | 285,661.58 ns | 15,658.08 ns | 1.016 |    0.02 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 847,559.7 ns |  34,521.93 ns |  1,892.26 ns | 1.457 |    0.01 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
