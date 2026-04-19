```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.202
  [Host]     : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.6 (10.0.6, 10.0.626.17701), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error        | StdDev     | Median        | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-------------:|-----------:|--------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  11,190.48 ns |   392.444 ns | 259.577 ns |  11,117.24 ns | 1.000 |    0.03 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  15,700.60 ns |   366.867 ns | 242.660 ns |  15,631.28 ns | 1.404 |    0.04 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 103,066.42 ns | 1,374.078 ns | 908.868 ns | 102,770.46 ns | 9.215 |    0.22 | 3.9063 | 0.2441 |   66725 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     236.13 ns |     0.667 ns |   0.397 ns |     236.09 ns | 0.021 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     293.11 ns |     1.532 ns |   0.912 ns |     293.10 ns | 0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      28.00 ns |     0.775 ns |   0.512 ns |      28.04 ns | 0.003 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      30.42 ns |     0.512 ns |   0.339 ns |      30.48 ns | 0.003 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     410.24 ns |     1.385 ns |   0.916 ns |     410.69 ns | 0.037 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     271.20 ns |     3.731 ns |   2.468 ns |     271.21 ns | 0.024 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |               |              |            |               |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  11,135.70 ns |    74.470 ns | 111.462 ns |  11,121.24 ns | 1.000 |    0.01 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  15,391.15 ns |   135.482 ns | 194.304 ns |  15,316.43 ns | 1.382 |    0.02 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 103,502.82 ns |   511.774 ns | 733.971 ns | 103,385.19 ns | 9.296 |    0.11 | 3.9063 | 0.2441 |   66725 B |       5.374 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     236.29 ns |     1.706 ns |   2.501 ns |     237.98 ns | 0.021 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     293.27 ns |     1.579 ns |   2.265 ns |     293.15 ns | 0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      27.77 ns |     0.192 ns |   0.288 ns |      27.78 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      27.60 ns |     0.404 ns |   0.593 ns |      27.87 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     407.47 ns |     1.310 ns |   1.836 ns |     407.82 ns | 0.037 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     268.36 ns |     2.254 ns |   3.304 ns |     269.35 ns | 0.024 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
