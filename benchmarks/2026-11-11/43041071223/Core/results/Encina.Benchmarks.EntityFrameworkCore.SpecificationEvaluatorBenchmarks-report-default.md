
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.61GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.20 μs** |   **2.282 μs** |  **0.125 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.30 μs |   2.189 μs |  0.120 μs |   0.86 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.11 μs |   4.211 μs |  0.231 μs |   1.09 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.26 μs |   0.914 μs |  0.050 μs |   1.14 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    47.71 μs |   3.133 μs |  0.172 μs |   2.25 |    0.01 |    3 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.03 μs |   2.615 μs |  0.143 μs |   4.15 |    0.02 |    4 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    41.06 μs |   3.894 μs |  0.213 μs |   1.94 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,919.85 μs |  17.700 μs |  0.970 μs | 184.88 |    0.95 |    5 | 3.9063 | 109.91 KB |       17.05 |
 'Lambda Include'                     | 2             |    11.27 μs |   0.765 μs |  0.042 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.18 μs |   0.932 μs |  0.051 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.17 μs |   0.738 μs |  0.040 μs |   1.19 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.70 μs |   1.561 μs |  0.086 μs |   1.40 |    0.01 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    86.91 μs |  10.451 μs |  0.573 μs |   4.10 |    0.03 |    4 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.48 μs** |   **1.297 μs** |  **0.071 μs** |   **1.00** |    **0.00** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.81 μs |   1.755 μs |  0.096 μs |   0.88 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    85.76 μs |   5.688 μs |  0.312 μs |   3.99 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.85 μs |   2.837 μs |  0.156 μs |   1.11 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.51 μs |   2.258 μs |  0.124 μs |   2.30 |    0.01 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    86.44 μs |   4.033 μs |  0.221 μs |   4.02 |    0.01 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.11 μs |   6.640 μs |  0.364 μs |   1.87 |    0.02 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,898.04 μs | 235.574 μs | 12.913 μs | 181.44 |    0.74 |    6 | 3.9063 | 109.12 KB |       16.93 |
 'Lambda Include'                     | 10            |    11.16 μs |   0.637 μs |  0.035 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.16 μs |   0.588 μs |  0.032 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.29 μs |   6.480 μs |  0.355 μs |   1.18 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.97 μs |   2.132 μs |  0.117 μs |   1.39 |    0.01 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    88.53 μs |   4.291 μs |  0.235 μs |   4.12 |    0.02 |    5 | 1.2207 |  20.18 KB |        3.13 |
