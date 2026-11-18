```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6975P-C 3.25GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  11,653.48 ns |    74.865 ns |  49.518 ns | 1.000 |    0.01 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  14,632.83 ns |    69.756 ns |  46.139 ns | 1.256 |    0.01 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 105,849.85 ns |   684.691 ns | 407.449 ns | 9.083 |    0.05 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     148.19 ns |     0.691 ns |   0.457 ns | 0.013 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     235.48 ns |    11.210 ns |   7.415 ns | 0.020 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      25.04 ns |     0.142 ns |   0.094 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      24.83 ns |     0.295 ns |   0.195 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     306.37 ns |     1.325 ns |   0.788 ns | 0.026 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     233.25 ns |     8.638 ns |   5.713 ns | 0.020 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |            |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  11,382.10 ns | 1,869.626 ns | 102.480 ns | 1.000 |    0.01 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  14,557.70 ns |   359.923 ns |  19.729 ns | 1.279 |    0.01 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 105,656.57 ns | 9,289.916 ns | 509.212 ns | 9.283 |    0.08 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     138.08 ns |    17.360 ns |   0.952 ns | 0.012 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     223.56 ns |    15.083 ns |   0.827 ns | 0.020 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      24.20 ns |     0.882 ns |   0.048 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      24.75 ns |     4.188 ns |   0.230 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     303.59 ns |    15.996 ns |   0.877 ns | 0.027 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     241.37 ns |   133.722 ns |   7.330 ns | 0.021 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
