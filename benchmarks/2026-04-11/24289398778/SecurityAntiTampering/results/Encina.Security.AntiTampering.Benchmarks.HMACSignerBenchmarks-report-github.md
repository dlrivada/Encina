```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.4 LTS (Noble Numbat)
AMD EPYC 7763 2.45GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.201
  [Host]     : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.5 (10.0.5, 10.0.526.15411), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | 3           |  4.166 μs | 0.0174 μs | 0.0115 μs |  1.22 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.314 μs | 0.0143 μs | 0.0085 μs |  1.27 |    0.00 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.480 μs | 0.0145 μs | 0.0096 μs |  1.32 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 3           | 44.836 μs | 0.1163 μs | 0.0769 μs | 13.17 |    0.05 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  3.405 μs | 0.0209 μs | 0.0125 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | 3           |  7.167 μs | 0.0419 μs | 0.0249 μs |  2.11 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | 3           |  3.974 μs | 0.0138 μs | 0.0082 μs |  1.17 |    0.00 | 0.1068 |   1.81 KB |        1.03 |
|                            |            |                |             |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | MediumRun  | 15             | 2           | 10          |  4.091 μs | 0.0093 μs | 0.0134 μs |  1.18 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.368 μs | 0.0080 μs | 0.0115 μs |  1.26 |    0.01 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.605 μs | 0.0118 μs | 0.0169 μs |  1.33 |    0.02 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | MediumRun  | 15             | 2           | 10          | 44.894 μs | 0.0668 μs | 0.1000 μs | 12.98 |    0.15 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | MediumRun  | 15             | 2           | 10          |  3.459 μs | 0.0272 μs | 0.0399 μs |  1.00 |    0.02 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | MediumRun  | 15             | 2           | 10          |  7.389 μs | 0.1285 μs | 0.1759 μs |  2.14 |    0.06 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | MediumRun  | 15             | 2           | 10          |  3.858 μs | 0.0558 μs | 0.0835 μs |  1.12 |    0.03 | 0.1106 |   1.81 KB |        1.03 |
