
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.83 μs** |   **1.356 μs** |  **0.074 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.23 μs |   1.493 μs |  0.082 μs |   0.75 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.58 μs |   2.331 μs |  0.128 μs |   1.03 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.10 μs |   2.326 μs |  0.128 μs |   1.01 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.04 μs |   7.457 μs |  0.409 μs |   2.15 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.41 μs |   7.367 μs |  0.404 μs |   3.87 |    0.02 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    39.24 μs |   2.855 μs |  0.156 μs |   1.72 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,971.18 μs | 475.038 μs | 26.038 μs | 173.91 |    1.10 |    7 |      - | 109.72 KB |       17.02 |
 'Lambda Include'                     | 2             |    11.54 μs |   0.319 μs |  0.017 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.47 μs |   0.625 μs |  0.034 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.43 μs |   0.762 μs |  0.042 μs |   1.11 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.61 μs |   4.656 μs |  0.255 μs |   1.30 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    86.44 μs |  10.018 μs |  0.549 μs |   3.79 |    0.02 |    6 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.46 μs** |   **1.212 μs** |  **0.066 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    16.99 μs |   1.710 μs |  0.094 μs |   0.79 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.40 μs |   5.177 μs |  0.284 μs |   4.12 |    0.02 |    7 | 1.2207 |  21.06 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    22.79 μs |   0.789 μs |  0.043 μs |   1.06 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    48.49 μs |   1.884 μs |  0.103 μs |   2.26 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.34 μs |   3.317 μs |  0.182 μs |   4.07 |    0.01 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.41 μs |   2.984 μs |  0.164 μs |   1.88 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,955.41 μs | 423.571 μs | 23.217 μs | 184.33 |    1.06 |    8 |      - | 108.63 KB |       16.85 |
 'Lambda Include'                     | 10            |    11.03 μs |   0.235 μs |  0.013 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.00 μs |   1.187 μs |  0.065 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    24.33 μs |   0.562 μs |  0.031 μs |   1.13 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.19 μs |   1.316 μs |  0.072 μs |   1.41 |    0.00 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    85.28 μs |   5.081 μs |  0.279 μs |   3.97 |    0.02 |    7 | 1.0986 |  19.88 KB |        3.08 |
