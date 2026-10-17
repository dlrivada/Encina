```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  16,128.52 ns |    81.918 ns |  54.183 ns |  1.000 |    0.00 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  21,430.68 ns |    83.449 ns |  55.196 ns |  1.329 |    0.01 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 162,189.84 ns |   238.440 ns | 124.709 ns | 10.056 |    0.03 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     171.99 ns |     0.742 ns |   0.388 ns |  0.011 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     288.91 ns |     0.653 ns |   0.432 ns |  0.018 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      29.25 ns |     0.177 ns |   0.117 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      29.23 ns |     0.160 ns |   0.095 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     437.26 ns |     2.597 ns |   1.718 ns |  0.027 |    0.00 | 0.0081 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     272.89 ns |     5.842 ns |   3.864 ns |  0.017 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  16,446.68 ns | 1,716.448 ns |  94.084 ns |  1.000 |    0.01 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  21,633.31 ns |   350.518 ns |  19.213 ns |  1.315 |    0.01 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 164,139.59 ns | 6,162.783 ns | 337.803 ns |  9.980 |    0.05 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     175.03 ns |     3.923 ns |   0.215 ns |  0.011 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     289.83 ns |     9.712 ns |   0.532 ns |  0.018 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      29.46 ns |     1.061 ns |   0.058 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      29.71 ns |     3.537 ns |   0.194 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     434.76 ns |    14.047 ns |   0.770 ns |  0.026 |    0.00 | 0.0081 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     277.88 ns |    19.754 ns |   1.083 ns |  0.017 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
