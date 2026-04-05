```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.62GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean              | Error      | StdDev     | Ratio  | RatioSD | Gen0   | Gen1   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |------------------:|-----------:|-----------:|-------:|--------:|-------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      12,761.31 ns | 166.767 ns | 110.306 ns |  1.000 |    0.01 | 0.7324 | 0.0153 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      19,204.86 ns |  67.981 ns |  44.965 ns |  1.505 |    0.01 | 0.7629 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     136,823.12 ns | 703.154 ns | 465.093 ns | 10.722 |    0.10 | 3.9063 | 0.2441 |   66724 B |       5.374 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         255.08 ns |   1.565 ns |   0.931 ns |  0.020 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         339.84 ns |   0.558 ns |   0.332 ns |  0.027 |    0.00 | 0.0119 |      - |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          28.53 ns |   0.182 ns |   0.121 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          28.86 ns |   0.409 ns |   0.271 ns |  0.002 |    0.00 | 0.0029 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         454.72 ns |   1.237 ns |   0.818 ns |  0.036 |    0.00 | 0.0124 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         278.77 ns |   2.455 ns |   1.624 ns |  0.022 |    0.00 | 0.0386 |      - |     648 B |       0.052 |
|                                   |            |                |             |             |              |             |                   |            |            |        |         |        |        |           |             |
| SanitizeHtml_CleanInput           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 502,266,412.00 ns |         NA |   0.000 ns |  1.000 |    0.00 |      - |      - |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 528,242,120.00 ns |         NA |   0.000 ns |  1.052 |    0.00 |      - |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 620,605,258.00 ns |         NA |   0.000 ns |  1.236 |    0.00 |      - |      - |  165056 B |      13.294 |
| SanitizeForSql_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  11,046,794.00 ns |         NA |   0.000 ns |  0.022 |    0.00 |      - |      - |     256 B |       0.021 |
| SanitizeForSql_InjectionAttempt   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  11,007,771.00 ns |         NA |   0.000 ns |  0.022 |    0.00 |      - |      - |     408 B |       0.033 |
| SanitizeForShell_SimpleInput      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     698,694.00 ns |         NA |   0.000 ns |  0.001 |    0.00 |      - |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     726,667.00 ns |         NA |   0.000 ns |  0.001 |    0.00 |      - |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  23,291,955.00 ns |         NA |   0.000 ns |  0.046 |    0.00 |      - |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   1,127,084.00 ns |         NA |   0.000 ns |  0.002 |    0.00 |      - |      - |     648 B |       0.052 |
