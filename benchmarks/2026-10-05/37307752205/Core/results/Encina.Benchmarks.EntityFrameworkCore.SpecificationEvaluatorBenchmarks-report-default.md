
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **10.340 μs** |     **1.9601 μs** |  **0.1074 μs** |   **1.00** |    **0.01** |    **3** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     7.974 μs |     5.0926 μs |  0.2791 μs |   0.77 |    0.02 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    10.364 μs |     0.8242 μs |  0.0452 μs |   1.00 |    0.01 |    3 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    10.782 μs |     2.1786 μs |  0.1194 μs |   1.04 |    0.01 |    3 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    23.684 μs |     2.0393 μs |  0.1118 μs |   2.29 |    0.02 |    5 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    44.384 μs |     1.6226 μs |  0.0889 μs |   4.29 |    0.04 |    6 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    17.186 μs |    23.7725 μs |  1.3031 μs |   1.66 |    0.11 |    4 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,053.746 μs | 1,646.1327 μs | 90.2301 μs | 295.34 |    8.01 |    7 |      - | 108.81 KB |       16.88 |
 'Lambda Include'                     | 2             |     5.784 μs |     3.8691 μs |  0.2121 μs |   0.56 |    0.02 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     5.516 μs |     0.2130 μs |  0.0117 μs |   0.53 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    13.278 μs |     0.9068 μs |  0.0497 μs |   1.28 |    0.01 |    4 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    15.816 μs |     2.2729 μs |  0.1246 μs |   1.53 |    0.02 |    4 | 0.1068 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    39.487 μs |    34.5851 μs |  1.8957 μs |   3.82 |    0.16 |    6 | 0.1831 |  19.88 KB |        3.08 |
                                      |               |              |               |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **9.422 μs** |     **0.4818 μs** |  **0.0264 μs** |   **1.00** |    **0.00** |    **3** | **0.0763** |   **6.56 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     7.767 μs |     0.6890 μs |  0.0378 μs |   0.82 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.80 |
 'Complex predicates (parameterized)' | 10            |    48.999 μs |    48.7167 μs |  2.6703 μs |   5.20 |    0.25 |    7 | 0.2441 |  20.91 KB |        3.19 |
 'Two criteria (AND)'                 | 10            |    10.840 μs |     0.8318 μs |  0.0456 μs |   1.15 |    0.01 |    3 | 0.0610 |   6.14 KB |        0.94 |
 'Five criteria (AND)'                | 10            |    25.342 μs |    38.1382 μs |  2.0905 μs |   2.69 |    0.19 |    6 | 0.1221 |  11.55 KB |        1.76 |
 'Ten criteria (AND)'                 | 10            |    44.707 μs |     1.6743 μs |  0.0918 μs |   4.74 |    0.01 |    7 | 0.2441 |  20.91 KB |        3.19 |
 'Keyset pagination'                  | 10            |    17.390 μs |    14.5157 μs |  0.7957 μs |   1.85 |    0.07 |    5 | 0.1221 |  10.43 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,012.554 μs |   167.5587 μs |  9.1845 μs | 319.74 |    1.15 |    8 |      - | 109.36 KB |       16.66 |
 'Lambda Include'                     | 10            |     5.699 μs |     1.3166 μs |  0.0722 μs |   0.60 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.65 |
 'String Include'                     | 10            |     5.746 μs |     1.1871 μs |  0.0651 μs |   0.61 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.65 |
 'Multi-column ordering'              | 10            |    13.727 μs |     0.9658 μs |  0.0529 μs |   1.46 |    0.01 |    4 | 0.1068 |   8.77 KB |        1.34 |
 'Offset pagination (Skip/Take)'      | 10            |    14.109 μs |     0.7234 μs |  0.0397 μs |   1.50 |    0.01 |    4 | 0.1068 |   9.36 KB |        1.43 |
 'Full specification (all features)'  | 10            |    38.798 μs |     5.1783 μs |  0.2838 μs |   4.12 |    0.03 |    7 | 0.2441 |  20.18 KB |        3.08 |
