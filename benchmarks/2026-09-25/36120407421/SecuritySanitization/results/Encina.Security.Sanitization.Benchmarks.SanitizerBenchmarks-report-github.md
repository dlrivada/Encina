```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  16,024.37 ns |   149.349 ns |  88.875 ns |  1.000 |    0.01 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  22,373.59 ns |   210.870 ns | 139.478 ns |  1.396 |    0.01 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 187,958.22 ns | 1,348.895 ns | 802.706 ns | 11.730 |    0.08 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     242.72 ns |     0.930 ns |   0.553 ns |  0.015 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     326.75 ns |     1.559 ns |   1.031 ns |  0.020 |    0.00 | 0.0119 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      28.64 ns |     0.283 ns |   0.169 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      28.39 ns |     0.107 ns |   0.056 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     462.30 ns |     0.755 ns |   0.499 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     292.05 ns |     4.823 ns |   3.190 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  15,796.77 ns | 2,000.412 ns | 109.649 ns |  1.000 |    0.01 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  22,127.01 ns | 1,693.939 ns |  92.851 ns |  1.401 |    0.01 | 0.8240 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 190,677.63 ns | 6,236.399 ns | 341.838 ns | 12.071 |    0.07 | 4.3945 | 0.2441 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     254.56 ns |     9.437 ns |   0.517 ns |  0.016 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     335.60 ns |    15.754 ns |   0.864 ns |  0.021 |    0.00 | 0.0119 |      - |     200 B |       0.014 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      28.40 ns |     3.687 ns |   0.202 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      28.22 ns |     2.418 ns |   0.133 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     463.03 ns |    55.030 ns |   3.016 ns |  0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     290.12 ns |    50.926 ns |   2.791 ns |  0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
