```

BenchmarkDotNet v0.15.8, Linux Ubuntu 26.04.1 LTS (Resolute Raccoon)
AMD EPYC 7763 3.02GHz, 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  Job-YFEFPZ : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  MediumRun  : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method                     | Job        | IterationCount | LaunchCount | WarmupCount | Mean      | Error     | StdDev    | Median    | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|--------------------------- |----------- |--------------- |------------ |------------ |----------:|----------:|----------:|----------:|------:|--------:|-------:|----------:|------------:|
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | 3           |  3.692 μs | 0.0316 μs | 0.0209 μs |  3.689 μs |  1.19 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  3.966 μs | 0.0318 μs | 0.0189 μs |  3.957 μs |  1.28 |    0.01 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  3.975 μs | 0.0328 μs | 0.0195 μs |  3.971 μs |  1.28 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 3           | 44.428 μs | 0.1541 μs | 0.1019 μs | 44.447 μs | 14.29 |    0.07 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  3.110 μs | 0.0227 μs | 0.0135 μs |  3.106 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | 3           |  6.531 μs | 0.0287 μs | 0.0171 μs |  6.527 μs |  2.10 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | 3           |  3.670 μs | 0.0200 μs | 0.0132 μs |  3.666 μs |  1.18 |    0.01 | 0.1106 |   1.81 KB |        1.03 |
|                            |            |                |             |             |           |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | MediumRun  | 15             | 2           | 10          |  3.824 μs | 0.0674 μs | 0.0944 μs |  3.755 μs |  1.21 |    0.03 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.047 μs | 0.0185 μs | 0.0253 μs |  4.050 μs |  1.29 |    0.01 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.112 μs | 0.0533 μs | 0.0782 μs |  4.157 μs |  1.31 |    0.02 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | MediumRun  | 15             | 2           | 10          | 44.574 μs | 0.0748 μs | 0.1120 μs | 44.583 μs | 14.15 |    0.04 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | MediumRun  | 15             | 2           | 10          |  3.149 μs | 0.0046 μs | 0.0061 μs |  3.150 μs |  1.00 |    0.00 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | MediumRun  | 15             | 2           | 10          |  6.628 μs | 0.0453 μs | 0.0663 μs |  6.624 μs |  2.10 |    0.02 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | MediumRun  | 15             | 2           | 10          |  3.636 μs | 0.0551 μs | 0.0825 μs |  3.645 μs |  1.15 |    0.03 | 0.1106 |   1.81 KB |        1.03 |
