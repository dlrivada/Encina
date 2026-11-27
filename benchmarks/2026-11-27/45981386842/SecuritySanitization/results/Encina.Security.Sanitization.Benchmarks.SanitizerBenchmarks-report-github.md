```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 2.82GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  11,364.82 ns |    148.989 ns |  98.547 ns | 1.000 |    0.01 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  14,408.79 ns |     83.819 ns |  55.441 ns | 1.268 |    0.01 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 105,585.03 ns |    582.217 ns | 385.100 ns | 9.291 |    0.08 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     147.74 ns |      1.485 ns |   0.777 ns | 0.013 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     224.45 ns |      0.940 ns |   0.559 ns | 0.020 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      25.04 ns |      0.061 ns |   0.041 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      24.52 ns |      0.094 ns |   0.062 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     306.42 ns |      0.630 ns |   0.375 ns | 0.027 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     244.79 ns |     14.200 ns |   9.393 ns | 0.022 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |               |            |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  11,466.66 ns |  1,127.606 ns |  61.808 ns | 1.000 |    0.01 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  14,526.28 ns |    990.497 ns |  54.293 ns | 1.267 |    0.01 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 105,976.03 ns | 16,039.027 ns | 879.153 ns | 9.242 |    0.08 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     139.72 ns |      1.756 ns |   0.096 ns | 0.012 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     224.66 ns |      4.685 ns |   0.257 ns | 0.020 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      24.67 ns |      0.241 ns |   0.013 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      24.76 ns |      1.512 ns |   0.083 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     305.00 ns |     23.123 ns |   1.267 ns | 0.027 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     249.48 ns |     54.715 ns |   2.999 ns | 0.022 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
