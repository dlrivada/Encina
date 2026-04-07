```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 2.79GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v4


```
| Method                            | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean              | Error      | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|---------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |------------------:|-----------:|----------:|------:|--------:|-------:|----------:|------------:|
| SanitizeHtml_CleanInput           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      12,761.62 ns |  43.314 ns | 25.775 ns | 1.000 |    0.00 | 0.4883 |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |      18,499.22 ns | 103.245 ns | 68.290 ns | 1.450 |    0.01 | 0.5188 |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |     126,298.53 ns | 142.223 ns | 94.071 ns | 9.897 |    0.02 | 2.4414 |   66732 B |       5.375 |
| SanitizeForSql_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         237.27 ns |   0.175 ns |  0.092 ns | 0.019 |    0.00 | 0.0019 |      48 B |       0.004 |
| SanitizeForSql_InjectionAttempt   | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         313.09 ns |   0.788 ns |  0.521 ns | 0.025 |    0.00 | 0.0076 |     200 B |       0.016 |
| SanitizeForShell_SimpleInput      | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          28.86 ns |   0.301 ns |  0.199 ns | 0.002 |    0.00 | 0.0019 |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |          27.77 ns |   0.270 ns |  0.178 ns | 0.002 |    0.00 | 0.0019 |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         521.80 ns |   2.036 ns |  1.065 ns | 0.041 |    0.00 | 0.0076 |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |         259.31 ns |   8.720 ns |  5.768 ns | 0.020 |    0.00 | 0.0257 |     648 B |       0.052 |
|                                   |            |                |             |             |              |             |                   |            |           |       |         |        |           |             |
| SanitizeHtml_CleanInput           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 453,290,924.00 ns |         NA |  0.000 ns | 1.000 |    0.00 |      - |   12416 B |       1.000 |
| SanitizeHtml_MaliciousInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 481,083,832.00 ns |         NA |  0.000 ns | 1.061 |    0.00 |      - |   13080 B |       1.053 |
| SanitizeHtml_ComplexDocument      | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 562,503,263.00 ns |         NA |  0.000 ns | 1.241 |    0.00 |      - |   66704 B |       5.372 |
| SanitizeForSql_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  10,553,196.00 ns |         NA |  0.000 ns | 0.023 |    0.00 |      - |     256 B |       0.021 |
| SanitizeForSql_InjectionAttempt   | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  10,531,484.00 ns |         NA |  0.000 ns | 0.023 |    0.00 |      - |     408 B |       0.033 |
| SanitizeForShell_SimpleInput      | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     640,518.00 ns |         NA |  0.000 ns | 0.001 |    0.00 |      - |      48 B |       0.004 |
| SanitizeForShell_InjectionAttempt | Dry        | 1              | 1           | ColdStart   | 1            | 1           |     652,477.00 ns |         NA |  0.000 ns | 0.001 |    0.00 |      - |      48 B |       0.004 |
| SanitizeForJson_SimpleInput       | Dry        | 1              | 1           | ColdStart   | 1            | 1           |  20,961,449.00 ns |         NA |  0.000 ns | 0.046 |    0.00 |      - |     208 B |       0.017 |
| SanitizeForXml_SimpleInput        | Dry        | 1              | 1           | ColdStart   | 1            | 1           |   1,035,682.00 ns |         NA |  0.000 ns | 0.002 |    0.00 |      - |     648 B |       0.052 |
