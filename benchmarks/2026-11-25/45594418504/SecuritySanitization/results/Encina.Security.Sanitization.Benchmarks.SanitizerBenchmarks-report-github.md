```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.60GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error        | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|-------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  12,477.08 ns |   364.248 ns |   240.928 ns | 1.000 |    0.03 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  15,846.51 ns |   177.253 ns |   117.242 ns | 1.270 |    0.03 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 116,534.34 ns | 1,954.260 ns | 1,162.949 ns | 9.343 |    0.19 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     144.76 ns |     4.075 ns |     2.425 ns | 0.012 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     232.83 ns |     2.828 ns |     1.870 ns | 0.019 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      27.19 ns |     0.653 ns |     0.432 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      27.67 ns |     0.543 ns |     0.359 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     343.22 ns |     1.638 ns |     1.083 ns | 0.028 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     239.06 ns |     4.201 ns |     2.500 ns | 0.019 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |              |              |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  12,363.73 ns | 1,725.890 ns |    94.602 ns | 1.000 |    0.01 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  16,074.57 ns | 3,427.135 ns |   187.853 ns | 1.300 |    0.02 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 117,113.14 ns | 8,730.885 ns |   478.569 ns | 9.473 |    0.07 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     147.78 ns |    39.914 ns |     2.188 ns | 0.012 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     234.51 ns |    60.205 ns |     3.300 ns | 0.019 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      27.60 ns |     5.398 ns |     0.296 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      28.73 ns |     6.777 ns |     0.371 ns | 0.002 |    0.00 | 0.0005 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     347.46 ns |    81.480 ns |     4.466 ns | 0.028 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     258.82 ns |    38.710 ns |     2.122 ns | 0.021 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
