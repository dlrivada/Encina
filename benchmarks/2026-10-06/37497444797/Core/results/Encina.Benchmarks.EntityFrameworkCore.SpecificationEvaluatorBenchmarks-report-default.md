
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

 Method                               | CriteriaCount | Mean         | Error       | StdDev     | Ratio  | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
------------------------------------- |-------------- |-------------:|------------:|-----------:|-------:|--------:|-----:|-------:|----------:|------------:|
 **'Simple Where (single criterion)'**    | **2**             |     **7.013 μs** |   **0.0738 μs** |  **0.0040 μs** |   **1.00** |    **0.00** |    **3** | **0.3967** |   **6.56 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 2             |     5.647 μs |   0.4040 μs |  0.0221 μs |   0.81 |    0.00 |    2 | 0.3204 |   5.26 KB |        0.80 |
 'Complex predicates (parameterized)' | 2             |     7.998 μs |   2.3232 μs |  0.1273 μs |   1.14 |    0.02 |    3 | 0.3662 |   6.14 KB |        0.94 |
 'Two criteria (AND)'                 | 2             |     8.015 μs |   3.4457 μs |  0.1889 μs |   1.14 |    0.02 |    3 | 0.3662 |   6.14 KB |        0.94 |
 'Five criteria (AND)'                | 2             |    18.774 μs |   0.7457 μs |  0.0409 μs |   2.68 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.76 |
 'Ten criteria (AND)'                 | 2             |    36.346 μs |   2.1116 μs |  0.1157 μs |   5.18 |    0.01 |    7 | 1.2207 |  20.91 KB |        3.19 |
 'Keyset pagination'                  | 2             |    12.073 μs |   0.2816 μs |  0.0154 μs |   1.72 |    0.00 |    4 | 0.6256 |  10.43 KB |        1.59 |
 'Keyset pagination (fresh cursor)'   | 2             | 2,503.637 μs | 272.3110 μs | 14.9263 μs | 357.02 |    1.85 |    8 | 3.9063 | 108.91 KB |       16.60 |
 'Lambda Include'                     | 2             |     4.535 μs |   1.0492 μs |  0.0575 μs |   0.65 |    0.01 |    1 | 0.2594 |   4.26 KB |        0.65 |
 'String Include'                     | 2             |     4.398 μs |   0.4911 μs |  0.0269 μs |   0.63 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.65 |
 'Multi-column ordering'              | 2             |    10.318 μs |   0.5998 μs |  0.0329 μs |   1.47 |    0.00 |    4 | 0.5341 |   8.77 KB |        1.34 |
 'Offset pagination (Skip/Take)'      | 2             |    10.815 μs |   0.2224 μs |  0.0122 μs |   1.54 |    0.00 |    4 | 0.5646 |   9.36 KB |        1.43 |
 'Full specification (all features)'  | 2             |    28.088 μs |   0.6121 μs |  0.0335 μs |   4.01 |    0.00 |    6 | 1.1902 |  19.88 KB |        3.03 |
                                      |               |              |             |            |        |         |      |        |           |             |
 **'Simple Where (single criterion)'**    | **10**            |     **6.774 μs** |   **0.4529 μs** |  **0.0248 μs** |   **1.00** |    **0.00** |    **2** | **0.3891** |   **6.45 KB** |        **1.00** |
 'Direct LINQ Where (baseline)'       | 10            |     5.567 μs |   0.6461 μs |  0.0354 μs |   0.82 |    0.01 |    1 | 0.3204 |   5.26 KB |        0.82 |
 'Complex predicates (parameterized)' | 10            |    35.703 μs |   0.3610 μs |  0.0198 μs |   5.27 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Two criteria (AND)'                 | 10            |     8.108 μs |   0.3021 μs |  0.0166 μs |   1.20 |    0.00 |    2 | 0.3662 |   6.14 KB |        0.95 |
 'Five criteria (AND)'                | 10            |    18.630 μs |   0.7475 μs |  0.0410 μs |   2.75 |    0.01 |    5 | 0.7019 |  11.55 KB |        1.79 |
 'Ten criteria (AND)'                 | 10            |    35.199 μs |   0.9220 μs |  0.0505 μs |   5.20 |    0.02 |    7 | 1.2207 |  20.91 KB |        3.24 |
 'Keyset pagination'                  | 10            |    12.945 μs |   0.3830 μs |  0.0210 μs |   1.91 |    0.01 |    4 | 0.6561 |  10.73 KB |        1.66 |
 'Keyset pagination (fresh cursor)'   | 10            | 2,513.737 μs | 114.1215 μs |  6.2554 μs | 371.08 |    1.42 |    8 | 3.9063 | 109.47 KB |       16.98 |
 'Lambda Include'                     | 10            |     4.390 μs |   0.2466 μs |  0.0135 μs |   0.65 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'String Include'                     | 10            |     4.680 μs |   0.2528 μs |  0.0139 μs |   0.69 |    0.00 |    1 | 0.2594 |   4.26 KB |        0.66 |
 'Multi-column ordering'              | 10            |    10.434 μs |   0.1111 μs |  0.0061 μs |   1.54 |    0.00 |    3 | 0.5341 |   8.77 KB |        1.36 |
 'Offset pagination (Skip/Take)'      | 10            |    10.455 μs |   1.1097 μs |  0.0608 μs |   1.54 |    0.01 |    3 | 0.5646 |   9.36 KB |        1.45 |
 'Full specification (all features)'  | 10            |    27.768 μs |   1.8644 μs |  0.1022 μs |   4.10 |    0.02 |    6 | 1.1902 |  19.88 KB |        3.08 |
