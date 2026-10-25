
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev      | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|------------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **7.260 μs** |     **1.4651 μs** |   **0.0803 μs** |   **1.00** |    **0.01** |    **2** | **0.3891** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     6.187 μs |     6.1977 μs |   0.3397 μs |   0.85 |    0.04 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |     8.518 μs |     1.1406 μs |   0.0625 μs |   1.17 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |     8.890 μs |     6.9812 μs |   0.3827 μs |   1.22 |    0.05 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    20.178 μs |    17.9668 μs |   0.9848 μs |   2.78 |    0.12 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    37.636 μs |     8.7233 μs |   0.4782 μs |   5.18 |    0.08 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    12.839 μs |     1.2998 μs |   0.0712 μs |   1.77 |    0.02 |    3 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 2,661.794 μs | 1,202.3475 μs |  65.9047 μs | 366.67 |    8.62 |    7 | 3.9063 | 110.66 KB |       17.17 |
 'Lambda Include'                     | 2             |     4.710 μs |     0.2106 μs |   0.0115 μs |   0.65 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     4.610 μs |     1.3516 μs |   0.0741 μs |   0.64 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    11.138 μs |     0.8671 μs |   0.0475 μs |   1.53 |    0.02 |    3 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    11.511 μs |     9.4761 μs |   0.5194 μs |   1.59 |    0.06 |    3 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    28.683 μs |     8.9299 μs |   0.4895 μs |   3.95 |    0.07 |    5 | 1.1902 |  19.88 KB |        3.08 |
                                      |               |              |               |             |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **7.271 μs** |     **4.8854 μs** |   **0.2678 μs** |   **1.00** |    **0.04** |    **2** | **0.3891** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     6.037 μs |     0.2866 μs |   0.0157 μs |   0.83 |    0.03 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    37.635 μs |     3.7221 μs |   0.2040 μs |   5.18 |    0.17 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |     8.501 μs |     6.0134 μs |   0.3296 μs |   1.17 |    0.05 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    19.491 μs |     1.7229 μs |   0.0944 μs |   2.68 |    0.09 |    4 | 0.7019 |  11.71 KB |        1.82 |
 'Ten criteria (AND)'                 | 10            |    36.791 μs |    20.2161 μs |   1.1081 μs |   5.06 |    0.21 |    5 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    13.195 μs |     8.0715 μs |   0.4424 μs |   1.82 |    0.08 |    3 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,679.374 μs | 2,581.7980 μs | 141.5171 μs | 368.86 |   20.53 |    6 | 3.9063 |  109.5 KB |       16.99 |
 'Lambda Include'                     | 10            |     4.667 μs |     0.3858 μs |   0.0211 μs |   0.64 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     4.689 μs |     0.4201 μs |   0.0230 μs |   0.65 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    11.208 μs |     3.7285 μs |   0.2044 μs |   1.54 |    0.05 |    3 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    11.237 μs |     1.4770 μs |   0.0810 μs |   1.55 |    0.05 |    3 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    31.809 μs |    12.9854 μs |   0.7118 μs |   4.38 |    0.16 |    5 | 1.2207 |  20.57 KB |        3.19 |
