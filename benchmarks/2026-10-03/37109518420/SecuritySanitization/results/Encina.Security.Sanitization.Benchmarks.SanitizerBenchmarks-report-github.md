```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
INTEL XEON PLATINUM 8573C 2.30GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.401
  [Host]     : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.12 (10.0.12, 10.0.1226.42308), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  13,254.19 ns |   713.098 ns |   471.670 ns | 1.001 |    0.05 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  16,176.94 ns |   260.213 ns |   172.115 ns | 1.222 |    0.04 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 122,480.36 ns | 1,875.175 ns | 1,240.312 ns | 9.251 |    0.32 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     143.31 ns |     1.538 ns |     1.018 ns | 0.011 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     234.79 ns |     4.291 ns |     2.554 ns | 0.018 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      25.81 ns |     0.885 ns |     0.585 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      25.97 ns |     0.450 ns |     0.298 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     347.96 ns |     2.578 ns |     1.534 ns | 0.026 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     243.26 ns |     3.757 ns |     2.485 ns | 0.018 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |              |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  12,042.96 ns | 2,527.406 ns |   138.536 ns | 1.000 |    0.01 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  15,829.10 ns | 1,783.775 ns |    97.775 ns | 1.315 |    0.01 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 119,165.00 ns | 9,818.143 ns |   538.166 ns | 9.896 |    0.11 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     144.91 ns |    23.931 ns |     1.312 ns | 0.012 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     237.50 ns |    42.968 ns |     2.355 ns | 0.020 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      26.65 ns |     2.908 ns |     0.159 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      26.87 ns |     3.096 ns |     0.170 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     445.21 ns |   191.374 ns |    10.490 ns | 0.037 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     248.21 ns |    41.546 ns |     2.277 ns | 0.021 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
