
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                               | CriteriaCount | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------- |-------------- |---------:|------:|------:|-----:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             | **377.6 ms** |    **NA** |  **1.00** |    **7** |   **6.71 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             | 372.7 ms |    NA |  0.99 |    3 |   5.45 KB |        0.81 |
 'Complex predicates (parameterized)' | 2             | 375.4 ms |    NA |  0.99 |    5 |   6.14 KB |        0.92 |
 'Two criteria (AND)'                 | 2             | 375.2 ms |    NA |  0.99 |    4 |   6.22 KB |        0.93 |
 'Five criteria (AND)'                | 2             | 404.7 ms |    NA |  1.07 |   10 |  11.71 KB |        1.75 |
 'Ten criteria (AND)'                 | 2             | 424.2 ms |    NA |  1.12 |   13 |  21.14 KB |        3.15 |
 'Keyset pagination'                  | 2             | 417.9 ms |    NA |  1.11 |   12 |  10.62 KB |        1.58 |
 'Keyset pagination (fresh cursor)'   | 2             | 393.6 ms |    NA |  1.04 |    9 | 114.67 KB |       17.09 |
 'Lambda Include'                     | 2             | 362.0 ms |    NA |  0.96 |    1 |   4.26 KB |        0.63 |
 'String Include'                     | 2             | 362.8 ms |    NA |  0.96 |    2 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 2             | 376.6 ms |    NA |  1.00 |    6 |   8.85 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 2             | 384.8 ms |    NA |  1.02 |    8 |   9.55 KB |        1.42 |
 'Full specification (all features)'  | 2             | 417.2 ms |    NA |  1.10 |   11 |  20.07 KB |        2.99 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **5**             | **399.5 ms** |    **NA** |  **1.00** |   **11** |   **6.63 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 5             | 371.3 ms |    NA |  0.93 |    3 |   5.45 KB |        0.82 |
 'Complex predicates (parameterized)' | 5             | 395.3 ms |    NA |  0.99 |    9 |  11.55 KB |        1.74 |
 'Two criteria (AND)'                 | 5             | 376.4 ms |    NA |  0.94 |    4 |   6.21 KB |        0.94 |
 'Five criteria (AND)'                | 5             | 395.1 ms |    NA |  0.99 |    8 |  11.55 KB |        1.74 |
 'Ten criteria (AND)'                 | 5             | 413.3 ms |    NA |  1.03 |   12 |  21.15 KB |        3.19 |
 'Keyset pagination'                  | 5             | 396.8 ms |    NA |  0.99 |   10 |   10.7 KB |        1.61 |
 'Keyset pagination (fresh cursor)'   | 5             | 387.6 ms |    NA |  0.97 |    6 | 211.12 KB |       31.83 |
 'Lambda Include'                     | 5             | 362.2 ms |    NA |  0.91 |    1 |   4.26 KB |        0.64 |
 'String Include'                     | 5             | 364.1 ms |    NA |  0.91 |    2 |   4.26 KB |        0.64 |
 'Multi-column ordering'              | 5             | 377.9 ms |    NA |  0.95 |    5 |   8.77 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 5             | 388.2 ms |    NA |  0.97 |    7 |   9.63 KB |        1.45 |
 'Full specification (all features)'  | 5             | 423.3 ms |    NA |  1.06 |   13 |  20.53 KB |        3.10 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **10**            | **389.1 ms** |    **NA** |  **1.00** |    **7** |    **6.7 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            | 374.7 ms |    NA |  0.96 |    3 |   5.52 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            | 413.6 ms |    NA |  1.06 |   12 |  20.99 KB |        3.13 |
 'Two criteria (AND)'                 | 10            | 382.6 ms |    NA |  0.98 |    5 |   6.14 KB |        0.92 |
 'Five criteria (AND)'                | 10            | 391.8 ms |    NA |  1.01 |    8 |  11.55 KB |        1.72 |
 'Ten criteria (AND)'                 | 10            | 413.2 ms |    NA |  1.06 |   11 |  20.98 KB |        3.13 |
 'Keyset pagination'                  | 10            | 397.6 ms |    NA |  1.02 |    9 |  10.62 KB |        1.58 |
 'Keyset pagination (fresh cursor)'   | 10            | 398.5 ms |    NA |  1.02 |   10 | 234.85 KB |       35.04 |
 'Lambda Include'                     | 10            | 366.2 ms |    NA |  0.94 |    2 |   4.34 KB |        0.65 |
 'String Include'                     | 10            | 356.0 ms |    NA |  0.92 |    1 |   4.41 KB |        0.66 |
 'Multi-column ordering'              | 10            | 377.7 ms |    NA |  0.97 |    4 |   8.85 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 10            | 386.0 ms |    NA |  0.99 |    6 |   9.77 KB |        1.46 |
 'Full specification (all features)'  | 10            | 425.4 ms |    NA |  1.09 |   13 |  20.52 KB |        3.06 |
