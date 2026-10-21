
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **12.863 μs** |   **0.2414 μs** |  **0.0132 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    10.938 μs |   1.0719 μs |  0.0588 μs |   0.85 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    15.123 μs |   0.2300 μs |  0.0126 μs |   1.18 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    15.395 μs |   2.0230 μs |  0.1109 μs |   1.20 |    0.01 |    2 | 0.0610 |   6.29 KB |        0.98 |
 'Five criteria (AND)'                | 2             |    34.764 μs |   6.9445 μs |  0.3807 μs |   2.70 |    0.03 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    65.913 μs |   9.4998 μs |  0.5207 μs |   5.12 |    0.04 |    5 | 0.2441 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    23.233 μs |   0.8774 μs |  0.0481 μs |   1.81 |    0.00 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,083.171 μs | 595.9040 μs | 32.6635 μs | 317.44 |    2.22 |    6 |      - | 110.11 KB |       17.08 |
 'Lambda Include'                     | 2             |     7.925 μs |   1.9750 μs |  0.1083 μs |   0.62 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.971 μs |   0.6878 μs |  0.0377 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    18.989 μs |   1.0822 μs |  0.0593 μs |   1.48 |    0.00 |    3 | 0.0916 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    20.472 μs |   9.5447 μs |  0.5232 μs |   1.59 |    0.04 |    3 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    55.725 μs |   4.3634 μs |  0.2392 μs |   4.33 |    0.02 |    5 | 0.2441 |  20.04 KB |        3.11 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **12.855 μs** |   **1.5571 μs** |  **0.0853 μs** |   **1.00** |    **0.01** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    10.774 μs |   1.1356 μs |  0.0622 μs |   0.84 |    0.01 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    65.058 μs |   3.5262 μs |  0.1933 μs |   5.06 |    0.03 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    14.943 μs |   0.8720 μs |  0.0478 μs |   1.16 |    0.01 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    34.210 μs |   1.6452 μs |  0.0902 μs |   2.66 |    0.02 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    64.653 μs |   2.6248 μs |  0.1439 μs |   5.03 |    0.03 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    23.626 μs |   8.6786 μs |  0.4757 μs |   1.84 |    0.03 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,074.784 μs | 338.9976 μs | 18.5816 μs | 316.98 |    2.21 |    6 |      - | 109.47 KB |       16.98 |
 'Lambda Include'                     | 10            |     7.997 μs |   2.1735 μs |  0.1191 μs |   0.62 |    0.01 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.888 μs |   0.7016 μs |  0.0385 μs |   0.61 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    19.026 μs |   0.9195 μs |  0.0504 μs |   1.48 |    0.01 |    3 | 0.0916 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    21.230 μs |   2.9804 μs |  0.1634 μs |   1.65 |    0.01 |    3 | 0.0916 |   9.67 KB |        1.50 |
 'Full specification (all features)'  | 10            |    54.942 μs |   4.9316 μs |  0.2703 μs |   4.27 |    0.03 |    5 | 0.1831 |  19.88 KB |        3.08 |
