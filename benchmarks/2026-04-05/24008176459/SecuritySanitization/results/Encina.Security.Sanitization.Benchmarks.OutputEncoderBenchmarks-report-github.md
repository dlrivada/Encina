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
| EncodeForHtml_SafeText           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        10.46 ns |  0.023 ns | 0.015 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForHtml_SpecialChars       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       149.64 ns |  0.954 ns | 0.631 ns | 14.31 |    0.06 | 0.0095 |     160 B |          NA |
| EncodeForJavaScript_SafeText     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        10.47 ns |  0.042 ns | 0.028 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForJavaScript_SpecialChars | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       127.71 ns |  1.248 ns | 0.743 ns | 12.21 |    0.07 | 0.0086 |     144 B |          NA |
| EncodeForUrl_SafeText            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       140.57 ns |  1.213 ns | 0.802 ns | 13.44 |    0.08 | 0.0091 |     152 B |          NA |
| EncodeForUrl_SpecialChars        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       121.40 ns |  0.860 ns | 0.512 ns | 11.61 |    0.05 | 0.0076 |     128 B |          NA |
| EncodeForCss_SafeText            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       333.59 ns | 10.372 ns | 6.172 ns | 31.91 |    0.56 | 0.0749 |    1256 B |          NA |
| EncodeForCss_SpecialChars        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       370.17 ns | 10.542 ns | 6.973 ns | 35.40 |    0.64 | 0.0710 |    1192 B |          NA |
|                                  |            |                |             |             |              |             |                 |           |          |       |         |        |           |             |
| EncodeForHtml_SafeText           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,111,714.00 ns |        NA | 0.000 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForHtml_SpecialChars       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,215,578.00 ns |        NA | 0.000 ns |  1.03 |    0.00 |      - |     160 B |          NA |
| EncodeForJavaScript_SafeText     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,423,375.00 ns |        NA | 0.000 ns |  1.08 |    0.00 |      - |         - |          NA |
| EncodeForJavaScript_SpecialChars | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,349,308.00 ns |        NA | 0.000 ns |  1.06 |    0.00 |      - |     144 B |          NA |
| EncodeForUrl_SafeText            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,161,918.00 ns |        NA | 0.000 ns |  1.01 |    0.00 |      - |     152 B |          NA |
| EncodeForUrl_SpecialChars        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,063,250.00 ns |        NA | 0.000 ns |  0.99 |    0.00 |      - |     128 B |          NA |
| EncodeForCss_SafeText            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,107,737.00 ns |        NA | 0.000 ns |  0.27 |    0.00 |      - |    1256 B |          NA |
| EncodeForCss_SpecialChars        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,066,910.00 ns |        NA | 0.000 ns |  0.26 |    0.00 |      - |    1192 B |          NA |
