```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-----------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  12,565.97 ns | 112.852 ns |  74.645 ns |  1.000 |    0.01 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  18,478.46 ns |  84.950 ns |  50.552 ns |  1.471 |    0.01 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 136,216.07 ns | 521.969 ns | 345.250 ns | 10.840 |    0.07 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     246.91 ns |   0.566 ns |   0.337 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     326.22 ns |   1.961 ns |   1.026 ns |  0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      28.49 ns |   0.428 ns |   0.283 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      28.04 ns |   0.379 ns |   0.251 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     448.51 ns |   0.946 ns |   0.626 ns |  0.036 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     265.30 ns |   1.952 ns |   1.162 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |               |            |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  12,726.15 ns |  45.554 ns |  68.184 ns |  1.000 |    0.01 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  18,723.20 ns | 151.776 ns | 217.673 ns |  1.471 |    0.02 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 136,755.22 ns | 347.908 ns | 520.733 ns | 10.746 |    0.07 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     254.88 ns |   3.283 ns |   4.709 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     336.91 ns |   2.862 ns |   3.918 ns |  0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      27.80 ns |   0.079 ns |   0.110 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      27.93 ns |   0.102 ns |   0.150 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     454.06 ns |   1.302 ns |   1.909 ns |  0.036 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     268.97 ns |   1.206 ns |   1.730 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
