```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.46GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  16,416.21 ns |    84.817 ns |  56.101 ns | 1.000 |    0.00 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  21,713.53 ns |    69.148 ns |  41.149 ns | 1.323 |    0.00 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 163,083.42 ns |   598.517 ns | 356.168 ns | 9.934 |    0.04 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     174.65 ns |     0.499 ns |   0.330 ns | 0.011 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     291.72 ns |     0.700 ns |   0.366 ns | 0.018 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      30.68 ns |     0.087 ns |   0.058 ns | 0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      30.18 ns |     0.220 ns |   0.115 ns | 0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     449.09 ns |     2.726 ns |   1.803 ns | 0.027 |    0.00 | 0.0081 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     290.00 ns |     1.822 ns |   1.085 ns | 0.018 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |            |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  16,514.22 ns | 1,243.982 ns |  68.187 ns | 1.000 |    0.01 | 0.5493 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  22,052.16 ns | 1,314.230 ns |  72.037 ns | 1.335 |    0.01 | 0.5493 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 163,256.16 ns | 7,954.110 ns | 435.992 ns | 9.886 |    0.04 | 2.9297 | 0.2441 |   76686 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     180.08 ns |     2.677 ns |   0.147 ns | 0.011 |    0.00 | 0.0076 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     293.97 ns |    11.433 ns |   0.627 ns | 0.018 |    0.00 | 0.0095 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      30.34 ns |     1.903 ns |   0.104 ns | 0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      30.98 ns |     5.218 ns |   0.286 ns | 0.002 |    0.00 | 0.0019 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     439.07 ns |    19.667 ns |   1.078 ns | 0.027 |    0.00 | 0.0081 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     292.30 ns |    25.720 ns |   1.410 ns | 0.018 |    0.00 | 0.0257 |      - |     648 B |       0.045 |
