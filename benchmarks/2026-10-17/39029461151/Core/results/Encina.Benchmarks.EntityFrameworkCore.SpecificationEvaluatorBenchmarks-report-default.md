
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.26 μs** |   **1.705 μs** |  **0.093 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.79 μs |   2.368 μs |  0.130 μs |   0.84 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.31 μs |   0.621 μs |  0.034 μs |   1.14 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.94 μs |   2.455 μs |  0.135 μs |   1.13 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    48.77 μs |   2.207 μs |  0.121 μs |   2.29 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    86.89 μs |   5.512 μs |  0.302 μs |   4.09 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.01 μs |   1.964 μs |  0.108 μs |   1.88 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,946.06 μs | 479.870 μs | 26.303 μs | 185.57 |    1.28 |    7 |      - | 109.81 KB |       17.04 |
 'Lambda Include'                     | 2             |    11.31 μs |   0.874 μs |  0.048 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.65 μs |   0.314 μs |  0.017 μs |   0.55 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.18 μs |   1.042 μs |  0.057 μs |   1.18 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    31.07 μs |   4.434 μs |  0.243 μs |   1.46 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    86.87 μs |   8.414 μs |  0.461 μs |   4.09 |    0.02 |    6 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.79 μs** |   **1.358 μs** |  **0.074 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.90 μs |   1.563 μs |  0.086 μs |   0.82 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    89.13 μs |   7.757 μs |  0.425 μs |   4.09 |    0.02 |    6 | 1.2207 |  21.06 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    23.82 μs |   1.461 μs |  0.080 μs |   1.09 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.60 μs |   4.840 μs |  0.265 μs |   2.32 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    91.03 μs |  10.508 μs |  0.576 μs |   4.18 |    0.03 |    6 | 1.2207 |  21.21 KB |        3.29 |
 'Keyset pagination'                  | 10            |    39.20 μs |   5.746 μs |  0.315 μs |   1.80 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,941.15 μs | 410.181 μs | 22.483 μs | 180.90 |    1.04 |    7 |      - |  108.8 KB |       16.88 |
 'Lambda Include'                     | 10            |    11.62 μs |   0.720 μs |  0.039 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    10.73 μs |   0.812 μs |  0.044 μs |   0.49 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.57 μs |   0.857 μs |  0.047 μs |   1.17 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.92 μs |   3.138 μs |  0.172 μs |   1.37 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    89.45 μs |   2.067 μs |  0.113 μs |   4.11 |    0.01 |    6 | 1.2207 |   20.2 KB |        3.13 |
