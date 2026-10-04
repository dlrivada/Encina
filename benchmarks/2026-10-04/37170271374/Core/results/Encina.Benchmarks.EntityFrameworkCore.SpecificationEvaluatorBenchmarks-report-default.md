
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                               | CriteriaCount | Mean        | Error     | StdDev    | Median      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|----------:|----------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.71 μs** |  **0.347 μs** |  **0.509 μs** |    **22.06 μs** |   **1.00** |    **0.03** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.46 μs |  0.188 μs |  0.275 μs |    17.32 μs |   0.80 |    0.02 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.48 μs |  0.485 μs |  0.696 μs |    24.46 μs |   1.13 |    0.04 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.30 μs |  0.165 μs |  0.226 μs |    24.26 μs |   1.12 |    0.03 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.74 μs |  0.197 μs |  0.295 μs |    49.79 μs |   2.29 |    0.05 |    7 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    89.98 μs |  1.538 μs |  2.255 μs |    91.83 μs |   4.15 |    0.14 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.78 μs |  0.239 μs |  0.350 μs |    40.74 μs |   1.88 |    0.05 |    6 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,994.27 μs | 21.514 μs | 32.201 μs | 3,987.36 μs | 184.07 |    4.49 |    9 |      - | 109.35 KB |       16.97 |
 'Lambda Include'                     | 2             |    11.24 μs |  0.117 μs |  0.163 μs |    11.13 μs |   0.52 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    10.99 μs |  0.101 μs |  0.148 μs |    11.08 μs |   0.51 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.84 μs |  0.250 μs |  0.359 μs |    25.87 μs |   1.19 |    0.03 |    4 | 0.5493 |   9.07 KB |        1.41 |
 'Offset pagination (Skip/Take)'      | 2             |    30.24 μs |  0.264 μs |  0.379 μs |    30.00 μs |   1.39 |    0.04 |    5 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    87.80 μs |  0.242 μs |  0.347 μs |    87.76 μs |   4.05 |    0.09 |    8 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |           |           |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.20 μs** |  **0.307 μs** |  **0.440 μs** |    **22.44 μs** |   **1.00** |    **0.03** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.20 μs |  0.278 μs |  0.408 μs |    17.52 μs |   0.78 |    0.02 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.22 μs |  0.663 μs |  0.992 μs |    88.27 μs |   3.98 |    0.09 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.66 μs |  0.281 μs |  0.421 μs |    23.67 μs |   1.07 |    0.03 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.89 μs |  0.260 μs |  0.382 μs |    49.83 μs |   2.25 |    0.05 |    7 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    90.61 μs |  0.351 μs |  0.514 μs |    90.77 μs |   4.08 |    0.08 |    8 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 10            |    40.52 μs |  0.126 μs |  0.188 μs |    40.50 μs |   1.83 |    0.04 |    6 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,979.38 μs | 22.218 μs | 33.255 μs | 3,981.07 μs | 179.35 |    3.80 |    9 |      - | 109.05 KB |       16.92 |
 'Lambda Include'                     | 10            |    11.27 μs |  0.073 μs |  0.107 μs |    11.22 μs |   0.51 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.25 μs |  0.143 μs |  0.210 μs |    11.40 μs |   0.51 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.53 μs |  0.224 μs |  0.329 μs |    24.76 μs |   1.11 |    0.03 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.66 μs |  0.520 μs |  0.746 μs |    30.08 μs |   1.38 |    0.04 |    5 | 0.5493 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 10            |    89.58 μs |  0.752 μs |  1.079 μs |    89.67 μs |   4.04 |    0.09 |    8 | 1.2207 |  20.04 KB |        3.11 |
