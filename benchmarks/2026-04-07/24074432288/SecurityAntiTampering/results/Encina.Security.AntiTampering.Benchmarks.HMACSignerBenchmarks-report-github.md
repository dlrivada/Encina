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
| Sign_SHA256_MediumPayload  | Job-YFEFPZ | 10             | Default     | 3           |  4.161 μs | 0.0133 μs | 0.0079 μs |  1.21 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.569 μs | 0.0122 μs | 0.0081 μs |  1.33 |    0.01 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  4.373 μs | 0.0155 μs | 0.0081 μs |  1.27 |    0.01 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | Job-YFEFPZ | 10             | Default     | 3           | 44.861 μs | 0.0986 μs | 0.0586 μs | 13.04 |    0.07 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | Job-YFEFPZ | 10             | Default     | 3           |  3.440 μs | 0.0293 μs | 0.0174 μs |  1.00 |    0.01 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | Job-YFEFPZ | 10             | Default     | 3           |  7.268 μs | 0.0587 μs | 0.0389 μs |  2.11 |    0.01 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | Job-YFEFPZ | 10             | Default     | 3           |  3.962 μs | 0.0229 μs | 0.0136 μs |  1.15 |    0.01 | 0.1068 |   1.81 KB |        1.03 |
|                            |            |                |             |             |           |           |           |       |         |        |           |             |
| Sign_SHA256_MediumPayload  | MediumRun  | 15             | 2           | 10          |  4.153 μs | 0.0795 μs | 0.1190 μs |  1.19 |    0.04 | 0.1068 |   1.77 KB |        1.00 |
| Sign_SHA384_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.379 μs | 0.0156 μs | 0.0229 μs |  1.26 |    0.02 | 0.1068 |   1.82 KB |        1.03 |
| Sign_SHA512_SmallPayload   | MediumRun  | 15             | 2           | 10          |  4.538 μs | 0.1349 μs | 0.1934 μs |  1.30 |    0.06 | 0.1144 |   1.88 KB |        1.07 |
| Sign_SHA256_LargePayload   | MediumRun  | 15             | 2           | 10          | 44.808 μs | 0.0509 μs | 0.0731 μs | 12.86 |    0.14 | 0.0610 |   1.77 KB |        1.00 |
| Sign_SHA256_SmallPayload   | MediumRun  | 15             | 2           | 10          |  3.485 μs | 0.0273 μs | 0.0391 μs |  1.00 |    0.02 | 0.1068 |   1.77 KB |        1.00 |
| SignAndVerify_Roundtrip    | MediumRun  | 15             | 2           | 10          |  7.178 μs | 0.0272 μs | 0.0390 μs |  2.06 |    0.03 | 0.2136 |   3.51 KB |        1.99 |
| Verify_SHA256_SmallPayload | MediumRun  | 15             | 2           | 10          |  3.828 μs | 0.0198 μs | 0.0297 μs |  1.10 |    0.01 | 0.1106 |   1.81 KB |        1.03 |
