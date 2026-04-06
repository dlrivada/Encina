```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  ShortRun   : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.424 μs | 0.0092 μs | 0.0061 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  4.239 μs | 0.0066 μs | 0.0043 μs |  1.24 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 44.932 μs | 0.0721 μs | 0.0377 μs | 13.12 |    0.02 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.539 μs | 0.0077 μs | 0.0046 μs |  1.33 |    0.00 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.397 μs | 0.0053 μs | 0.0032 μs |  1.28 |    0.00 | 0.1144 |   1.88 KB |        1.07 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  3.893 μs | 0.0091 μs | 0.0054 μs |  1.14 |    0.00 | 0.1068 |   1.81 KB |        1.03 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  7.592 μs | 0.0248 μs | 0.0164 μs |  2.22 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.659 μs | 0.1037 μs | 0.0057 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  4.314 μs | 0.1536 μs | 0.0084 μs |  1.18 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 45.097 μs | 1.7210 μs | 0.0943 μs | 12.32 |    0.03 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.391 μs | 0.2489 μs | 0.0136 μs |  1.20 |    0.00 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.510 μs | 0.4588 μs | 0.0251 μs |  1.23 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  4.002 μs | 0.1902 μs | 0.0104 μs |  1.09 |    0.00 | 0.1068 |   1.81 KB |        1.03 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  7.235 μs | 0.7366 μs | 0.0404 μs |  1.98 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
