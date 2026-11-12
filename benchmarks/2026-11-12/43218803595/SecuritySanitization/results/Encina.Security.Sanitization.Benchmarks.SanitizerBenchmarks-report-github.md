```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  15,701.86 ns |   223.517 ns | 133.011 ns |  1.000 |    0.01 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  21,768.40 ns |    98.675 ns |  65.267 ns |  1.386 |    0.01 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 160,683.20 ns |   691.231 ns | 411.341 ns | 10.234 |    0.09 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     176.12 ns |     1.050 ns |   0.694 ns |  0.011 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     288.69 ns |     1.304 ns |   0.682 ns |  0.018 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      29.40 ns |     0.294 ns |   0.194 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      29.27 ns |     0.201 ns |   0.120 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     452.77 ns |     2.518 ns |   1.317 ns |  0.029 |    0.00 | 0.0081 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     277.50 ns |     2.570 ns |   1.700 ns |  0.018 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  16,311.48 ns |   826.950 ns |  45.328 ns |  1.000 |    0.00 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  21,069.67 ns |   943.980 ns |  51.743 ns |  1.292 |    0.00 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 161,135.92 ns | 9,632.030 ns | 527.964 ns |  9.879 |    0.04 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     176.27 ns |     5.933 ns |   0.325 ns |  0.011 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     286.84 ns |    26.011 ns |   1.426 ns |  0.018 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      28.70 ns |     5.589 ns |   0.306 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      29.02 ns |     1.857 ns |   0.102 ns |  0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     444.07 ns |     9.136 ns |   0.501 ns |  0.027 |    0.00 | 0.0081 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     273.69 ns |   133.212 ns |   7.302 ns |  0.017 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
