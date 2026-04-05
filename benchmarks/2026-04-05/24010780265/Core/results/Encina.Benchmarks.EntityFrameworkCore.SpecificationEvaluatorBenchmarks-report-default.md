
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host] : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry    : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

Job=Dry  IterationCount=1  LaunchCount=1  
RunStrategy=ColdStart  UnrollFactor=1  WarmupCount=1  

 Method                               | CriteriaCount | Mean     | Error | Ratio | Rank | Allocated | Alloc Ratio |
------------------------------------- |-------------- |---------:|------:|------:|-----:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             | **376.1 ms** |    **NA** |  **1.00** |    **8** |   **6.77 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             | 367.0 ms |    NA |  0.98 |    4 |   5.48 KB |        0.81 |
 'Complex predicates (parameterized)' | 2             | 367.8 ms |    NA |  0.98 |    5 |   6.14 KB |        0.91 |
 'Two criteria (AND)'                 | 2             | 374.0 ms |    NA |  0.99 |    7 |   6.14 KB |        0.91 |
 'Five criteria (AND)'                | 2             | 381.9 ms |    NA |  1.02 |   10 |  11.93 KB |        1.76 |
 'Ten criteria (AND)'                 | 2             | 397.8 ms |    NA |  1.06 |   12 |  21.07 KB |        3.11 |
 'Keyset pagination'                  | 2             | 380.9 ms |    NA |  1.01 |    9 |  10.62 KB |        1.57 |
 'Keyset pagination (fresh cursor)'   | 2             | 383.4 ms |    NA |  1.02 |   11 | 114.27 KB |       16.87 |
 'Lambda Include'                     | 2             | 353.2 ms |    NA |  0.94 |    2 |   4.26 KB |        0.63 |
 'String Include'                     | 2             | 352.6 ms |    NA |  0.94 |    1 |   4.26 KB |        0.63 |
 'Multi-column ordering'              | 2             | 362.8 ms |    NA |  0.96 |    3 |   8.77 KB |        1.30 |
 'Offset pagination (Skip/Take)'      | 2             | 373.7 ms |    NA |  0.99 |    6 |   9.63 KB |        1.42 |
 'Full specification (all features)'  | 2             | 411.5 ms |    NA |  1.09 |   13 |  20.23 KB |        2.99 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **5**             | **375.6 ms** |    **NA** |  **1.00** |    **7** |   **6.63 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 5             | 362.5 ms |    NA |  0.97 |    4 |   5.45 KB |        0.82 |
 'Complex predicates (parameterized)' | 5             | 377.1 ms |    NA |  1.00 |    8 |  11.85 KB |        1.79 |
 'Two criteria (AND)'                 | 5             | 367.4 ms |    NA |  0.98 |    5 |   6.14 KB |        0.93 |
 'Five criteria (AND)'                | 5             | 377.8 ms |    NA |  1.01 |    9 |  11.63 KB |        1.75 |
 'Ten criteria (AND)'                 | 5             | 396.1 ms |    NA |  1.05 |   12 |  21.07 KB |        3.18 |
 'Keyset pagination'                  | 5             | 380.6 ms |    NA |  1.01 |   10 |  10.69 KB |        1.61 |
 'Keyset pagination (fresh cursor)'   | 5             | 382.3 ms |    NA |  1.02 |   11 | 115.02 KB |       17.34 |
 'Lambda Include'                     | 5             | 353.3 ms |    NA |  0.94 |    2 |   4.26 KB |        0.64 |
 'String Include'                     | 5             | 350.8 ms |    NA |  0.93 |    1 |   4.26 KB |        0.64 |
 'Multi-column ordering'              | 5             | 361.3 ms |    NA |  0.96 |    3 |   8.77 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 5             | 371.3 ms |    NA |  0.99 |    6 |   9.62 KB |        1.45 |
 'Full specification (all features)'  | 5             | 408.8 ms |    NA |  1.09 |   13 | 116.73 KB |       17.60 |
                                      |               |          |       |       |      |           |             |
 **'Simple Where (single criterion)'**    | **10**            | **380.3 ms** |    **NA** |  **1.00** |    **8** |   **6.63 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            | 362.3 ms |    NA |  0.95 |    3 |   5.45 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            | 397.3 ms |    NA |  1.04 |   11 |  20.98 KB |        3.16 |
 'Two criteria (AND)'                 | 10            | 379.6 ms |    NA |  1.00 |    6 |   6.21 KB |        0.94 |
 'Five criteria (AND)'                | 10            | 383.9 ms |    NA |  1.01 |   10 |  11.55 KB |        1.74 |
 'Ten criteria (AND)'                 | 10            | 398.8 ms |    NA |  1.05 |   12 |  21.07 KB |        3.18 |
 'Keyset pagination'                  | 10            | 382.2 ms |    NA |  1.01 |    9 |  10.77 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 380.3 ms |    NA |  1.00 |    7 | 115.51 KB |       17.41 |
 'Lambda Include'                     | 10            | 354.1 ms |    NA |  0.93 |    2 |   4.26 KB |        0.64 |
 'String Include'                     | 10            | 352.4 ms |    NA |  0.93 |    1 |   4.26 KB |        0.64 |
 'Multi-column ordering'              | 10            | 362.9 ms |    NA |  0.95 |    4 |   8.77 KB |        1.32 |
 'Offset pagination (Skip/Take)'      | 10            | 374.1 ms |    NA |  0.98 |    5 |   9.63 KB |        1.45 |
 'Full specification (all features)'  | 10            | 409.8 ms |    NA |  1.08 |   13 |  20.59 KB |        3.10 |
