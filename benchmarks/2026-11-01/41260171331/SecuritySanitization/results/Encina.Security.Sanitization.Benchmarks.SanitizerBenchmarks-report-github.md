```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.23GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error        | StdDev       | Median       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |-------------:|-------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  8,364.40 ns |   354.268 ns |   210.819 ns |  8,261.31 ns | 1.001 |    0.03 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           | 10,695.64 ns |   126.736 ns |    66.285 ns | 10,701.04 ns | 1.279 |    0.03 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 77,790.86 ns | 1,308.496 ns |   778.665 ns | 77,663.38 ns | 9.305 |    0.23 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |    119.73 ns |     2.470 ns |     1.634 ns |    119.69 ns | 0.014 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |    203.46 ns |     4.105 ns |     2.715 ns |    203.72 ns | 0.024 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |     15.80 ns |     0.553 ns |     0.329 ns |     15.70 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |     16.17 ns |     0.388 ns |     0.231 ns |     16.23 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |    254.44 ns |     3.071 ns |     1.828 ns |    254.09 ns | 0.030 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |    157.00 ns |     3.120 ns |     1.857 ns |    157.22 ns | 0.019 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |             |              |              |              |              |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  8,203.63 ns |    58.398 ns |    77.960 ns |  8,224.57 ns | 1.000 |    0.01 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          | 10,748.97 ns |   579.511 ns |   812.393 ns | 10,111.01 ns | 1.310 |    0.10 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 75,497.77 ns | 1,594.627 ns | 2,337.383 ns | 75,054.10 ns | 9.204 |    0.29 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |    121.46 ns |     2.423 ns |     3.551 ns |    121.69 ns | 0.015 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |    199.44 ns |     4.012 ns |     5.753 ns |    197.99 ns | 0.024 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |     15.55 ns |     0.345 ns |     0.483 ns |     15.39 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |     15.71 ns |     0.341 ns |     0.489 ns |     15.71 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |    235.37 ns |     3.276 ns |     4.592 ns |    234.84 ns | 0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |    156.29 ns |     5.259 ns |     7.871 ns |    155.78 ns | 0.019 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
