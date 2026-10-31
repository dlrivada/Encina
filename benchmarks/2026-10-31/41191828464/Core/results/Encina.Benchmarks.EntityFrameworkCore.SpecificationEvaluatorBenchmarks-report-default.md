
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **23.52 μs** |   **1.996 μs** |  **0.109 μs** |   **1.00** |    **0.01** |    **3** | **0.3967** |   **6.55 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.86 μs |   1.442 μs |  0.079 μs |   0.76 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.80 |
 'Complex predicates (parameterized)' | 2             |    24.55 μs |   3.173 μs |  0.174 μs |   1.04 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.94 |
 'Two criteria (AND)'                 | 2             |    24.16 μs |   6.410 μs |  0.351 μs |   1.03 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.94 |
 'Five criteria (AND)'                | 2             |    50.74 μs |   8.615 μs |  0.472 μs |   2.16 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.76 |
 'Ten criteria (AND)'                 | 2             |    89.78 μs |   4.451 μs |  0.244 μs |   3.82 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.19 |
 'Keyset pagination'                  | 2             |    41.31 μs |   2.276 μs |  0.125 μs |   1.76 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,957.31 μs | 490.436 μs | 26.882 μs | 168.25 |    1.20 |    7 |      - | 108.48 KB |       16.55 |
 'Lambda Include'                     | 2             |    11.32 μs |   0.902 μs |  0.049 μs |   0.48 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.65 |
 'String Include'                     | 2             |    11.66 μs |   0.807 μs |  0.044 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.65 |
 'Multi-column ordering'              | 2             |    25.80 μs |   7.166 μs |  0.393 μs |   1.10 |    0.02 |    3 | 0.5188 |   8.77 KB |        1.34 |
 'Offset pagination (Skip/Take)'      | 2             |    29.77 μs |   1.266 μs |  0.069 μs |   1.27 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.43 |
 'Full specification (all features)'  | 2             |    88.00 μs |  10.094 μs |  0.553 μs |   3.74 |    0.03 |    6 | 1.0986 |  19.88 KB |        3.03 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.48 μs** |   **1.814 μs** |  **0.099 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.54 μs |   2.116 μs |  0.116 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.98 μs |   9.742 μs |  0.534 μs |   4.14 |    0.03 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.38 μs |   2.315 μs |  0.127 μs |   1.14 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.27 μs |   3.728 μs |  0.204 μs |   2.34 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    89.71 μs |  25.916 μs |  1.421 μs |   4.18 |    0.06 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    41.25 μs |   4.450 μs |  0.244 μs |   1.92 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,012.61 μs | 714.074 μs | 39.141 μs | 186.83 |    1.75 |    8 |      - | 108.54 KB |       16.84 |
 'Lambda Include'                     | 10            |    11.05 μs |   0.995 μs |  0.055 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.67 μs |   1.923 μs |  0.105 μs |   0.54 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.06 μs |   1.235 μs |  0.068 μs |   1.17 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.25 μs |   1.494 μs |  0.082 μs |   1.41 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    89.30 μs |   3.495 μs |  0.192 μs |   4.16 |    0.02 |    7 | 1.0986 |  19.88 KB |        3.08 |
