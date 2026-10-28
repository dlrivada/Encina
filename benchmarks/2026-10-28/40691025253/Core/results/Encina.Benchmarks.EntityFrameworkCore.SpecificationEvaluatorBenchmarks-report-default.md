
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 3.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.413 μs** |   **0.5998 μs** |  **0.0329 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.166 μs |   0.2943 μs |  0.0161 μs |   0.80 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    13.286 μs |   3.6023 μs |  0.1975 μs |   1.16 |    0.02 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.426 μs |   1.8661 μs |  0.1023 μs |   1.18 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.337 μs |   1.0059 μs |  0.0551 μs |   2.57 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.424 μs |   2.5984 μs |  0.1424 μs |   4.86 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    20.311 μs |   0.5022 μs |  0.0275 μs |   1.78 |    0.00 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,221.787 μs | 228.8141 μs | 12.5421 μs | 282.29 |    1.18 |    7 | 3.9063 | 109.03 KB |       16.92 |
 'Lambda Include'                     | 2             |     7.148 μs |   0.4388 μs |  0.0240 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.126 μs |   0.2608 μs |  0.0143 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    16.092 μs |   1.0769 μs |  0.0590 μs |   1.41 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    16.903 μs |   0.6208 μs |  0.0340 μs |   1.48 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    46.668 μs |   1.2200 μs |  0.0669 μs |   4.09 |    0.01 |    6 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.195 μs** |   **0.1884 μs** |  **0.0103 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.327 μs |   0.8677 μs |  0.0476 μs |   0.83 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.527 μs |   0.2815 μs |  0.0154 μs |   4.96 |    0.00 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.072 μs |   1.2183 μs |  0.0668 μs |   1.17 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.141 μs |   2.4558 μs |  0.1346 μs |   2.60 |    0.01 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    55.417 μs |   1.5526 μs |  0.0851 μs |   4.95 |    0.01 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    20.045 μs |   2.0781 μs |  0.1139 μs |   1.79 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,241.448 μs | 838.1486 μs | 45.9418 μs | 289.54 |    3.56 |    6 | 3.9063 | 109.38 KB |       16.97 |
 'Lambda Include'                     | 10            |     7.302 μs |   0.6542 μs |  0.0359 μs |   0.65 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.058 μs |   0.4146 μs |  0.0227 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.392 μs |   0.6489 μs |  0.0356 μs |   1.46 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    16.953 μs |   1.0802 μs |  0.0592 μs |   1.51 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    46.327 μs |   3.7013 μs |  0.2029 μs |   4.14 |    0.02 |    5 | 1.1597 |  19.88 KB |        3.08 |
