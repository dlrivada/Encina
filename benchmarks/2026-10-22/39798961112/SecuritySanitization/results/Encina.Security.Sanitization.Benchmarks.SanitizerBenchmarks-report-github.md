```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error        | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |-------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  7,954.37 ns |    76.441 ns |    50.561 ns | 1.000 |    0.01 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  9,305.52 ns |    97.201 ns |    64.292 ns | 1.170 |    0.01 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 68,399.84 ns |   549.457 ns |   287.377 ns | 8.599 |    0.06 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |    109.22 ns |     3.232 ns |     2.138 ns | 0.014 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |    187.86 ns |     6.045 ns |     3.998 ns | 0.024 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |     14.71 ns |     0.404 ns |     0.267 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |     14.44 ns |     0.472 ns |     0.312 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |    232.78 ns |     6.021 ns |     3.982 ns | 0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |    142.71 ns |     7.995 ns |     5.288 ns | 0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |             |              |              |              |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  7,167.85 ns |    86.851 ns |   121.752 ns | 1.000 |    0.02 | 0.8621 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  9,297.68 ns |    94.427 ns |   138.411 ns | 1.297 |    0.03 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 71,113.12 ns | 1,499.694 ns | 2,102.362 ns | 9.924 |    0.33 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |    114.15 ns |     3.073 ns |     4.600 ns | 0.016 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |    186.27 ns |     3.658 ns |     5.475 ns | 0.026 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |     14.59 ns |     0.178 ns |     0.261 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |     14.37 ns |     0.246 ns |     0.353 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |    216.64 ns |     2.786 ns |     3.996 ns | 0.030 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |    139.64 ns |     2.022 ns |     2.767 ns | 0.019 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
