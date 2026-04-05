```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean              | Error        | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |------------------:|-------------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      12,681.74 ns |   275.159 ns | 182.001 ns |  1.000 |    0.02 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      17,847.48 ns |    63.718 ns |  42.145 ns |  1.408 |    0.02 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     134,092.04 ns | 1,350.729 ns | 893.424 ns | 10.576 |    0.16 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         244.95 ns |     1.263 ns |   0.835 ns |  0.019 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         329.19 ns |     2.894 ns |   1.722 ns |  0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          28.06 ns |     0.450 ns |   0.298 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          28.25 ns |     1.018 ns |   0.673 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         437.61 ns |     2.104 ns |   1.391 ns |  0.035 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         262.60 ns |     3.084 ns |   2.040 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |              |             |                   |              |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 489,718,114.00 ns |           NA |   0.000 ns |  1.000 |    0.00 |      - |      - |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 517,841,769.00 ns |           NA |   0.000 ns |  1.057 |    0.00 |      - |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 603,147,649.00 ns |           NA |   0.000 ns |  1.232 |    0.00 |      - |      - |  165056 B |      13.294 |
| SanitizeForSql_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  10,885,090.00 ns |           NA |   0.000 ns |  0.022 |    0.00 |      - |      - |     256 B |       0.021 |
| SanitizeForSql_InjectionAttempt   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  10,911,199.00 ns |           NA |   0.000 ns |  0.022 |    0.00 |      - |      - |     408 B |       0.033 |
| SanitizeForShell_SimpleInput      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     721,306.00 ns |           NA |   0.000 ns |  0.001 |    0.00 |      - |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     699,265.00 ns |           NA |   0.000 ns |  0.001 |    0.00 |      - |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  22,675,971.00 ns |           NA |   0.000 ns |  0.046 |    0.00 |      - |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   1,065,628.00 ns |           NA |   0.000 ns |  0.002 |    0.00 |      - |      - |     648 B |       0.052 |
