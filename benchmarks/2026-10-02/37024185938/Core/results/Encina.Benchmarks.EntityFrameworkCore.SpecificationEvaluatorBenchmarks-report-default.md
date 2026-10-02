
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.09 μs** |     **1.619 μs** |   **0.089 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.07 μs |     3.999 μs |   0.219 μs |   0.86 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.66 μs |     3.659 μs |   0.201 μs |   1.17 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    25.41 μs |     2.545 μs |   0.140 μs |   1.21 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    52.01 μs |     2.467 μs |   0.135 μs |   2.47 |    0.01 |    4 | 0.6714 |  11.85 KB |        1.84 |
 'Ten criteria (AND)'                 | 2             |    90.39 μs |     7.665 μs |   0.420 μs |   4.29 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    39.90 μs |     3.608 μs |   0.198 μs |   1.89 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,055.05 μs | 1,968.227 μs | 107.885 μs | 192.31 |    4.49 |    6 |      - | 109.61 KB |       17.01 |
 'Lambda Include'                     | 2             |    11.08 μs |     1.062 μs |   0.058 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.06 μs |     1.458 μs |   0.080 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.55 μs |     0.408 μs |   0.022 μs |   1.16 |    0.00 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.36 μs |     2.670 μs |   0.146 μs |   1.44 |    0.01 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    89.83 μs |    11.809 μs |   0.647 μs |   4.26 |    0.03 |    5 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |             |              |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **23.14 μs** |     **2.018 μs** |   **0.111 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.46 μs |     1.570 μs |   0.086 μs |   0.75 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    89.63 μs |     7.295 μs |   0.400 μs |   3.87 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.74 μs |     1.624 μs |   0.089 μs |   1.03 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.77 μs |     0.958 μs |   0.053 μs |   2.19 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.90 μs |     8.487 μs |   0.465 μs |   3.80 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    41.84 μs |     6.828 μs |   0.374 μs |   1.81 |    0.02 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,243.99 μs |   828.318 μs |  45.403 μs | 183.39 |    1.86 |    8 |      - | 109.31 KB |       16.96 |
 'Lambda Include'                     | 10            |    11.54 μs |     0.730 μs |   0.040 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.46 μs |     4.107 μs |   0.225 μs |   0.50 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.81 μs |     1.480 μs |   0.081 μs |   1.07 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.07 μs |     2.462 μs |   0.135 μs |   1.30 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    90.01 μs |     8.736 μs |   0.479 μs |   3.89 |    0.02 |    7 | 1.2207 |  20.03 KB |        3.11 |
