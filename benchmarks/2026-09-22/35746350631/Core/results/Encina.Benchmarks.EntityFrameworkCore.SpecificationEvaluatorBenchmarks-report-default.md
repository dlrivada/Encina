
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.06 μs** |   **0.544 μs** |  **0.030 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.15 μs |   1.469 μs |  0.081 μs |   0.82 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    25.09 μs |   1.411 μs |  0.077 μs |   1.14 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.15 μs |   0.824 μs |  0.045 μs |   1.09 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.97 μs |   5.425 μs |  0.297 μs |   2.26 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    89.22 μs |   7.890 μs |  0.432 μs |   4.04 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.60 μs |   5.157 μs |  0.283 μs |   1.84 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,909.53 μs | 441.639 μs | 24.208 μs | 177.21 |    0.97 |    7 |      - | 108.93 KB |       16.90 |
 'Lambda Include'                     | 2             |    11.40 μs |   2.767 μs |  0.152 μs |   0.52 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.61 μs |   0.332 μs |  0.018 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.32 μs |   2.764 μs |  0.151 μs |   1.15 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.90 μs |   0.834 μs |  0.046 μs |   1.36 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    89.93 μs |   6.172 μs |  0.338 μs |   4.08 |    0.01 |    6 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **20.75 μs** |   **3.566 μs** |  **0.195 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.93 μs |   1.505 μs |  0.082 μs |   0.86 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    87.95 μs |   3.845 μs |  0.211 μs |   4.24 |    0.04 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.29 μs |   0.852 μs |  0.047 μs |   1.17 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    51.12 μs |   2.776 μs |  0.152 μs |   2.46 |    0.02 |    5 | 0.6714 |   11.7 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    92.60 μs |  10.900 μs |  0.597 μs |   4.46 |    0.04 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    41.09 μs |   6.882 μs |  0.377 μs |   1.98 |    0.02 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,945.57 μs | 204.372 μs | 11.202 μs | 190.17 |    1.62 |    7 |      - | 108.75 KB |       16.87 |
 'Lambda Include'                     | 10            |    11.54 μs |   0.930 μs |  0.051 μs |   0.56 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.58 μs |   1.033 μs |  0.057 μs |   0.56 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.42 μs |   2.316 μs |  0.127 μs |   1.23 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.62 μs |   2.095 μs |  0.115 μs |   1.48 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    90.41 μs |   6.075 μs |  0.333 μs |   4.36 |    0.04 |    6 | 1.0986 |  19.88 KB |        3.08 |
