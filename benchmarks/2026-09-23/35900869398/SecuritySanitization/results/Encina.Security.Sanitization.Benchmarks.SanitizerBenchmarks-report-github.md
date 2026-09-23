```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev       | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-------------:|------:|--------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  14,987.90 ns |     14.771 ns |     8.790 ns | 1.000 |    0.00 | 0.1678 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  18,977.32 ns |    120.576 ns |    71.753 ns | 1.266 |    0.00 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 142,490.17 ns |    622.498 ns |   411.744 ns | 9.507 |    0.03 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     251.63 ns |      0.839 ns |     0.499 ns | 0.017 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     306.79 ns |      3.205 ns |     1.907 ns | 0.020 |    0.00 | 0.0024 |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      33.11 ns |      0.234 ns |     0.155 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      32.73 ns |      0.223 ns |     0.147 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     402.51 ns |      0.705 ns |     0.420 ns | 0.027 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     309.26 ns |      2.806 ns |     1.670 ns | 0.021 |    0.00 | 0.0076 |     648 B |       0.045 |
|                                   |            |                |             |               |               |              |       |         |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  14,890.28 ns |    282.589 ns |    15.490 ns | 1.000 |    0.00 | 0.1678 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  18,910.57 ns |    683.999 ns |    37.492 ns | 1.270 |    0.00 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 140,913.06 ns | 28,107.932 ns | 1,540.691 ns | 9.463 |    0.09 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     250.56 ns |      9.752 ns |     0.535 ns | 0.017 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     307.09 ns |      8.016 ns |     0.439 ns | 0.021 |    0.00 | 0.0024 |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      32.51 ns |      2.003 ns |     0.110 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      33.08 ns |      2.485 ns |     0.136 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     401.61 ns |      5.012 ns |     0.275 ns | 0.027 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     310.74 ns |     15.592 ns |     0.855 ns | 0.021 |    0.00 | 0.0076 |     648 B |       0.045 |
