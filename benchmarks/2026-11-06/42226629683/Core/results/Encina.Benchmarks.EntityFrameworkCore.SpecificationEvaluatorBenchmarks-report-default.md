
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **21.61 μs** |     **2.636 μs** |  **0.145 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    18.13 μs |     1.229 μs |  0.067 μs |   0.84 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.21 μs |     3.233 μs |  0.177 μs |   1.12 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.54 μs |     1.620 μs |  0.089 μs |   1.09 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.47 μs |     4.070 μs |  0.223 μs |   2.34 |    0.02 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    87.42 μs |    11.238 μs |  0.616 μs |   4.05 |    0.03 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    41.85 μs |     8.848 μs |  0.485 μs |   1.94 |    0.02 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,979.73 μs | 1,219.598 μs | 66.850 μs | 184.19 |    2.88 |    6 |      - | 109.15 KB |       16.93 |
 'Lambda Include'                     | 2             |    11.67 μs |     1.493 μs |  0.082 μs |   0.54 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.19 μs |     1.158 μs |  0.063 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.64 μs |     1.435 μs |  0.079 μs |   1.19 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    29.56 μs |     3.874 μs |  0.212 μs |   1.37 |    0.01 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    89.47 μs |     4.595 μs |  0.252 μs |   4.14 |    0.03 |    5 | 1.2207 |  20.18 KB |        3.13 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.10 μs** |     **2.358 μs** |  **0.129 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.23 μs |     1.703 μs |  0.093 μs |   0.82 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    90.78 μs |    15.481 μs |  0.849 μs |   4.11 |    0.04 |    6 | 1.2207 |  21.22 KB |        3.29 |
 'Two criteria (AND)'                 | 10            |    24.48 μs |    16.246 μs |  0.890 μs |   1.11 |    0.04 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.12 μs |     0.970 μs |  0.053 μs |   2.27 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.36 μs |     4.344 μs |  0.238 μs |   4.00 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    42.72 μs |     1.126 μs |  0.062 μs |   1.93 |    0.01 |    5 | 0.6104 |  10.88 KB |        1.69 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,041.29 μs |   813.626 μs | 44.598 μs | 182.85 |    1.98 |    7 |      - | 110.13 KB |       17.09 |
 'Lambda Include'                     | 10            |    11.49 μs |     0.784 μs |  0.043 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.63 μs |     2.207 μs |  0.121 μs |   0.53 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.85 μs |     2.216 μs |  0.121 μs |   1.17 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    31.11 μs |     3.245 μs |  0.178 μs |   1.41 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    89.56 μs |    10.387 μs |  0.569 μs |   4.05 |    0.03 |    6 | 1.0986 |  19.88 KB |        3.08 |
