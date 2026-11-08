```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.54GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean          | Error      | StdDev       | Median        | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |--------------:|-----------:|-------------:|--------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  13,287.60 ns | 213.399 ns |   141.150 ns |  13,312.63 ns | 1.000 |    0.01 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           |  16,416.37 ns | 205.236 ns |   135.751 ns |  16,467.04 ns | 1.236 |    0.02 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 119,775.59 ns | 519.078 ns |   343.338 ns | 119,867.18 ns | 9.015 |    0.09 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     147.15 ns |   1.855 ns |     1.227 ns |     147.09 ns | 0.011 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |     233.27 ns |   2.186 ns |     1.301 ns |     233.11 ns | 0.018 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |      26.05 ns |   0.313 ns |     0.186 ns |      26.14 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |      25.87 ns |   0.423 ns |     0.280 ns |      25.85 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |     343.05 ns |   0.701 ns |     0.417 ns |     343.02 ns | 0.026 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |     241.29 ns |   5.542 ns |     3.298 ns |     241.29 ns | 0.018 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |             |               |            |              |               |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  12,404.22 ns | 102.528 ns |   153.459 ns |  12,402.36 ns | 1.000 |    0.02 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          |  15,897.87 ns | 163.454 ns |   244.650 ns |  15,995.92 ns | 1.282 |    0.02 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 117,909.43 ns | 853.219 ns | 1,223.662 ns | 118,152.86 ns | 9.507 |    0.15 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |     146.02 ns |   1.734 ns |     2.542 ns |     146.52 ns | 0.012 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |     230.99 ns |   1.876 ns |     2.750 ns |     230.47 ns | 0.019 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |      26.10 ns |   0.277 ns |     0.407 ns |      26.15 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |      27.02 ns |   0.289 ns |     0.423 ns |      27.00 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |     345.46 ns |   1.623 ns |     2.166 ns |     343.95 ns | 0.028 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |     240.10 ns |   3.391 ns |     4.970 ns |     239.89 ns | 0.019 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
