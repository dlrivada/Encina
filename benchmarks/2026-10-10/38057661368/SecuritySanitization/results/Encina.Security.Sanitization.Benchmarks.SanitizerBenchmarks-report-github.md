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
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  13,415.51 ns |    266.176 ns |   158.397 ns | 1.000 |    0.02 | 0.1678 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  17,033.08 ns |    159.042 ns |    94.643 ns | 1.270 |    0.02 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 127,189.22 ns |  2,055.588 ns | 1,359.644 ns | 9.482 |    0.14 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     156.58 ns |      1.149 ns |     0.760 ns | 0.012 |    0.00 | 0.0021 |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     247.17 ns |      2.322 ns |     1.382 ns | 0.018 |    0.00 | 0.0029 |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      29.23 ns |      1.230 ns |     0.814 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      28.60 ns |      0.431 ns |     0.285 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     358.28 ns |      3.768 ns |     2.492 ns | 0.027 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     274.87 ns |     11.795 ns |     7.802 ns | 0.020 |    0.00 | 0.0076 |     648 B |       0.045 |
|                                   |            |                |             |               |               |              |       |         |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  13,155.01 ns |  1,916.669 ns |   105.059 ns | 1.000 |    0.01 | 0.1678 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  16,923.48 ns |  2,455.892 ns |   134.616 ns | 1.287 |    0.01 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 122,782.01 ns | 14,272.692 ns |   782.334 ns | 9.334 |    0.08 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     156.24 ns |     20.182 ns |     1.106 ns | 0.012 |    0.00 | 0.0021 |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     252.42 ns |     67.801 ns |     3.716 ns | 0.019 |    0.00 | 0.0029 |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      29.87 ns |     25.447 ns |     1.395 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      28.96 ns |     10.061 ns |     0.551 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     368.27 ns |     18.547 ns |     1.017 ns | 0.028 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     264.87 ns |     73.410 ns |     4.024 ns | 0.020 |    0.00 | 0.0076 |     648 B |       0.045 |
