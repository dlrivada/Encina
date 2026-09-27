```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error      | StdDev     | Median        | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-----------:|-----------:|--------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  15,756.73 ns |  86.023 ns |  56.899 ns |  15,746.49 ns |  1.000 |    0.00 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  21,992.55 ns | 108.999 ns |  72.096 ns |  21,986.00 ns |  1.396 |    0.01 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 183,385.19 ns | 889.631 ns | 588.436 ns | 183,574.12 ns | 11.639 |    0.05 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     251.99 ns |   0.728 ns |   0.482 ns |     251.75 ns |  0.016 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     335.19 ns |   1.259 ns |   0.749 ns |     335.05 ns |  0.021 |    0.00 | 0.0119 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      26.97 ns |   0.139 ns |   0.092 ns |      26.93 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      27.00 ns |   0.236 ns |   0.124 ns |      26.95 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     453.85 ns |   1.149 ns |   0.760 ns |     453.98 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     283.72 ns |   5.290 ns |   3.499 ns |     283.34 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |             |               |            |            |               |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  15,628.40 ns | 155.545 ns | 227.995 ns |  15,691.92 ns |  1.000 |    0.02 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  21,951.11 ns |  79.119 ns | 115.971 ns |  21,946.68 ns |  1.405 |    0.02 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 185,141.47 ns | 446.827 ns | 654.954 ns | 185,203.12 ns | 11.849 |    0.18 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     255.01 ns |   1.811 ns |   2.539 ns |     253.55 ns |  0.016 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     330.10 ns |   1.113 ns |   1.597 ns |     329.55 ns |  0.021 |    0.00 | 0.0119 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      26.94 ns |   0.078 ns |   0.114 ns |      26.93 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      27.10 ns |   0.083 ns |   0.121 ns |      27.11 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     450.17 ns |   3.345 ns |   4.797 ns |     447.10 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     277.85 ns |   2.081 ns |   2.985 ns |     278.18 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
