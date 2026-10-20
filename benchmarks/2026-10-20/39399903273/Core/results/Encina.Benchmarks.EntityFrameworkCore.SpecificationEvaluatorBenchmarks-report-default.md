
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.166 μs** |   **0.7283 μs** |  **0.0399 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.332 μs |   0.2617 μs |  0.0143 μs |   0.84 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    12.935 μs |   0.8163 μs |  0.0447 μs |   1.16 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.592 μs |   0.8631 μs |  0.0473 μs |   1.22 |    0.01 |    2 | 0.3815 |   6.44 KB |        1.00 |
 'Five criteria (AND)'                | 2             |    29.508 μs |   3.2239 μs |  0.1767 μs |   2.64 |    0.02 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.603 μs |   3.3430 μs |  0.1832 μs |   4.98 |    0.02 |    5 | 1.2817 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    20.534 μs |   1.9956 μs |  0.1094 μs |   1.84 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,218.413 μs | 122.9347 μs |  6.7385 μs | 288.23 |    1.03 |    6 | 3.9063 | 109.98 KB |       17.06 |
 'Lambda Include'                     | 2             |     7.118 μs |   2.4459 μs |  0.1341 μs |   0.64 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.115 μs |   0.2694 μs |  0.0148 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    15.935 μs |   1.4782 μs |  0.0810 μs |   1.43 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    16.820 μs |   0.1591 μs |  0.0087 μs |   1.51 |    0.00 |    2 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    46.488 μs |   1.2738 μs |  0.0698 μs |   4.16 |    0.01 |    5 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.477 μs** |   **0.7270 μs** |  **0.0398 μs** |   **1.00** |    **0.00** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.180 μs |   0.9774 μs |  0.0536 μs |   0.80 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.563 μs |   3.3944 μs |  0.1861 μs |   4.84 |    0.02 |    6 | 1.2817 |  21.07 KB |        3.27 |
 'Two criteria (AND)'                 | 10            |    13.355 μs |   0.6620 μs |  0.0363 μs |   1.16 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.270 μs |   0.6118 μs |  0.0335 μs |   2.55 |    0.01 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    55.178 μs |   2.9131 μs |  0.1597 μs |   4.81 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    20.205 μs |   0.6456 μs |  0.0354 μs |   1.76 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,208.678 μs | 255.4158 μs | 14.0002 μs | 279.58 |    1.35 |    7 | 3.9063 | 108.65 KB |       16.86 |
 'Lambda Include'                     | 10            |     7.125 μs |   0.4599 μs |  0.0252 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.141 μs |   0.6165 μs |  0.0338 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    15.986 μs |   1.6659 μs |  0.0913 μs |   1.39 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    16.971 μs |   1.0858 μs |  0.0595 μs |   1.48 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    45.768 μs |   4.2955 μs |  0.2354 μs |   3.99 |    0.02 |    5 | 1.1597 |  19.88 KB |        3.08 |
