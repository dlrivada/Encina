```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.00GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error      | StdDev     | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-----------:|-----------:|------:|--------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  15,345.00 ns | 213.110 ns | 140.959 ns | 1.000 |    0.01 | 0.1526 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  19,408.79 ns |  84.698 ns |  50.402 ns | 1.265 |    0.01 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 143,051.08 ns | 817.269 ns | 540.573 ns | 9.323 |    0.09 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     175.92 ns |   0.893 ns |   0.590 ns | 0.011 |    0.00 | 0.0021 |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     278.70 ns |   1.440 ns |   0.857 ns | 0.018 |    0.00 | 0.0029 |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      33.80 ns |   0.238 ns |   0.158 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      34.58 ns |   0.139 ns |   0.083 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     414.02 ns |   0.971 ns |   0.578 ns | 0.027 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     313.40 ns |   2.168 ns |   1.290 ns | 0.020 |    0.00 | 0.0076 |     648 B |       0.045 |
|                                   |            |                |             |             |               |            |            |       |         |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  15,005.72 ns |  60.508 ns |  88.692 ns | 1.000 |    0.01 | 0.1678 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  19,324.05 ns |  95.382 ns | 142.764 ns | 1.288 |    0.01 | 0.1526 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 141,888.08 ns | 665.721 ns | 996.421 ns | 9.456 |    0.09 | 0.7324 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     177.51 ns |   0.718 ns |   1.030 ns | 0.012 |    0.00 | 0.0021 |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     277.58 ns |   1.220 ns |   1.750 ns | 0.018 |    0.00 | 0.0029 |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      34.24 ns |   0.131 ns |   0.193 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      33.99 ns |   0.083 ns |   0.116 ns | 0.002 |    0.00 | 0.0005 |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     410.29 ns |   0.895 ns |   1.339 ns | 0.027 |    0.00 | 0.0024 |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     316.87 ns |   2.282 ns |   3.416 ns | 0.021 |    0.00 | 0.0076 |     648 B |       0.045 |
