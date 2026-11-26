```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 9V45 4.29GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean         | Error         | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |-------------:|--------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  7,574.48 ns |    207.566 ns |   137.292 ns | 1.000 |    0.02 | 0.8621 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  9,601.93 ns |    345.864 ns |   228.768 ns | 1.268 |    0.04 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 70,475.04 ns |  1,628.973 ns | 1,077.464 ns | 9.307 |    0.21 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |    120.02 ns |      4.417 ns |     2.922 ns | 0.016 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |    189.89 ns |      8.375 ns |     5.540 ns | 0.025 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |     16.05 ns |      0.286 ns |     0.150 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |     15.86 ns |      0.486 ns |     0.289 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |    238.90 ns |      3.652 ns |     2.415 ns | 0.032 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |    158.59 ns |      5.589 ns |     3.697 ns | 0.021 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
|                                   |            |                |             |              |               |              |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  8,450.07 ns |    643.222 ns |    35.257 ns | 1.000 |    0.01 | 0.8545 | 0.0305 |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           | 10,609.95 ns |    252.930 ns |    13.864 ns | 1.256 |    0.00 | 0.8240 | 0.0153 |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 80,876.97 ns | 70,819.978 ns | 3,881.882 ns | 9.571 |    0.40 | 4.5166 | 0.3662 |   76680 B |       5.313 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |    116.81 ns |     17.364 ns |     0.952 ns | 0.014 |    0.00 | 0.0114 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |    198.31 ns |     60.277 ns |     3.304 ns | 0.023 |    0.00 | 0.0148 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |     15.65 ns |      4.964 ns |     0.272 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |     15.91 ns |      3.122 ns |     0.171 ns | 0.002 |    0.00 | 0.0029 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |    230.10 ns |     30.214 ns |     1.656 ns | 0.027 |    0.00 | 0.0124 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |    154.65 ns |     83.398 ns |     4.571 ns | 0.018 |    0.00 | 0.0386 |      - |     648 B |       0.045 |
