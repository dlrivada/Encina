```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|------:|--------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  14,950.53 ns |    41.042 ns |  24.424 ns | 1.000 |    0.00 | 0.1678 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  19,149.68 ns |   196.268 ns | 129.819 ns | 1.281 |    0.01 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 141,947.50 ns |   393.021 ns | 259.959 ns | 9.495 |    0.02 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     253.77 ns |     0.743 ns |   0.491 ns | 0.017 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     310.00 ns |     0.832 ns |   0.495 ns | 0.021 |    0.00 | 0.0024 |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      32.94 ns |     0.359 ns |   0.237 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      33.36 ns |     0.137 ns |   0.090 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     404.86 ns |     0.768 ns |   0.508 ns | 0.027 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     312.04 ns |     1.056 ns |   0.629 ns | 0.021 |    0.00 | 0.0076 |     648 B |       0.045 |
|                                   |            |                |             |               |              |            |       |         |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  14,804.80 ns |   715.512 ns |  39.220 ns | 1.000 |    0.00 | 0.1678 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  18,871.24 ns |   694.568 ns |  38.072 ns | 1.275 |    0.00 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 140,660.59 ns | 5,860.968 ns | 321.259 ns | 9.501 |    0.03 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     249.66 ns |    19.434 ns |   1.065 ns | 0.017 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     306.99 ns |     9.791 ns |   0.537 ns | 0.021 |    0.00 | 0.0024 |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      33.13 ns |     8.508 ns |   0.466 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      32.74 ns |     0.587 ns |   0.032 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     403.76 ns |     9.087 ns |   0.498 ns | 0.027 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     312.07 ns |    16.175 ns |   0.887 ns | 0.021 |    0.00 | 0.0076 |     648 B |       0.045 |
