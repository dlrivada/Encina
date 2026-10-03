
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **23.27 μs** |   **4.176 μs** |  **0.229 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.35 μs |   3.845 μs |  0.211 μs |   0.79 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.10 μs |   1.264 μs |  0.069 μs |   1.04 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.96 μs |   1.469 μs |  0.081 μs |   1.03 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.77 μs |   2.684 μs |  0.147 μs |   2.14 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.92 μs |   7.952 μs |  0.436 μs |   3.82 |    0.04 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    39.76 μs |   1.726 μs |  0.095 μs |   1.71 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,006.16 μs |  62.447 μs |  3.423 μs | 172.18 |    1.47 |    7 |      - | 109.31 KB |       16.96 |
 'Lambda Include'                     | 2             |    11.73 μs |   1.339 μs |  0.073 μs |   0.50 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.46 μs |   3.011 μs |  0.165 μs |   0.49 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.57 μs |   2.112 μs |  0.116 μs |   1.10 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.06 μs |   1.922 μs |  0.105 μs |   1.29 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    88.88 μs |  21.556 μs |  1.182 μs |   3.82 |    0.05 |    6 | 1.2207 |  20.18 KB |        3.13 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.93 μs** |   **1.867 μs** |  **0.102 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.99 μs |   3.421 μs |  0.188 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    89.66 μs |   4.372 μs |  0.240 μs |   4.09 |    0.02 |    5 | 1.2207 |  21.06 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    23.78 μs |   2.285 μs |  0.125 μs |   1.08 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.00 μs |   6.091 μs |  0.334 μs |   2.23 |    0.02 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    90.81 μs |   5.997 μs |  0.329 μs |   4.14 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    41.24 μs |   6.245 μs |  0.342 μs |   1.88 |    0.02 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,937.19 μs | 267.446 μs | 14.660 μs | 179.55 |    0.93 |    6 | 3.9063 | 109.29 KB |       16.96 |
 'Lambda Include'                     | 10            |    10.98 μs |   0.283 μs |  0.016 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.60 μs |   0.640 μs |  0.035 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.82 μs |   1.716 μs |  0.094 μs |   1.18 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.45 μs |   0.726 μs |  0.040 μs |   1.34 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    84.98 μs |   7.692 μs |  0.422 μs |   3.88 |    0.02 |    5 | 1.0986 |  19.88 KB |        3.08 |
