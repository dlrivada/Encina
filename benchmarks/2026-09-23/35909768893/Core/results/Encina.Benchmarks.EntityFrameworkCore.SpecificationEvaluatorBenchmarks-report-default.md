
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **11.262 μs** |   **2.0805 μs** | **0.1140 μs** |   **1.00** |    **0.01** |    **3** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     9.163 μs |   0.3215 μs | 0.0176 μs |   0.81 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    13.121 μs |   3.0553 μs | 0.1675 μs |   1.17 |    0.02 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    13.167 μs |   0.9693 μs | 0.0531 μs |   1.17 |    0.01 |    3 | 0.3815 |   6.29 KB |        0.98 |
 'Five criteria (AND)'                | 2             |    29.414 μs |   1.2519 μs | 0.0686 μs |   2.61 |    0.02 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    55.600 μs |   1.6486 μs | 0.0904 μs |   4.94 |    0.04 |    6 | 1.2817 |  21.22 KB |        3.29 |
 'Keyset pagination'                  | 2             |    19.631 μs |   0.3118 μs | 0.0171 μs |   1.74 |    0.02 |    3 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,199.172 μs |  43.7376 μs | 2.3974 μs | 284.08 |    2.49 |    7 | 3.9063 | 110.31 KB |       17.11 |
 'Lambda Include'                     | 2             |     6.857 μs |   0.3056 μs | 0.0168 μs |   0.61 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     6.934 μs |   0.1984 μs | 0.0109 μs |   0.62 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    15.647 μs |   0.3178 μs | 0.0174 μs |   1.39 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    16.519 μs |   0.4094 μs | 0.0224 μs |   1.47 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    44.734 μs |   2.6009 μs | 0.1426 μs |   3.97 |    0.04 |    5 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |             |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **11.028 μs** |   **0.3968 μs** | **0.0218 μs** |   **1.00** |    **0.00** |    **2** | **0.3815** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     9.611 μs |   1.1696 μs | 0.0641 μs |   0.87 |    0.01 |    2 | 0.3357 |   5.54 KB |        0.86 |
 'Complex predicates (parameterized)' | 10            |    53.827 μs |   1.7724 μs | 0.0972 μs |   4.88 |    0.01 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    12.694 μs |   0.5068 μs | 0.0278 μs |   1.15 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    29.189 μs |   7.8309 μs | 0.4292 μs |   2.65 |    0.03 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    54.933 μs |   9.5685 μs | 0.5245 μs |   4.98 |    0.04 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    21.199 μs |   1.3958 μs | 0.0765 μs |   1.92 |    0.01 |    4 | 0.6714 |  11.04 KB |        1.71 |
 'Keyset pagination (fresh cursor)'   | 10            | 3,206.211 μs | 170.4633 μs | 9.3437 μs | 290.75 |    0.89 |    7 | 3.9063 | 109.15 KB |       16.94 |
 'Lambda Include'                     | 10            |     7.074 μs |   0.9896 μs | 0.0542 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     7.030 μs |   1.0666 μs | 0.0585 μs |   0.64 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    16.333 μs |   0.7302 μs | 0.0400 μs |   1.48 |    0.00 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    16.801 μs |   2.3817 μs | 0.1305 μs |   1.52 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    45.884 μs |   2.6932 μs | 0.1476 μs |   4.16 |    0.01 |    6 | 1.2207 |   20.2 KB |        3.13 |
