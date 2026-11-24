
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V74 3.69GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.633 μs** |   **0.5661 μs** |  **0.0310 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.654 μs |   0.6424 μs |  0.0352 μs |   0.83 |    0.00 |    2 | 0.3204 |   5.37 KB |        0.83 |
 'Complex predicates (parameterized)' | 2             |    13.375 μs |   0.4990 μs |  0.0274 μs |   1.15 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.156 μs |   1.3482 μs |  0.0739 μs |   1.13 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.867 μs |   1.5682 μs |  0.0860 μs |   2.57 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.527 μs |   5.9008 μs |  0.3234 μs |   4.77 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    20.241 μs |   0.7271 μs |  0.0399 μs |   1.74 |    0.00 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,256.088 μs | 378.7554 μs | 20.7609 μs | 279.91 |    1.68 |    7 | 3.9063 | 109.04 KB |       16.92 |
 'Lambda Include'                     | 2             |     7.179 μs |   0.8454 μs |  0.0463 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.242 μs |   4.9085 μs |  0.2690 μs |   0.62 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    16.204 μs |   1.4852 μs |  0.0814 μs |   1.39 |    0.01 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    17.333 μs |   0.8884 μs |  0.0487 μs |   1.49 |    0.00 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    46.180 μs |   1.1588 μs |  0.0635 μs |   3.97 |    0.01 |    6 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.305 μs** |   **0.5235 μs** |  **0.0287 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.563 μs |   1.6143 μs |  0.0885 μs |   0.85 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    54.982 μs |   1.7950 μs |  0.0984 μs |   4.86 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.027 μs |   0.9423 μs |  0.0516 μs |   1.15 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.462 μs |   0.9551 μs |  0.0524 μs |   2.61 |    0.01 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    55.122 μs |   0.8267 μs |  0.0453 μs |   4.88 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    20.454 μs |   2.4764 μs |  0.1357 μs |   1.81 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,222.392 μs | 212.4709 μs | 11.6462 μs | 285.04 |    1.09 |    7 | 3.9063 | 108.81 KB |       16.88 |
 'Lambda Include'                     | 10            |     7.097 μs |   0.1161 μs |  0.0064 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.145 μs |   0.6812 μs |  0.0373 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.220 μs |   1.4036 μs |  0.0769 μs |   1.43 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    17.176 μs |   1.3067 μs |  0.0716 μs |   1.52 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    45.791 μs |   0.4912 μs |  0.0269 μs |   4.05 |    0.01 |    5 | 1.1597 |  19.88 KB |        3.08 |
