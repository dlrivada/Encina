
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.11 μs** |   **1.659 μs** |  **0.091 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.78 μs |   1.520 μs |  0.083 μs |   0.84 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.59 μs |   2.193 μs |  0.120 μs |   1.12 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.54 μs |   1.434 μs |  0.079 μs |   1.11 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.36 μs |   3.179 μs |  0.174 μs |   2.34 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.58 μs |   8.244 μs |  0.452 μs |   4.20 |    0.02 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    40.00 μs |   2.588 μs |  0.142 μs |   1.89 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,930.59 μs | 875.390 μs | 47.983 μs | 186.16 |    2.09 |    7 | 3.9063 | 108.79 KB |       16.88 |
 'Lambda Include'                     | 2             |    11.27 μs |   0.711 μs |  0.039 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.08 μs |   0.468 μs |  0.026 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.85 μs |   1.276 μs |  0.070 μs |   1.18 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    31.21 μs |   1.561 μs |  0.086 μs |   1.48 |    0.01 |    3 | 0.5493 |   9.67 KB |        1.50 |
 'Full specification (all features)'  | 2             |    85.66 μs |   3.968 μs |  0.217 μs |   4.06 |    0.02 |    6 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.18 μs** |   **1.063 μs** |  **0.058 μs** |   **1.00** |    **0.00** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.13 μs |   3.099 μs |  0.170 μs |   0.86 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    89.09 μs |  11.541 μs |  0.633 μs |   4.21 |    0.03 |    5 | 1.2207 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    22.79 μs |   1.404 μs |  0.077 μs |   1.08 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.24 μs |   3.039 μs |  0.167 μs |   2.32 |    0.01 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.65 μs |  12.823 μs |  0.703 μs |   4.18 |    0.03 |    5 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 10            |    39.54 μs |   4.564 μs |  0.250 μs |   1.87 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,948.38 μs | 706.096 μs | 38.704 μs | 186.39 |    1.64 |    6 | 3.9063 | 110.18 KB |       17.09 |
 'Lambda Include'                     | 10            |    11.13 μs |   0.943 μs |  0.052 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.23 μs |   0.372 μs |  0.020 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.16 μs |   1.955 μs |  0.107 μs |   1.19 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.90 μs |   2.049 μs |  0.112 μs |   1.41 |    0.01 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    86.50 μs |   4.610 μs |  0.253 μs |   4.08 |    0.01 |    5 | 1.0986 |  19.88 KB |        3.08 |
