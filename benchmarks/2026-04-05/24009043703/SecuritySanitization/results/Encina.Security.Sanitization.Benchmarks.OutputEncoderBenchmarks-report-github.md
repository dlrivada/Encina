```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Dry        : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                           | Job        | IterationCount | LaunchCount | RunStrategy | UnrollFactor | WarmupCount | Mean            | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------------- |----------- |--------------- |------------ |------------ |------------- |------------ |----------------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| EncodeForHtml_SafeText           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        10.47 ns | 0.035 ns | 0.023 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForHtml_SpecialChars       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       145.05 ns | 1.091 ns | 0.721 ns | 13.85 |    0.07 | 0.0095 |     160 B |          NA |
| EncodeForJavaScript_SafeText     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        10.45 ns | 0.007 ns | 0.004 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForJavaScript_SpecialChars | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       124.53 ns | 0.656 ns | 0.434 ns | 11.89 |    0.05 | 0.0086 |     144 B |          NA |
| EncodeForUrl_SafeText            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       137.23 ns | 0.295 ns | 0.195 ns | 13.10 |    0.03 | 0.0091 |     152 B |          NA |
| EncodeForUrl_SpecialChars        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       119.33 ns | 0.378 ns | 0.250 ns | 11.39 |    0.03 | 0.0076 |     128 B |          NA |
| EncodeForCss_SafeText            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       321.73 ns | 2.838 ns | 1.877 ns | 30.72 |    0.18 | 0.0749 |    1256 B |          NA |
| EncodeForCss_SpecialChars        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       354.45 ns | 2.186 ns | 1.446 ns | 33.84 |    0.15 | 0.0710 |    1192 B |          NA |
|                                  |            |                |             |             |              |             |                 |          |          |       |         |        |           |             |
| EncodeForHtml_SafeText           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,902,798.00 ns |       NA | 0.000 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForHtml_SpecialChars       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,977,016.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |      - |     160 B |          NA |
| EncodeForJavaScript_SafeText     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,011,792.00 ns |       NA | 0.000 ns |  1.03 |    0.00 |      - |         - |          NA |
| EncodeForJavaScript_SpecialChars | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,198,939.00 ns |       NA | 0.000 ns |  1.08 |    0.00 |      - |     144 B |          NA |
| EncodeForUrl_SafeText            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,978,890.00 ns |       NA | 0.000 ns |  1.02 |    0.00 |      - |     152 B |          NA |
| EncodeForUrl_SpecialChars        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,340,694.00 ns |       NA | 0.000 ns |  1.11 |    0.00 |      - |     128 B |          NA |
| EncodeForCss_SafeText            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,038,294.00 ns |       NA | 0.000 ns |  0.27 |    0.00 |      - |    1256 B |          NA |
| EncodeForCss_SpecialChars        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,421,848.00 ns |       NA | 0.000 ns |  0.36 |    0.00 |      - |    1192 B |          NA |
