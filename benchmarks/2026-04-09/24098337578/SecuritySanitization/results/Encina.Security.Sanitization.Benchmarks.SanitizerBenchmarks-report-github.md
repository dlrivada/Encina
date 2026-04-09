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
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  12,151.27 ns |  28.634 ns |    18.940 ns |  12,153.59 ns |  1.000 |    0.00 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  18,099.18 ns | 131.710 ns |    87.118 ns |  18,079.27 ns |  1.489 |    0.01 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 131,106.98 ns | 651.882 ns |   387.925 ns | 131,172.98 ns | 10.790 |    0.03 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     245.08 ns |   0.748 ns |     0.495 ns |     244.89 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     326.05 ns |   0.726 ns |     0.480 ns |     326.23 ns |  0.027 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      27.22 ns |   0.045 ns |     0.023 ns |      27.21 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      27.25 ns |   0.077 ns |     0.046 ns |      27.23 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     428.42 ns |   3.115 ns |     2.060 ns |     428.18 ns |  0.035 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     260.17 ns |   3.310 ns |     1.731 ns |     260.85 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |               |            |              |               |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  12,156.18 ns |  33.065 ns |    49.490 ns |  12,150.25 ns |  1.000 |    0.01 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  18,552.82 ns | 197.323 ns |   295.343 ns |  18,581.06 ns |  1.526 |    0.02 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 136,011.43 ns | 695.374 ns | 1,019.271 ns | 136,061.02 ns | 11.189 |    0.09 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     249.44 ns |   1.284 ns |     1.841 ns |     248.50 ns |  0.021 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     334.22 ns |   1.528 ns |     2.240 ns |     335.22 ns |  0.027 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      27.39 ns |   0.073 ns |     0.105 ns |      27.40 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      27.32 ns |   0.042 ns |     0.056 ns |      27.31 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     437.33 ns |   6.676 ns |     9.359 ns |     445.32 ns |  0.036 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     261.92 ns |   0.956 ns |     1.371 ns |     261.62 ns |  0.022 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
