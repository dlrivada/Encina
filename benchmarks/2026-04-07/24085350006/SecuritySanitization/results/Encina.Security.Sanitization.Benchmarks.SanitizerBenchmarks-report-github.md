```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev       | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  13,015.53 ns |     59.095 ns |    30.908 ns |  1.000 |    0.00 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  19,813.03 ns |     36.754 ns |    21.872 ns |  1.522 |    0.00 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 140,893.10 ns |    792.529 ns |   471.621 ns | 10.825 |    0.04 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     252.51 ns |      0.498 ns |     0.296 ns |  0.019 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     330.30 ns |      1.345 ns |     0.801 ns |  0.025 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      29.66 ns |      0.129 ns |     0.085 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      29.50 ns |      0.157 ns |     0.104 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     466.70 ns |      1.321 ns |     0.874 ns |  0.036 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     291.91 ns |      2.408 ns |     1.593 ns |  0.022 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |               |               |              |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  13,527.84 ns |    814.378 ns |    44.639 ns |  1.000 |    0.00 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  18,835.70 ns |  1,130.130 ns |    61.946 ns |  1.392 |    0.01 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 145,564.83 ns | 66,537.449 ns | 3,647.142 ns | 10.760 |    0.24 | 3.9063 | 0.2441 |   66727 B |       5.374 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     265.92 ns |     72.144 ns |     3.954 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     354.81 ns |     11.856 ns |     0.650 ns |  0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      29.82 ns |      2.320 ns |     0.127 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      29.34 ns |      3.407 ns |     0.187 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     457.72 ns |     35.364 ns |     1.938 ns |  0.034 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     292.83 ns |     11.364 ns |     0.623 ns |  0.022 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
