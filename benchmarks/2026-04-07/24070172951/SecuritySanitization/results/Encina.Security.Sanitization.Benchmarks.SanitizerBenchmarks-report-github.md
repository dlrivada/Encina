```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 9V74 2.85GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  11,007.20 ns |    41.895 ns |  24.931 ns | 1.000 |    0.00 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  15,339.87 ns |   114.575 ns |  75.784 ns | 1.394 |    0.01 | 0.7782 | 0.0153 |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 104,110.23 ns | 1,307.236 ns | 864.656 ns | 9.458 |    0.08 | 3.9063 | 0.2441 |   66725 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     230.61 ns |     0.673 ns |   0.352 ns | 0.021 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     289.98 ns |     1.783 ns |   1.061 ns | 0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      29.17 ns |     0.551 ns |   0.364 ns | 0.003 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      27.98 ns |     1.226 ns |   0.811 ns | 0.003 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     406.78 ns |     1.262 ns |   0.660 ns | 0.037 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     270.30 ns |     5.492 ns |   3.632 ns | 0.025 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |               |              |            |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  11,093.56 ns | 2,863.942 ns | 156.982 ns | 1.000 |    0.02 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  15,903.91 ns | 6,989.496 ns | 383.118 ns | 1.434 |    0.03 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 103,327.36 ns | 5,402.828 ns | 296.147 ns | 9.315 |    0.12 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     230.43 ns |     3.110 ns |   0.170 ns | 0.021 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     289.39 ns |    17.341 ns |   0.951 ns | 0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      27.33 ns |     5.029 ns |   0.276 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      27.85 ns |     4.990 ns |   0.274 ns | 0.003 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     413.18 ns |    16.546 ns |   0.907 ns | 0.037 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     266.80 ns |    55.494 ns |   3.042 ns | 0.024 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
