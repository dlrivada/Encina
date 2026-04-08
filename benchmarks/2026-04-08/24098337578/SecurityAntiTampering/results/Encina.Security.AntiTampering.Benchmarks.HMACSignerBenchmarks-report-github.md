```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | 3           |  4.049 μs | 0.0202 μs | 0.0106 μs |  4.053 μs |  1.12 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.334 μs | 0.0271 μs | 0.0161 μs |  4.333 μs |  1.20 |    0.01 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.407 μs | 0.0248 μs | 0.0148 μs |  4.412 μs |  1.22 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 3           | 45.041 μs | 0.1284 μs | 0.0849 μs | 45.052 μs | 12.50 |    0.09 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  3.604 μs | 0.0410 μs | 0.0271 μs |  3.601 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | 3           |  7.466 μs | 0.0310 μs | 0.0184 μs |  7.464 μs |  2.07 |    0.02 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | 3           |  3.795 μs | 0.0099 μs | 0.0059 μs |  3.795 μs |  1.05 |    0.01 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |             |           |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | MediumRun  | 15             | 2           | 10          |  4.168 μs | 0.0311 μs | 0.0436 μs |  4.198 μs |  1.17 |    0.02 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.527 μs | 0.0159 μs | 0.0233 μs |  4.520 μs |  1.27 |    0.02 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.572 μs | 0.0621 μs | 0.0929 μs |  4.567 μs |  1.28 |    0.03 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | MediumRun  | 15             | 2           | 10          | 44.836 μs | 0.0618 μs | 0.0845 μs | 44.831 μs | 12.59 |    0.20 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | MediumRun  | 15             | 2           | 10          |  3.563 μs | 0.0397 μs | 0.0569 μs |  3.558 μs |  1.00 |    0.02 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | MediumRun  | 15             | 2           | 10          |  7.222 μs | 0.0393 μs | 0.0588 μs |  7.221 μs |  2.03 |    0.04 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | MediumRun  | 15             | 2           | 10          |  3.893 μs | 0.0743 μs | 0.1089 μs |  3.810 μs |  1.09 |    0.03 | 0.1106 |   1.81 KB |        1.03 |
