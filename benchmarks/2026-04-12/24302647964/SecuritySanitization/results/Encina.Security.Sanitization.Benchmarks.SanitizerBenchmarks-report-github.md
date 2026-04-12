```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error        | StdDev       | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-------------:|-------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  12,334.87 ns |    70.289 ns |    46.492 ns |  1.000 |    0.01 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  18,880.49 ns |   119.032 ns |    78.732 ns |  1.531 |    0.01 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 134,217.41 ns |   741.626 ns |   441.330 ns | 10.881 |    0.05 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     244.25 ns |     1.266 ns |     0.837 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     327.73 ns |     1.997 ns |     1.321 ns |  0.027 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      27.72 ns |     0.187 ns |     0.111 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      27.61 ns |     0.121 ns |     0.072 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     442.33 ns |     1.301 ns |     0.861 ns |  0.036 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     263.97 ns |     1.184 ns |     0.783 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |               |              |              |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  12,501.16 ns |   113.963 ns |   170.574 ns |  1.000 |    0.02 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  18,333.91 ns |   114.974 ns |   168.527 ns |  1.467 |    0.02 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 138,452.23 ns | 1,708.542 ns | 2,504.359 ns | 11.077 |    0.25 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     257.59 ns |     0.520 ns |     0.745 ns |  0.021 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     336.14 ns |     1.176 ns |     1.724 ns |  0.027 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      27.70 ns |     0.108 ns |     0.151 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      27.59 ns |     0.073 ns |     0.106 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     455.72 ns |     1.726 ns |     2.583 ns |  0.036 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     268.30 ns |     1.369 ns |     2.050 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
