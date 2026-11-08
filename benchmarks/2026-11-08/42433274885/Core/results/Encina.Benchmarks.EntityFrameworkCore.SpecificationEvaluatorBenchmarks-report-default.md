
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]    : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                               | CriteriaCount | Mean        | Error     | StdDev    | Median      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|----------:|----------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.12 μs** |  **0.188 μs** |  **0.264 μs** |    **22.15 μs** |   **1.00** |    **0.02** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.56 μs |  0.431 μs |  0.590 μs |    18.55 μs |   0.84 |    0.03 |    2 | 0.3357 |   5.55 KB |        0.86 |
 'Complex predicates (parameterized)' | 2             |    24.17 μs |  0.277 μs |  0.406 μs |    24.40 μs |   1.09 |    0.02 |    4 | 0.3662 |   6.44 KB |        1.00 |
 'Two criteria (AND)'                 | 2             |    24.19 μs |  0.047 μs |  0.066 μs |    24.19 μs |   1.09 |    0.01 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.32 μs |  0.134 μs |  0.192 μs |    49.31 μs |   2.23 |    0.03 |    8 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.57 μs |  0.457 μs |  0.655 μs |    88.70 μs |   4.00 |    0.06 |    9 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    41.17 μs |  0.389 μs |  0.546 μs |    40.81 μs |   1.86 |    0.03 |    7 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,022.76 μs | 58.794 μs | 86.179 μs | 3,986.66 μs | 181.87 |    4.38 |   10 | 3.9063 | 109.61 KB |       17.01 |
 'Lambda Include'                     | 2             |    11.54 μs |  0.202 μs |  0.303 μs |    11.54 μs |   0.52 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.64 μs |  0.049 μs |  0.071 μs |    11.65 μs |   0.53 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    26.03 μs |  0.118 μs |  0.176 μs |    25.98 μs |   1.18 |    0.02 |    5 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.08 μs |  0.169 μs |  0.253 μs |    30.09 μs |   1.36 |    0.02 |    6 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    87.75 μs |  0.425 μs |  0.623 μs |    87.72 μs |   3.97 |    0.05 |    9 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |           |           |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.98 μs** |  **0.134 μs** |  **0.183 μs** |    **21.90 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.08 μs |  0.041 μs |  0.057 μs |    18.06 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.16 μs |  0.335 μs |  0.481 μs |    88.34 μs |   4.01 |    0.04 |    9 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.97 μs |  0.066 μs |  0.095 μs |    23.99 μs |   1.09 |    0.01 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.60 μs |  0.460 μs |  0.629 μs |    49.35 μs |   2.26 |    0.03 |    8 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.22 μs |  0.478 μs |  0.655 μs |    88.50 μs |   4.01 |    0.04 |    9 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    41.14 μs |  0.799 μs |  1.146 μs |    41.21 μs |   1.87 |    0.05 |    7 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,920.75 μs | 21.299 μs | 29.858 μs | 3,923.16 μs | 178.42 |    1.98 |   10 |      - | 109.02 KB |       16.91 |
 'Lambda Include'                     | 10            |    11.59 μs |  0.114 μs |  0.164 μs |    11.46 μs |   0.53 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.74 μs |  0.078 μs |  0.114 μs |    11.77 μs |   0.53 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.86 μs |  0.045 μs |  0.066 μs |    25.84 μs |   1.18 |    0.01 |    5 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.60 μs |  0.124 μs |  0.178 μs |    30.61 μs |   1.39 |    0.01 |    6 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    89.16 μs |  0.294 μs |  0.403 μs |    89.11 μs |   4.06 |    0.04 |    9 | 1.0986 |  19.88 KB |        3.08 |
