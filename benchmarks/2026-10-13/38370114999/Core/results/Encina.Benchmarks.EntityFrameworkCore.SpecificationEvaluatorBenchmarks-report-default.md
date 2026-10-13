
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **13.264 μs** |   **0.7737 μs** |  **0.0424 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    11.437 μs |   2.3207 μs |  0.1272 μs |   0.86 |    0.01 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    15.371 μs |   0.6817 μs |  0.0374 μs |   1.16 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    15.497 μs |   2.0770 μs |  0.1138 μs |   1.17 |    0.01 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    35.214 μs |   2.6330 μs |  0.1443 μs |   2.65 |    0.01 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    67.190 μs |   8.0876 μs |  0.4433 μs |   5.07 |    0.03 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    23.788 μs |   0.9778 μs |  0.0536 μs |   1.79 |    0.01 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,407.024 μs | 423.3618 μs | 23.2059 μs | 332.25 |    1.77 |    6 |      - | 109.18 KB |       16.94 |
 'Lambda Include'                     | 2             |     8.138 μs |   1.2330 μs |  0.0676 μs |   0.61 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     8.116 μs |   0.4516 μs |  0.0248 μs |   0.61 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    19.543 μs |   1.8720 μs |  0.1026 μs |   1.47 |    0.01 |    3 | 0.0916 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    20.613 μs |   2.0595 μs |  0.1129 μs |   1.55 |    0.01 |    3 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    57.858 μs |  16.8939 μs |  0.9260 μs |   4.36 |    0.06 |    5 | 0.2441 |  20.02 KB |        3.11 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **13.521 μs** |   **5.3565 μs** |  **0.2936 μs** |   **1.00** |    **0.03** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    11.318 μs |   0.7594 μs |  0.0416 μs |   0.84 |    0.02 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    67.659 μs |  13.5632 μs |  0.7434 μs |   5.01 |    0.10 |    5 | 0.2441 |  21.06 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    15.287 μs |   0.5909 μs |  0.0324 μs |   1.13 |    0.02 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    35.010 μs |   0.7932 μs |  0.0435 μs |   2.59 |    0.05 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    66.532 μs |   1.8084 μs |  0.0991 μs |   4.92 |    0.09 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    24.374 μs |   0.1899 μs |  0.0104 μs |   1.80 |    0.03 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,387.787 μs | 170.9359 μs |  9.3696 μs | 324.63 |    6.06 |    6 |      - | 109.79 KB |       17.03 |
 'Lambda Include'                     | 10            |     8.177 μs |   0.6157 μs |  0.0337 μs |   0.60 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     8.019 μs |   0.1834 μs |  0.0101 μs |   0.59 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    19.733 μs |   1.7958 μs |  0.0984 μs |   1.46 |    0.03 |    3 | 0.0916 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    20.479 μs |   2.3394 μs |  0.1282 μs |   1.52 |    0.03 |    3 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    56.194 μs |   3.1294 μs |  0.1715 μs |   4.16 |    0.08 |    5 | 0.1831 |  19.88 KB |        3.08 |
