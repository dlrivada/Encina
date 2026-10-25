
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]    : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3

Job=MediumRun  IterationCount=15  LaunchCount=2  
WarmupCount=10  

 Method                               | CriteriaCount | Mean        | Error     | StdDev     | Median      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|----------:|-----------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.97 μs** |  **0.434 μs** |   **0.650 μs** |    **22.01 μs** |   **1.00** |    **0.04** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.99 μs |  0.175 μs |   0.250 μs |    17.98 μs |   0.82 |    0.03 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.05 μs |  0.066 μs |   0.093 μs |    24.02 μs |   1.10 |    0.03 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.62 μs |  0.211 μs |   0.296 μs |    24.53 μs |   1.12 |    0.04 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.75 μs |  0.492 μs |   0.705 μs |    49.79 μs |   2.27 |    0.07 |    8 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.84 μs |  0.275 μs |   0.376 μs |    88.96 μs |   4.05 |    0.12 |    9 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    41.12 μs |  1.033 μs |   1.482 μs |    39.96 μs |   1.87 |    0.09 |    7 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,250.59 μs | 77.210 μs | 115.565 μs | 4,221.00 μs | 193.68 |    7.66 |   10 |      - | 109.15 KB |       16.93 |
 'Lambda Include'                     | 2             |    11.71 μs |  0.245 μs |   0.359 μs |    11.95 μs |   0.53 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.42 μs |  0.091 μs |   0.130 μs |    11.38 μs |   0.52 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.66 μs |  0.140 μs |   0.201 μs |    25.67 μs |   1.17 |    0.04 |    5 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.32 μs |  0.132 μs |   0.194 μs |    30.33 μs |   1.38 |    0.04 |    6 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    89.39 μs |  1.798 μs |   2.579 μs |    89.44 μs |   4.07 |    0.17 |    9 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |           |            |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.26 μs** |  **0.087 μs** |   **0.125 μs** |    **22.26 μs** |   **1.00** |    **0.01** |    **4** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.16 μs |  0.283 μs |   0.407 μs |    18.16 μs |   0.82 |    0.02 |    3 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    89.16 μs |  0.439 μs |   0.630 μs |    89.15 μs |   4.01 |    0.04 |   10 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.73 μs |  0.134 μs |   0.192 μs |    23.74 μs |   1.07 |    0.01 |    5 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.57 μs |  0.691 μs |   0.946 μs |    49.51 μs |   2.23 |    0.04 |    9 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.28 μs |  0.315 μs |   0.452 μs |    88.36 μs |   3.97 |    0.03 |   10 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.21 μs |  0.187 μs |   0.268 μs |    40.24 μs |   1.81 |    0.02 |    8 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,008.32 μs | 33.256 μs |  49.777 μs | 4,023.19 μs | 180.07 |    2.41 |   11 |      - | 108.71 KB |       16.87 |
 'Lambda Include'                     | 10            |    11.58 μs |  0.027 μs |   0.039 μs |    11.58 μs |   0.52 |    0.00 |    2 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.09 μs |  0.069 μs |   0.101 μs |    11.09 μs |   0.50 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.65 μs |  0.348 μs |   0.499 μs |    25.75 μs |   1.15 |    0.02 |    6 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.16 μs |  0.070 μs |   0.100 μs |    30.14 μs |   1.36 |    0.01 |    7 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    88.58 μs |  0.611 μs |   0.895 μs |    88.98 μs |   3.98 |    0.05 |   10 | 1.0986 |  19.88 KB |        3.08 |
