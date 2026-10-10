
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.71GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **23.29 μs** |     **2.947 μs** |  **0.162 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.06 μs |     0.892 μs |  0.049 μs |   0.78 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.18 μs |     1.677 μs |  0.092 μs |   1.00 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.21 μs |     1.789 μs |  0.098 μs |   1.04 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    52.37 μs |     8.529 μs |  0.468 μs |   2.25 |    0.02 |    6 | 0.6714 |  11.71 KB |        1.82 |
 'Ten criteria (AND)'                 | 2             |    88.29 μs |     8.665 μs |  0.475 μs |   3.79 |    0.03 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    39.90 μs |     3.116 μs |  0.171 μs |   1.71 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,947.02 μs |   649.815 μs | 35.619 μs | 169.45 |    1.67 |    8 |      - | 109.39 KB |       16.97 |
 'Lambda Include'                     | 2             |    11.51 μs |     1.103 μs |  0.060 μs |   0.49 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.15 μs |     0.501 μs |  0.027 μs |   0.48 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.79 μs |     1.840 μs |  0.101 μs |   1.06 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.18 μs |     3.357 μs |  0.184 μs |   1.30 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    85.88 μs |     6.574 μs |  0.360 μs |   3.69 |    0.03 |    7 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.95 μs** |     **1.977 μs** |  **0.108 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.87 μs |     3.893 μs |  0.213 μs |   0.81 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.77 μs |    13.625 μs |  0.747 μs |   4.04 |    0.03 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.89 μs |     3.364 μs |  0.184 μs |   1.09 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.86 μs |     2.493 μs |  0.137 μs |   2.32 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.98 μs |    14.737 μs |  0.808 μs |   4.01 |    0.04 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    41.31 μs |     3.045 μs |  0.167 μs |   1.88 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,092.85 μs | 1,196.448 μs | 65.581 μs | 186.45 |    2.71 |    8 |      - |  109.2 KB |       16.94 |
 'Lambda Include'                     | 10            |    11.46 μs |     0.585 μs |  0.032 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.06 μs |     0.292 μs |  0.016 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.97 μs |     2.828 μs |  0.155 μs |   1.14 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.35 μs |     0.951 μs |  0.052 μs |   1.38 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    88.42 μs |     7.766 μs |  0.426 μs |   4.03 |    0.02 |    7 | 1.0986 |  19.88 KB |        3.08 |
