```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-----------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  15,798.35 ns |  92.941 ns |  55.308 ns |  1.000 |    0.00 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  22,196.30 ns | 163.302 ns |  97.178 ns |  1.405 |    0.01 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 185,794.58 ns | 666.826 ns | 441.064 ns | 11.761 |    0.05 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     194.14 ns |   1.149 ns |   0.760 ns |  0.012 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     308.81 ns |   1.450 ns |   0.959 ns |  0.020 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      27.42 ns |   0.114 ns |   0.075 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      29.55 ns |   0.360 ns |   0.238 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     464.39 ns |   1.606 ns |   0.956 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     288.24 ns |   9.349 ns |   6.184 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |             |               |            |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  15,999.92 ns | 155.207 ns | 227.500 ns |  1.000 |    0.02 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  21,936.04 ns | 123.639 ns | 177.319 ns |  1.371 |    0.02 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 188,830.48 ns | 681.705 ns | 955.656 ns | 11.804 |    0.18 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     196.26 ns |   1.270 ns |   1.900 ns |  0.012 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     307.09 ns |   0.449 ns |   0.658 ns |  0.019 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      27.91 ns |   0.292 ns |   0.437 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      28.79 ns |   0.105 ns |   0.154 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     465.88 ns |   1.324 ns |   1.812 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     302.96 ns |   2.735 ns |   4.009 ns |  0.019 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
