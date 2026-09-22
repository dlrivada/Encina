```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.91GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  15,445.77 ns |   152.030 ns |  90.471 ns |  1.000 |    0.01 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  22,083.08 ns |   192.946 ns | 114.819 ns |  1.430 |    0.01 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 184,549.33 ns |   894.437 ns | 532.265 ns | 11.949 |    0.07 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     240.94 ns |     0.957 ns |   0.569 ns |  0.016 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     332.45 ns |     2.347 ns |   1.553 ns |  0.022 |    0.00 | 0.0119 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      27.15 ns |     0.232 ns |   0.122 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      27.96 ns |     0.317 ns |   0.210 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     450.34 ns |     1.068 ns |   0.558 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     280.25 ns |     2.576 ns |   1.347 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  15,896.23 ns | 8,736.966 ns | 478.903 ns |  1.001 |    0.04 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  22,285.21 ns |   557.541 ns |  30.561 ns |  1.403 |    0.04 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 186,560.95 ns | 7,490.364 ns | 410.572 ns | 11.743 |    0.30 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     259.53 ns |    27.781 ns |   1.523 ns |  0.016 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     341.04 ns |    22.932 ns |   1.257 ns |  0.021 |    0.00 | 0.0119 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      28.26 ns |     1.366 ns |   0.075 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      28.41 ns |     9.816 ns |   0.538 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     451.95 ns |    11.897 ns |   0.652 ns |  0.028 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     293.39 ns |    59.213 ns |   3.246 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
