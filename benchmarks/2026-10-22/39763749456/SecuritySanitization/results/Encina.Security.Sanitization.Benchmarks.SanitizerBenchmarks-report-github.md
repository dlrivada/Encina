```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-------------:|------:|--------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  15,648.57 ns |    149.358 ns |    88.881 ns | 1.000 |    0.01 | 0.1526 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  19,277.86 ns |    139.133 ns |    92.028 ns | 1.232 |    0.01 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 141,977.98 ns |    580.962 ns |   345.721 ns | 9.073 |    0.05 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     173.09 ns |      0.779 ns |     0.515 ns | 0.011 |    0.00 | 0.0021 |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     277.46 ns |      2.149 ns |     1.422 ns | 0.018 |    0.00 | 0.0029 |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      32.80 ns |      0.343 ns |     0.227 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      32.90 ns |      0.304 ns |     0.201 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     406.27 ns |      0.986 ns |     0.652 ns | 0.026 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     296.95 ns |      2.714 ns |     1.795 ns | 0.019 |    0.00 | 0.0076 |     648 B |       0.045 |
|                                   |            |                |             |               |               |              |       |         |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  15,163.56 ns |    367.652 ns |    20.152 ns | 1.000 |    0.00 | 0.1678 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  19,750.20 ns |  1,819.599 ns |    99.738 ns | 1.302 |    0.01 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 141,350.52 ns | 18,728.969 ns | 1,026.598 ns | 9.322 |    0.06 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     176.51 ns |     44.149 ns |     2.420 ns | 0.012 |    0.00 | 0.0021 |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     277.74 ns |      7.427 ns |     0.407 ns | 0.018 |    0.00 | 0.0029 |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      32.29 ns |      2.020 ns |     0.111 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      32.80 ns |      2.946 ns |     0.162 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     410.17 ns |     62.867 ns |     3.446 ns | 0.027 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     305.35 ns |     21.274 ns |     1.166 ns | 0.020 |    0.00 | 0.0076 |     648 B |       0.045 |
