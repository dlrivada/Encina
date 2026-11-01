```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 9V45 4.46GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.403
  [Host]     : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  Job-YFEFPZ : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4
  MediumRun  : .NET 10.0.14 (10.0.14, 10.0.1426.52326), X64 RyuJIT x86-64-v4


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | 3           |  2.728 μs | 0.1157 μs | 0.0689 μs |  1.26 |    0.04 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  2.664 μs | 0.0639 μs | 0.0423 μs |  1.23 |    0.03 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  2.628 μs | 0.0769 μs | 0.0458 μs |  1.21 |    0.03 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 3           | 37.838 μs | 1.0816 μs | 0.6436 μs | 17.43 |    0.45 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  2.172 μs | 0.0774 μs | 0.0461 μs |  1.00 |    0.03 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | 3           |  4.560 μs | 0.1255 μs | 0.0747 μs |  2.10 |    0.05 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | 3           |  2.476 μs | 0.0417 μs | 0.0248 μs |  1.14 |    0.03 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | MediumRun  | 15             | 2           | 10          |  2.733 μs | 0.0359 μs | 0.0503 μs |  1.24 |    0.03 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | MediumRun  | 15             | 2           | 10          |  2.579 μs | 0.0266 μs | 0.0390 μs |  1.17 |    0.03 | 0.1106 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | MediumRun  | 15             | 2           | 10          |  2.578 μs | 0.0232 μs | 0.0339 μs |  1.17 |    0.03 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | MediumRun  | 15             | 2           | 10          | 39.273 μs | 0.5271 μs | 0.7890 μs | 17.78 |    0.51 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | MediumRun  | 15             | 2           | 10          |  2.210 μs | 0.0310 μs | 0.0454 μs |  1.00 |    0.03 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | MediumRun  | 15             | 2           | 10          |  4.548 μs | 0.0546 μs | 0.0801 μs |  2.06 |    0.06 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | MediumRun  | 15             | 2           | 10          |  2.354 μs | 0.0257 μs | 0.0352 μs |  1.07 |    0.03 | 0.1106 |   1.81 KB |        1.03 |
