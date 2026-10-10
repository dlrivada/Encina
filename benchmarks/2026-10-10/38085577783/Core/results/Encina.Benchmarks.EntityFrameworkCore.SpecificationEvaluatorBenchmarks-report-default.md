
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **20.89 μs** |   **0.642 μs** |  **0.035 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.09 μs |   1.456 μs |  0.080 μs |   0.82 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.77 μs |   1.378 μs |  0.076 μs |   1.14 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.70 μs |   1.532 μs |  0.084 μs |   1.18 |    0.00 |    3 | 0.2441 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.70 μs |   2.588 μs |  0.142 μs |   2.43 |    0.01 |    5 | 0.4272 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    87.73 μs |   3.005 μs |  0.165 μs |   4.20 |    0.01 |    6 | 0.7324 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    38.11 μs |   2.063 μs |  0.113 μs |   1.82 |    0.01 |    4 | 0.3662 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,987.84 μs | 410.275 μs | 22.489 μs | 190.90 |    0.97 |    7 |      - | 109.11 KB |       16.93 |
 'Lambda Include'                     | 2             |    11.71 μs |   3.664 μs |  0.201 μs |   0.56 |    0.01 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.80 μs |   1.683 μs |  0.092 μs |   0.56 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.94 μs |   0.755 μs |  0.041 μs |   1.19 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    27.13 μs |   0.538 μs |  0.029 μs |   1.30 |    0.00 |    3 | 0.3662 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    81.50 μs |   5.577 μs |  0.306 μs |   3.90 |    0.01 |    6 | 0.7324 |  20.04 KB |        3.11 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.87 μs** |   **0.517 μs** |  **0.028 μs** |   **1.00** |    **0.00** |    **3** | **0.2441** |   **6.56 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.08 μs |   0.676 μs |  0.037 μs |   0.78 |    0.00 |    2 | 0.2136 |   5.26 KB |        0.80 |
 'Complex predicates (parameterized)' | 10            |    85.29 μs |   2.864 μs |  0.157 μs |   3.90 |    0.01 |    6 | 0.7324 |  20.91 KB |        3.19 |
 'Two criteria (AND)'                 | 10            |    24.53 μs |   2.797 μs |  0.153 μs |   1.12 |    0.01 |    3 | 0.2441 |   6.14 KB |        0.94 |
 'Five criteria (AND)'                | 10            |    51.00 μs |   2.047 μs |  0.112 μs |   2.33 |    0.01 |    5 | 0.4272 |  11.55 KB |        1.76 |
 'Ten criteria (AND)'                 | 10            |    85.48 μs |   2.827 μs |  0.155 μs |   3.91 |    0.01 |    6 | 0.7324 |  20.91 KB |        3.19 |
 'Keyset pagination'                  | 10            |    37.54 μs |   7.757 μs |  0.425 μs |   1.72 |    0.02 |    4 | 0.3662 |  10.43 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,953.01 μs | 109.573 μs |  6.006 μs | 180.78 |    0.31 |    7 |      - | 109.37 KB |       16.67 |
 'Lambda Include'                     | 10            |    11.83 μs |   0.911 μs |  0.050 μs |   0.54 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.65 |
 'String Include'                     | 10            |    11.53 μs |   0.507 μs |  0.028 μs |   0.53 |    0.00 |    1 | 0.1678 |   4.26 KB |        0.65 |
 'Multi-column ordering'              | 10            |    24.72 μs |   0.839 μs |  0.046 μs |   1.13 |    0.00 |    3 | 0.3357 |   8.77 KB |        1.34 |
 'Offset pagination (Skip/Take)'      | 10            |    27.27 μs |   0.276 μs |  0.015 μs |   1.25 |    0.00 |    3 | 0.3662 |   9.36 KB |        1.43 |
 'Full specification (all features)'  | 10            |    81.97 μs |   4.757 μs |  0.261 μs |   3.75 |    0.01 |    6 | 0.7324 |  19.88 KB |        3.03 |
