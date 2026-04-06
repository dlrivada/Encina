```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean              | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |------------------:|-----------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      12,378.94 ns |  58.986 ns |  35.102 ns |  1.000 |    0.00 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      18,648.95 ns |  65.309 ns |  34.158 ns |  1.507 |    0.00 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     135,017.92 ns | 804.484 ns | 532.116 ns | 10.907 |    0.05 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         250.07 ns |   0.788 ns |   0.469 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         324.17 ns |   1.118 ns |   0.740 ns |  0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          27.35 ns |   0.201 ns |   0.120 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          27.43 ns |   0.219 ns |   0.130 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         434.61 ns |   3.684 ns |   2.437 ns |  0.035 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         260.22 ns |   2.336 ns |   1.390 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |              |             |                   |            |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 492,137,058.00 ns |         NA |   0.000 ns |  1.000 |    0.00 |      - |      - |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 520,363,904.00 ns |         NA |   0.000 ns |  1.057 |    0.00 |      - |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 608,044,449.00 ns |         NA |   0.000 ns |  1.236 |    0.00 |      - |      - |  165056 B |      13.294 |
| SanitizeForSql_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  10,954,393.00 ns |         NA |   0.000 ns |  0.022 |    0.00 |      - |      - |     256 B |       0.021 |
| SanitizeForSql_InjectionAttempt   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  10,922,592.00 ns |         NA |   0.000 ns |  0.022 |    0.00 |      - |      - |     408 B |       0.033 |
| SanitizeForShell_SimpleInput      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     701,092.00 ns |         NA |   0.000 ns |  0.001 |    0.00 |      - |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     701,222.00 ns |         NA |   0.000 ns |  0.001 |    0.00 |      - |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  22,876,229.00 ns |         NA |   0.000 ns |  0.046 |    0.00 |      - |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   1,068,845.00 ns |         NA |   0.000 ns |  0.002 |    0.00 |      - |      - |     648 B |       0.052 |
