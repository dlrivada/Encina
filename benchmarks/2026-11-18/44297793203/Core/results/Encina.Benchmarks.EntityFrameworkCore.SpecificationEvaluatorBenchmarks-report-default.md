
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.85 μs** |     **2.055 μs** |   **0.113 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.88 μs |     2.825 μs |   0.155 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.20 μs |     4.526 μs |   0.248 μs |   1.11 |    0.01 |    3 | 0.3662 |   6.29 KB |        0.98 |
 'Two criteria (AND)'                 | 2             |    24.10 μs |     1.696 μs |   0.093 μs |   1.10 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.66 μs |     3.696 μs |   0.203 μs |   2.27 |    0.01 |    6 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.23 μs |     4.606 μs |   0.252 μs |   4.04 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    40.91 μs |     4.144 μs |   0.227 μs |   1.87 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,168.13 μs |   865.802 μs |  47.458 μs | 190.73 |    2.07 |    8 |      - |  109.2 KB |       16.94 |
 'Lambda Include'                     | 2             |    11.31 μs |     0.178 μs |   0.010 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.52 μs |     0.929 μs |   0.051 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.76 μs |     0.875 μs |   0.048 μs |   1.18 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    32.08 μs |     3.631 μs |   0.199 μs |   1.47 |    0.01 |    4 | 0.5493 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 2             |    91.15 μs |    14.425 μs |   0.791 μs |   4.17 |    0.04 |    7 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |             |              |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.39 μs** |     **2.187 μs** |   **0.120 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.44 μs |     1.155 μs |   0.063 μs |   0.86 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.06 μs |     3.680 μs |   0.202 μs |   4.12 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.05 μs |     2.740 μs |   0.150 μs |   1.08 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.05 μs |    13.254 μs |   0.726 μs |   2.29 |    0.03 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.96 μs |     9.476 μs |   0.519 μs |   4.11 |    0.03 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    41.00 μs |     3.028 μs |   0.166 μs |   1.92 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,371.50 μs | 2,207.556 μs | 121.004 μs | 204.38 |    5.00 |    6 |      - | 109.61 KB |       17.01 |
 'Lambda Include'                     | 10            |    11.25 μs |     0.612 μs |   0.034 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.43 μs |     0.363 μs |   0.020 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.08 μs |     1.303 μs |   0.071 μs |   1.17 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.12 μs |     2.151 μs |   0.118 μs |   1.41 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    88.97 μs |    10.682 μs |   0.586 μs |   4.16 |    0.03 |    5 | 1.0986 |  19.88 KB |        3.08 |
