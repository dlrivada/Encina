
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **12.014 μs** |   **0.6658 μs** |  **0.0365 μs** |   **1.00** |    **0.00** |    **3** | **0.4120** |   **6.74 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.426 μs |   0.2305 μs |  0.0126 μs |   0.78 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.78 |
 'Complex predicates (parameterized)' | 2             |    12.994 μs |   0.7931 μs |  0.0435 μs |   1.08 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Two criteria (AND)'                 | 2             |    13.294 μs |   1.2398 μs |  0.0680 μs |   1.11 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 2             |    30.217 μs |   1.0770 μs |  0.0590 μs |   2.52 |    0.01 |    6 | 0.7019 |  11.85 KB |        1.76 |
 'Ten criteria (AND)'                 | 2             |    55.889 μs |   6.6020 μs |  0.3619 μs |   4.65 |    0.03 |    8 | 1.2207 |  20.91 KB |        3.10 |
 'Keyset pagination'                  | 2             |    20.845 μs |   3.1492 μs |  0.1726 μs |   1.74 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.55 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,290.319 μs | 135.6951 μs |  7.4379 μs | 273.87 |    0.90 |    9 | 3.9063 | 109.04 KB |       16.17 |
 'Lambda Include'                     | 2             |     7.301 μs |   2.3877 μs |  0.1309 μs |   0.61 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'String Include'                     | 2             |     7.234 μs |   0.1148 μs |  0.0063 μs |   0.60 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 2             |    16.219 μs |   0.3578 μs |  0.0196 μs |   1.35 |    0.00 |    4 | 0.5188 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 2             |    17.075 μs |   0.8908 μs |  0.0488 μs |   1.42 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.39 |
 'Full specification (all features)'  | 2             |    45.753 μs |   4.5327 μs |  0.2485 μs |   3.81 |    0.02 |    7 | 1.1597 |  19.88 KB |        2.95 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.643 μs** |   **1.2783 μs** |  **0.0701 μs** |   **1.00** |    **0.01** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.407 μs |   0.2799 μs |  0.0153 μs |   0.81 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.607 μs |   5.1201 μs |  0.2806 μs |   4.78 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.069 μs |   0.4180 μs |  0.0229 μs |   1.12 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.442 μs |   3.3135 μs |  0.1816 μs |   2.53 |    0.02 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    56.046 μs |   5.1452 μs |  0.2820 μs |   4.81 |    0.03 |    6 | 1.2817 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 10            |    21.052 μs |   0.2845 μs |  0.0156 μs |   1.81 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,260.319 μs | 446.0607 μs | 24.4501 μs | 280.03 |    2.33 |    7 | 3.9063 | 109.09 KB |       16.93 |
 'Lambda Include'                     | 10            |     7.208 μs |   0.7195 μs |  0.0394 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.291 μs |   0.1693 μs |  0.0093 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.535 μs |   2.1940 μs |  0.1203 μs |   1.42 |    0.01 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    17.633 μs |   2.2504 μs |  0.1234 μs |   1.51 |    0.01 |    4 | 0.5798 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 10            |    46.558 μs |   5.1435 μs |  0.2819 μs |   4.00 |    0.03 |    6 | 1.2207 |   20.2 KB |        3.13 |
