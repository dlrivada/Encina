
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean           | Error        | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |---------------:|-------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |   **115,086.7 ns** |  **1,821.30 ns** |    **99.83 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |       316.8 ns |     68.82 ns |     3.77 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |   116,674.9 ns | 14,353.61 ns |   786.77 ns | 1.014 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |   114,598.6 ns | 12,502.21 ns |   685.29 ns | 0.996 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |   115,635.5 ns | 13,952.82 ns |   764.80 ns | 1.005 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |                |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |   **153,875.5 ns** | **16,617.58 ns** |   **910.87 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |       441.0 ns |     20.75 ns |     1.14 ns | 0.003 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |   153,427.5 ns |  8,433.96 ns |   462.29 ns | 0.997 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |   154,355.5 ns | 26,522.86 ns | 1,453.81 ns | 1.003 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |   167,239.3 ns | 10,759.89 ns |   589.79 ns | 1.087 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |                |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |   **140,798.0 ns** | **15,748.84 ns** |   **863.25 ns** |  **1.00** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |     1,411.7 ns |    111.34 ns |     6.10 ns |  0.01 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |   143,635.0 ns |  7,882.63 ns |   432.07 ns |  1.02 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |   144,028.3 ns |  4,370.17 ns |   239.54 ns |  1.02 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |   156,489.5 ns | 10,067.56 ns |   551.84 ns |  1.11 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |                |              |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           |   **585,965.4 ns** | **36,489.28 ns** | **2,000.10 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |     2,418.9 ns |    672.87 ns |    36.88 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           |   587,760.3 ns | 19,296.05 ns | 1,057.68 ns | 1.003 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           |   596,323.4 ns | 65,065.50 ns | 3,566.46 ns | 1.018 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 1,710,908.7 ns | 45,107.86 ns | 2,472.51 ns | 2.920 |    3 | 7.8125 | 1.9531 |  154724 B |        1.00 |
