
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.63 μs** |     **3.155 μs** |  **0.173 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.00 μs |     2.582 μs |  0.142 μs |   0.80 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.65 μs |     3.109 μs |  0.170 μs |   1.09 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.22 μs |     3.204 μs |  0.176 μs |   1.07 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.56 μs |     0.898 μs |  0.049 μs |   2.23 |    0.01 |    5 | 0.6714 |   11.7 KB |        1.82 |
 'Ten criteria (AND)'                 | 2             |    88.58 μs |     4.666 μs |  0.256 μs |   3.91 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.83 μs |     4.282 μs |  0.235 μs |   1.80 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,079.66 μs | 1,096.529 μs | 60.104 μs | 180.29 |    2.59 |    7 |      - | 108.51 KB |       16.84 |
 'Lambda Include'                     | 2             |    11.69 μs |     0.057 μs |  0.003 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.27 μs |     0.546 μs |  0.030 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.55 μs |     3.599 μs |  0.197 μs |   1.13 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.51 μs |     2.147 μs |  0.118 μs |   1.30 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    88.83 μs |    12.981 μs |  0.712 μs |   3.93 |    0.04 |    6 | 1.2207 |  20.04 KB |        3.11 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **23.32 μs** |     **1.558 μs** |  **0.085 μs** |   **1.00** |    **0.00** |    **3** | **0.3967** |   **6.73 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.77 μs |     1.780 μs |  0.098 μs |   0.76 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.78 |
 'Complex predicates (parameterized)' | 10            |    87.32 μs |     7.942 μs |  0.435 μs |   3.75 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.11 |
 'Two criteria (AND)'                 | 10            |    24.41 μs |     2.197 μs |  0.120 μs |   1.05 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 10            |    49.82 μs |     8.555 μs |  0.469 μs |   2.14 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.72 |
 'Ten criteria (AND)'                 | 10            |    88.01 μs |     6.598 μs |  0.362 μs |   3.77 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.11 |
 'Keyset pagination'                  | 10            |    39.95 μs |     7.227 μs |  0.396 μs |   1.71 |    0.02 |    4 | 0.6104 |  10.43 KB |        1.55 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,007.54 μs |    85.196 μs |  4.670 μs | 171.88 |    0.57 |    7 |      - | 109.07 KB |       16.22 |
 'Lambda Include'                     | 10            |    11.18 μs |     0.913 μs |  0.050 μs |   0.48 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'String Include'                     | 10            |    11.09 μs |     0.260 μs |  0.014 μs |   0.48 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 10            |    25.74 μs |     0.650 μs |  0.036 μs |   1.10 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 10            |    30.21 μs |     0.954 μs |  0.052 μs |   1.30 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.39 |
 'Full specification (all features)'  | 10            |    87.91 μs |     9.256 μs |  0.507 μs |   3.77 |    0.02 |    6 | 1.0986 |  19.88 KB |        2.96 |
