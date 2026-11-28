
BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error         | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|--------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **7.704 μs** |     **1.8189 μs** |  **0.0997 μs** |   **1.00** |    **0.02** |    **2** | **0.3891** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     6.506 μs |     4.3481 μs |  0.2383 μs |   0.84 |    0.03 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 2             |     9.238 μs |     1.5770 μs |  0.0864 μs |   1.20 |    0.02 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Two criteria (AND)'                 | 2             |     8.932 μs |     0.9468 μs |  0.0519 μs |   1.16 |    0.01 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 2             |    20.644 μs |     0.8195 μs |  0.0449 μs |   2.68 |    0.03 |    4 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 2             |    40.400 μs |     8.3985 μs |  0.4604 μs |   5.24 |    0.08 |    6 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 2             |    14.139 μs |     9.7223 μs |  0.5329 μs |   1.84 |    0.06 |    3 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 2             | 2,837.405 μs | 1,591.9418 μs | 87.2597 μs | 368.35 |   10.65 |    7 | 3.9063 |  109.5 KB |       16.99 |
 'Lambda Include'                     | 2             |     5.122 μs |     0.7204 μs |  0.0395 μs |   0.66 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 2             |     5.079 μs |     0.6745 μs |  0.0370 μs |   0.66 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 2             |    11.689 μs |     1.6335 μs |  0.0895 μs |   1.52 |    0.02 |    3 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 2             |    12.166 μs |     0.6053 μs |  0.0332 μs |   1.58 |    0.02 |    3 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 2             |    31.211 μs |     1.2411 μs |  0.0680 μs |   4.05 |    0.05 |    5 | 1.1597 |  19.88 KB |        3.08 |
                                      |               |              |               |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **7.584 μs** |     **1.0006 μs** |  **0.0548 μs** |   **1.00** |    **0.01** |    **3** | **0.3891** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     6.215 μs |     0.2906 μs |  0.0159 μs |   0.82 |    0.01 |    2 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    39.330 μs |    17.0050 μs |  0.9321 μs |   5.19 |    0.11 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |     9.128 μs |     0.9761 μs |  0.0535 μs |   1.20 |    0.01 |    4 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    21.036 μs |     1.9265 μs |  0.1056 μs |   2.77 |    0.02 |    6 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    39.622 μs |     3.9863 μs |  0.2185 μs |   5.22 |    0.04 |    8 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    13.491 μs |     3.6455 μs |  0.1998 μs |   1.78 |    0.03 |    5 | 0.6256 |  10.43 KB |        1.62 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,712.315 μs | 1,319.2464 μs | 72.3123 μs | 357.65 |    8.56 |    9 | 3.9063 | 110.82 KB |       17.19 |
 'Lambda Include'                     | 10            |     4.943 μs |     1.6494 μs |  0.0904 μs |   0.65 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     4.817 μs |     2.8599 μs |  0.1568 μs |   0.64 |    0.02 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    11.569 μs |     0.9182 μs |  0.0503 μs |   1.53 |    0.01 |    5 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    12.208 μs |     2.0057 μs |  0.1099 μs |   1.61 |    0.02 |    5 | 0.5798 |   9.66 KB |        1.50 |
 'Full specification (all features)'  | 10            |    32.001 μs |    11.4945 μs |  0.6301 μs |   4.22 |    0.08 |    7 | 1.1597 |  19.88 KB |        3.08 |
