```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V74 2.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error        | StdDev       | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-------------:|-------------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  10,181.11 ns |   150.080 ns |    99.268 ns |  1.000 |    0.01 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  13,617.91 ns |   236.082 ns |   140.489 ns |  1.338 |    0.02 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 117,187.39 ns | 3,184.262 ns | 2,106.192 ns | 11.511 |    0.22 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     148.34 ns |     0.191 ns |     0.113 ns |  0.015 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     233.88 ns |     2.189 ns |     1.303 ns |  0.023 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      20.95 ns |     0.674 ns |     0.446 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      20.32 ns |     0.136 ns |     0.081 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     327.61 ns |     2.106 ns |     1.101 ns |  0.032 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     207.15 ns |     5.125 ns |     3.390 ns |  0.020 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |             |               |              |              |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  10,983.68 ns |   138.246 ns |   198.269 ns |  1.000 |    0.02 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  13,607.52 ns |   209.841 ns |   314.081 ns |  1.239 |    0.04 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 119,531.15 ns |   646.271 ns |   926.862 ns | 10.886 |    0.21 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     147.88 ns |     0.684 ns |     0.959 ns |  0.013 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     232.69 ns |     0.743 ns |     1.113 ns |  0.021 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      21.13 ns |     0.220 ns |     0.322 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      20.78 ns |     0.085 ns |     0.124 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     318.38 ns |     0.824 ns |     1.100 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     211.44 ns |     3.320 ns |     4.867 ns |  0.019 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
