
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.66GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev    | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **22.04 μs** |     **4.164 μs** |  **0.228 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.37 μs |     1.551 μs |  0.085 μs |   0.79 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    24.11 μs |     2.761 μs |  0.151 μs |   1.09 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    24.17 μs |     1.591 μs |  0.087 μs |   1.10 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    48.51 μs |     2.819 μs |  0.155 μs |   2.20 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    91.50 μs |    24.932 μs |  1.367 μs |   4.15 |    0.07 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    41.52 μs |     3.892 μs |  0.213 μs |   1.88 |    0.02 |    5 | 0.6104 |  10.73 KB |        1.66 |
 'Keyset pagination (fresh cursor)'   | 2             | 3,997.69 μs |   647.320 μs | 35.482 μs | 181.37 |    2.15 |    7 |      - | 109.15 KB |       16.93 |
 'Lambda Include'                     | 2             |    11.37 μs |     1.066 μs |  0.058 μs |   0.52 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.29 μs |     0.581 μs |  0.032 μs |   0.51 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    26.09 μs |     1.292 μs |  0.071 μs |   1.18 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    31.77 μs |     2.796 μs |  0.153 μs |   1.44 |    0.01 |    4 | 0.5493 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 2             |    88.54 μs |     8.233 μs |  0.451 μs |   4.02 |    0.04 |    6 | 1.0986 |  19.88 KB |        3.08 |
                                      |               |             |              |           |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.00 μs** |     **1.697 μs** |  **0.093 μs** |   **1.00** |    **0.01** |    **2** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    19.44 μs |     2.742 μs |  0.150 μs |   0.88 |    0.01 |    2 | 0.3357 |   5.55 KB |        0.86 |
 'Complex predicates (parameterized)' | 10            |    87.99 μs |     4.670 μs |  0.256 μs |   4.00 |    0.02 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    24.33 μs |     2.278 μs |  0.125 μs |   1.11 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    48.84 μs |     1.978 μs |  0.108 μs |   2.22 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.50 μs |     9.397 μs |  0.515 μs |   4.02 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.44 μs |     5.006 μs |  0.274 μs |   1.84 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,149.45 μs | 1,654.508 μs | 90.689 μs | 188.63 |    3.64 |    7 |      - | 109.01 KB |       16.91 |
 'Lambda Include'                     | 10            |    12.78 μs |     0.886 μs |  0.049 μs |   0.58 |    0.00 |    1 | 0.2747 |   4.55 KB |        0.71 |
 'String Include'                     | 10            |    12.02 μs |     0.761 μs |  0.042 μs |   0.55 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.12 μs |     1.765 μs |  0.097 μs |   1.14 |    0.01 |    2 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    30.28 μs |     1.568 μs |  0.086 μs |   1.38 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    88.66 μs |     6.054 μs |  0.332 μs |   4.03 |    0.02 |    6 | 1.0986 |  19.88 KB |        3.08 |
