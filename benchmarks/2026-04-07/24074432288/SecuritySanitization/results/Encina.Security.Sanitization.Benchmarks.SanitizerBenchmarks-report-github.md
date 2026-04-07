```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error      | StdDev       | Median        | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-----------:|-------------:|--------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  12,725.45 ns |  96.784 ns |    50.620 ns |  12,741.02 ns |  1.000 |    0.01 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  18,746.71 ns | 132.438 ns |    87.600 ns |  18,717.27 ns |  1.473 |    0.01 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 136,987.17 ns | 383.874 ns |   228.437 ns | 136,928.38 ns | 10.765 |    0.04 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     248.60 ns |   0.764 ns |     0.455 ns |     248.38 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     335.23 ns |   1.640 ns |     0.976 ns |     334.82 ns |  0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      28.13 ns |   0.209 ns |     0.138 ns |      28.10 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      28.16 ns |   0.318 ns |     0.210 ns |      28.14 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     470.28 ns |   0.929 ns |     0.553 ns |     470.48 ns |  0.037 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     268.46 ns |   3.072 ns |     2.032 ns |     268.40 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |               |            |              |               |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  12,689.09 ns |  99.923 ns |   140.078 ns |  12,764.24 ns |  1.000 |    0.02 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  19,215.08 ns | 204.778 ns |   300.161 ns |  19,394.93 ns |  1.514 |    0.03 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 138,145.04 ns | 705.575 ns | 1,011.914 ns | 137,851.62 ns | 10.888 |    0.14 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     263.69 ns |   1.099 ns |     1.576 ns |     263.82 ns |  0.021 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     342.02 ns |   0.608 ns |     0.891 ns |     341.95 ns |  0.027 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      28.23 ns |   0.141 ns |     0.211 ns |      28.23 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      28.08 ns |   0.114 ns |     0.164 ns |      28.10 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     472.81 ns |   5.124 ns |     7.349 ns |     473.00 ns |  0.037 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     271.01 ns |   3.188 ns |     4.772 ns |     269.70 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
