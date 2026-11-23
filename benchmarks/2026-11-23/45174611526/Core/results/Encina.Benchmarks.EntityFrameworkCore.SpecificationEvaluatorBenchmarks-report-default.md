
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.27GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **7.729 μs** |     **1.0613 μs** |  **0.0582 μs** |   **1.00** |    **0.01** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     6.572 μs |     4.3956 μs |  0.2409 μs |   0.85 |    0.03 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |     9.075 μs |     0.6700 μs |  0.0367 μs |   1.17 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |     9.486 μs |     7.9878 μs |  0.4378 μs |   1.23 |    0.05 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    21.386 μs |     2.1406 μs |  0.1173 μs |   2.77 |    0.02 |    3 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    38.656 μs |    19.0479 μs |  1.0441 μs |   5.00 |    0.12 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    12.762 μs |     3.0406 μs |  0.1667 μs |   1.65 |    0.02 |    2 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 2,597.808 μs |   701.9860 μs | 38.4782 μs | 336.11 |    4.84 |    6 | 3.9063 | 109.71 KB |       17.02 |
 'Lambda Include'                     | 2             |     4.918 μs |     3.2509 μs |  0.1782 μs |   0.64 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     4.815 μs |     1.7739 μs |  0.0972 μs |   0.62 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    11.226 μs |     4.9900 μs |  0.2735 μs |   1.45 |    0.03 |    2 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    11.719 μs |     0.7428 μs |  0.0407 μs |   1.52 |    0.01 |    2 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    30.578 μs |     9.7620 μs |  0.5351 μs |   3.96 |    0.07 |    4 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |              |               |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **7.258 μs** |     **0.9230 μs** |  **0.0506 μs** |   **1.00** |    **0.01** |    **3** | **0.3891** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     6.005 μs |     1.1230 μs |  0.0616 μs |   0.83 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    38.547 μs |     9.5875 μs |  0.5255 μs |   5.31 |    0.07 |    8 | 1.2817 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |     8.737 μs |     5.4146 μs |  0.2968 μs |   1.20 |    0.04 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    20.389 μs |    16.3942 μs |  0.8986 μs |   2.81 |    0.11 |    6 | 0.7019 |  11.85 KB |        1.84 |
 'Ten criteria (AND)'                 | 10            |    39.043 μs |     4.8292 μs |  0.2647 μs |   5.38 |    0.05 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    13.397 μs |     3.2059 μs |  0.1757 μs |   1.85 |    0.02 |    5 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,751.938 μs | 1,422.3036 μs | 77.9613 μs | 379.15 |    9.58 |    9 | 3.9063 | 110.06 KB |       17.08 |
 'Lambda Include'                     | 10            |     4.848 μs |     0.8215 μs |  0.0450 μs |   0.67 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     4.552 μs |     1.4209 μs |  0.0779 μs |   0.63 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    10.997 μs |     7.1992 μs |  0.3946 μs |   1.52 |    0.05 |    5 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    11.152 μs |     2.1491 μs |  0.1178 μs |   1.54 |    0.02 |    5 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    29.011 μs |    12.5391 μs |  0.6873 μs |   4.00 |    0.09 |    7 | 1.1902 |  19.88 KB |        3.08 |
