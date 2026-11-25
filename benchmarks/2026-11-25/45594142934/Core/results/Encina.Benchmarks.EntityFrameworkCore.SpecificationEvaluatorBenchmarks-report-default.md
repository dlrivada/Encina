
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **7.612 μs** |   **2.0254 μs** |  **0.1110 μs** |   **1.00** |    **0.02** |    **3** | **0.4120** |   **6.74 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     6.239 μs |   4.3409 μs |  0.2379 μs |   0.82 |    0.03 |    2 | 0.3204 |   5.26 KB |        0.78 |
 'Complex predicates (parameterized)' | 2             |     8.351 μs |   0.4312 μs |  0.0236 μs |   1.10 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Two criteria (AND)'                 | 2             |     8.536 μs |   7.1494 μs |  0.3919 μs |   1.12 |    0.05 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 2             |    23.781 μs |   3.6942 μs |  0.2025 μs |   3.12 |    0.05 |    5 | 0.7019 |  11.55 KB |        1.71 |
 'Ten criteria (AND)'                 | 2             |    41.645 μs |  30.8632 μs |  1.6917 μs |   5.47 |    0.20 |    7 | 1.2207 |  20.91 KB |        3.10 |
 'Keyset pagination'                  | 2             |    12.668 μs |   1.9714 μs |  0.1081 μs |   1.66 |    0.02 |    4 | 0.6256 |  10.43 KB |        1.55 |
 'Keyset pagination (fresh cursor)'   | 2             | 2,633.021 μs | 707.3347 μs | 38.7714 μs | 345.97 |    6.20 |    8 | 3.9063 | 109.25 KB |       16.20 |
 'Lambda Include'                     | 2             |     5.072 μs |   4.6847 μs |  0.2568 μs |   0.67 |    0.03 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'String Include'                     | 2             |     4.910 μs |   0.3897 μs |  0.0214 μs |   0.65 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 2             |    11.703 μs |   5.1065 μs |  0.2799 μs |   1.54 |    0.04 |    4 | 0.5341 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 2             |    12.241 μs |   1.3668 μs |  0.0749 μs |   1.61 |    0.02 |    4 | 0.5798 |   9.67 KB |        1.43 |
 'Full specification (all features)'  | 2             |    31.489 μs |   1.8822 μs |  0.1032 μs |   4.14 |    0.05 |    6 | 1.2207 |  20.03 KB |        2.97 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **7.588 μs** |   **1.4315 μs** |  **0.0785 μs** |   **1.00** |    **0.01** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     6.265 μs |   0.7752 μs |  0.0425 μs |   0.83 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    39.329 μs |   9.7438 μs |  0.5341 μs |   5.18 |    0.08 |    7 | 1.2817 |  21.22 KB |        3.29 |
 'Two criteria (AND)'                 | 10            |     8.858 μs |   3.0127 μs |  0.1651 μs |   1.17 |    0.02 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    19.700 μs |   5.8645 μs |  0.3215 μs |   2.60 |    0.04 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    39.383 μs |   4.1330 μs |  0.2265 μs |   5.19 |    0.05 |    7 | 1.2817 |  21.06 KB |        3.27 |
 'Keyset pagination'                  | 10            |    13.520 μs |   6.5349 μs |  0.3582 μs |   1.78 |    0.04 |    4 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,559.849 μs | 592.7159 μs | 32.4888 μs | 337.37 |    4.77 |    8 | 3.9063 | 109.11 KB |       16.93 |
 'Lambda Include'                     | 10            |     4.847 μs |   0.4785 μs |  0.0262 μs |   0.64 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     4.815 μs |   0.4058 μs |  0.0222 μs |   0.63 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    11.218 μs |   0.7009 μs |  0.0384 μs |   1.48 |    0.01 |    4 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    11.572 μs |   1.5571 μs |  0.0854 μs |   1.53 |    0.02 |    4 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    30.578 μs |   1.8882 μs |  0.1035 μs |   4.03 |    0.04 |    6 | 1.1902 |  19.88 KB |        3.08 |
