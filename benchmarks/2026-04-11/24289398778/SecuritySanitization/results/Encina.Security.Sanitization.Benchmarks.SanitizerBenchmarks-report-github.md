```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error        | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-------------:|-----------:|--------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  12,910.47 ns |    86.425 ns |  57.165 ns |  12,921.18 ns |  1.000 |    0.01 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  18,727.43 ns |   259.586 ns | 171.700 ns |  18,733.06 ns |  1.451 |    0.01 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 135,525.41 ns | 1,304.901 ns | 682.488 ns | 135,427.08 ns | 10.498 |    0.07 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     258.17 ns |     2.532 ns |   1.506 ns |     258.46 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     334.47 ns |     4.744 ns |   2.823 ns |     334.37 ns |  0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      28.75 ns |     0.708 ns |   0.468 ns |      28.86 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      28.01 ns |     0.590 ns |   0.390 ns |      28.07 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     452.12 ns |     1.897 ns |   1.255 ns |     452.13 ns |  0.035 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     265.59 ns |     6.971 ns |   4.148 ns |     264.45 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |               |              |            |               |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  12,527.28 ns |   231.697 ns | 339.618 ns |  12,421.89 ns |  1.001 |    0.04 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  18,948.64 ns |   144.784 ns | 216.706 ns |  18,912.52 ns |  1.514 |    0.04 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 137,920.48 ns |   506.941 ns | 758.765 ns | 138,039.15 ns | 11.017 |    0.30 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     261.47 ns |     3.469 ns |   4.975 ns |     265.29 ns |  0.021 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     338.11 ns |     1.354 ns |   1.942 ns |     337.63 ns |  0.027 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      27.99 ns |     0.233 ns |   0.342 ns |      27.93 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      28.52 ns |     0.306 ns |   0.458 ns |      28.50 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     462.71 ns |     2.635 ns |   3.862 ns |     461.83 ns |  0.037 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     266.01 ns |     3.442 ns |   4.826 ns |     264.60 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
