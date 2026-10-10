
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.423 μs** |   **0.9702 μs** |  **0.0532 μs** |   **1.00** |    **0.01** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.284 μs |   0.7264 μs |  0.0398 μs |   0.81 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    13.561 μs |   3.6239 μs |  0.1986 μs |   1.19 |    0.02 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.138 μs |   3.7838 μs |  0.2074 μs |   1.15 |    0.02 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.563 μs |   1.6724 μs |  0.0917 μs |   2.59 |    0.01 |    6 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.650 μs |   9.4991 μs |  0.5207 μs |   4.87 |    0.04 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    20.933 μs |   1.3819 μs |  0.0757 μs |   1.83 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,363.301 μs | 495.4735 μs | 27.1586 μs | 294.43 |    2.38 |    8 | 3.9063 | 109.37 KB |       16.97 |
 'Lambda Include'                     | 2             |     7.191 μs |   2.9281 μs |  0.1605 μs |   0.63 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.131 μs |   0.8738 μs |  0.0479 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    16.234 μs |   0.6363 μs |  0.0349 μs |   1.42 |    0.01 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    17.302 μs |   4.7794 μs |  0.2620 μs |   1.51 |    0.02 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    46.623 μs |   1.6902 μs |  0.0926 μs |   4.08 |    0.02 |    7 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.495 μs** |   **0.6080 μs** |  **0.0333 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.357 μs |   0.4137 μs |  0.0227 μs |   0.81 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.166 μs |   0.9330 μs |  0.0511 μs |   4.80 |    0.01 |    5 | 1.2817 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    13.562 μs |   0.6430 μs |  0.0352 μs |   1.18 |    0.00 |    3 | 0.3815 |   6.44 KB |        1.00 |
 'Five criteria (AND)'                | 10            |    29.762 μs |   6.4545 μs |  0.3538 μs |   2.59 |    0.03 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    56.365 μs |   4.7490 μs |  0.2603 μs |   4.90 |    0.02 |    5 | 1.2817 |  21.23 KB |        3.29 |
 'Keyset pagination'                  | 10            |    20.330 μs |   0.7261 μs |  0.0398 μs |   1.77 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,254.436 μs |  70.5736 μs |  3.8684 μs | 283.12 |    0.77 |    6 | 3.9063 | 109.04 KB |       16.92 |
 'Lambda Include'                     | 10            |     7.075 μs |   0.3534 μs |  0.0194 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.149 μs |   0.2600 μs |  0.0142 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.154 μs |   0.5024 μs |  0.0275 μs |   1.41 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    17.086 μs |   1.3354 μs |  0.0732 μs |   1.49 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    47.755 μs |   1.9192 μs |  0.1052 μs |   4.15 |    0.01 |    5 | 1.2207 |   20.2 KB |        3.13 |
