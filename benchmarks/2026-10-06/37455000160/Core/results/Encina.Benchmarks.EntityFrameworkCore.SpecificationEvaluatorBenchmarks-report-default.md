
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.47 μs** |   **0.750 μs** |  **0.041 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.06 μs |   2.533 μs |  0.139 μs |   0.80 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.85 μs |   1.735 μs |  0.095 μs |   1.11 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.10 μs |   1.942 μs |  0.106 μs |   1.03 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.45 μs |   1.905 μs |  0.104 μs |   2.25 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    89.19 μs |   9.515 μs |  0.522 μs |   3.97 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.39 μs |   3.115 μs |  0.171 μs |   1.80 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,945.84 μs | 487.237 μs | 26.707 μs | 175.59 |    1.07 |    8 |      - | 109.32 KB |       16.96 |
 'Lambda Include'                     | 2             |    11.46 μs |   0.756 μs |  0.041 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.53 μs |   1.555 μs |  0.085 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.20 μs |   2.249 μs |  0.123 μs |   1.12 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    31.46 μs |   2.277 μs |  0.125 μs |   1.40 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    87.88 μs |   6.015 μs |  0.330 μs |   3.91 |    0.01 |    7 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.34 μs** |   **2.587 μs** |  **0.142 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.23 μs |   1.201 μs |  0.066 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.98 μs |  10.360 μs |  0.568 μs |   3.98 |    0.03 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.23 μs |   1.561 μs |  0.086 μs |   1.04 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.94 μs |   4.569 μs |  0.250 μs |   2.24 |    0.02 |    6 | 0.6714 |  11.71 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    89.02 μs |   4.376 μs |  0.240 μs |   3.99 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.68 μs |   2.124 μs |  0.116 μs |   1.82 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,971.61 μs | 419.752 μs | 23.008 μs | 177.82 |    1.32 |    8 |      - | 109.45 KB |       16.98 |
 'Lambda Include'                     | 10            |    11.07 μs |   0.942 μs |  0.052 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.12 μs |   0.830 μs |  0.046 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.06 μs |   5.268 μs |  0.289 μs |   1.12 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.72 μs |   3.278 μs |  0.180 μs |   1.38 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    91.32 μs |   5.945 μs |  0.326 μs |   4.09 |    0.03 |    7 | 1.0986 |  19.88 KB |        3.08 |
