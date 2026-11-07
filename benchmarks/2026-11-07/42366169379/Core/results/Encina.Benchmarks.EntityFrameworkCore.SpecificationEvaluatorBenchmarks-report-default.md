
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error      | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-----------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.53 μs** |   **0.570 μs** |  **0.031 μs** |   **1.00** |    **0.00** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.15 μs |   2.403 μs |  0.132 μs |   0.84 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.31 μs |   3.494 μs |  0.192 μs |   1.08 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.63 μs |   1.352 μs |  0.074 μs |   1.14 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.38 μs |   4.287 μs |  0.235 μs |   2.29 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    89.17 μs |   4.159 μs |  0.228 μs |   4.14 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    39.79 μs |   6.295 μs |  0.345 μs |   1.85 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,910.59 μs | 442.575 μs | 24.259 μs | 181.65 |    1.00 |    7 |      - |  109.6 KB |       17.00 |
 'Lambda Include'                     | 2             |    11.82 μs |   3.645 μs |  0.200 μs |   0.55 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.24 μs |   0.673 μs |  0.037 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    26.03 μs |   1.844 μs |  0.101 μs |   1.21 |    0.00 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    31.62 μs |   3.903 μs |  0.214 μs |   1.47 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    91.36 μs |   6.326 μs |  0.347 μs |   4.24 |    0.01 |    6 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |             |            |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **21.78 μs** |   **1.168 μs** |  **0.064 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.99 μs |   1.226 μs |  0.067 μs |   0.83 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    91.28 μs |   3.851 μs |  0.211 μs |   4.19 |    0.01 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    24.34 μs |   1.786 μs |  0.098 μs |   1.12 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.24 μs |   3.890 μs |  0.213 μs |   2.31 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.76 μs |   4.636 μs |  0.254 μs |   4.03 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.30 μs |   4.490 μs |  0.246 μs |   1.85 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,982.42 μs | 813.077 μs | 44.567 μs | 182.89 |    1.83 |    7 |      - | 109.61 KB |       17.01 |
 'Lambda Include'                     | 10            |    11.41 μs |   0.944 μs |  0.052 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.08 μs |   1.061 μs |  0.058 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.87 μs |   7.326 μs |  0.402 μs |   1.19 |    0.02 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.75 μs |   1.877 μs |  0.103 μs |   1.37 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    87.05 μs |   4.195 μs |  0.230 μs |   4.00 |    0.01 |    6 | 1.0986 |  19.88 KB |        3.08 |
