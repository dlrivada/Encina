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
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | 3           |  4.099 μs | 0.0114 μs | 0.0068 μs |  4.097 μs |  1.18 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.410 μs | 0.0183 μs | 0.0109 μs |  4.405 μs |  1.26 |    0.00 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.393 μs | 0.0181 μs | 0.0107 μs |  4.389 μs |  1.26 |    0.00 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 3           | 45.245 μs | 0.2372 μs | 0.1241 μs | 45.209 μs | 12.97 |    0.04 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  3.488 μs | 0.0068 μs | 0.0040 μs |  3.488 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | 3           |  7.452 μs | 0.0121 μs | 0.0072 μs |  7.453 μs |  2.14 |    0.00 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | 3           |  4.072 μs | 0.0307 μs | 0.0183 μs |  4.064 μs |  1.17 |    0.01 | 0.1068 |   1.81 KB |        1.03 |
|                            |            |                |             |             |           |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | MediumRun  | 15             | 2           | 10          |  4.190 μs | 0.0327 μs | 0.0448 μs |  4.157 μs |  1.15 |    0.03 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.627 μs | 0.1009 μs | 0.1415 μs |  4.525 μs |  1.27 |    0.05 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.644 μs | 0.0098 μs | 0.0138 μs |  4.642 μs |  1.27 |    0.03 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | MediumRun  | 15             | 2           | 10          | 45.092 μs | 0.1142 μs | 0.1673 μs | 45.096 μs | 12.37 |    0.29 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | MediumRun  | 15             | 2           | 10          |  3.649 μs | 0.0599 μs | 0.0859 μs |  3.722 μs |  1.00 |    0.03 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | MediumRun  | 15             | 2           | 10          |  7.420 μs | 0.0120 μs | 0.0168 μs |  7.417 μs |  2.03 |    0.05 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | MediumRun  | 15             | 2           | 10          |  3.896 μs | 0.0127 μs | 0.0178 μs |  3.893 μs |  1.07 |    0.03 | 0.1068 |   1.81 KB |        1.03 |
