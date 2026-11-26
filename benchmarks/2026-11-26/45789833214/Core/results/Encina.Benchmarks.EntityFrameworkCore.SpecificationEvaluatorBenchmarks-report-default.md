
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.10GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **12.891 μs** |   **0.7852 μs** |  **0.0430 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    10.886 μs |   0.6173 μs |  0.0338 μs |   0.84 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    15.020 μs |   0.7091 μs |  0.0389 μs |   1.17 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    14.879 μs |   0.5101 μs |  0.0280 μs |   1.15 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    34.947 μs |   3.4040 μs |  0.1866 μs |   2.71 |    0.01 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    66.898 μs |   4.1714 μs |  0.2286 μs |   5.19 |    0.02 |    6 | 0.2441 |  21.22 KB |        3.29 |
 'Keyset pagination'                  | 2             |    24.300 μs |  10.6410 μs |  0.5833 μs |   1.89 |    0.04 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,123.862 μs | 741.8899 μs | 40.6655 μs | 319.91 |    2.88 |    7 |      - | 109.36 KB |       16.97 |
 'Lambda Include'                     | 2             |     7.912 μs |   0.4570 μs |  0.0250 μs |   0.61 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.942 μs |   0.8209 μs |  0.0450 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    19.235 μs |   1.1008 μs |  0.0603 μs |   1.49 |    0.01 |    3 | 0.0916 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    20.750 μs |   0.9919 μs |  0.0544 μs |   1.61 |    0.01 |    3 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    55.338 μs |   5.7588 μs |  0.3157 μs |   4.29 |    0.02 |    5 | 0.1831 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **12.934 μs** |   **0.1158 μs** |  **0.0063 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    10.811 μs |   0.6067 μs |  0.0333 μs |   0.84 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    65.754 μs |   6.9851 μs |  0.3829 μs |   5.08 |    0.03 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    14.830 μs |   1.2570 μs |  0.0689 μs |   1.15 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    34.289 μs |   3.0713 μs |  0.1683 μs |   2.65 |    0.01 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    65.163 μs |   3.9703 μs |  0.2176 μs |   5.04 |    0.01 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    23.651 μs |   0.7557 μs |  0.0414 μs |   1.83 |    0.00 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,083.239 μs | 409.3507 μs | 22.4379 μs | 315.69 |    1.51 |    6 |      - | 109.14 KB |       16.93 |
 'Lambda Include'                     | 10            |     7.963 μs |   0.7317 μs |  0.0401 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.988 μs |   0.9327 μs |  0.0511 μs |   0.62 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    19.117 μs |   1.4847 μs |  0.0814 μs |   1.48 |    0.01 |    3 | 0.0916 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    20.499 μs |   0.7548 μs |  0.0414 μs |   1.58 |    0.00 |    3 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    55.113 μs |   2.2496 μs |  0.1233 μs |   4.26 |    0.01 |    5 | 0.1831 |  19.88 KB |        3.08 |
