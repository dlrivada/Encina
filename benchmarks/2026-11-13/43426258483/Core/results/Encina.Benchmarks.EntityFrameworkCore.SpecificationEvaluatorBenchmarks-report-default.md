
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **10.322 μs** |   **0.8928 μs** |  **0.0489 μs** |   **1.00** |    **0.01** |    **3** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     8.534 μs |   0.2119 μs |  0.0116 μs |   0.83 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    11.843 μs |   0.7233 μs |  0.0396 μs |   1.15 |    0.01 |    3 | 0.0610 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    11.647 μs |   0.9776 μs |  0.0536 μs |   1.13 |    0.01 |    3 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    26.175 μs |   1.1777 μs |  0.0646 μs |   2.54 |    0.01 |    5 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    48.778 μs |   4.3864 μs |  0.2404 μs |   4.73 |    0.03 |    6 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    18.572 μs |   0.4266 μs |  0.0234 μs |   1.80 |    0.01 |    4 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,237.134 μs | 297.7604 μs | 16.3213 μs | 313.63 |    1.88 |    7 |      - | 109.08 KB |       16.92 |
 'Lambda Include'                     | 2             |     6.267 μs |   0.2538 μs |  0.0139 μs |   0.61 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     6.297 μs |   0.1467 μs |  0.0080 μs |   0.61 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    14.823 μs |   0.1061 μs |  0.0058 μs |   1.44 |    0.01 |    4 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    15.702 μs |   0.4913 μs |  0.0269 μs |   1.52 |    0.01 |    4 | 0.0916 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    43.040 μs |   1.3358 μs |  0.0732 μs |   4.17 |    0.02 |    6 | 0.2441 |  20.04 KB |        3.11 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **10.018 μs** |   **0.4671 μs** |  **0.0256 μs** |   **1.00** |    **0.00** |    **2** | **0.0763** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     8.474 μs |   0.1908 μs |  0.0105 μs |   0.85 |    0.00 |    2 | 0.0610 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    48.921 μs |   1.4652 μs |  0.0803 μs |   4.88 |    0.01 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    11.596 μs |   0.4186 μs |  0.0229 μs |   1.16 |    0.00 |    2 | 0.0610 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    26.225 μs |   0.7091 μs |  0.0389 μs |   2.62 |    0.01 |    4 | 0.1221 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    49.681 μs |   2.5312 μs |  0.1387 μs |   4.96 |    0.02 |    5 | 0.2441 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    18.149 μs |   1.3061 μs |  0.0716 μs |   1.81 |    0.01 |    3 | 0.1221 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,246.520 μs | 611.0807 μs | 33.4954 μs | 324.07 |    2.98 |    6 |      - | 109.63 KB |       17.01 |
 'Lambda Include'                     | 10            |     6.441 μs |   0.1291 μs |  0.0071 μs |   0.64 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     6.380 μs |   0.3411 μs |  0.0187 μs |   0.64 |    0.00 |    1 | 0.0458 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    14.806 μs |   0.1710 μs |  0.0094 μs |   1.48 |    0.00 |    3 | 0.1068 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    16.967 μs |   1.6807 μs |  0.0921 μs |   1.69 |    0.01 |    3 | 0.0916 |   9.97 KB |        1.55 |
 'Full specification (all features)'  | 10            |    41.936 μs |   4.5840 μs |  0.2513 μs |   4.19 |    0.02 |    5 | 0.1831 |  19.88 KB |        3.08 |
