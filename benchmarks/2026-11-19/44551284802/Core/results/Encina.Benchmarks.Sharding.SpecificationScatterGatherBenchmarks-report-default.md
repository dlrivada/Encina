
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                                                | ShardCount | ItemsPerShard | Mean         | Error        | StdDev      | Ratio | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------------------------ |----------- |-------------- |-------------:|-------------:|------------:|------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'MergeAndOrder with ascending ordering'**               | **3**          | **10**            |  **31,278.9 ns** |  **1,057.06 ns** |    **57.94 ns** | **1.000** |    **0.00** |    **2** | **0.0610** |      **-** |    **6539 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 10            |     266.0 ns |     39.86 ns |     2.18 ns | 0.009 |    0.00 |    1 | 0.0043 |      - |     376 B |        0.06 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 10            |  31,252.4 ns |  3,984.03 ns |   218.38 ns | 0.999 |    0.01 |    2 | 0.0610 |      - |    6850 B |        1.05 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 10            |  29,692.7 ns |  3,524.64 ns |   193.20 ns | 0.949 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
 'MergeAndOrder with descending ordering'              | 3          | 10            |  31,415.9 ns |  6,676.63 ns |   365.97 ns | 1.004 |    0.01 |    2 | 0.0610 |      - |    6539 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **3**          | **100**           |  **67,349.6 ns** |  **6,903.71 ns** |   **378.42 ns** | **1.000** |    **0.01** |    **2** | **0.2441** | **0.1221** |   **22734 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 3          | 100           |     377.0 ns |    102.05 ns |     5.59 ns | 0.006 |    0.00 |    1 | 0.0300 |      - |    2536 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 3          | 100           |  70,763.7 ns |  8,027.58 ns |   440.02 ns | 1.051 |    0.01 |    2 | 0.2441 | 0.1221 |   23045 B |        1.01 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 3          | 100           |  68,936.0 ns |  4,215.88 ns |   231.09 ns | 1.024 |    0.01 |    2 | 0.2441 | 0.1221 |   23685 B |        1.04 |
 'MergeAndOrder with descending ordering'              | 3          | 100           |  82,416.8 ns | 26,582.85 ns | 1,457.10 ns | 1.224 |    0.02 |    2 | 0.2441 | 0.1221 |   22734 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **10**            |  **58,105.1 ns** |  **5,695.79 ns** |   **312.21 ns** |  **1.00** |    **0.01** |    **2** | **0.1831** | **0.1221** |   **19739 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 10            |   1,378.1 ns |    863.36 ns |    47.32 ns |  0.02 |    0.00 |    1 | 0.0248 |      - |    2136 B |        0.11 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 10            |  58,080.4 ns |  3,750.62 ns |   205.58 ns |  1.00 |    0.01 |    2 | 0.1831 | 0.1221 |   20050 B |        1.02 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 10            |  60,076.8 ns |  5,545.57 ns |   303.97 ns |  1.03 |    0.01 |    2 | 0.2441 | 0.1221 |   20688 B |        1.05 |
 'MergeAndOrder with descending ordering'              | 25         | 10            |  69,855.6 ns |  8,888.76 ns |   487.22 ns |  1.20 |    0.01 |    2 | 0.1221 |      - |   19724 B |        1.00 |
                                                       |            |               |              |              |             |       |         |      |        |        |           |             |
 **'MergeAndOrder with ascending ordering'**               | **25**         | **100**           | **471,849.5 ns** |  **8,313.46 ns** |   **455.69 ns** | **1.000** |    **0.00** |    **2** | **1.4648** | **0.9766** |  **154731 B** |        **1.00** |
 'MergeAndOrder without ordering'                      | 25         | 100           |   2,332.4 ns |  2,385.12 ns |   130.74 ns | 0.005 |    0.00 |    1 | 0.2403 | 0.0153 |   20136 B |        0.13 |
 'MergeOrderAndPaginate overfetch (page 1, size 20)'   | 25         | 100           | 473,419.1 ns | 35,122.03 ns | 1,925.16 ns | 1.003 |    0.00 |    2 | 1.4648 | 0.9766 |  155043 B |        1.00 |
 'MergeOrderAndPaginate large page (page 2, size 100)' | 25         | 100           | 477,359.1 ns | 59,471.22 ns | 3,259.82 ns | 1.012 |    0.01 |    2 | 1.4648 | 0.9766 |  155683 B |        1.01 |
 'MergeAndOrder with descending ordering'              | 25         | 100           | 719,731.9 ns | 61,161.54 ns | 3,352.47 ns | 1.525 |    0.01 |    3 | 0.9766 |      - |  154717 B |        1.00 |
