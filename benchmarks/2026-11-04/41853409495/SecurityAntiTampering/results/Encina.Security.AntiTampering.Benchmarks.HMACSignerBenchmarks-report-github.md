```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
Intel Xeon Platinum 8370C CPU 2.80GHz (Max: 0.80GHz), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  ShortRun   : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4

WarmupCount=3  

```
| Method                     | Job        | IterationCount | LaunchCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     |  4.144 μs | 0.0187 μs | 0.0112 μs |  1.20 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.413 μs | 0.0153 μs | 0.0080 μs |  1.28 |    0.00 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     |  4.402 μs | 0.0192 μs | 0.0114 μs |  1.28 |    0.00 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 54.400 μs | 0.1592 μs | 0.0947 μs | 15.81 |    0.03 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     |  3.442 μs | 0.0063 μs | 0.0037 μs |  1.00 |    0.00 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     |  7.296 μs | 0.0626 μs | 0.0414 μs |  2.12 |    0.01 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     |  3.865 μs | 0.0170 μs | 0.0101 μs |  1.12 |    0.00 | 0.0687 |   1.81 KB |        1.03 |
|                            |            |                |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | ShortRun   | 3              | 1           |  4.137 μs | 0.0619 μs | 0.0034 μs |  1.21 |    0.01 | 0.0687 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | ShortRun   | 3              | 1           |  4.387 μs | 0.1582 μs | 0.0087 μs |  1.29 |    0.01 | 0.0687 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | ShortRun   | 3              | 1           |  4.510 μs | 0.2283 μs | 0.0125 μs |  1.32 |    0.01 | 0.0763 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | ShortRun   | 3              | 1           | 54.563 μs | 7.5658 μs | 0.4147 μs | 16.01 |    0.13 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | ShortRun   | 3              | 1           |  3.408 μs | 0.3080 μs | 0.0169 μs |  1.00 |    0.01 | 0.0687 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | ShortRun   | 3              | 1           |  7.167 μs | 0.5418 μs | 0.0297 μs |  2.10 |    0.01 | 0.1373 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | ShortRun   | 3              | 1           |  3.822 μs | 0.2685 μs | 0.0147 μs |  1.12 |    0.01 | 0.0687 |   1.81 KB |        1.03 |
