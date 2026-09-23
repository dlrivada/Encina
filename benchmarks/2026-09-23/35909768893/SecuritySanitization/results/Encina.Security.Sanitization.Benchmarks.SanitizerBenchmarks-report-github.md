```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  12,690.91 ns |     43.411 ns |  25.833 ns | 1.000 |    0.00 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  16,100.52 ns |     63.596 ns |  37.845 ns | 1.269 |    0.00 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 118,684.97 ns |    663.594 ns | 394.894 ns | 9.352 |    0.03 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     208.87 ns |      0.726 ns |   0.432 ns | 0.016 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     259.55 ns |      1.195 ns |   0.790 ns | 0.020 |    0.00 | 0.0024 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      27.13 ns |      0.129 ns |   0.077 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      27.08 ns |      0.258 ns |   0.135 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     341.67 ns |      7.650 ns |   5.060 ns | 0.027 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     261.67 ns |      6.751 ns |   4.466 ns | 0.021 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |               |            |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  12,201.45 ns |  2,767.994 ns | 151.723 ns | 1.000 |    0.02 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  16,184.42 ns | 11,896.108 ns | 652.066 ns | 1.327 |    0.05 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 116,734.50 ns |  3,774.720 ns | 206.905 ns | 9.568 |    0.10 | 0.7324 |      - |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     209.84 ns |      2.551 ns |   0.140 ns | 0.017 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     261.83 ns |     78.179 ns |   4.285 ns | 0.021 |    0.00 | 0.0024 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      27.19 ns |      4.177 ns |   0.229 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      27.56 ns |      7.147 ns |   0.392 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     337.92 ns |     23.944 ns |   1.312 ns | 0.028 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     251.16 ns |     99.027 ns |   5.428 ns | 0.021 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
