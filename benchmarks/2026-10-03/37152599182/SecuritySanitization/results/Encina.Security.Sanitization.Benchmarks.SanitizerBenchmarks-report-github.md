```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  16,607.49 ns |     53.705 ns |    35.523 ns | 1.000 |    0.00 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  21,544.24 ns |     45.237 ns |    26.920 ns | 1.297 |    0.00 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 162,226.38 ns |    660.331 ns |   436.768 ns | 9.768 |    0.03 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     173.00 ns |      0.441 ns |     0.292 ns | 0.010 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     287.44 ns |      0.997 ns |     0.659 ns | 0.017 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      29.02 ns |      0.096 ns |     0.063 ns | 0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      29.07 ns |      0.180 ns |     0.107 ns | 0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     487.54 ns |      8.736 ns |     5.778 ns | 0.029 |    0.00 | 0.0076 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     274.61 ns |      3.432 ns |     2.270 ns | 0.017 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |               |              |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  16,261.61 ns |  1,119.975 ns |    61.390 ns | 1.000 |    0.00 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  21,111.08 ns |  1,347.075 ns |    73.838 ns | 1.298 |    0.01 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 162,140.25 ns | 29,373.137 ns | 1,610.041 ns | 9.971 |    0.09 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     174.66 ns |      4.943 ns |     0.271 ns | 0.011 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     288.09 ns |      7.406 ns |     0.406 ns | 0.018 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      29.65 ns |      2.501 ns |     0.137 ns | 0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      29.47 ns |      1.018 ns |     0.056 ns | 0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     443.23 ns |     88.879 ns |     4.872 ns | 0.027 |    0.00 | 0.0081 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     278.01 ns |     17.024 ns |     0.933 ns | 0.017 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
