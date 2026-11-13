
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.44 μs** |   **2.855 μs** |  **0.156 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.38 μs |   1.634 μs |  0.090 μs |   0.81 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    22.69 μs |   0.366 μs |  0.020 μs |   1.06 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.33 μs |   2.406 μs |  0.132 μs |   1.09 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.61 μs |   4.031 μs |  0.221 μs |   2.31 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    89.44 μs |  13.832 μs |  0.758 μs |   4.17 |    0.04 |    6 | 1.2207 |  21.22 KB |        3.29 |
 'Keyset pagination'                  | 2             |    41.29 μs |  22.857 μs |  1.253 μs |   1.93 |    0.05 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,917.91 μs | 390.129 μs | 21.384 μs | 182.74 |    1.44 |    7 |      - | 109.61 KB |       17.01 |
 'Lambda Include'                     | 2             |    11.17 μs |   1.071 μs |  0.059 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.55 μs |   1.611 μs |  0.088 μs |   0.54 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.88 μs |   3.911 μs |  0.214 μs |   1.16 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.93 μs |   1.375 μs |  0.075 μs |   1.40 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    85.52 μs |   4.489 μs |  0.246 μs |   3.99 |    0.03 |    6 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.65 μs** |   **4.435 μs** |  **0.243 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.78 μs |   2.773 μs |  0.152 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.92 μs |   5.992 μs |  0.328 μs |   4.06 |    0.04 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.39 μs |   0.911 μs |  0.050 μs |   1.08 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.08 μs |   3.843 μs |  0.211 μs |   2.31 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.47 μs |   8.155 μs |  0.447 μs |   4.09 |    0.04 |    6 | 1.2207 |  21.06 KB |        3.27 |
 'Keyset pagination'                  | 10            |    41.44 μs |   3.017 μs |  0.165 μs |   1.91 |    0.02 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,926.24 μs | 347.839 μs | 19.066 μs | 181.36 |    1.92 |    7 | 3.9063 | 108.86 KB |       16.89 |
 'Lambda Include'                     | 10            |    11.68 μs |   1.032 μs |  0.057 μs |   0.54 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.46 μs |   0.973 μs |  0.053 μs |   0.53 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.26 μs |   0.616 μs |  0.034 μs |   1.17 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.95 μs |   1.027 μs |  0.056 μs |   1.38 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    86.11 μs |   5.285 μs |  0.290 μs |   3.98 |    0.04 |    6 | 1.0986 |  19.88 KB |        3.08 |
