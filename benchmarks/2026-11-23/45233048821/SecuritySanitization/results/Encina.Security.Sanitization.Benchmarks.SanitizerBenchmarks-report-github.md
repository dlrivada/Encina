```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev     | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-----------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  12,195.11 ns |     92.401 ns |  61.118 ns | 1.000 |    0.01 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  15,431.07 ns |    143.494 ns |  94.912 ns | 1.265 |    0.01 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 115,722.27 ns |    732.952 ns | 484.802 ns | 9.489 |    0.06 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     141.38 ns |      1.654 ns |   1.094 ns | 0.012 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     225.42 ns |      1.873 ns |   1.115 ns | 0.018 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      26.90 ns |      0.339 ns |   0.224 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      26.87 ns |      0.279 ns |   0.185 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     340.73 ns |      1.651 ns |   1.092 ns | 0.028 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     231.43 ns |      1.294 ns |   0.856 ns | 0.019 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |               |            |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  11,873.53 ns |  1,395.705 ns |  76.503 ns | 1.000 |    0.01 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  15,819.01 ns | 10,318.753 ns | 565.606 ns | 1.332 |    0.04 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 114,926.32 ns | 17,542.330 ns | 961.554 ns | 9.679 |    0.09 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     141.00 ns |     21.519 ns |   1.180 ns | 0.012 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     229.36 ns |     52.252 ns |   2.864 ns | 0.019 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      26.57 ns |      3.428 ns |   0.188 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      26.53 ns |      2.413 ns |   0.132 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     339.82 ns |     24.439 ns |   1.340 ns | 0.029 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     233.75 ns |      2.628 ns |   0.144 ns | 0.020 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
