
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.67GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean        | Error        | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |------------:|-------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |    **23.23 μs** |     **2.888 μs** |   **0.158 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |    17.61 μs |     2.202 μs |   0.121 μs |   0.76 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |    23.71 μs |     3.815 μs |   0.209 μs |   1.02 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |    23.98 μs |     2.639 μs |   0.145 μs |   1.03 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    50.01 μs |     2.246 μs |   0.123 μs |   2.15 |    0.01 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    91.50 μs |    11.419 μs |   0.626 μs |   3.94 |    0.03 |    6 | 1.2207 |  21.07 KB |        3.27 |
 'Keyset pagination'                  | 2             |    41.94 μs |     4.653 μs |   0.255 μs |   1.81 |    0.01 |    5 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 4,009.97 μs | 1,153.444 μs |  63.224 μs | 172.63 |    2.57 |    7 |      - | 108.94 KB |       16.90 |
 'Lambda Include'                     | 2             |    11.57 μs |     0.639 μs |   0.035 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |    11.68 μs |     0.807 μs |   0.044 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    25.09 μs |     4.133 μs |   0.227 μs |   1.08 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    30.56 μs |     4.516 μs |   0.248 μs |   1.32 |    0.01 |    4 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    89.40 μs |     9.909 μs |   0.543 μs |   3.85 |    0.03 |    6 | 1.2207 |  20.03 KB |        3.11 |
                                      |               |             |              |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |    **22.80 μs** |     **3.493 μs** |   **0.191 μs** |   **1.00** |    **0.01** |    **3** | **0.3662** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |    17.71 μs |     1.042 μs |   0.057 μs |   0.78 |    0.01 |    2 | 0.3052 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    89.66 μs |    10.766 μs |   0.590 μs |   3.93 |    0.04 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |    23.21 μs |     0.503 μs |   0.028 μs |   1.02 |    0.01 |    3 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    50.16 μs |     3.433 μs |   0.188 μs |   2.20 |    0.02 |    5 | 0.6714 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    88.92 μs |     9.209 μs |   0.505 μs |   3.90 |    0.03 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    40.32 μs |     2.869 μs |   0.157 μs |   1.77 |    0.01 |    4 | 0.6104 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 4,044.48 μs | 2,124.595 μs | 116.456 μs | 177.43 |    4.61 |    7 |      - | 109.48 KB |       16.99 |
 'Lambda Include'                     | 10            |    11.47 μs |     1.048 μs |   0.057 μs |   0.50 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |    11.76 μs |     0.563 μs |   0.031 μs |   0.52 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    25.78 μs |     0.080 μs |   0.004 μs |   1.13 |    0.01 |    3 | 0.5188 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    29.87 μs |     2.976 μs |   0.163 μs |   1.31 |    0.01 |    3 | 0.5493 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    86.36 μs |    12.896 μs |   0.707 μs |   3.79 |    0.04 |    6 | 1.2207 |  20.03 KB |        3.11 |
