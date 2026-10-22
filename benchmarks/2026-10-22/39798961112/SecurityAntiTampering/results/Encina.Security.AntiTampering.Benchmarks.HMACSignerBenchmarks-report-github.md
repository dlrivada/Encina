```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.5 LTS (Noble Numbat)
AMD EPYC 7763 2.68GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.402
  [Host]     : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.13 (10.0.13, 10.0.1326.50504), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | 3           |  4.023 μs | 0.0226 μs | 0.0134 μs |  4.020 μs |  1.15 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.256 μs | 0.0093 μs | 0.0056 μs |  4.256 μs |  1.22 |    0.01 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.631 μs | 0.0136 μs | 0.0090 μs |  4.632 μs |  1.33 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 3           | 44.833 μs | 0.2040 μs | 0.1350 μs | 44.774 μs | 12.84 |    0.07 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  3.492 μs | 0.0268 μs | 0.0159 μs |  3.483 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | 3           |  7.275 μs | 0.0127 μs | 0.0075 μs |  7.272 μs |  2.08 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | 3           |  3.765 μs | 0.0086 μs | 0.0051 μs |  3.767 μs |  1.08 |    0.00 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |             |           |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | MediumRun  | 15             | 2           | 10          |  4.065 μs | 0.0127 μs | 0.0178 μs |  4.068 μs |  1.16 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.341 μs | 0.0069 μs | 0.0099 μs |  4.338 μs |  1.24 |    0.01 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.339 μs | 0.0208 μs | 0.0299 μs |  4.338 μs |  1.24 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | MediumRun  | 15             | 2           | 10          | 44.942 μs | 0.1060 μs | 0.1520 μs | 44.895 μs | 12.85 |    0.09 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | MediumRun  | 15             | 2           | 10          |  3.497 μs | 0.0147 μs | 0.0206 μs |  3.510 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | MediumRun  | 15             | 2           | 10          |  7.152 μs | 0.0477 μs | 0.0669 μs |  7.203 μs |  2.05 |    0.02 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | MediumRun  | 15             | 2           | 10          |  3.796 μs | 0.0087 μs | 0.0116 μs |  3.802 μs |  1.09 |    0.01 | 0.1106 |   1.81 KB |        1.03 |
