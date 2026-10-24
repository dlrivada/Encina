
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **12.156 μs** |   **0.6255 μs** |  **0.0343 μs** |   **1.00** |    **0.00** |    **3** | **0.3967** |   **6.73 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.582 μs |   3.0721 μs |  0.1684 μs |   0.79 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.78 |
 'Complex predicates (parameterized)' | 2             |    13.863 μs |   0.7980 μs |  0.0437 μs |   1.14 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Two criteria (AND)'                 | 2             |    13.732 μs |   0.3968 μs |  0.0218 μs |   1.13 |    0.00 |    3 | 0.3662 |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 2             |    29.893 μs |   1.9733 μs |  0.1082 μs |   2.46 |    0.01 |    4 | 0.7019 |  11.85 KB |        1.76 |
 'Ten criteria (AND)'                 | 2             |    55.409 μs |   4.6803 μs |  0.2565 μs |   4.56 |    0.02 |    5 | 1.2207 |  20.91 KB |        3.11 |
 'Keyset pagination'                  | 2             |    20.596 μs |   1.3357 μs |  0.0732 μs |   1.69 |    0.01 |    3 | 0.6104 |  10.43 KB |        1.55 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,269.232 μs | 258.0809 μs | 14.1463 μs | 268.94 |    1.20 |    6 | 3.9063 | 109.82 KB |       16.33 |
 'Lambda Include'                     | 2             |     7.222 μs |   0.5025 μs |  0.0275 μs |   0.59 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'String Include'                     | 2             |     7.202 μs |   0.4377 μs |  0.0240 μs |   0.59 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 2             |    16.532 μs |   3.7060 μs |  0.2031 μs |   1.36 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 2             |    17.464 μs |   2.3362 μs |  0.1281 μs |   1.44 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.39 |
 'Full specification (all features)'  | 2             |    47.373 μs |   2.3867 μs |  0.1308 μs |   3.90 |    0.01 |    5 | 1.2207 |   20.2 KB |        3.00 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.319 μs** |   **1.7557 μs** |  **0.0962 μs** |   **1.00** |    **0.01** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.545 μs |   0.6786 μs |  0.0372 μs |   0.84 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    55.212 μs |   2.6741 μs |  0.1466 μs |   4.88 |    0.04 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.687 μs |   1.8686 μs |  0.1024 μs |   1.21 |    0.01 |    3 | 0.3815 |   6.44 KB |        1.00 |
 'Five criteria (AND)'                | 10            |    29.016 μs |   1.1835 μs |  0.0649 μs |   2.56 |    0.02 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    55.141 μs |   3.9537 μs |  0.2167 μs |   4.87 |    0.04 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    20.054 μs |   1.7761 μs |  0.0974 μs |   1.77 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,220.716 μs | 191.9756 μs | 10.5228 μs | 284.54 |    2.24 |    8 | 3.9063 | 109.22 KB |       16.95 |
 'Lambda Include'                     | 10            |     7.242 μs |   0.7782 μs |  0.0427 μs |   0.64 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.041 μs |   0.5801 μs |  0.0318 μs |   0.62 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.448 μs |   0.8857 μs |  0.0485 μs |   1.45 |    0.01 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    16.841 μs |   2.0066 μs |  0.1100 μs |   1.49 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    45.905 μs |   3.3385 μs |  0.1830 μs |   4.06 |    0.03 |    6 | 1.1597 |  19.88 KB |        3.08 |
