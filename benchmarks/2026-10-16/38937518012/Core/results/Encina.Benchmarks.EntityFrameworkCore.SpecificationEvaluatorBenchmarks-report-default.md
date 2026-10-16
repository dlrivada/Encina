
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.373 μs** |   **1.1555 μs** |  **0.0633 μs** |   **1.00** |    **0.01** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.276 μs |   1.0389 μs |  0.0569 μs |   0.82 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    13.640 μs |   0.7674 μs |  0.0421 μs |   1.20 |    0.01 |    3 | 0.3815 |    6.3 KB |        0.98 |
 'Two criteria (AND)'                 | 2             |    13.355 μs |   1.0011 μs |  0.0549 μs |   1.17 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.734 μs |   0.3875 μs |  0.0212 μs |   2.61 |    0.01 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.136 μs |   5.4763 μs |  0.3002 μs |   4.85 |    0.03 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    20.224 μs |   1.3419 μs |  0.0736 μs |   1.78 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,240.387 μs | 384.6407 μs | 21.0835 μs | 284.92 |    2.11 |    6 | 3.9063 | 109.57 KB |       17.00 |
 'Lambda Include'                     | 2             |     7.233 μs |   0.2096 μs |  0.0115 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.306 μs |   1.7363 μs |  0.0952 μs |   0.64 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    16.326 μs |   1.5069 μs |  0.0826 μs |   1.44 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    17.223 μs |   1.0176 μs |  0.0558 μs |   1.51 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    46.384 μs |   1.6396 μs |  0.0899 μs |   4.08 |    0.02 |    5 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.460 μs** |   **0.5300 μs** |  **0.0290 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.701 μs |   1.5828 μs |  0.0868 μs |   0.85 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.328 μs |   3.7158 μs |  0.2037 μs |   4.83 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.156 μs |   0.9103 μs |  0.0499 μs |   1.15 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    30.037 μs |   0.9354 μs |  0.0513 μs |   2.62 |    0.01 |    4 | 0.7019 |   11.7 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    56.133 μs |   2.7314 μs |  0.1497 μs |   4.90 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    20.257 μs |   2.8548 μs |  0.1565 μs |   1.77 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,230.206 μs | 148.9402 μs |  8.1639 μs | 281.86 |    0.87 |    6 | 3.9063 | 108.86 KB |       16.89 |
 'Lambda Include'                     | 10            |     7.244 μs |   0.8660 μs |  0.0475 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.188 μs |   1.6195 μs |  0.0888 μs |   0.63 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.338 μs |   2.1715 μs |  0.1190 μs |   1.43 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    16.964 μs |   0.0686 μs |  0.0038 μs |   1.48 |    0.00 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    46.273 μs |   1.9101 μs |  0.1047 μs |   4.04 |    0.01 |    5 | 1.1597 |  19.88 KB |        3.08 |
