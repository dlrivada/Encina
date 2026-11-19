```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon 6973P-C 4.18GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  2.328 μs | 0.0293 μs | 0.0175 μs |  1.26 |    0.01 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.714 μs | 0.2731 μs | 0.1806 μs |  1.47 |    0.09 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.550 μs | 0.0246 μs | 0.0129 μs |  1.38 |    0.01 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 34.371 μs | 0.1108 μs | 0.0659 μs | 18.62 |    0.07 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  1.846 μs | 0.0115 μs | 0.0069 μs |  1.00 |    0.00 | 0.0210 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  4.161 μs | 0.4662 μs | 0.3084 μs |  2.25 |    0.16 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  2.107 μs | 0.0125 μs | 0.0075 μs |  1.14 |    0.01 | 0.0191 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  2.319 μs | 0.1214 μs | 0.0067 μs |  1.23 |    0.02 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  2.964 μs | 0.1761 μs | 0.0097 μs |  1.57 |    0.03 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  2.558 μs | 0.6608 μs | 0.0362 μs |  1.35 |    0.03 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 34.539 μs | 3.5585 μs | 0.1951 μs | 18.28 |    0.37 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  1.890 μs | 0.8034 μs | 0.0440 μs |  1.00 |    0.03 | 0.0210 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  4.021 μs | 1.1713 μs | 0.0642 μs |  2.13 |    0.05 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  2.138 μs | 0.4682 μs | 0.0257 μs |  1.13 |    0.03 | 0.0191 |   1.81 KB |        1.03 |
