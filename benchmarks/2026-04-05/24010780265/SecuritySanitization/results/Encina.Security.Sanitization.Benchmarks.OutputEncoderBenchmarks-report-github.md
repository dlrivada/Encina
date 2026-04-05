```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                           | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error     | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|----------:|---------:|------:|--------:|-------:|----------:|------------:|
| EncodeForHtml_SafeText           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        10.48 ns |  0.056 ns | 0.033 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForHtml_SpecialChars       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       150.05 ns |  0.483 ns | 0.320 ns | 14.32 |    0.05 | 0.0095 |     160 B |          NA |
| EncodeForJavaScript_SafeText     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        10.45 ns |  0.007 ns | 0.004 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForJavaScript_SpecialChars | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       132.24 ns |  0.476 ns | 0.283 ns | 12.62 |    0.05 | 0.0086 |     144 B |          NA |
| EncodeForUrl_SafeText            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       142.33 ns |  0.785 ns | 0.519 ns | 13.59 |    0.06 | 0.0091 |     152 B |          NA |
| EncodeForUrl_SpecialChars        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       124.18 ns |  0.391 ns | 0.259 ns | 11.85 |    0.04 | 0.0076 |     128 B |          NA |
| EncodeForCss_SafeText            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       354.47 ns |  6.870 ns | 4.544 ns | 33.84 |    0.43 | 0.0749 |    1256 B |          NA |
| EncodeForCss_SpecialChars        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       394.12 ns | 11.502 ns | 7.608 ns | 37.62 |    0.70 | 0.0710 |    1192 B |          NA |
|                                  |            |                |             |             |              |             |                 |           |          |       |         |        |           |             |
| EncodeForHtml_SafeText           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,993,525.00 ns |        NA | 0.000 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForHtml_SpecialChars       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,233,144.00 ns |        NA | 0.000 ns |  1.06 |    0.00 |      - |     160 B |          NA |
| EncodeForJavaScript_SafeText     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,242,311.00 ns |        NA | 0.000 ns |  1.06 |    0.00 |      - |         - |          NA |
| EncodeForJavaScript_SpecialChars | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,266,687.00 ns |        NA | 0.000 ns |  1.07 |    0.00 |      - |     144 B |          NA |
| EncodeForUrl_SafeText            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,058,977.00 ns |        NA | 0.000 ns |  1.02 |    0.00 |      - |     152 B |          NA |
| EncodeForUrl_SpecialChars        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,062,985.00 ns |        NA | 0.000 ns |  1.02 |    0.00 |      - |     128 B |          NA |
| EncodeForCss_SafeText            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,142,869.00 ns |        NA | 0.000 ns |  0.29 |    0.00 |      - |    1256 B |          NA |
| EncodeForCss_SpecialChars        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,074,892.00 ns |        NA | 0.000 ns |  0.27 |    0.00 |      - |    1192 B |          NA |
