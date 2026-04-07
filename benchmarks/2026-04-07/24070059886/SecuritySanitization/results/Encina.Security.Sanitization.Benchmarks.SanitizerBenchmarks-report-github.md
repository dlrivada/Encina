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
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      12,711.18 ns |   120.725 ns |  79.852 ns |  1.000 |    0.01 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      18,597.70 ns |   373.296 ns | 246.912 ns |  1.463 |    0.02 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     138,185.74 ns | 1,347.013 ns | 801.586 ns | 10.872 |    0.09 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         247.11 ns |     9.266 ns |   5.514 ns |  0.019 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         333.75 ns |     0.967 ns |   0.506 ns |  0.026 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          28.86 ns |     0.245 ns |   0.128 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          28.39 ns |     0.646 ns |   0.427 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         444.21 ns |     1.621 ns |   1.072 ns |  0.035 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         269.42 ns |     2.578 ns |   1.706 ns |  0.021 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |              |             |                   |              |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 487,587,908.00 ns |           NA |   0.000 ns |  1.000 |    0.00 |      - |      - |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 523,755,139.00 ns |           NA |   0.000 ns |  1.074 |    0.00 |      - |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 606,711,031.00 ns |           NA |   0.000 ns |  1.244 |    0.00 |      - |      - |   66704 B |       5.372 |
| SanitizeForSql_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  11,003,621.00 ns |           NA |   0.000 ns |  0.023 |    0.00 |      - |      - |     256 B |       0.021 |
| SanitizeForSql_InjectionAttempt   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  10,895,845.00 ns |           NA |   0.000 ns |  0.022 |    0.00 |      - |      - |     408 B |       0.033 |
| SanitizeForShell_SimpleInput      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     724,550.00 ns |           NA |   0.000 ns |  0.001 |    0.00 |      - |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     711,716.00 ns |           NA |   0.000 ns |  0.001 |    0.00 |      - |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  23,072,907.00 ns |           NA |   0.000 ns |  0.047 |    0.00 |      - |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   1,063,973.00 ns |           NA |   0.000 ns |  0.002 |    0.00 |      - |      - |     648 B |       0.052 |
