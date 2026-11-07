
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.59GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error         | StdDev      | Ratio | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|--------------:|------------:|------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            | **116,161.4 ns** |   **6,782.68 ns** |   **371.78 ns** | **1.000** |    **2** | **0.3662** | **0.2441** |    **6535 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     317.0 ns |      55.99 ns |     3.07 ns | 0.003 |    1 | 0.0224 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            | 117,152.3 ns |   3,931.38 ns |   215.49 ns | 1.009 |    2 | 0.3662 | 0.2441 |    6845 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            | 115,019.8 ns |   8,090.44 ns |   443.46 ns | 0.990 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            | 116,139.4 ns |   9,379.29 ns |   514.11 ns | 1.000 |    2 | 0.3662 | 0.2441 |    6535 B |        1.00 |
                                                       |            |               |              |               |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           | **154,107.6 ns** |   **2,485.57 ns** |   **136.24 ns** | **1.000** |    **2** | **1.2207** | **0.9766** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     450.5 ns |      84.79 ns |     4.65 ns | 0.003 |    1 | 0.1512 | 0.0010 |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           | 155,708.6 ns |  11,620.36 ns |   636.95 ns | 1.010 |    2 | 1.2207 | 0.9766 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           | 156,015.4 ns |   9,863.44 ns |   540.65 ns | 1.012 |    2 | 1.2207 | 0.9766 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 168,325.1 ns |   6,172.83 ns |   338.35 ns | 1.092 |    2 | 1.2207 | 0.9766 |   22734 B |        1.00 |
                                                       |            |               |              |               |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            | **142,489.1 ns** |  **13,593.13 ns** |   **745.09 ns** | **1.000** |    **2** | **0.9766** | **0.7324** |   **19732 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,418.2 ns |     150.96 ns |     8.27 ns | 0.010 |    1 | 0.1259 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            | 144,797.6 ns |  23,505.12 ns | 1,288.39 ns | 1.016 |    2 | 0.9766 | 0.7324 |   20044 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            | 144,159.0 ns |  12,023.26 ns |   659.04 ns | 1.012 |    2 | 1.2207 | 0.9766 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            | 156,579.7 ns |   3,726.21 ns |   204.25 ns | 1.099 |    2 | 0.9766 | 0.7324 |   19732 B |        1.00 |
                                                       |            |               |              |               |             |       |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **679,743.2 ns** |  **92,358.61 ns** | **5,062.49 ns** | **1.000** |    **2** | **8.7891** | **3.9063** |  **154727 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,427.7 ns |     595.72 ns |    32.65 ns | 0.004 |    1 | 1.2016 | 0.0763 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 602,057.4 ns | 129,542.60 ns | 7,100.67 ns | 0.886 |    2 | 8.7891 | 3.9063 |  155039 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 580,190.3 ns |   8,421.94 ns |   461.63 ns | 0.854 |    2 | 8.7891 | 3.9063 |  155679 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 868,170.0 ns |  47,179.66 ns | 2,586.08 ns | 1.277 |    3 | 8.7891 | 3.9063 |  154727 B |        1.00 |
