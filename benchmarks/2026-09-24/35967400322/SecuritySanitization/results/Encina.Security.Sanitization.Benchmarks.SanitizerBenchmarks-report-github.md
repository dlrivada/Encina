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
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  14,651.22 ns |     67.580 ns |    35.346 ns | 1.000 |    0.00 | 0.1678 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  18,630.16 ns |     87.685 ns |    45.861 ns | 1.272 |    0.00 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 138,566.66 ns |    527.106 ns |   313.673 ns | 9.458 |    0.03 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     245.47 ns |      1.105 ns |     0.731 ns | 0.017 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     300.30 ns |      0.649 ns |     0.339 ns | 0.020 |    0.00 | 0.0024 |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      32.36 ns |      0.213 ns |     0.127 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      32.39 ns |      0.230 ns |     0.137 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     394.45 ns |      1.749 ns |     1.041 ns | 0.027 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     304.51 ns |      1.182 ns |     0.782 ns | 0.021 |    0.00 | 0.0076 |     648 B |       0.045 |
|                                   |            |                |             |               |               |              |       |         |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  14,332.52 ns |    556.803 ns |    30.520 ns | 1.000 |    0.00 | 0.1526 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  18,604.72 ns |  2,396.589 ns |   131.365 ns | 1.298 |    0.01 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 136,777.82 ns | 29,331.937 ns | 1,607.782 ns | 9.543 |    0.10 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     243.11 ns |      2.635 ns |     0.144 ns | 0.017 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     298.17 ns |     33.829 ns |     1.854 ns | 0.021 |    0.00 | 0.0024 |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      32.10 ns |      2.073 ns |     0.114 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      32.23 ns |      1.231 ns |     0.067 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     394.63 ns |     14.499 ns |     0.795 ns | 0.028 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     301.13 ns |     15.128 ns |     0.829 ns | 0.021 |    0.00 | 0.0076 |     648 B |       0.045 |
