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
| EncodeForHtml_SafeText           | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        10.45 ns |  0.010 ns | 0.005 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForHtml_SpecialChars       | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       146.65 ns |  2.347 ns | 1.397 ns | 14.04 |    0.13 | 0.0095 |     160 B |          NA |
| EncodeForJavaScript_SafeText     | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |        10.45 ns |  0.012 ns | 0.008 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForJavaScript_SpecialChars | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       127.88 ns |  2.916 ns | 1.929 ns | 12.24 |    0.18 | 0.0086 |     144 B |          NA |
| EncodeForUrl_SafeText            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       137.53 ns |  0.471 ns | 0.280 ns | 13.17 |    0.03 | 0.0091 |     152 B |          NA |
| EncodeForUrl_SpecialChars        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       120.72 ns |  2.063 ns | 1.228 ns | 11.56 |    0.11 | 0.0076 |     128 B |          NA |
| EncodeForCss_SafeText            | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       336.59 ns | 13.542 ns | 8.059 ns | 32.22 |    0.73 | 0.0749 |    1256 B |          NA |
| EncodeForCss_SpecialChars        | Job-YFEFPZ | 10             | Default     | Default     | 16           | 3           |       362.70 ns |  5.712 ns | 3.778 ns | 34.72 |    0.35 | 0.0710 |    1192 B |          NA |
|                                  |            |                |             |             |              |             |                 |           |          |       |         |        |           |             |
| EncodeForHtml_SafeText           | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,003,256.00 ns |        NA | 0.000 ns |  1.00 |    0.00 |      - |         - |          NA |
| EncodeForHtml_SpecialChars       | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,065,824.00 ns |        NA | 0.000 ns |  1.02 |    0.00 |      - |     160 B |          NA |
| EncodeForJavaScript_SafeText     | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,263,866.00 ns |        NA | 0.000 ns |  1.07 |    0.00 |      - |         - |          NA |
| EncodeForJavaScript_SpecialChars | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 4,192,643.00 ns |        NA | 0.000 ns |  1.05 |    0.00 |      - |     144 B |          NA |
| EncodeForUrl_SafeText            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,983,168.00 ns |        NA | 0.000 ns |  0.99 |    0.00 |      - |     152 B |          NA |
| EncodeForUrl_SpecialChars        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 3,956,307.00 ns |        NA | 0.000 ns |  0.99 |    0.00 |      - |     128 B |          NA |
| EncodeForCss_SafeText            | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,080,567.00 ns |        NA | 0.000 ns |  0.27 |    0.00 |      - |    1256 B |          NA |
| EncodeForCss_SpecialChars        | Dry        | 1              | 1           | ColdStart   | 1            | 1           | 1,075,868.00 ns |        NA | 0.000 ns |  0.27 |    0.00 |      - |    1192 B |          NA |
