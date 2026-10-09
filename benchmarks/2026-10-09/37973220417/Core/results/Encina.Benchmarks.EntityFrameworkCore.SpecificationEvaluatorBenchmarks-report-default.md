
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.39 μs** |     **1.196 μs** |  **0.066 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    16.93 μs |     2.211 μs |  0.121 μs |   0.76 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.19 μs |     2.864 μs |  0.157 μs |   1.08 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.81 μs |     1.840 μs |  0.101 μs |   1.06 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    51.75 μs |     4.624 μs |  0.253 μs |   2.31 |    0.01 |    6 | 0.6714 |   11.7 KB |        1.82 |
 'Ten criteria (AND)'                 | 2             |    90.63 μs |     9.478 μs |  0.519 μs |   4.05 |    0.02 |    7 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    40.26 μs |     2.417 μs |  0.133 μs |   1.80 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,083.63 μs | 1,056.334 μs | 57.901 μs | 182.43 |    2.29 |    8 |      - | 108.62 KB |       16.85 |
 'Lambda Include'                     | 2             |    11.49 μs |     0.850 μs |  0.047 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.02 μs |     0.728 μs |  0.040 μs |   0.49 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    24.75 μs |     1.233 μs |  0.068 μs |   1.11 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.42 μs |     2.266 μs |  0.124 μs |   1.36 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    87.62 μs |    10.588 μs |  0.580 μs |   3.91 |    0.02 |    7 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.38 μs** |     **2.454 μs** |  **0.135 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.00 μs |     0.823 μs |  0.045 μs |   0.80 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    90.04 μs |     8.180 μs |  0.448 μs |   4.21 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.09 μs |     2.945 μs |  0.161 μs |   1.08 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.58 μs |     2.676 μs |  0.147 μs |   2.32 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    89.29 μs |     9.990 μs |  0.548 μs |   4.18 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    39.31 μs |     1.124 μs |  0.062 μs |   1.84 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,033.54 μs |   514.553 μs | 28.204 μs | 188.64 |    1.53 |    7 |      - | 109.42 KB |       16.98 |
 'Lambda Include'                     | 10            |    11.39 μs |     0.748 μs |  0.041 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.13 μs |     1.434 μs |  0.079 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.95 μs |     1.086 μs |  0.060 μs |   1.17 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.63 μs |     0.917 μs |  0.050 μs |   1.39 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    90.46 μs |     7.069 μs |  0.387 μs |   4.23 |    0.03 |    6 | 1.2207 |  20.04 KB |        3.11 |
