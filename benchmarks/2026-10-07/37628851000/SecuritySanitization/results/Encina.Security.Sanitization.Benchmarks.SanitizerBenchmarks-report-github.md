```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean         | Error         | StdDev       | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |-------------:|--------------:|-------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  7,148.57 ns |    191.233 ns |   100.018 ns |  1.000 |    0.02 | 0.8621 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  9,465.19 ns |    309.317 ns |   204.594 ns |  1.324 |    0.03 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 70,490.79 ns |  2,786.786 ns | 1,658.371 ns |  9.863 |    0.26 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |    118.81 ns |      5.694 ns |     3.766 ns |  0.017 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |    194.45 ns |      9.651 ns |     5.743 ns |  0.027 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |     15.75 ns |      0.358 ns |     0.237 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |     14.90 ns |      0.451 ns |     0.299 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |    231.48 ns |      1.333 ns |     0.793 ns |  0.032 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |    155.49 ns |      8.789 ns |     5.230 ns |  0.022 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |              |               |              |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  7,600.33 ns |  3,947.886 ns |   216.397 ns |  1.001 |    0.03 | 0.8621 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  9,854.99 ns |  1,506.268 ns |    82.564 ns |  1.297 |    0.03 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 79,795.72 ns | 67,897.009 ns | 3,721.664 ns | 10.505 |    0.50 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |    122.46 ns |     72.953 ns |     3.999 ns |  0.016 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |    203.39 ns |    122.385 ns |     6.708 ns |  0.027 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |     16.76 ns |      6.855 ns |     0.376 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |     15.49 ns |      5.105 ns |     0.280 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |    227.02 ns |     65.049 ns |     3.566 ns |  0.030 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |    156.66 ns |    248.459 ns |    13.619 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
