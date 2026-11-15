```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.21GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4


```
| Method                            | Job        | IterationCount | LaunchCount | WarmupCount | Mean         | Error        | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |-------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | 3           |  8,492.21 ns |   215.269 ns |   128.103 ns | 1.000 |    0.02 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | 3           | 10,810.88 ns |   272.901 ns |   180.507 ns | 1.273 |    0.03 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 3           | 79,109.11 ns | 5,837.221 ns | 3,860.960 ns | 9.317 |    0.45 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |    119.18 ns |     8.799 ns |     5.820 ns | 0.014 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | 3           |    196.42 ns |     1.917 ns |     1.141 ns | 0.023 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | 3           |     15.07 ns |     0.847 ns |     0.504 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | 3           |     15.44 ns |     0.391 ns |     0.258 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | 3           |    239.64 ns |    10.257 ns |     6.785 ns | 0.028 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | 3           |    157.67 ns |     7.811 ns |     5.167 ns | 0.019 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |             |              |              |              |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | MediumRun  | 15             | 2           | 10          |  8,333.03 ns |    96.677 ns |   138.651 ns | 1.000 |    0.02 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | MediumRun  | 15             | 2           | 10          | 10,801.40 ns |   256.773 ns |   351.474 ns | 1.297 |    0.05 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | MediumRun  | 15             | 2           | 10          | 79,500.61 ns | 1,781.175 ns | 2,554.507 ns | 9.543 |    0.34 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | MediumRun  | 15             | 2           | 10          |    121.26 ns |     2.435 ns |     3.492 ns | 0.015 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | MediumRun  | 15             | 2           | 10          |    200.13 ns |     2.884 ns |     4.317 ns | 0.024 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | MediumRun  | 15             | 2           | 10          |     16.37 ns |     0.185 ns |     0.272 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | MediumRun  | 15             | 2           | 10          |     16.25 ns |     0.343 ns |     0.458 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | MediumRun  | 15             | 2           | 10          |    240.08 ns |     2.144 ns |     3.142 ns | 0.029 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | MediumRun  | 15             | 2           | 10          |    174.80 ns |     2.498 ns |     3.661 ns | 0.021 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
