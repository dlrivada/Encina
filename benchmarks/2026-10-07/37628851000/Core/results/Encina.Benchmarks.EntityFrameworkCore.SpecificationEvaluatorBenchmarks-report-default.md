
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Gen1   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **7.525 μs** |     **8.1177 μs** |  **0.4450 μs** |   **1.00** |    **0.07** |    **2** | **0.3891** |      **-** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     6.178 μs |     1.2731 μs |  0.0698 μs |   0.82 |    0.04 |    2 | 0.3204 |      - |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |     8.419 μs |     1.4966 μs |  0.0820 μs |   1.12 |    0.06 |    2 | 0.3662 |      - |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |     8.623 μs |     1.1815 μs |  0.0648 μs |   1.15 |    0.06 |    2 | 0.3662 |      - |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    20.867 μs |    17.1364 μs |  0.9393 μs |   2.78 |    0.18 |    5 | 0.7019 |      - |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    39.074 μs |     7.0562 μs |  0.3868 μs |   5.20 |    0.26 |    7 | 1.2207 |      - |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    13.906 μs |     7.2643 μs |  0.3982 μs |   1.85 |    0.10 |    4 | 0.6256 |      - |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 2,761.368 μs |   413.8724 μs | 22.6857 μs | 367.81 |   18.61 |    8 | 3.9063 |      - |  109.9 KB |       17.05 |
 'Lambda Include'                     | 2             |     4.813 μs |     0.5308 μs |  0.0291 μs |   0.64 |    0.03 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     4.975 μs |     4.0216 μs |  0.2204 μs |   0.66 |    0.04 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    11.442 μs |     0.6665 μs |  0.0365 μs |   1.52 |    0.08 |    3 | 0.5341 |      - |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    11.592 μs |     0.2240 μs |  0.0123 μs |   1.54 |    0.08 |    3 | 0.5646 |      - |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    30.861 μs |    13.3809 μs |  0.7335 μs |   4.11 |    0.22 |    6 | 1.1902 |      - |  19.88 KB |        3.08 |
                                      |               |              |               |            |        |         |      |        |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **7.553 μs** |     **2.9455 μs** |  **0.1615 μs** |   **1.00** |    **0.03** |    **2** | **0.3891** |      **-** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     6.612 μs |     6.9085 μs |  0.3787 μs |   0.88 |    0.05 |    2 | 0.3204 |      - |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    38.987 μs |     1.8655 μs |  0.1023 μs |   5.16 |    0.10 |    6 | 1.2207 |      - |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |     8.620 μs |     0.7981 μs |  0.0437 μs |   1.14 |    0.02 |    2 | 0.3662 |      - |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    20.358 μs |     1.8614 μs |  0.1020 μs |   2.70 |    0.05 |    4 | 0.7019 |      - |   11.7 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    38.335 μs |     2.9162 μs |  0.1598 μs |   5.08 |    0.09 |    6 | 1.2207 |      - |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    13.640 μs |     8.0953 μs |  0.4437 μs |   1.81 |    0.06 |    3 | 0.6256 |      - |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,735.779 μs | 1,327.9428 μs | 72.7890 μs | 362.31 |   10.66 |    7 | 3.9063 |      - | 109.15 KB |       16.94 |
 'Lambda Include'                     | 10            |     4.786 μs |     0.7463 μs |  0.0409 μs |   0.63 |    0.01 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     4.918 μs |     1.4389 μs |  0.0789 μs |   0.65 |    0.01 |    1 | 0.2594 |      - |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    11.336 μs |     1.5302 μs |  0.0839 μs |   1.50 |    0.03 |    3 | 0.5493 |      - |   9.07 KB |        1.41 |
 'Offset pagination (Skip/Take)'      | 10            |    11.564 μs |     6.8488 μs |  0.3754 μs |   1.53 |    0.05 |    3 | 0.5646 |      - |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    31.009 μs |     3.1436 μs |  0.1723 μs |   4.11 |    0.08 |    5 | 1.2207 | 0.0305 |   20.2 KB |        3.13 |
