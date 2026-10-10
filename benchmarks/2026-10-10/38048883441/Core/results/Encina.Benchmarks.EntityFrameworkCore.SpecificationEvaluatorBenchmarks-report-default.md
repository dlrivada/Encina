
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.212 μs** |   **0.9532 μs** |  **0.0522 μs** |   **1.00** |    **0.01** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.291 μs |   0.6277 μs |  0.0344 μs |   0.83 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    13.033 μs |   1.7841 μs |  0.0978 μs |   1.16 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.089 μs |   1.4087 μs |  0.0772 μs |   1.17 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    29.986 μs |   7.3421 μs |  0.4024 μs |   2.67 |    0.03 |    5 | 0.7019 |  11.84 KB |        1.84 |
 'Ten criteria (AND)'                 | 2             |    55.438 μs |   3.3748 μs |  0.1850 μs |   4.94 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    20.204 μs |   0.6512 μs |  0.0357 μs |   1.80 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,226.474 μs | 200.0940 μs | 10.9678 μs | 287.78 |    1.44 |    8 | 3.9063 | 109.07 KB |       16.92 |
 'Lambda Include'                     | 2             |     7.034 μs |   0.3020 μs |  0.0166 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     7.005 μs |   0.4679 μs |  0.0256 μs |   0.62 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    15.967 μs |   1.6389 μs |  0.0898 μs |   1.42 |    0.01 |    4 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    17.040 μs |   1.6884 μs |  0.0925 μs |   1.52 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    45.246 μs |   7.7678 μs |  0.4258 μs |   4.04 |    0.04 |    6 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.262 μs** |   **0.9186 μs** |  **0.0504 μs** |   **1.00** |    **0.01** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.479 μs |   0.7646 μs |  0.0419 μs |   0.84 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    56.138 μs |   3.9649 μs |  0.2173 μs |   4.98 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    13.127 μs |   0.2241 μs |  0.0123 μs |   1.17 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    30.009 μs |   7.4135 μs |  0.4064 μs |   2.66 |    0.03 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    55.609 μs |   5.2814 μs |  0.2895 μs |   4.94 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    20.434 μs |   9.6531 μs |  0.5291 μs |   1.81 |    0.04 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,291.493 μs | 928.3635 μs | 50.8867 μs | 292.26 |    4.07 |    7 | 3.9063 | 109.32 KB |       16.96 |
 'Lambda Include'                     | 10            |     7.230 μs |   2.5798 μs |  0.1414 μs |   0.64 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.133 μs |   0.7865 μs |  0.0431 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.047 μs |   1.8567 μs |  0.1018 μs |   1.42 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    16.916 μs |   1.0971 μs |  0.0601 μs |   1.50 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    46.496 μs |  27.2929 μs |  1.4960 μs |   4.13 |    0.12 |    5 | 1.2207 |  20.04 KB |        3.11 |
