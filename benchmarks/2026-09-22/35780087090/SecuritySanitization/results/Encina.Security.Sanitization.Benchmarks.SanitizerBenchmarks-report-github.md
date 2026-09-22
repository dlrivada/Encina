```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean         | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |-------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 10,672.89 ns | 1,187.596 ns | 785.521 ns | 1.005 |    0.10 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 12,954.40 ns |   408.521 ns | 243.105 ns | 1.220 |    0.09 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 94,965.50 ns | 1,275.334 ns | 843.554 ns | 8.940 |    0.62 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |    178.76 ns |    20.742 ns |  13.719 ns | 0.017 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |    204.94 ns |     0.773 ns |   0.460 ns | 0.019 |    0.00 | 0.0024 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |     20.21 ns |     0.208 ns |   0.137 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |     20.88 ns |     0.182 ns |   0.121 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |    272.13 ns |     3.012 ns |   1.992 ns | 0.026 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |    220.57 ns |    20.284 ns |  13.416 ns | 0.021 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |              |              |            |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           | 10,381.33 ns | 4,330.784 ns | 237.385 ns | 1.000 |    0.03 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           | 13,161.71 ns | 2,135.642 ns | 117.062 ns | 1.268 |    0.03 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 95,805.28 ns | 4,709.332 ns | 258.134 ns | 9.232 |    0.19 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |    173.37 ns |     8.363 ns |   0.458 ns | 0.017 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |    205.58 ns |    29.409 ns |   1.612 ns | 0.020 |    0.00 | 0.0024 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |     21.25 ns |     2.549 ns |   0.140 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |     21.88 ns |     9.005 ns |   0.494 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |    270.98 ns |    16.950 ns |   0.929 ns | 0.026 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |    204.41 ns |     6.346 ns |   0.348 ns | 0.020 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
