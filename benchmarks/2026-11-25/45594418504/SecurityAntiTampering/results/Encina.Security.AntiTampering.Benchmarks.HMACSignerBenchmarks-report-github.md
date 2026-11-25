```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
INTEL XEON PLATINUM 8573C 3.58GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  2.872 μs | 0.0242 μs | 0.0160 μs |  1.24 |    0.01 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.138 μs | 0.0184 μs | 0.0122 μs |  1.35 |    0.01 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.112 μs | 0.0216 μs | 0.0129 μs |  1.34 |    0.01 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 40.286 μs | 0.1502 μs | 0.0994 μs | 17.39 |    0.08 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  2.317 μs | 0.0136 μs | 0.0090 μs |  1.00 |    0.01 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  4.955 μs | 0.0318 μs | 0.0189 μs |  2.14 |    0.01 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  2.624 μs | 0.0189 μs | 0.0125 μs |  1.13 |    0.01 | 0.0191 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  2.834 μs | 0.2693 μs | 0.0148 μs |  1.23 |    0.01 | 0.0191 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  3.163 μs | 0.2431 μs | 0.0133 μs |  1.37 |    0.01 | 0.0191 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  3.140 μs | 0.2688 μs | 0.0147 μs |  1.36 |    0.01 | 0.0229 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 40.250 μs | 0.8729 μs | 0.0478 μs | 17.47 |    0.09 |      - |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  2.304 μs | 0.2485 μs | 0.0136 μs |  1.00 |    0.01 | 0.0191 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  4.949 μs | 0.2550 μs | 0.0140 μs |  2.15 |    0.01 | 0.0381 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  2.679 μs | 0.1820 μs | 0.0100 μs |  1.16 |    0.01 | 0.0191 |   1.81 KB |        1.03 |
