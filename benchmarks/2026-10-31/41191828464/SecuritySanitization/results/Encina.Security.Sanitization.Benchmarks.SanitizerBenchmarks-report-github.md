```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
Intel Xeon 6973P-C 3.94GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                            | Job        | IterationCount | LaunchCount | Mean          | Error         | StdDev       | Ratio | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |--------------:|--------------:|-------------:|------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     |  12,606.65 ns |    461.532 ns |   305.275 ns | 1.001 |    0.03 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     |  14,994.36 ns |    621.407 ns |   369.790 ns | 1.190 |    0.04 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | 105,969.90 ns |  2,738.802 ns | 1,811.548 ns | 8.410 |    0.24 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     |     144.67 ns |      5.100 ns |     3.373 ns | 0.011 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     |     231.43 ns |      5.192 ns |     3.434 ns | 0.018 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     |      24.84 ns |      0.348 ns |     0.230 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     |      25.32 ns |      1.620 ns |     1.071 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     |     311.36 ns |     12.906 ns |     8.537 ns | 0.025 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     |     272.76 ns |     10.351 ns |     6.846 ns | 0.022 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
|                                   |            |                |             |               |               |              |       |         |        |        |           |             |
| SanitizeHtml_CleanInput           | ShortRun   | 3              | 1           |  12,162.81 ns |  3,442.777 ns |   188.710 ns | 1.000 |    0.02 | 0.1678 |      - |   14432 B |       1.000 |
| SanitizeHtml_MaliciousInput       | ShortRun   | 3              | 1           |  15,457.14 ns |  6,678.230 ns |   366.056 ns | 1.271 |    0.03 | 0.1526 |      - |   13880 B |       0.962 |
| SanitizeHtml_ComplexDocument      | ShortRun   | 3              | 1           | 106,453.36 ns | 27,166.681 ns | 1,489.097 ns | 8.754 |    0.16 | 0.8545 | 0.1221 |   76691 B |       5.314 |
| SanitizeForSql_SimpleInput        | ShortRun   | 3              | 1           |     139.16 ns |     52.219 ns |     2.862 ns | 0.011 |    0.00 | 0.0021 |      - |     192 B |       0.013 |
| SanitizeForSql_InjectionAttempt   | ShortRun   | 3              | 1           |     254.60 ns |     68.800 ns |     3.771 ns | 0.021 |    0.00 | 0.0029 |      - |     248 B |       0.017 |
| SanitizeForShell_SimpleInput      | ShortRun   | 3              | 1           |      24.53 ns |     16.641 ns |     0.912 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForShell_InjectionAttempt | ShortRun   | 3              | 1           |      25.28 ns |     25.665 ns |     1.407 ns | 0.002 |    0.00 | 0.0006 |      - |      48 B |       0.003 |
| SanitizeForJson_SimpleInput       | ShortRun   | 3              | 1           |     308.79 ns |    215.258 ns |    11.799 ns | 0.025 |    0.00 | 0.0024 |      - |     208 B |       0.014 |
| SanitizeForXml_SimpleInput        | ShortRun   | 3              | 1           |     275.68 ns |     71.412 ns |     3.914 ns | 0.023 |    0.00 | 0.0076 |      - |     648 B |       0.045 |
