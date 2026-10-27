
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev   | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|---------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.93 μs** |   **1.001 μs** | **0.055 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.33 μs |   3.190 μs | 0.175 μs |   0.79 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    22.72 μs |   1.073 μs | 0.059 μs |   1.04 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.40 μs |   1.156 μs | 0.063 μs |   1.07 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    48.84 μs |   6.421 μs | 0.352 μs |   2.23 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    85.29 μs |   5.912 μs | 0.324 μs |   3.89 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    38.73 μs |   2.758 μs | 0.151 μs |   1.77 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,902.17 μs |  96.877 μs | 5.310 μs | 177.90 |    0.44 |    8 | 3.9063 | 108.83 KB |       16.89 |
 'Lambda Include'                     | 2             |    11.08 μs |   1.413 μs | 0.077 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.10 μs |   2.176 μs | 0.119 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.11 μs |   3.536 μs | 0.194 μs |   1.14 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.10 μs |   2.176 μs | 0.119 μs |   1.37 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    84.86 μs |  17.458 μs | 0.957 μs |   3.87 |    0.04 |    7 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |          |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.55 μs** |   **1.754 μs** | **0.096 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.25 μs |   0.631 μs | 0.035 μs |   0.80 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    84.02 μs |   6.947 μs | 0.381 μs |   3.90 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.19 μs |   0.862 μs | 0.047 μs |   1.08 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    48.37 μs |   3.573 μs | 0.196 μs |   2.24 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.21 μs |  10.219 μs | 0.560 μs |   4.09 |    0.03 |    7 | 1.2207 |   21.2 KB |        3.29 |
 'Keyset pagination'                  | 10            |    39.82 μs |   2.433 μs | 0.133 μs |   1.85 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,859.18 μs | 180.742 μs | 9.907 μs | 179.10 |    0.80 |    8 | 3.9063 | 108.89 KB |       16.90 |
 'Lambda Include'                     | 10            |    10.94 μs |   0.690 μs | 0.038 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    10.90 μs |   0.305 μs | 0.017 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.37 μs |   2.073 μs | 0.114 μs |   1.13 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.00 μs |   1.768 μs | 0.097 μs |   1.39 |    0.01 |    4 | 0.5798 |   9.67 KB |        1.50 |
 'Full specification (all features)'  | 10            |    82.88 μs |   7.643 μs | 0.419 μs |   3.85 |    0.02 |    7 | 1.0986 |  19.88 KB |        3.08 |
