```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  12,296.17 ns |     55.371 ns |  32.951 ns |  1.000 |    0.00 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  18,278.72 ns |     74.319 ns |  44.226 ns |  1.487 |    0.01 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 132,877.81 ns |    338.911 ns | 177.257 ns | 10.807 |    0.03 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     242.01 ns |      0.489 ns |   0.256 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     324.71 ns |      0.629 ns |   0.329 ns |  0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      27.18 ns |      0.048 ns |   0.029 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      27.16 ns |      0.094 ns |   0.056 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     446.56 ns |      1.976 ns |   1.176 ns |  0.036 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     258.37 ns |      1.068 ns |   0.636 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |               |               |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  12,310.39 ns |    511.805 ns |  28.054 ns |  1.000 |    0.00 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  18,438.18 ns |    763.810 ns |  41.867 ns |  1.498 |    0.00 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 132,740.04 ns | 13,081.882 ns | 717.062 ns | 10.783 |    0.05 | 3.9063 | 0.2441 |   66727 B |       5.374 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     254.06 ns |     23.786 ns |   1.304 ns |  0.021 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     337.45 ns |     12.652 ns |   0.694 ns |  0.027 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      27.30 ns |      0.893 ns |   0.049 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      27.44 ns |      2.117 ns |   0.116 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     457.41 ns |      3.046 ns |   0.167 ns |  0.037 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     260.50 ns |     15.773 ns |   0.865 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
