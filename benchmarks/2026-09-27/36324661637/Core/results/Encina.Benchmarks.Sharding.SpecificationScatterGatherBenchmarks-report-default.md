
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.08GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **41,000.0 ns** | **14,098.86 ns** |   **772.81 ns** | **1.000** |    **0.02** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     354.9 ns |      2.39 ns |     0.13 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  42,796.6 ns |  4,350.59 ns |   238.47 ns | 1.044 |    0.02 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  41,159.9 ns |  5,554.60 ns |   304.47 ns | 1.004 |    0.02 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  41,337.4 ns |  9,754.04 ns |   534.65 ns | 1.008 |    0.02 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **92,260.4 ns** |  **3,390.71 ns** |   **185.86 ns** | **1.000** |    **0.00** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     605.9 ns |    454.53 ns |    24.91 ns | 0.007 |    0.00 |    1 | 0.0296 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  92,864.5 ns |  1,269.84 ns |    69.60 ns | 1.007 |    0.00 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  93,138.7 ns |  2,831.65 ns |   155.21 ns | 1.010 |    0.00 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           | 113,202.6 ns |  5,504.42 ns |   301.72 ns | 1.227 |    0.00 |    3 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **76,208.4 ns** |  **5,409.57 ns** |   **296.52 ns** |  **1.00** |    **0.00** |    **2** | **0.1221** |      **-** |   **19724 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,644.0 ns |     83.29 ns |     4.57 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  77,217.6 ns |  2,752.65 ns |   150.88 ns |  1.01 |    0.00 |    2 | 0.1221 |      - |   20036 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  80,250.0 ns | 24,016.89 ns | 1,316.45 ns |  1.05 |    0.02 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  95,716.3 ns |  2,570.15 ns |   140.88 ns |  1.26 |    0.00 |    2 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **645,780.0 ns** | **17,038.72 ns** |   **933.95 ns** | **1.000** |    **0.00** |    **2** | **0.9766** |      **-** |  **154717 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   3,009.0 ns |    434.22 ns |    23.80 ns | 0.005 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 645,136.8 ns | 32,302.12 ns | 1,770.59 ns | 0.999 |    0.00 |    2 | 0.9766 |      - |  155029 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 639,285.5 ns | 34,141.41 ns | 1,871.41 ns | 0.990 |    0.00 |    2 | 0.9766 |      - |  155669 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 983,499.4 ns | 30,984.26 ns | 1,698.35 ns | 1.523 |    0.00 |    3 |      - |      - |  154704 B |        1.00 |
