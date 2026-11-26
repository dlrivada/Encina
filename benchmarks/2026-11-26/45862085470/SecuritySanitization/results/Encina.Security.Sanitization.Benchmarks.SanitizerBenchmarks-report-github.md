```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.70GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  11,599.71 ns |   312.815 ns | 206.908 ns | 1.000 |    0.02 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  14,439.57 ns |   160.724 ns | 106.309 ns | 1.245 |    0.02 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 106,174.33 ns |   336.963 ns | 200.522 ns | 9.156 |    0.16 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     151.78 ns |     4.314 ns |   2.854 ns | 0.013 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     225.43 ns |     0.839 ns |   0.499 ns | 0.019 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      25.06 ns |     0.087 ns |   0.052 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      24.66 ns |     0.069 ns |   0.046 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     307.23 ns |     1.128 ns |   0.746 ns | 0.026 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     246.78 ns |     5.177 ns |   3.424 ns | 0.021 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |            |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  11,475.68 ns |   182.657 ns |  10.012 ns | 1.000 |    0.00 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  14,550.37 ns |   680.981 ns |  37.327 ns | 1.268 |    0.00 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 105,660.18 ns | 2,508.875 ns | 137.520 ns | 9.207 |    0.01 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     139.68 ns |     3.301 ns |   0.181 ns | 0.012 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     223.03 ns |     2.894 ns |   0.159 ns | 0.019 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      23.96 ns |     1.869 ns |   0.102 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      24.15 ns |     0.428 ns |   0.023 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     307.39 ns |     9.582 ns |   0.525 ns | 0.027 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     263.29 ns |    82.785 ns |   4.538 ns | 0.023 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
