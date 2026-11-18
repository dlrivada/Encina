
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **30,646.0 ns** |  **3,297.05 ns** |   **180.72 ns** | **1.000** |    **0.01** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     272.1 ns |     14.40 ns |     0.79 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  30,415.0 ns |  3,784.36 ns |   207.43 ns | 0.992 |    0.01 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  30,283.3 ns |  5,086.90 ns |   278.83 ns | 0.988 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  30,671.9 ns | 10,663.08 ns |   584.48 ns | 1.001 |    0.02 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **69,630.2 ns** |  **5,520.68 ns** |   **302.61 ns** | **1.000** |    **0.01** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     465.2 ns |    399.53 ns |    21.90 ns | 0.007 |    0.00 |    1 | 0.0300 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  71,270.1 ns | 13,560.14 ns |   743.28 ns | 1.024 |    0.01 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  69,703.3 ns | 12,384.44 ns |   678.83 ns | 1.001 |    0.01 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  84,124.8 ns |  8,910.87 ns |   488.43 ns | 1.208 |    0.01 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **57,553.3 ns** |  **5,191.71 ns** |   **284.58 ns** |  **1.00** |    **0.01** |    **2** | **0.1831** | **0.1221** |   **19739 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,349.3 ns |     75.83 ns |     4.16 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  59,116.9 ns |  2,091.82 ns |   114.66 ns |  1.03 |    0.00 |    2 | 0.1831 | 0.1221 |   20050 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  61,149.5 ns | 11,077.31 ns |   607.18 ns |  1.06 |    0.01 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  68,496.0 ns |  3,673.23 ns |   201.34 ns |  1.19 |    0.01 |    2 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **472,169.5 ns** |  **3,362.95 ns** |   **184.33 ns** | **1.000** |    **0.00** |    **2** | **1.4648** | **0.9766** |  **154731 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,617.4 ns |    471.08 ns |    25.82 ns | 0.006 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 473,838.3 ns | 96,982.66 ns | 5,315.95 ns | 1.004 |    0.01 |    2 | 1.4648 | 0.9766 |  155043 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 471,724.2 ns | 14,191.49 ns |   777.88 ns | 0.999 |    0.00 |    2 | 1.4648 | 0.9766 |  155683 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 717,813.2 ns | 64,541.55 ns | 3,537.74 ns | 1.520 |    0.01 |    3 | 0.9766 |      - |  154717 B |        1.00 |
