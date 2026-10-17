```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  16,328.57 ns |    58.605 ns |  38.764 ns |  1.000 |    0.00 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  21,883.23 ns |    88.411 ns |  58.478 ns |  1.340 |    0.00 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 162,755.57 ns |   281.320 ns | 147.136 ns |  9.968 |    0.02 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     174.72 ns |     0.664 ns |   0.395 ns |  0.011 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     291.92 ns |     3.266 ns |   1.944 ns |  0.018 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      29.71 ns |     0.145 ns |   0.096 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      30.29 ns |     0.082 ns |   0.049 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     447.72 ns |     2.751 ns |   1.637 ns |  0.027 |    0.00 | 0.0081 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     281.93 ns |     1.862 ns |   0.974 ns |  0.017 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  15,812.74 ns | 1,188.872 ns |  65.166 ns |  1.000 |    0.01 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  21,439.23 ns |   672.036 ns |  36.837 ns |  1.356 |    0.01 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 162,945.87 ns | 5,836.445 ns | 319.915 ns | 10.305 |    0.04 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     175.94 ns |     1.456 ns |   0.080 ns |  0.011 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     289.85 ns |     4.634 ns |   0.254 ns |  0.018 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      30.05 ns |     2.941 ns |   0.161 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      30.10 ns |     1.579 ns |   0.087 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     438.54 ns |    21.047 ns |   1.154 ns |  0.028 |    0.00 | 0.0081 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     286.91 ns |    22.559 ns |   1.237 ns |  0.018 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
