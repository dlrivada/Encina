
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 2.63GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.24 μs** |     **1.893 μs** |  **0.104 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    19.02 μs |     4.488 μs |  0.246 μs |   0.86 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.02 μs |     2.247 μs |  0.123 μs |   1.08 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.95 μs |     0.868 μs |  0.048 μs |   1.12 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    49.96 μs |     1.898 μs |  0.104 μs |   2.25 |    0.01 |    4 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    88.05 μs |     8.490 μs |  0.465 μs |   3.96 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    41.40 μs |     3.816 μs |  0.209 μs |   1.86 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,056.02 μs | 1,145.331 μs | 62.779 μs | 182.36 |    2.55 |    6 |      - | 109.67 KB |       17.02 |
 'Lambda Include'                     | 2             |    12.00 μs |     1.399 μs |  0.077 μs |   0.54 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.30 μs |     0.569 μs |  0.031 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.97 μs |     2.013 μs |  0.110 μs |   1.17 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.94 μs |     7.720 μs |  0.423 μs |   1.39 |    0.02 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    89.57 μs |     3.935 μs |  0.216 μs |   4.03 |    0.02 |    5 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.09 μs** |     **1.222 μs** |  **0.067 μs** |   **1.00** |    **0.00** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    18.21 μs |     1.624 μs |  0.089 μs |   0.82 |    0.00 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    88.51 μs |     3.272 μs |  0.179 μs |   4.01 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.86 μs |     3.104 μs |  0.170 μs |   1.08 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    49.39 μs |    10.911 μs |  0.598 μs |   2.24 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    87.92 μs |     9.992 μs |  0.548 μs |   3.98 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.48 μs |     4.975 μs |  0.273 μs |   1.83 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,126.48 μs | 1,467.362 μs | 80.431 μs | 186.83 |    3.19 |    7 |      - | 108.64 KB |       16.86 |
 'Lambda Include'                     | 10            |    11.41 μs |     0.936 μs |  0.051 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.70 μs |     0.584 μs |  0.032 μs |   0.53 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    27.25 μs |     6.501 μs |  0.356 μs |   1.23 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.71 μs |     4.001 μs |  0.219 μs |   1.39 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    89.12 μs |     5.221 μs |  0.286 μs |   4.03 |    0.02 |    6 | 1.0986 |  19.88 KB |        3.08 |
