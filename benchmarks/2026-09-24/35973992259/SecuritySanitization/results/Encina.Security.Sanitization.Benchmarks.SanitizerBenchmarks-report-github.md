```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  14,948.00 ns |     36.369 ns |  21.642 ns |  1.000 |    0.00 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  21,394.48 ns |    151.958 ns | 100.511 ns |  1.431 |    0.01 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 185,782.05 ns |  1,142.942 ns | 755.986 ns | 12.429 |    0.05 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     248.13 ns |      0.444 ns |   0.294 ns |  0.017 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     318.42 ns |      1.454 ns |   0.865 ns |  0.021 |    0.00 | 0.0119 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      27.08 ns |      0.215 ns |   0.142 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      26.93 ns |      0.126 ns |   0.083 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     449.18 ns |      1.206 ns |   0.798 ns |  0.030 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     274.76 ns |      1.245 ns |   0.741 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |               |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  15,487.60 ns |    927.804 ns |  50.856 ns |  1.000 |    0.00 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  22,062.46 ns |    646.702 ns |  35.448 ns |  1.425 |    0.00 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 186,291.34 ns | 14,991.076 ns | 821.711 ns | 12.029 |    0.06 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     261.04 ns |      6.370 ns |   0.349 ns |  0.017 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     332.04 ns |      5.780 ns |   0.317 ns |  0.021 |    0.00 | 0.0119 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      27.28 ns |      2.566 ns |   0.141 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      27.24 ns |      1.335 ns |   0.073 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     445.51 ns |      9.161 ns |   0.502 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     281.22 ns |     29.335 ns |   1.608 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
